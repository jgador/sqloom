using System;
using System.Collections.Generic;

namespace Sqloom.AspNetCore.Endpoints;

/// <summary>
/// Captures one SQL command observed during replay.
/// </summary>
public sealed class CapturedSqlCommand
{
    public required CapturedSqlSourceKind SourceKind { get; init; }

    public required string Source { get; init; }

    public required string CommandText { get; init; }

    public required string NormalizedCommandText { get; init; }

    public required string Fingerprint { get; init; }

    public IReadOnlyList<CapturedSqlParameter> Parameters { get; init; } =
        Array.Empty<CapturedSqlParameter>();

    public TimeSpan Duration { get; init; }

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
    public required string Name { get; init; }

    public string? DbType { get; init; }

    public int? Size { get; init; }

    public byte? Precision { get; init; }

    public byte? Scale { get; init; }

    public string? Value { get; init; }
}
