using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.AspNetCore.Endpoints;

/// <summary>
/// Captures the HTTP response and SQL evidence for one replayed operation.
/// </summary>
public sealed class EndpointReplayResult
{
    [JsonPropertyName("operationKey")]
    public required string OperationKey { get; init; }

    [JsonPropertyName("httpMethod")]
    public required string HttpMethod { get; init; }

    [JsonPropertyName("route")]
    public required string Route { get; init; }

    [JsonPropertyName("persona")]
    public string? Persona { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("httpStatusCode")]
    public int? HttpStatusCode { get; init; }

    [JsonPropertyName("durationMilliseconds")]
    public double DurationMilliseconds { get; init; }

    [JsonPropertyName("responseBody")]
    public string ResponseBody { get; init; } = string.Empty;

    [JsonPropertyName("responseHeaders")]
    public IReadOnlyDictionary<string, string[]> ResponseHeaders { get; init; } =
        new Dictionary<string, string[]>();

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }

    [JsonPropertyName("request")]
    public required EndpointReplayRequest Request { get; init; }

    [JsonPropertyName("capturedSqlCommands")]
    public IReadOnlyList<CapturedSqlCommand> CapturedSqlCommands { get; init; } =
        [];

    [JsonPropertyName("artifactPath")]
    public required string ArtifactPath { get; init; }
}
