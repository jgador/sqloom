using System;
using System.Collections.Generic;

namespace Sqloom.Pipeline.Showplan.Plans;

/// <summary>
/// Captures the extracted facts from a SQL Server SHOWPLAN fragment.
/// </summary>
public sealed class ShowplanFacts
{
    /// <summary>
    /// Gets the statement text.
    /// </summary>
    public required string StatementText { get; init; }

    /// <summary>
    /// Gets the dominant operator.
    /// </summary>
    public required string DominantOperator { get; init; }

    /// <summary>
    /// Gets the has key lookup.
    /// </summary>
    public bool HasKeyLookup { get; init; }

    /// <summary>
    /// Gets the has spill.
    /// </summary>
    public bool HasSpill { get; init; }

    /// <summary>
    /// Gets the estimated rows.
    /// </summary>
    public double EstimatedRows { get; init; }

    /// <summary>
    /// Gets the actual rows.
    /// </summary>
    public double ActualRows { get; init; }

    /// <summary>
    /// Gets the warnings.
    /// </summary>
    public IReadOnlyList<ShowplanWarning> Warnings { get; init; } = Array.Empty<ShowplanWarning>();
}
