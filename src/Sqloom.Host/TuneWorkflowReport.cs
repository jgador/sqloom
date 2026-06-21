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
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    public required string AppName { get; init; }

    public required string WorkflowArtifactDirectory { get; init; }

    public required string QueryStoreSnapshotPath { get; init; }

    public required string ReplayArtifactDirectory { get; init; }

    public required string QueryStoreCorrelationPath { get; init; }

    public required string TuningAdvicePath { get; init; }

    public required string SqlProposalJsonPath { get; init; }

    public required string SqlProposalScriptPath { get; init; }

    [JsonPropertyName("modelProvider")]
    public string ModelProvider { get; init; } = "openai";

    public string? ModelName { get; init; }

    public required PipelineReport Pipeline { get; init; }

    public required TuneWorkflowSummary Summary { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Summarizes the key counts from a completed Sqloom tune workflow.
/// </summary>
internal sealed class TuneWorkflowSummary
{
    public int QueryStorePlanCount { get; init; }

    public int QueryStoreWaitCount { get; init; }

    public int ReplayOperationCount { get; init; }

    public int ReplayedOperationCount { get; init; }

    public int FailedOperationCount { get; init; }

    public int CapturedCommandCount { get; init; }

    public int MatchedCommandCount { get; init; }

    public int RecommendationCount { get; init; }

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
