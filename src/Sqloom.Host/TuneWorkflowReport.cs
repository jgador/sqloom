using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Sqloom.Core.Execution;

namespace Sqloom.Host;

/// <summary>
/// Captures the persisted summary for a completed Sqloom tune workflow.
/// </summary>
internal sealed class TuneWorkflowReport
{
    [JsonPropertyName("generatedAtUtc")]
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    [JsonPropertyName("appName")]
    public required string AppName { get; init; }

    [JsonPropertyName("workflowArtifactDir")]
    public required string WorkflowArtifactDir { get; init; }

    [JsonPropertyName("queryStoreSnapshotPath")]
    public required string QueryStoreSnapshotPath { get; init; }

    [JsonPropertyName("replayArtifactDir")]
    public required string ReplayArtifactDir { get; init; }

    [JsonPropertyName("queryStoreCorrelationPath")]
    public required string QueryStoreCorrelationPath { get; init; }

    [JsonPropertyName("tuningAdvicePath")]
    public required string TuningAdvicePath { get; init; }

    [JsonPropertyName("sqlProposalJsonPath")]
    public required string SqlProposalJsonPath { get; init; }

    [JsonPropertyName("sqlProposalScriptPath")]
    public required string SqlProposalScriptPath { get; init; }

    [JsonPropertyName("modelProvider")]
    public string ModelProvider { get; init; } = "openai";

    [JsonPropertyName("modelName")]
    public string? ModelName { get; init; }

    [JsonPropertyName("pipeline")]
    public required PipelineReport Pipeline { get; init; }

    [JsonPropertyName("summary")]
    public required TuneWorkflowSummary Summary { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Summarizes the key counts from a completed Sqloom tune workflow.
/// </summary>
internal sealed class TuneWorkflowSummary
{
    [JsonPropertyName("queryStorePlanCount")]
    public int QueryStorePlanCount { get; init; }

    [JsonPropertyName("queryStoreWaitCount")]
    public int QueryStoreWaitCount { get; init; }

    [JsonPropertyName("replayOperationCount")]
    public int ReplayOperationCount { get; init; }

    [JsonPropertyName("replayedOperationCount")]
    public int ReplayedOperationCount { get; init; }

    [JsonPropertyName("failedOperationCount")]
    public int FailedOperationCount { get; init; }

    [JsonPropertyName("capturedCommandCount")]
    public int CapturedCommandCount { get; init; }

    [JsonPropertyName("matchedCommandCount")]
    public int MatchedCommandCount { get; init; }

    [JsonPropertyName("recommendationCount")]
    public int RecommendationCount { get; init; }

    [JsonPropertyName("proposalCount")]
    public int ProposalCount { get; init; }
}

/// <summary>
/// Carries the typed in-memory results for a completed Sqloom tune workflow.
/// </summary>
internal sealed class TuneWorkflowResult
{
    public required ObserveCommandResult ObserveResult { get; init; }

    public required ReplayCommandResult ReplayResult { get; init; }

    public required CorrelateCommandResult CorrelateResult { get; init; }

    public required AdviceCommandResult AdviceResult { get; init; }

    public required TuneWorkflowReport Report { get; init; }

    public required string SummaryOutputPath { get; init; }

    public int ExitCode { get; init; }
}
