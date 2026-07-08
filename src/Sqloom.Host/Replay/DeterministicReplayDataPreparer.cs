using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Core.Execution;

namespace Sqloom.Host.Replay;

/// <summary>
/// Generates primitive replay values from OpenAPI metadata without calling a model.
/// </summary>
internal sealed class DeterministicReplayDataPreparer : IReplayDataPreparer
{
    public Task<ReplayDataPreparationOperation> PrepareAsync(
        ReplayDataPreparationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        Dictionary<string, string> pathValues = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> queryValues = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> headerValues = new(StringComparer.OrdinalIgnoreCase);
        List<string> sourcesUsed = [];
        List<string> warnings = [];

        foreach (var parameter in context.Operation.Parameters.Where(static parameter => parameter.Required))
        {
            var existingValues = parameter.Location switch
            {
                "path" => context.ResolvedOperation.PathValues,
                "query" => context.ResolvedOperation.QueryValues,
                "header" => context.ResolvedOperation.HeaderValues,
                _ => EmptyStringDictionary.Instance,
            };
            if (existingValues.ContainsKey(parameter.Name))
            {
                continue;
            }

            var generatedValue = GenerateParameterValue(parameter);
            switch (parameter.Location)
            {
                case "path":
                    pathValues[parameter.Name] = generatedValue;
                    break;
                case "query":
                    queryValues[parameter.Name] = generatedValue;
                    break;
                case "header":
                    headerValues[parameter.Name] = generatedValue;
                    break;
                default:
                    warnings.Add(
                        $"Unsupported required OpenAPI parameter location '{parameter.Location}' for '{parameter.Name}'.");
                    continue;
            }

            sourcesUsed.Add($"openapi-parameter:{parameter.Location}:{parameter.Name}");
        }

        string? requestBodyJson = null;
        if (context.Operation.RequestBodyRequired
            && string.IsNullOrWhiteSpace(context.ResolvedOperation.RequestBodyJson)
            && string.IsNullOrWhiteSpace(context.Operation.JsonBodyExample))
        {
            requestBodyJson = "{}";
            sourcesUsed.Add("openapi-request-body");
            warnings.Add(
                "Generated an empty JSON object because the required request body had no OpenAPI example.");
        }

        var generated = pathValues.Count + queryValues.Count + headerValues.Count > 0
            || requestBodyJson is not null;
        return Task.FromResult(new ReplayDataPreparationOperation
        {
            OperationKey = context.Operation.StableOperationKey,
            Strategy = "deterministic-openapi",
            Status = generated ? "generated" : "not-needed",
            Confidence = generated ? 0.75 : 1.0,
            PreparedData = new ReplayPreparedData
            {
                RequestBodyJson = requestBodyJson,
                PathValues = pathValues,
                QueryValues = queryValues,
                HeaderValues = headerValues,
            },
            Warnings = warnings,
            SourcesUsed = sourcesUsed.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            Notes = generated
                ? "Generated missing required replay values from OpenAPI parameter metadata."
                : "Resolved replay operation already had the required values.",
        });
    }

    private static string GenerateParameterValue(OpenApiParameter parameter)
    {
        var schemaType = parameter.SchemaType?.Trim();
        var format = parameter.Format?.Trim();

        if (string.Equals(schemaType, "integer", StringComparison.OrdinalIgnoreCase)
            || string.Equals(schemaType, "number", StringComparison.OrdinalIgnoreCase))
        {
            return "1";
        }

        if (string.Equals(schemaType, "boolean", StringComparison.OrdinalIgnoreCase))
        {
            return "true";
        }

        if (string.Equals(format, "date-time", StringComparison.OrdinalIgnoreCase))
        {
            return "2026-01-01T00:00:00Z";
        }

        if (string.Equals(format, "date", StringComparison.OrdinalIgnoreCase))
        {
            return "2026-01-01";
        }

        if (string.Equals(format, "uuid", StringComparison.OrdinalIgnoreCase))
        {
            return "00000000-0000-0000-0000-000000000001";
        }

        if (parameter.Name.EndsWith("id", StringComparison.OrdinalIgnoreCase))
        {
            return "1";
        }

        return "sqloom";
    }

    private static class EmptyStringDictionary
    {
        public static IReadOnlyDictionary<string, string> Instance { get; } =
            new Dictionary<string, string>();
    }
}
