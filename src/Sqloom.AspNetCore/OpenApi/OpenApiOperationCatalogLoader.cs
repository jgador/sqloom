using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sqloom.AspNetCore.OpenApi;

/// <summary>
/// Loads the replayable operation catalog from an OpenAPI document.
/// </summary>
public sealed class OpenApiOperationCatalogLoader
{
    private static readonly string[] _supportedHttpMethods =
    [
        "get",
        "post",
        "put",
        "patch",
        "delete",
        "head",
        "options"
    ];

    public async Task<IReadOnlyList<DiscoveredOpenApiOperation>> LoadAsync(
        string openApiDocumentPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(openApiDocumentPath);

        var json = await File.ReadAllTextAsync(openApiDocumentPath, cancellationToken).ConfigureAwait(false);
        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;
        var defaultRequiresAuthentication = HasSecurityRequirements(root);
        if (!root.TryGetProperty("paths", out var pathsElement)
            || pathsElement.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<DiscoveredOpenApiOperation>();
        }

        var operations = new List<DiscoveredOpenApiOperation>();
        foreach (var pathProperty in pathsElement.EnumerateObject())
        {
            var route = pathProperty.Name;
            var pathItem = pathProperty.Value;
            foreach (var methodProperty in pathItem.EnumerateObject())
            {
                if (!_supportedHttpMethods.Contains(methodProperty.Name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var operationElement = methodProperty.Value;
                var httpMethod = methodProperty.Name.ToUpperInvariant();
                var requiresAuthentication = operationElement.TryGetProperty("security", out var securityElement)
                    ? HasSecurityRequirements(securityElement)
                    : defaultRequiresAuthentication;
                var parameters = ReadParameters(pathItem, operationElement);
                var hasJsonRequestBody = TryGetJsonRequestBody(operationElement, out var requestBodyRequired, out var bodyExample);

                operations.Add(new DiscoveredOpenApiOperation
                {
                    StableOperationKey = BuildStableOperationKey(httpMethod, route),
                    OperationId = ReadOptionalString(operationElement, "operationId"),
                    HttpMethod = httpMethod,
                    Route = route,
                    RequiresAuthentication = requiresAuthentication,
                    Tags = ReadTags(operationElement),
                    Parameters = parameters,
                    HasJsonRequestBody = hasJsonRequestBody,
                    RequestBodyRequired = requestBodyRequired,
                    JsonRequestBodyExample = bodyExample
                });
            }
        }

        return operations
            .OrderBy(operation => operation.Route, StringComparer.Ordinal)
            .ThenBy(operation => operation.HttpMethod, StringComparer.Ordinal)
            .ToArray();
    }

    public static string BuildStableOperationKey(string httpMethod, string route)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(httpMethod);
        ArgumentException.ThrowIfNullOrWhiteSpace(route);

        return $"{httpMethod.Trim().ToUpperInvariant()} {route.Trim()}";
    }

    private static IReadOnlyList<string> ReadTags(JsonElement operationElement)
    {
        if (!operationElement.TryGetProperty("tags", out var tagsElement)
            || tagsElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return tagsElement.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();
    }

    private static IReadOnlyList<OpenApiParameterDefinition> ReadParameters(
        JsonElement pathItem,
        JsonElement operationElement)
    {
        var parameters = new Dictionary<string, OpenApiParameterDefinition>(StringComparer.OrdinalIgnoreCase);
        AddParameters(parameters, pathItem);
        AddParameters(parameters, operationElement);
        return parameters.Values.ToArray();
    }

    private static void AddParameters(
        IDictionary<string, OpenApiParameterDefinition> parameters,
        JsonElement container)
    {
        if (!container.TryGetProperty("parameters", out var parametersElement)
            || parametersElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var parameterElement in parametersElement.EnumerateArray())
        {
            if (parameterElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = ReadOptionalString(parameterElement, "name");
            var location = ReadOptionalString(parameterElement, "in");
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(location))
            {
                continue;
            }

            var schemaElement = parameterElement.TryGetProperty("schema", out var foundSchema)
                ? foundSchema
                : default;
            var key = $"{location}:{name}";
            parameters[key] = new OpenApiParameterDefinition
            {
                Name = name,
                Location = location,
                Required = parameterElement.TryGetProperty("required", out var requiredElement)
                    && requiredElement.ValueKind == JsonValueKind.True,
                SchemaType = ReadOptionalString(schemaElement, "type"),
                Format = ReadOptionalString(schemaElement, "format")
            };
        }
    }

    private static bool TryGetJsonRequestBody(
        JsonElement operationElement,
        out bool requestBodyRequired,
        out string? exampleJson)
    {
        requestBodyRequired = false;
        exampleJson = null;

        if (!operationElement.TryGetProperty("requestBody", out var requestBodyElement)
            || requestBodyElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        requestBodyRequired = requestBodyElement.TryGetProperty("required", out var requiredElement)
            && requiredElement.ValueKind == JsonValueKind.True;
        if (!requestBodyElement.TryGetProperty("content", out var contentElement)
            || contentElement.ValueKind != JsonValueKind.Object
            || !contentElement.TryGetProperty("application/json", out var jsonElement)
            || jsonElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        exampleJson = ReadExampleJson(jsonElement);
        return true;
    }

    private static string? ReadExampleJson(JsonElement element)
    {
        if (element.TryGetProperty("example", out var exampleElement))
        {
            return exampleElement.GetRawText();
        }

        if (!element.TryGetProperty("examples", out var examplesElement)
            || examplesElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var exampleProperty in examplesElement.EnumerateObject())
        {
            if (exampleProperty.Value.ValueKind == JsonValueKind.Object
                && exampleProperty.Value.TryGetProperty("value", out var valueElement))
            {
                return valueElement.GetRawText();
            }
        }

        return null;
    }

    private static string? ReadOptionalString(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(propertyName, out var propertyElement)
            && propertyElement.ValueKind == JsonValueKind.String
                ? propertyElement.GetString()
                : null;
    }

    private static bool HasSecurityRequirements(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            return element.TryGetProperty("security", out var securityElement)
                && HasSecurityRequirements(securityElement);
        }

        return element.ValueKind == JsonValueKind.Array
            && element.GetArrayLength() > 0;
    }
}
