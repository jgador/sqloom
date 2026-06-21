using System.Collections.Generic;

namespace Sqloom.Core.Execution;

/// <summary>
/// Captures the high-level outcome of a Sqloom run.
/// </summary>
public sealed class RunReport
{
    public required string AppName { get; init; }

    public required string ArtifactRoot { get; init; }

    public int DiscoveredOperationCount { get; init; }

    public int PlannedOperationCount { get; init; }

    public ReplayBootstrapReport ReplayBootstrap { get; init; } = new();

    public required PipelineReport Pipeline { get; init; }

    public required IReadOnlyList<EndpointOperationResult> Operations { get; init; }
}

/// <summary>
/// Captures the per-operation outcome of a replay run.
/// </summary>
public sealed class EndpointOperationResult
{
    public required string OperationKey { get; init; }

    public required string HttpMethod { get; init; }

    public required string Route { get; init; }

    public required string Status { get; init; }

    public string? SkipReason { get; init; }

    public int? HttpStatusCode { get; init; }

    public double? DurationMilliseconds { get; init; }

    public int CapturedSqlCommandCount { get; init; }

    public required IReadOnlyList<string> ArtifactPaths { get; init; }
}
