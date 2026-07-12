using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Captures the HTTP response and SQL evidence for one replayed operation.
/// </summary>
public sealed class EndpointReplayResult
{
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
    /// Gets the persona.
    /// </summary>
    [JsonPropertyName("persona")]
    public string? Persona { get; init; }

    /// <summary>
    /// Gets the status.
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>
    /// Gets the http status code.
    /// </summary>
    [JsonPropertyName("httpStatusCode")]
    public int? HttpStatusCode { get; init; }

    /// <summary>
    /// Gets the duration milliseconds.
    /// </summary>
    [JsonPropertyName("durationMilliseconds")]
    public double DurationMilliseconds { get; init; }

    /// <summary>
    /// Gets the response body.
    /// </summary>
    [JsonPropertyName("responseBody")]
    public string ResponseBody { get; init; } = string.Empty;

    /// <summary>
    /// Gets the response headers.
    /// </summary>
    [JsonPropertyName("responseHeaders")]
    public IReadOnlyDictionary<string, string[]> ResponseHeaders { get; init; } =
        new Dictionary<string, string[]>();

    /// <summary>
    /// Gets the error message.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the request.
    /// </summary>
    [JsonPropertyName("request")]
    public required EndpointReplayRequest Request { get; init; }

    /// <summary>
    /// Gets the captured sql commands.
    /// </summary>
    [JsonPropertyName("capturedSqlCommands")]
    public IReadOnlyList<CapturedSqlCommand> CapturedSqlCommands { get; init; } =
        [];

    /// <summary>
    /// Gets the artifact path.
    /// </summary>
    [JsonPropertyName("artifactPath")]
    public required string ArtifactPath { get; init; }
}
