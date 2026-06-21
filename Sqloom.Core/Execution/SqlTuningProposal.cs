using System;
using System.Collections.Generic;

namespace Sqloom.Core.Execution;

/// <summary>
/// Captures one concrete SQL Server change proposal derived from Sqloom evidence.
/// </summary>
public sealed class SqlTuningProposal
{
    public required string Title { get; init; }

    public required string Diagnosis { get; init; }

    /// <summary>
    /// Free-form proposal classifier supplied by the emitting advisor or heuristic path.
    /// </summary>
    public required string ProposalKind { get; init; }

    /// <summary>
    /// Advisor-supplied target hint for the proposal, such as a table, view, or broader object scope.
    /// </summary>
    public required string TargetObject { get; init; }

    /// <summary>
    /// SQL Server script the reviewer can inspect and apply manually.
    /// </summary>
    public required string SqlScript { get; init; }

    /// <summary>
    /// Optional rollback script paired with <see cref="SqlScript"/>.
    /// </summary>
    public string? RollbackSqlScript { get; init; }

    public required string ExpectedBenefit { get; init; }

    public required string VerificationMetric { get; init; }

    public double Confidence { get; init; }

    public IReadOnlyList<int> SourceCommandOrdinals { get; init; } = Array.Empty<int>();

    public IReadOnlyList<long> MatchedPlanIds { get; init; } = Array.Empty<long>();
}
