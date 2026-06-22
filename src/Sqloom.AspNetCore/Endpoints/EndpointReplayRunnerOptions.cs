using System.Collections.Generic;
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
    public required string AppName { get; init; }

    public required string ReplayArtifactDirectory { get; init; }

    public required string OpenApiDocumentPath { get; init; }

    public required string DiscoveredOperationsArtifactPath { get; init; }

    public required string ReplayPlanArtifactPath { get; init; }

    public required string SummaryArtifactPath { get; init; }

    public required IReadOnlyList<DiscoveredOpenApiOperation> DiscoveredOperations { get; init; }

    public required EndpointReplayPlan ReplayPlan { get; init; }

    public required PipelineReport Pipeline { get; init; }

    public ReplayBootstrapReport ReplayBootstrap { get; init; } = new();

    public required IReadOnlyList<EndpointReplayResult> Results { get; init; }
}
