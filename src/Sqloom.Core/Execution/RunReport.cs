using System.Collections.Generic;

namespace Sqloom.Core.Execution;

/// <summary>
/// Captures the high-level outcome of a Sqloom run.
/// </summary>
public sealed class RunReport
{
    /// <summary>
    /// Gets the app name.
    /// </summary>
    public required string AppName { get; init; }

    /// <summary>
    /// Gets the artifact root.
    /// </summary>
    public required string ArtifactRoot { get; init; }

    /// <summary>
    /// Gets the discovered operation count.
    /// </summary>
    public int DiscoveredOperationCount { get; init; }

    /// <summary>
    /// Gets the planned operation count.
    /// </summary>
    public int PlannedOperationCount { get; init; }

    /// <summary>
    /// Gets the replay bootstrap.
    /// </summary>
    public ReplayBootstrapReport ReplayBootstrap { get; init; } = new();

    /// <summary>
    /// Gets the pipeline.
    /// </summary>
    public required PipelineReport Pipeline { get; init; }

    /// <summary>
    /// Gets the operations.
    /// </summary>
    public required IReadOnlyList<EndpointOperationResult> Operations { get; init; }
}

/// <summary>
/// Captures the per-operation outcome of a replay run.
/// </summary>
public sealed class EndpointOperationResult
{
    /// <summary>
    /// Gets the operation key.
    /// </summary>
    public required string OperationKey { get; init; }

    /// <summary>
    /// Gets the http method.
    /// </summary>
    public required string HttpMethod { get; init; }

    /// <summary>
    /// Gets the route.
    /// </summary>
    public required string Route { get; init; }

    /// <summary>
    /// Gets the status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the skip reason.
    /// </summary>
    public string? SkipReason { get; init; }

    /// <summary>
    /// Gets the http status code.
    /// </summary>
    public int? HttpStatusCode { get; init; }

    /// <summary>
    /// Gets the duration milliseconds.
    /// </summary>
    public double? DurationMilliseconds { get; init; }

    /// <summary>
    /// Gets the captured sql command count.
    /// </summary>
    public int CapturedSqlCommandCount { get; init; }

    /// <summary>
    /// Gets the artifact paths.
    /// </summary>
    public required IReadOnlyList<string> ArtifactPaths { get; init; }
}
