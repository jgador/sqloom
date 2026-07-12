using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Captures one SQL command observed during replay.
/// </summary>
public sealed class CapturedSqlCommand
{
    /// <summary>
    /// Gets the source kind.
    /// </summary>
    [JsonPropertyName("sourceKind")]
    public required CapturedSqlSourceKind SourceKind { get; init; }

    /// <summary>
    /// Gets the source.
    /// </summary>
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    /// <summary>
    /// Gets the command text.
    /// </summary>
    [JsonPropertyName("commandText")]
    public required string CommandText { get; init; }

    /// <summary>
    /// Gets the normalized command text.
    /// </summary>
    [JsonPropertyName("normalizedCommandText")]
    public required string NormalizedCommandText { get; init; }

    /// <summary>
    /// Gets the fingerprint.
    /// </summary>
    [JsonPropertyName("fingerprint")]
    public required string Fingerprint { get; init; }

    /// <summary>
    /// Gets the parameters.
    /// </summary>
    [JsonPropertyName("parameters")]
    public IReadOnlyList<CapturedSqlParameter> Parameters { get; init; } =
        Array.Empty<CapturedSqlParameter>();

    /// <summary>
    /// Gets the duration.
    /// </summary>
    [JsonPropertyName("duration")]
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the records affected.
    /// </summary>
    [JsonPropertyName("recordsAffected")]
    public int? RecordsAffected { get; init; }
}
