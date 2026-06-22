using System.Text.Json.Serialization;

namespace Sqloom.OpenAI.Advice;

/// <summary>
/// Carries the OpenAI request payload used for Sqloom advice generation.
/// </summary>
public sealed class OpenAITuningAdviceRequest
{
    [JsonPropertyName("appName")]
    public required string AppName { get; init; }

    [JsonPropertyName("operationKey")]
    public required string OperationKey { get; init; }

    [JsonPropertyName("httpMethod")]
    public required string HttpMethod { get; init; }

    [JsonPropertyName("route")]
    public required string Route { get; init; }

    [JsonPropertyName("artifactManifestJson")]
    public required string ArtifactManifestJson { get; init; }

    [JsonPropertyName("sourceEvidenceJson")]
    public required string SourceEvidenceJson { get; init; }

    [JsonPropertyName("sqlServerSchemaText")]
    public required string SqlServerSchemaText { get; init; }
}
