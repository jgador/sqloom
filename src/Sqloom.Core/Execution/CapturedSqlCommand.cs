using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Captures one SQL command observed during replay.
/// </summary>
public sealed class CapturedSqlCommand
{
    [JsonPropertyName("sourceKind")]
    public required CapturedSqlSourceKind SourceKind { get; init; }

    [JsonPropertyName("source")]
    public required string Source { get; init; }

    [JsonPropertyName("commandText")]
    public required string CommandText { get; init; }

    [JsonPropertyName("normalizedCommandText")]
    public required string NormalizedCommandText { get; init; }

    [JsonPropertyName("fingerprint")]
    public required string Fingerprint { get; init; }

    [JsonPropertyName("parameters")]
    public IReadOnlyList<CapturedSqlParameter> Parameters { get; init; } =
        Array.Empty<CapturedSqlParameter>();

    [JsonPropertyName("duration")]
    public TimeSpan Duration { get; init; }

    [JsonPropertyName("recordsAffected")]
    public int? RecordsAffected { get; init; }
}
