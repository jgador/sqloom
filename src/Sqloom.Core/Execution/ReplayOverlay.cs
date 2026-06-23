using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Describes replay operation overlay.
/// </summary>
public sealed class ReplayOverlay
{
    [JsonPropertyName("operationKey")]
    public required string OperationKey { get; init; }

    [JsonPropertyName("persona")]
    public string? Persona { get; init; }

    [JsonPropertyName("replayByDefault")]
    public bool ReplayByDefault { get; init; } = true;

    [JsonPropertyName("allowNonGetReplay")]
    public bool AllowNonGetReplay { get; init; }

    [JsonPropertyName("skipReason")]
    public string? SkipReason { get; init; }

    [JsonPropertyName("requestBodyJson")]
    public string? RequestBodyJson { get; init; }

    [JsonPropertyName("pathValues")]
    public IReadOnlyDictionary<string, string> PathValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("queryValues")]
    public IReadOnlyDictionary<string, string> QueryValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("headerValues")]
    public IReadOnlyDictionary<string, string> HeaderValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("notes")]
    public string Notes { get; init; } = string.Empty;
}
