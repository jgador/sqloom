using System.Text.Json.Serialization;

namespace Sqloom.OpenAI.Advice;

/// <summary>
/// Carries the OpenAI request payload used for Sqloom advice generation.
/// </summary>
public sealed class OpenAITuningAdviceRequest
{
    /// <summary>
    /// Gets the app name.
    /// </summary>
    [JsonPropertyName("appName")]
    public required string AppName { get; init; }

    /// <summary>
    /// Gets the operation key.
    /// </summary>
    [JsonPropertyName("operationKey")]
    public required string OperationKey { get; init; }

    /// <summary>
    /// Gets the http method.
    /// </summary>
    [JsonPropertyName("httpMethod")]
    public required string HttpMethod { get; init; }

    /// <summary>
    /// Gets the route.
    /// </summary>
    [JsonPropertyName("route")]
    public required string Route { get; init; }

    /// <summary>
    /// Gets the artifact manifest json.
    /// </summary>
    [JsonPropertyName("artifactManifestJson")]
    public required string ArtifactManifestJson { get; init; }

    /// <summary>
    /// Gets the source evidence json.
    /// </summary>
    [JsonPropertyName("sourceEvidenceJson")]
    public required string SourceEvidenceJson { get; init; }

    /// <summary>
    /// Gets the schema sql.
    /// </summary>
    [JsonPropertyName("schemaSql")]
    public required string SchemaSql { get; init; }
}
