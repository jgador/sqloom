using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Pipeline.OpenAI.Advice;

namespace Sqloom.Host;

/// <summary>
/// Posts advice requests to the configured OpenAI-compatible endpoint.
/// </summary>
internal static class OpenAIAdviceClient
{
    private const string ResponsesPath = "v1/responses";

    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    internal static async Task<OpenAITuningAdviceResponse> CreateAdviceAsync(
        HttpClient httpClient,
        string model,
        OpenAITuningAdviceRequest request,
        bool debugEnabled,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentNullException.ThrowIfNull(request);

        var requestPayload = BuildResponsesRequest(model, request);
        var requestJson = JsonSerializer.Serialize(
            requestPayload,
            _serializerOptions);
        var requestUri = httpClient.BaseAddress is { } baseAddress
            ? new Uri(baseAddress, ResponsesPath)
            : new Uri(ResponsesPath, UriKind.Relative);
        HostDebugWriter.PrintOpenAIRequest(
            debugEnabled,
            requestUri,
            httpClient.DefaultRequestHeaders.Authorization,
            requestJson);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(
                requestJson,
                Encoding.UTF8,
                "application/json"),
        };

        using var response = await httpClient
            .SendAsync(httpRequest, cancellationToken)
            .ConfigureAwait(false);
        var responseJson = await response.Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);
        HostDebugWriter.PrintOpenAIResponse(
            debugEnabled,
            response.StatusCode,
            responseJson);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenAI tuning advice failed with status {(int)response.StatusCode}.");
        }

        var content = ReadOutputText(responseJson);
        var parsedResponse = JsonSerializer.Deserialize<OpenAITuningAdviceResponse>(
                content,
                _serializerOptions)
            ?? throw new InvalidOperationException("OpenAI tuning advice returned invalid JSON.");
        var recommendations = OpenAIAdviceResponseNormalizer.NormalizeRecommendations(
            parsedResponse.Recommendations);
        var proposalResult = OpenAIAdviceResponseNormalizer.NormalizeProposals(
            parsedResponse.Proposals);
        if (recommendations.Count == 0)
        {
            throw new InvalidOperationException("OpenAI tuning advice returned no recommendations.");
        }

        return new OpenAITuningAdviceResponse
        {
            Recommendations = recommendations,
            Proposals = proposalResult.Proposals,
            Warnings = proposalResult.Warnings,
            ModelName = model,
        };
    }

    private static string ReadOutputText(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);

        // Responses payloads can expose output text either at the root or under output[].content[].
        if (TryReadStringProperty(document.RootElement, "output_text", out var outputText))
        {
            return outputText;
        }

        if (TryReadNestedOutputText(document.RootElement, out outputText))
        {
            return outputText;
        }

        throw new InvalidOperationException("OpenAI tuning advice returned empty output.");
    }

    private static bool TryReadNestedOutputText(JsonElement root, out string outputText)
    {
        outputText = string.Empty;
        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var outputItem in output.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (TryReadOutputTextContent(contentItem, out outputText))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryReadOutputTextContent(JsonElement contentItem, out string outputText)
    {
        outputText = string.Empty;
        return contentItem.TryGetProperty("type", out var type)
            && string.Equals(type.GetString(), "output_text", StringComparison.Ordinal)
            && TryReadStringProperty(contentItem, "text", out outputText);
    }

    private static bool TryReadStringProperty(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var text = property.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        value = text;
        return true;
    }

    private static object BuildResponsesRequest(
        string model,
        OpenAITuningAdviceRequest request)
    {
        return new
        {
            model,
            instructions = BuildSystemPrompt(),
            input = BuildInput(request),
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "sqloom_tuning_advice",
                    strict = true,
                    schema = OpenAIAdviceResponseSchemaBuilder.Build(),
                },
            },
        };
    }

    private static string BuildSystemPrompt()
    {
        return string.Join(
            "\n",
            "You generate operation-level SQL tuning advice for Sqloom from supplied evidence files and SQL Server schema text.",
            "Return one JSON object only that matches the provided schema.",
            "Treat artifact_manifest_json, source_evidence_json, and sql_server_schema_sql as the only ground truth.",
            "Use only the supplied evidence. Do not invent tables, indexes, joins, filters, plan operators, or metrics that are not supported by the evidence.",
            "Do not preload or assume a baseline fix. Derive every recommendation from the supplied evidence and schema only.",
            "Only reference objects and columns that exist in the supplied SQL Server schema text.",
            "Return 1 to 4 recommendations.",
            "Return 0 to 3 SQL proposals when the evidence supports a concrete database-side change.",
            "If replay status is not 'replayed', or if matched command count is 0, prioritize recovering replay or correlation evidence before suggesting query tuning changes.",
            "Keep each recommendation concise, specific, and actionable.",
            "SQL proposals must be SQL Server compatible and grounded in the supplied evidence and schema.",
            "SQL proposals are review artifacts only. Sqloom will not auto-apply them.",
            "proposalKind is a short free-form classifier owned by the model. Preserve the specific proposal shape in that string instead of forcing a fixed Sqloom enum.",
            "Fingerprint fallback correlation is weaker than exact ownership, but it can still support a concrete proposal when the captured SQL text, matched plan metrics, and schema align on the same change. Do not withhold a supported proposal solely because the correlation used fingerprint fallback.",
            "Include rollbackSqlScript when you can provide a meaningful rollback. If no rollback can be stated confidently from the evidence, set rollbackSqlScript to null.",
            "Every SQL proposal must include the supporting command ordinals and matched plan ids when available.",
            "If the evidence is insufficient for a concrete database-side change, explain the evidence gap in recommendations and emit no SQL proposal.",
            "Do not use markdown.");
    }

    private static string BuildInput(OpenAITuningAdviceRequest request)
    {
        return string.Join(
            "\n\n",
            $"App: {request.AppName}\nOperation: {request.OperationKey}\nHTTP: {request.HttpMethod} {request.Route}",
            $"artifact_manifest_json:\n{request.ArtifactManifestJson}",
            $"source_evidence_json:\n{request.SourceEvidenceJson}",
            $"sql_server_schema_sql:\n{request.SchemaSql}");
    }
}
