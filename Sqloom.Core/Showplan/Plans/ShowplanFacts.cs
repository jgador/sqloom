using System;
using System.Collections.Generic;

namespace Sqloom.Showplan.Plans;

/// <summary>
/// Captures the extracted facts from a SQL Server SHOWPLAN fragment.
/// </summary>
public sealed class ShowplanFacts
{
    public required string StatementText { get; init; }

    public required string DominantOperator { get; init; }

    public bool HasKeyLookup { get; init; }

    public bool HasSpill { get; init; }

    public double EstimatedRows { get; init; }

    public double ActualRows { get; init; }

    public IReadOnlyList<ShowplanWarning> Warnings { get; init; } = Array.Empty<ShowplanWarning>();
}
