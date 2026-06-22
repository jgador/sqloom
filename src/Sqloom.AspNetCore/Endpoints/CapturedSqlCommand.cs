using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.AspNetCore.Endpoints;

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

public enum CapturedSqlSourceKind
{
    EntityFramework = 0,
    AdoNet = 1
}

/// <summary>
/// Captures one SQL parameter observed during replay.
/// </summary>
public sealed class CapturedSqlParameter
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("dbType")]
    public string? DbType { get; init; }

    [JsonPropertyName("size")]
    public int? Size { get; init; }

    [JsonPropertyName("precision")]
    public byte? Precision { get; init; }

    [JsonPropertyName("scale")]
    public byte? Scale { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }
}
