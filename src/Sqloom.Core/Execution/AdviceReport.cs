using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Captures operation-level tuning guidance derived from a completed Sqloom correlation run.
/// </summary>
public sealed class AdviceReport
{
    [JsonPropertyName("generatedAtUtc")]
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    [JsonPropertyName("appName")]
    public required string AppName { get; init; }

    [JsonPropertyName("replayArtifactDirectory")]
    public required string ReplayArtifactDirectory { get; init; }

    [JsonPropertyName("queryStoreCorrelationPath")]
    public required string QueryStoreCorrelationPath { get; init; }

    [JsonPropertyName("modelProvider")]
    public string ModelProvider { get; init; } = "openai";

    [JsonPropertyName("modelName")]
    public string? ModelName { get; init; }

    [JsonPropertyName("strategyName")]
    public required string StrategyName { get; init; }

    [JsonPropertyName("sqlProposalJsonPath")]
    public required string SqlProposalJsonPath { get; init; }

    [JsonPropertyName("sqlProposalScriptPath")]
    public required string SqlProposalScriptPath { get; init; }

    [JsonPropertyName("pipeline")]
    public required PipelineReport Pipeline { get; init; }

    [JsonPropertyName("summary")]
    public required AdviceSummary Summary { get; init; }

    [JsonPropertyName("operations")]
    public required IReadOnlyList<AdviceOperationReport> Operations { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Summarizes the amount of advice emitted for a Sqloom advice run.
/// </summary>
public sealed class AdviceSummary
{
    [JsonPropertyName("operationCount")]
    public int OperationCount { get; init; }

    [JsonPropertyName("recommendationCount")]
    public int RecommendationCount { get; init; }

    [JsonPropertyName("proposalCount")]
    public int ProposalCount { get; init; }
}

/// <summary>
/// Carries the operation-scoped recommendations emitted by Sqloom advice.
/// </summary>
public sealed class AdviceOperationReport
{
    [JsonPropertyName("operationKey")]
    public required string OperationKey { get; init; }

    [JsonPropertyName("httpMethod")]
    public required string HttpMethod { get; init; }

    [JsonPropertyName("route")]
    public required string Route { get; init; }

    [JsonPropertyName("replayStatus")]
    public required string ReplayStatus { get; init; }

    [JsonPropertyName("capturedCommandCount")]
    public int CapturedCommandCount { get; init; }

    [JsonPropertyName("matchedCommandCount")]
    public int MatchedCommandCount { get; init; }

    [JsonPropertyName("recommendations")]
    public required IReadOnlyList<SqlTuningRecommendation> Recommendations { get; init; }

    [JsonPropertyName("proposals")]
    public required IReadOnlyList<SqlTuningProposal> Proposals { get; init; }
}
