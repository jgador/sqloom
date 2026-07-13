using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Pipeline.Execution;

/// <summary>
/// Describes one replayable HTTP request.
/// </summary>
public sealed class EndpointReplayRequest
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
    /// Gets the relative path and query.
    /// </summary>
    [JsonPropertyName("relativePathAndQuery")]
    public required string RelativePathAndQuery { get; init; }

    /// <summary>
    /// Gets the request body json.
    /// </summary>
    [JsonPropertyName("requestBodyJson")]
    public string? RequestBodyJson { get; init; }

    /// <summary>
    /// Gets the path values.
    /// </summary>
    [JsonPropertyName("pathValues")]
    public IReadOnlyDictionary<string, string> PathValues { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Gets the query values.
    /// </summary>
    [JsonPropertyName("queryValues")]
    public IReadOnlyDictionary<string, string> QueryValues { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Gets the header values.
    /// </summary>
    [JsonPropertyName("headerValues")]
    public IReadOnlyDictionary<string, string> HeaderValues { get; init; } =
        new Dictionary<string, string>();
}
