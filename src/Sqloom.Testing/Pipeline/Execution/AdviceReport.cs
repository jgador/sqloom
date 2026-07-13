using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Pipeline.Execution;

/// <summary>
/// Captures operation-level tuning guidance derived from a completed Sqloom correlation run.
/// </summary>
public sealed class AdviceReport
{
    /// <summary>
    /// Gets the generated at utc.
    /// </summary>
    [JsonPropertyName("generatedAtUtc")]
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    /// <summary>
    /// Gets the app name.
    /// </summary>
    [JsonPropertyName("appName")]
    public required string AppName { get; init; }

    /// <summary>
    /// Gets the replay artifact dir.
    /// </summary>
    [JsonPropertyName("replayArtifactDir")]
    public required string ReplayArtifactDir { get; init; }

    /// <summary>
    /// Gets the query store correlation path.
    /// </summary>
    [JsonPropertyName("queryStoreCorrelationPath")]
    public required string QueryStoreCorrelationPath { get; init; }

    /// <summary>
    /// Gets the model provider.
    /// </summary>
    [JsonPropertyName("modelProvider")]
    public string ModelProvider { get; init; } = "openai";

    /// <summary>
    /// Gets the model name.
    /// </summary>
    [JsonPropertyName("modelName")]
    public string? ModelName { get; init; }

    /// <summary>
    /// Gets the strategy name.
    /// </summary>
    [JsonPropertyName("strategyName")]
    public required string StrategyName { get; init; }

    /// <summary>
    /// Gets the sql proposal json path.
    /// </summary>
    [JsonPropertyName("sqlProposalJsonPath")]
    public required string SqlProposalJsonPath { get; init; }

    /// <summary>
    /// Gets the sql proposal script path.
    /// </summary>
    [JsonPropertyName("sqlProposalScriptPath")]
    public required string SqlProposalScriptPath { get; init; }

    /// <summary>
    /// Gets the pipeline.
    /// </summary>
    [JsonPropertyName("pipeline")]
    public required PipelineReport Pipeline { get; init; }

    /// <summary>
    /// Gets the summary.
    /// </summary>
    [JsonPropertyName("summary")]
    public required AdviceSummary Summary { get; init; }

    /// <summary>
    /// Gets the operations.
    /// </summary>
    [JsonPropertyName("operations")]
    public required IReadOnlyList<AdviceOperationReport> Operations { get; init; }

    /// <summary>
    /// Gets the warnings.
    /// </summary>
    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Summarizes the amount of advice emitted for a Sqloom advice run.
/// </summary>
public sealed class AdviceSummary
{
    /// <summary>
    /// Gets the operation count.
    /// </summary>
    [JsonPropertyName("operationCount")]
    public int OperationCount { get; init; }

    /// <summary>
    /// Gets the recommendation count.
    /// </summary>
    [JsonPropertyName("recommendationCount")]
    public int RecommendationCount { get; init; }

    /// <summary>
    /// Gets the proposal count.
    /// </summary>
    [JsonPropertyName("proposalCount")]
    public int ProposalCount { get; init; }
}

/// <summary>
/// Carries the operation-scoped recommendations emitted by Sqloom advice.
/// </summary>
public sealed class AdviceOperationReport
{
    /// <summary>
    /// Gets the operation key.
    /// </summary>
    [JsonPropertyName("operationKey")]
    public required string OperationKey { get; init; }

    /// <summary>
    /// Gets the http method.
    /// </summary>
    [JsonPropertyName("httpMethod")]
    public required string HttpMethod { get; init; }

    /// <summary>
    /// Gets the route.
    /// </summary>
    [JsonPropertyName("route")]
    public required string Route { get; init; }

    /// <summary>
    /// Gets the replay status.
    /// </summary>
    [JsonPropertyName("replayStatus")]
    public required string ReplayStatus { get; init; }

    /// <summary>
    /// Gets the captured command count.
    /// </summary>
    [JsonPropertyName("capturedCommandCount")]
    public int CapturedCommandCount { get; init; }

    /// <summary>
    /// Gets the matched command count.
    /// </summary>
    [JsonPropertyName("matchedCommandCount")]
    public int MatchedCommandCount { get; init; }

    /// <summary>
    /// Gets the recommendations.
    /// </summary>
    [JsonPropertyName("recommendations")]
    public required IReadOnlyList<SqlTuningRecommendation> Recommendations { get; init; }

    /// <summary>
    /// Gets the proposals.
    /// </summary>
    [JsonPropertyName("proposals")]
    public required IReadOnlyList<SqlTuningProposal> Proposals { get; init; }
}
