using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.AspNetCore.OpenApi;
using Sqloom.Core.Artifacts;
using Sqloom.Core.Execution;

namespace Sqloom.AspNetCore.Endpoints;

/// <summary>
/// Writes discovered operation, plan, and replay artifacts to disk.
/// </summary>
internal sealed class ReplayArtifactWriter
{
    public string GetDiscoveredOpsPath(string replayArtifactDirectory)
    {
        return ArtifactLayout.GetDiscoveredOpsPath(replayArtifactDirectory);
    }

    public string GetPlanPath(string replayArtifactDirectory)
    {
        return ArtifactLayout.GetReplayPlanPath(replayArtifactDirectory);
    }

    public string GetSummaryPath(string replayArtifactDirectory)
    {
        return ArtifactLayout.GetReplaySummaryPath(replayArtifactDirectory);
    }

    public string GetOperationArtifactPath(
        string replayArtifactDirectory,
        int ordinal,
        string operationKey)
    {
        return ArtifactLayout.GetOperationArtifactPath(
            replayArtifactDirectory,
            ordinal,
            operationKey);
    }

    public Task WriteDiscoveredOpsAsync(
        string path,
        IReadOnlyList<OpenApiOperation> discoveredOperations,
        CancellationToken cancellationToken)
    {
        return JsonFileWriter.WriteAsync(path, discoveredOperations, cancellationToken);
    }

    public Task WritePlanAsync(
        string path,
        EndpointReplayPlan replayPlan,
        CancellationToken cancellationToken)
    {
        return JsonFileWriter.WriteAsync(path, replayPlan, cancellationToken);
    }

    public Task WriteOperationResultAsync(
        EndpointReplayResult replayResult,
        CancellationToken cancellationToken)
    {
        return JsonFileWriter.WriteAsync(replayResult.ArtifactPath, replayResult, cancellationToken);
    }

    public Task WriteSummaryAsync(
        string path,
        EndpointReplayRunResult runResult,
        CancellationToken cancellationToken)
    {
        return JsonFileWriter.WriteAsync(path, runResult, cancellationToken);
    }
}
