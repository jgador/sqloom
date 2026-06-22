using System.Collections.Generic;
using System.Text.Json.Serialization;
using Sqloom.AspNetCore.OpenApi;
using Sqloom.Core.Execution;

namespace Sqloom.AspNetCore.Endpoints;

/// <summary>
/// Carries the inputs required to execute an endpoint replay run.
/// </summary>
public sealed class EndpointReplayRunnerOptions
{
    public required string AppName { get; init; }

    public required string OpenApiDocumentPath { get; init; }

    public required string ReplayArtifactDirectory { get; init; }

    public required ReplayProfile ReplayProfile { get; init; }

    public IReplayHostFactory? ReplayHostFactory { get; init; }

    public IReplayHost? ReplayHost { get; init; }

    public ReplayLaunchOptions ReplayLaunchOptions { get; init; } = new();

    public int MaxOperations { get; init; } = 25;

    public string? TargetFilter { get; init; }
}

/// <summary>
/// Captures the replay artifacts and per-operation results from one run.
/// </summary>
public sealed class EndpointReplayRunResult
{
    [JsonPropertyName("appName")]
    public required string AppName { get; init; }

    [JsonPropertyName("replayArtifactDirectory")]
    public required string ReplayArtifactDirectory { get; init; }

    [JsonPropertyName("openApiDocumentPath")]
    public required string OpenApiDocumentPath { get; init; }

    [JsonPropertyName("discoveredOperationsArtifactPath")]
    public required string DiscoveredOperationsArtifactPath { get; init; }

    [JsonPropertyName("replayPlanArtifactPath")]
    public required string ReplayPlanArtifactPath { get; init; }

    [JsonPropertyName("summaryArtifactPath")]
    public required string SummaryArtifactPath { get; init; }

    [JsonPropertyName("discoveredOperations")]
    public required IReadOnlyList<DiscoveredOpenApiOperation> DiscoveredOperations { get; init; }

    [JsonPropertyName("replayPlan")]
    public required EndpointReplayPlan ReplayPlan { get; init; }

    [JsonPropertyName("pipeline")]
    public required PipelineReport Pipeline { get; init; }

    [JsonPropertyName("replayBootstrap")]
    public ReplayBootstrapReport ReplayBootstrap { get; init; } = new();

    [JsonPropertyName("results")]
    public required IReadOnlyList<EndpointReplayResult> Results { get; init; }
}
