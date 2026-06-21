using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Captures operation-level tuning guidance derived from a completed Sqloom correlation run.
/// </summary>
public sealed class AdviceReport
{
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    public required string AppName { get; init; }

    public required string ReplayArtifactDirectory { get; init; }

    public required string QueryStoreCorrelationPath { get; init; }

    [JsonPropertyName("modelProvider")]
    public string ModelProvider { get; init; } = "openai";

    public string? ModelName { get; init; }

    public required string StrategyName { get; init; }

    public required string SqlProposalJsonPath { get; init; }

    public required string SqlProposalScriptPath { get; init; }

    public required PipelineReport Pipeline { get; init; }

    public required AdviceSummary Summary { get; init; }

    public required IReadOnlyList<AdviceOperationReport> Operations { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Summarizes the amount of advice emitted for a Sqloom advice run.
/// </summary>
public sealed class AdviceSummary
{
    public int OperationCount { get; init; }

    public int RecommendationCount { get; init; }

    public int ProposalCount { get; init; }
}

/// <summary>
/// Carries the operation-scoped recommendations emitted by Sqloom advice.
/// </summary>
public sealed class AdviceOperationReport
{
    public required string OperationKey { get; init; }

    public required string HttpMethod { get; init; }

    public required string Route { get; init; }

    public required string ReplayStatus { get; init; }

    public int CapturedCommandCount { get; init; }

    public int MatchedCommandCount { get; init; }

    public required IReadOnlyList<SqlTuningRecommendation> Recommendations { get; init; }

    public required IReadOnlyList<SqlTuningProposal> Proposals { get; init; }
}
