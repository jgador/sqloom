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
    /// Gets the open api path.
    /// </summary>
    [JsonPropertyName("openApiPath")]
    public required string OpenApiPath { get; init; }

    /// <summary>
    /// Gets the discovered ops path.
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
    /// Gets the replay data preparation path.
    /// </summary>
    [JsonPropertyName("replayDataPreparationPath")]
    public string? ReplayDataPreparationPath { get; init; }

    /// <summary>
    /// Gets the replay data preparation.
    /// </summary>
    [JsonPropertyName("replayDataPreparation")]
    public ReplayDataPreparationReport? ReplayDataPreparation { get; init; }

    /// <summary>
    /// Gets the discovered operations.
    /// </summary>
    [JsonPropertyName("discoveredOperations")]
    public required IReadOnlyList<OpenApiOperation> DiscoveredOperations { get; init; }

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
