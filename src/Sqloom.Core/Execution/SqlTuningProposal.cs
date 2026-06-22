using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Captures one concrete SQL Server change proposal derived from Sqloom evidence.
/// </summary>
public sealed class SqlTuningProposal
{
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("diagnosis")]
    public required string Diagnosis { get; init; }

    /// <summary>
    /// Free-form proposal classifier supplied by the emitting advisor or heuristic path.
    /// </summary>
    [JsonPropertyName("proposalKind")]
    public required string ProposalKind { get; init; }

    /// <summary>
    /// Advisor-supplied target hint for the proposal, such as a table, view, or broader object scope.
    /// </summary>
    [JsonPropertyName("targetObject")]
    public required string TargetObject { get; init; }

    /// <summary>
    /// SQL Server script the reviewer can inspect and apply manually.
    /// </summary>
    [JsonPropertyName("sqlScript")]
    public required string SqlScript { get; init; }

    /// <summary>
    /// Optional rollback script paired with <see cref="SqlScript"/>.
    /// </summary>
    [JsonPropertyName("rollbackSqlScript")]
    public string? RollbackSqlScript { get; init; }

    [JsonPropertyName("expectedBenefit")]
    public required string ExpectedBenefit { get; init; }

    [JsonPropertyName("verificationMetric")]
    public required string VerificationMetric { get; init; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    [JsonPropertyName("sourceCommandOrdinals")]
    public IReadOnlyList<int> SourceCommandOrdinals { get; init; } = Array.Empty<int>();

    [JsonPropertyName("matchedPlanIds")]
    public IReadOnlyList<long> MatchedPlanIds { get; init; } = Array.Empty<long>();
}
