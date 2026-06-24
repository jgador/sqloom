using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Captures the replay artifacts and per-operation results from one run.
/// </summary>
public sealed class EndpointReplayRunResult
{
    [JsonPropertyName("appName")]
    public required string AppName { get; init; }

    [JsonPropertyName("replayArtifactDir")]
    public required string ReplayArtifactDir { get; init; }

    [JsonPropertyName("openApiPath")]
    public required string OpenApiPath { get; init; }

    [JsonPropertyName("discoveredOpsPath")]
    public required string DiscoveredOpsPath { get; init; }

    [JsonPropertyName("replayPlanArtifactPath")]
    public required string ReplayPlanArtifactPath { get; init; }

    [JsonPropertyName("summaryArtifactPath")]
    public required string SummaryArtifactPath { get; init; }

    [JsonPropertyName("discoveredOperations")]
    public required IReadOnlyList<OpenApiOperation> DiscoveredOperations { get; init; }

    [JsonPropertyName("replayPlan")]
    public required EndpointReplayPlan ReplayPlan { get; init; }

    [JsonPropertyName("pipeline")]
    public required PipelineReport Pipeline { get; init; }

    [JsonPropertyName("replayBootstrap")]
    public ReplayBootstrapReport ReplayBootstrap { get; init; } = new();

    [JsonPropertyName("results")]
    public required IReadOnlyList<EndpointReplayResult> Results { get; init; }
}
