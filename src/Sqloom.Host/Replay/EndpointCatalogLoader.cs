using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Sqloom.Pipeline.Execution;

namespace Sqloom.Host.Replay;

/// <summary>
/// Discovers replayable MVC controller endpoints from ASP.NET Core project source using Roslyn.
/// </summary>
internal sealed partial class EndpointCatalogLoader
{
    private static readonly object MSBuildLocatorLock = new();
    private static bool _msbuildRegistered;

    public async Task<IReadOnlyList<ReplayOperation>> LoadAsync(
        string sourceProjectPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceProjectPath);
        var fullProjectPath = Path.GetFullPath(sourceProjectPath);
        if (!File.Exists(fullProjectPath))
        {
            throw new ArgumentException($"The source project does not exist: '{fullProjectPath}'.");
        }

        EnsureMSBuildRegistered();
        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace
            .OpenProjectAsync(fullProjectPath, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        List<ReplayOperation> operations = [];
        foreach (var document in project.Documents)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null)
            {
                continue;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel is null)
            {
                continue;
            }

            foreach (var controller in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (semanticModel.GetDeclaredSymbol(controller, cancellationToken) is not INamedTypeSymbol controllerSymbol
                    || !IsController(controllerSymbol))
                {
                    continue;
                }

                operations.AddRange(DiscoverControllerOperations(
                    controllerSymbol,
                    cancellationToken));
            }
        }

        return operations
            .GroupBy(static operation => operation.StableOperationKey, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group
                .OrderBy(static operation => operation.ControllerType, StringComparer.Ordinal)
                .ThenBy(static operation => operation.MethodName, StringComparer.Ordinal)
                .First())
            .OrderBy(static operation => operation.Route, StringComparer.Ordinal)
            .ThenBy(static operation => operation.HttpMethod, StringComparer.Ordinal)
            .ThenBy(static operation => operation.MethodSymbolId, StringComparer.Ordinal)
            .ToArray();
    }

    private static void EnsureMSBuildRegistered()
    {
        lock (MSBuildLocatorLock)
        {
            if (_msbuildRegistered || MSBuildLocator.IsRegistered)
            {
                _msbuildRegistered = true;
                return;
            }

            MSBuildLocator.RegisterDefaults();
            _msbuildRegistered = true;
        }
    }

    private static IEnumerable<ReplayOperation> DiscoverControllerOperations(
        INamedTypeSymbol controllerSymbol,
        CancellationToken cancellationToken)
    {
        var controllerRoutes = GetRouteTemplates(controllerSymbol.GetAttributes(), "RouteAttribute");
        if (controllerRoutes.Count == 0)
        {
            controllerRoutes = [string.Empty];
        }

        foreach (var method in controllerSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (method.MethodKind != MethodKind.Ordinary
                || method.DeclaredAccessibility != Accessibility.Public
                || method.IsStatic)
            {
                continue;
            }

            var httpAttributes = method.GetAttributes()
                .Select(TryCreateHttpRoute)
                .Where(static route => route is not null)
                .Cast<HttpRoute>()
                .ToArray();
            if (httpAttributes.Length == 0)
            {
                continue;
            }

            foreach (var httpRoute in httpAttributes)
            {
                foreach (var controllerRoute in controllerRoutes)
                {
                    var route = NormalizeRoute(ApplyRouteTokens(
                        CombineRoutes(controllerRoute, httpRoute.Template),
                        controllerSymbol,
                        method));
                    var parameters = CreateReplayParameters(method.Parameters, route, out var requestBodyType);
                    var httpMethod = httpRoute.HttpMethod.ToUpperInvariant();
                    yield return new ReplayOperation
                    {
                        StableOperationKey = ReplayOperationKeys.Build(httpMethod, route),
                        OperationId = method.Name,
                        HttpMethod = httpMethod,
                        Route = route,
                        ControllerType = controllerSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        MethodName = method.Name,
                        MethodSymbolId = DocumentationCommentId.CreateDeclarationId(method),
                        RequiresAuthentication = RequiresAuthentication(controllerSymbol, method),
                        Tags = [GetControllerName(controllerSymbol)],
                        Parameters = parameters,
                        HasJsonRequestBody = requestBodyType is not null,
                        RequestBodyRequired = requestBodyType?.Required == true,
                        RequestBodyClrType = requestBodyType?.ClrType,
                    };
                }
            }
        }
    }

    private static bool IsController(INamedTypeSymbol type)
    {
        return type.Name.EndsWith("Controller", StringComparison.Ordinal)
            || HasAttribute(type.GetAttributes(), "ApiControllerAttribute")
            || InheritsFrom(type, "Microsoft.AspNetCore.Mvc.ControllerBase")
            || InheritsFrom(type, "Microsoft.AspNetCore.Mvc.Controller");
    }

    private static bool InheritsFrom(INamedTypeSymbol type, string metadataName)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (string.Equals(current.ToDisplayString(), metadataName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static HttpRoute? TryCreateHttpRoute(AttributeData attribute)
    {
        var attributeName = attribute.AttributeClass?.Name;
        var httpMethod = attributeName switch
        {
            "HttpGetAttribute" => "GET",
            "HttpPostAttribute" => "POST",
            "HttpPutAttribute" => "PUT",
            "HttpPatchAttribute" => "PATCH",
            "HttpDeleteAttribute" => "DELETE",
            "HttpHeadAttribute" => "HEAD",
            "HttpOptionsAttribute" => "OPTIONS",
            _ => null,
        };
        if (httpMethod is null)
        {
            return null;
        }

        return new HttpRoute(httpMethod, ReadRouteTemplate(attribute));
    }

    private static IReadOnlyList<string> GetRouteTemplates(
        ImmutableArray<AttributeData> attributes,
        string attributeName)
    {
        return attributes
            .Where(attribute => string.Equals(attribute.AttributeClass?.Name, attributeName, StringComparison.Ordinal))
            .Select(ReadRouteTemplate)
            .Where(static template => template is not null)
            .Select(static template => template!)
            .DefaultIfEmpty(string.Empty)
            .ToArray();
    }

    private static string? ReadRouteTemplate(AttributeData attribute)
    {
        foreach (var argument in attribute.ConstructorArguments)
        {
            if (argument.Value is string template)
            {
                return template;
            }
        }

        foreach (var argument in attribute.NamedArguments)
        {
            if (string.Equals(argument.Key, "Template", StringComparison.Ordinal)
                && argument.Value.Value is string template)
            {
                return template;
            }
        }

        return string.Empty;
    }

    private static IReadOnlyList<ReplayParameter> CreateReplayParameters(
        ImmutableArray<IParameterSymbol> methodParameters,
        string route,
        out RequestBodyMetadata? requestBody)
    {
        requestBody = null;
        List<ReplayParameter> parameters = [];
        var routeTokens = ExtractRouteTokens(route);
        foreach (var parameter in methodParameters)
        {
            if (IsCancellationToken(parameter.Type))
            {
                continue;
            }

            var location = ResolveParameterLocation(parameter, routeTokens);
            var required = IsParameterRequired(parameter, location);
            var typeMetadata = ResolveTypeMetadata(parameter.Type);
            if (string.Equals(location, "body", StringComparison.OrdinalIgnoreCase))
            {
                requestBody = new RequestBodyMetadata(
                    parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    required);
                continue;
            }

            parameters.Add(new ReplayParameter
            {
                Name = ResolveParameterName(parameter),
                Location = location,
                Required = required,
                ClrType = parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                SchemaType = typeMetadata.SchemaType,
                Format = typeMetadata.Format,
            });
        }

        return parameters;
    }

    private static string ResolveParameterLocation(
        IParameterSymbol parameter,
        ISet<string> routeTokens)
    {
        var attributes = parameter.GetAttributes();
        if (HasAttribute(attributes, "FromRouteAttribute"))
        {
            return "path";
        }

        if (HasAttribute(attributes, "FromQueryAttribute"))
        {
            return "query";
        }

        if (HasAttribute(attributes, "FromHeaderAttribute"))
        {
            return "header";
        }

        if (HasAttribute(attributes, "FromBodyAttribute"))
        {
            return "body";
        }

        var name = ResolveParameterName(parameter);
        if (routeTokens.Contains(name))
        {
            return "path";
        }

        return IsSimpleType(parameter.Type) ? "query" : "body";
    }

    private static bool HasAttribute(
        ImmutableArray<AttributeData> attributes,
        string attributeName)
    {
        return attributes.Any(attribute => string.Equals(attribute.AttributeClass?.Name, attributeName, StringComparison.Ordinal));
    }

    private static bool RequiresAuthentication(
        INamedTypeSymbol controllerSymbol,
        IMethodSymbol method)
    {
        if (HasAttribute(method.GetAttributes(), "AllowAnonymousAttribute")
            || HasAttribute(controllerSymbol.GetAttributes(), "AllowAnonymousAttribute"))
        {
            return false;
        }

        return true;
    }

    private static string ResolveParameterName(IParameterSymbol parameter)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            var name = attribute.NamedArguments
                .FirstOrDefault(static argument => string.Equals(argument.Key, "Name", StringComparison.Ordinal))
                .Value
                .Value as string;
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return parameter.Name;
    }

    private static bool IsParameterRequired(
        IParameterSymbol parameter,
        string location)
    {
        if (parameter.HasExplicitDefaultValue)
        {
            return false;
        }

        if (string.Equals(location, "path", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return parameter.Type.IsValueType
            ? parameter.Type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T
            : parameter.NullableAnnotation != NullableAnnotation.Annotated;
    }

    private static TypeMetadata ResolveTypeMetadata(ITypeSymbol type)
    {
        var unwrappedType = UnwrapNullable(type);
        return unwrappedType.SpecialType switch
        {
            SpecialType.System_Boolean => new TypeMetadata("boolean", null),
            SpecialType.System_Byte
                or SpecialType.System_Int16
                or SpecialType.System_Int32
                or SpecialType.System_Int64
                or SpecialType.System_SByte
                or SpecialType.System_UInt16
                or SpecialType.System_UInt32
                or SpecialType.System_UInt64 => new TypeMetadata("integer", null),
            SpecialType.System_Decimal
                or SpecialType.System_Double
                or SpecialType.System_Single => new TypeMetadata("number", null),
            SpecialType.System_DateTime => new TypeMetadata("string", "date-time"),
            SpecialType.System_String => new TypeMetadata("string", null),
            _ => ResolveNamedTypeMetadata(unwrappedType),
        };
    }

    private static TypeMetadata ResolveNamedTypeMetadata(ITypeSymbol type)
    {
        var displayName = type.ToDisplayString();
        return displayName switch
        {
            "System.DateTimeOffset" => new TypeMetadata("string", "date-time"),
            "System.DateOnly" => new TypeMetadata("string", "date"),
            "System.Guid" => new TypeMetadata("string", "uuid"),
            _ => new TypeMetadata(null, null),
        };
    }

    private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        return type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableType
            && nullableType.TypeArguments.Length == 1
                ? nullableType.TypeArguments[0]
                : type;
    }

    private static bool IsSimpleType(ITypeSymbol type)
    {
        var metadata = ResolveTypeMetadata(type);
        return metadata.SchemaType is not null;
    }

    private static bool IsCancellationToken(ITypeSymbol type)
    {
        return string.Equals(type.ToDisplayString(), typeof(CancellationToken).FullName, StringComparison.Ordinal);
    }

    private static ISet<string> ExtractRouteTokens(string route)
    {
        HashSet<string> tokens = new(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in RouteTokenRegex().Matches(route))
        {
            var token = match.Groups["name"].Value;
            if (!string.IsNullOrWhiteSpace(token))
            {
                tokens.Add(token);
            }
        }

        return tokens;
    }

    private static string CombineRoutes(string controllerRoute, string? actionRoute)
    {
        var controller = controllerRoute.Trim('/');
        var action = (actionRoute ?? string.Empty).Trim('/');
        if (string.IsNullOrWhiteSpace(controller))
        {
            return string.IsNullOrWhiteSpace(action) ? "/" : "/" + action;
        }

        return string.IsNullOrWhiteSpace(action)
            ? "/" + controller
            : $"/{controller}/{action}";
    }

    private static string ApplyRouteTokens(
        string route,
        INamedTypeSymbol controllerSymbol,
        IMethodSymbol method)
    {
        return route
            .Replace("[controller]", GetControllerName(controllerSymbol), StringComparison.OrdinalIgnoreCase)
            .Replace("[action]", TrimAsyncSuffix(method.Name), StringComparison.OrdinalIgnoreCase);
    }

    private static string GetControllerName(INamedTypeSymbol controllerSymbol)
    {
        return controllerSymbol.Name.EndsWith("Controller", StringComparison.Ordinal)
            ? controllerSymbol.Name[..^"Controller".Length]
            : controllerSymbol.Name;
    }

    private static string TrimAsyncSuffix(string value)
    {
        return value.EndsWith("Async", StringComparison.Ordinal)
            ? value[..^"Async".Length]
            : value;
    }

    private static string NormalizeRoute(string route)
    {
        var normalized = "/" + string.Join(
            '/',
            route.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return normalized.Length == 0 ? "/" : normalized;
    }

    [GeneratedRegex(@"\{(?<name>[^}:=]+)(?::[^}=]+)?(?:=[^}]+)?\}")]
    private static partial Regex RouteTokenRegex();

    private sealed record HttpRoute(string HttpMethod, string? Template);

    private sealed record RequestBodyMetadata(string ClrType, bool Required);

    private sealed record TypeMetadata(string? SchemaType, string? Format);
}
