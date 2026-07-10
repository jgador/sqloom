using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Describes replay operation overlay.
/// </summary>
public sealed class ReplayOverlay
{
    /// <summary>
    /// Gets the operation key.
    /// </summary>
    [JsonPropertyName("operationKey")]
    public required string OperationKey { get; init; }

    /// <summary>
    /// Gets the persona.
    /// </summary>
    [JsonPropertyName("persona")]
    public string? Persona { get; init; }

    /// <summary>
    /// Gets the replay by default.
    /// </summary>
    [JsonPropertyName("replayByDefault")]
    public bool ReplayByDefault { get; init; } = true;

    /// <summary>
    /// Gets the allow non get replay.
    /// </summary>
    [JsonPropertyName("allowNonGetReplay")]
    public bool AllowNonGetReplay { get; init; }

    /// <summary>
    /// Gets the skip reason.
    /// </summary>
    [JsonPropertyName("skipReason")]
    public string? SkipReason { get; init; }

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
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the query values.
    /// </summary>
    [JsonPropertyName("queryValues")]
    public IReadOnlyDictionary<string, string> QueryValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the header values.
    /// </summary>
    [JsonPropertyName("headerValues")]
    public IReadOnlyDictionary<string, string> HeaderValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the notes.
    /// </summary>
    [JsonPropertyName("notes")]
    public string Notes { get; init; } = string.Empty;
}
