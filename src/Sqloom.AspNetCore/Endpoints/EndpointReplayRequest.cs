using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.AspNetCore.Endpoints;

/// <summary>
/// Describes one replayable HTTP request.
/// </summary>
public sealed class EndpointReplayRequest
{
    [JsonPropertyName("operationKey")]
    public required string OperationKey { get; init; }

    [JsonPropertyName("httpMethod")]
    public required string HttpMethod { get; init; }

    [JsonPropertyName("route")]
    public required string Route { get; init; }

    [JsonPropertyName("persona")]
    public string? Persona { get; init; }

    [JsonPropertyName("relativePathAndQuery")]
    public required string RelativePathAndQuery { get; init; }

    [JsonPropertyName("requestBodyJson")]
    public string? RequestBodyJson { get; init; }

    [JsonPropertyName("pathValues")]
    public IReadOnlyDictionary<string, string> PathValues { get; init; } =
        new Dictionary<string, string>();

    [JsonPropertyName("queryValues")]
    public IReadOnlyDictionary<string, string> QueryValues { get; init; } =
        new Dictionary<string, string>();

    [JsonPropertyName("headerValues")]
    public IReadOnlyDictionary<string, string> HeaderValues { get; init; } =
        new Dictionary<string, string>();
}
