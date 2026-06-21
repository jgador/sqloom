using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Persists the SQL proposal subset of a Sqloom advice run as a dedicated artifact.
/// </summary>
public sealed class SqlTuningProposalReport
{
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    public required string AppName { get; init; }

    public required string ReplayArtifactDirectory { get; init; }

    public required string QueryStoreCorrelationPath { get; init; }

    public required string SourceAdvicePath { get; init; }

    public required string SqlScriptPath { get; init; }

    [JsonPropertyName("modelProvider")]
    public string ModelProvider { get; init; } = "openai";

    public string? ModelName { get; init; }

    public required string StrategyName { get; init; }

    public required SqlTuningProposalSummary Summary { get; init; }

    public required IReadOnlyList<SqlTuningProposalOperationReport> Operations { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Summarizes the SQL proposals emitted by a Sqloom advice run.
/// </summary>
public sealed class SqlTuningProposalSummary
{
    public int OperationCount { get; init; }

    public int ProposalCount { get; init; }
}

/// <summary>
/// Captures the SQL proposals emitted for one replayed operation.
/// </summary>
public sealed class SqlTuningProposalOperationReport
{
    public required string OperationKey { get; init; }

    public required string HttpMethod { get; init; }

    public required string Route { get; init; }

    public required string ReplayStatus { get; init; }

    public required IReadOnlyList<SqlTuningProposal> Proposals { get; init; }
}
