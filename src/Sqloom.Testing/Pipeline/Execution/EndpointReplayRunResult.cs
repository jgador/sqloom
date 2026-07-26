using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Pipeline.Execution;

/// <summary>
/// Captures the replay artifacts and per-operation results from one run.
/// </summary>
public sealed class EndpointReplayRunResult
{
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
    /// ASP.NET Core source project used to discover replay operations.
    /// </summary>
    [JsonPropertyName("sourceProjectPath")]
    public required string SourceProjectPath { get; init; }

    /// <summary>
    /// Path to the canonical endpoint catalog stored with the replay run.
    /// </summary>
    [JsonPropertyName("discoveredOpsPath")]
    public required string DiscoveredOpsPath { get; init; }

    /// <summary>
    /// Gets the replay plan artifact path.
    /// </summary>
    [JsonPropertyName("replayPlanArtifactPath")]
    public required string ReplayPlanArtifactPath { get; init; }

    /// <summary>
    /// Gets the summary artifact path.
    /// </summary>
    [JsonPropertyName("summaryArtifactPath")]
    public required string SummaryArtifactPath { get; init; }

    /// <summary>
    /// Path to the persisted replay data generation report, or <see langword="null"/> when agent generation is disabled.
    /// </summary>
    [JsonPropertyName("replayDataGenerationPath")]
    public string? ReplayDataGenerationPath { get; init; }

    /// <summary>
    /// Per-operation replay data generation results, or <see langword="null"/> when agent generation is disabled.
    /// </summary>
    [JsonPropertyName("replayDataGeneration")]
    public ReplayDataGenerationReport? ReplayDataGeneration { get; init; }

    /// <summary>
    /// Gets the discovered operations.
    /// </summary>
    [JsonPropertyName("discoveredOperations")]
    public required IReadOnlyList<ReplayOperation> DiscoveredOperations { get; init; }

    /// <summary>
    /// Gets the replay plan.
    /// </summary>
    [JsonPropertyName("replayPlan")]
    public required EndpointReplayPlan ReplayPlan { get; init; }

    /// <summary>
    /// Gets the pipeline.
    /// </summary>
    [JsonPropertyName("pipeline")]
    public required PipelineReport Pipeline { get; init; }

    /// <summary>
    /// Gets the replay bootstrap.
    /// </summary>
    [JsonPropertyName("replayBootstrap")]
    public ReplayBootstrapReport ReplayBootstrap { get; init; } = new();

    /// <summary>
    /// Gets the results.
    /// </summary>
    [JsonPropertyName("results")]
    public required IReadOnlyList<EndpointReplayResult> Results { get; init; }
}
