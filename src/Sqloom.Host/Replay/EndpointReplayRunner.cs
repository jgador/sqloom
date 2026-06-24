using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sqloom.Testing.AspNetCore;
using Sqloom.Core.Execution;
using Sqloom.Core.Artifacts;

namespace Sqloom.Host.Replay;

/// <summary>
/// Executes OpenAPI-driven replay operations against a Sqloom app harness.
/// </summary>
public sealed class EndpointReplayRunner
{
    private readonly OpenApiCatalogLoader _catalogLoader = new();
    private readonly ReplayArtifactWriter _artifactWriter = new();
    private readonly ReplayPlanBuilder _planBuilder = new();
    private readonly ReplayRequestExecutor _requestExecutor = new();
    private readonly ReplayRequestResolver _requestResolver = new();

    public async Task<EndpointReplayRunResult> RunAsync(
        ReplayRunnerOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var discoveredOperations = await _catalogLoader
            .LoadAsync(options.OpenApiPath, cancellationToken)
            .ConfigureAwait(false);

        var discoveredOperationsPath = _artifactWriter.GetDiscoveredOpsPath(options.ReplayArtifactDir);
        await _artifactWriter
            .WriteDiscoveredOpsAsync(
                discoveredOperationsPath,
                discoveredOperations,
                cancellationToken)
            .ConfigureAwait(false);

        var initialPlan = _planBuilder.BuildInitialPlan(options, discoveredOperations);
        var replayPlanPath = _artifactWriter.GetPlanPath(options.ReplayArtifactDir);
        await _artifactWriter
            .WritePlanAsync(replayPlanPath, initialPlan, cancellationToken)
            .ConfigureAwait(false);

        var results = new List<EndpointReplayResult>();
        var finalizedPlanItems = new List<EndpointReplayPlanItem>();
        var discoveredByKey = discoveredOperations.ToDictionary(
            operation => operation.StableOperationKey,
            StringComparer.OrdinalIgnoreCase);
        var overlays = options.ReplayProfile.OperationOverlays.ToDictionary(
            operation => operation.OperationKey,
            StringComparer.OrdinalIgnoreCase);

        var ownsReplayHost = options.ReplayHost is null;
        var replayHost = options.ReplayHost
            ?? await CreateReplayHostAsync(options, cancellationToken).ConfigureAwait(false);
        ReplayBootstrapReport replayBootstrap;
        try
        {
            await ExecuteReplayPlanAsync(
                    options,
                    replayHost,
                    initialPlan,
                    discoveredByKey,
                    overlays,
                    results,
                    finalizedPlanItems,
                    cancellationToken)
                .ConfigureAwait(false);
            replayBootstrap = replayHost.Bootstrap;
        }
        finally
        {
            if (ownsReplayHost)
            {
                await replayHost.DisposeAsync().ConfigureAwait(false);
            }
        }

        var finalPlan = new EndpointReplayPlan()
        {
            AppName = initialPlan.AppName,
            OpenApiPath = initialPlan.OpenApiPath,
            PlannedAtUtc = initialPlan.PlannedAtUtc,
            Operations = finalizedPlanItems,
        };
        await _artifactWriter
            .WritePlanAsync(replayPlanPath, finalPlan, cancellationToken)
            .ConfigureAwait(false);

        var summaryPath = _artifactWriter.GetSummaryPath(options.ReplayArtifactDir);
        var runResult = new EndpointReplayRunResult()
        {
            AppName = options.AppName,
            ReplayArtifactDir = options.ReplayArtifactDir,
            OpenApiPath = options.OpenApiPath,
            DiscoveredOpsPath = discoveredOperationsPath,
            ReplayPlanArtifactPath = replayPlanPath,
            SummaryArtifactPath = summaryPath,
            DiscoveredOperations = discoveredOperations,
            ReplayPlan = finalPlan,
            Pipeline = CreatePipeline(options.ReplayArtifactDir, summaryPath, results),
            ReplayBootstrap = replayBootstrap,
            Results = results,
        };
        await _artifactWriter
            .WriteSummaryAsync(summaryPath, runResult, cancellationToken)
            .ConfigureAwait(false);

        return runResult;
    }

    private static async Task<IReplayHost> CreateReplayHostAsync(
        ReplayRunnerOptions options,
        CancellationToken cancellationToken)
    {
        if (options.ReplayHostFactory is null)
        {
            throw new InvalidOperationException(
                "Endpoint replay requires either a ReplayHost or a ReplayHostFactory.");
        }

        return await options.ReplayHostFactory
            .CreateAsync(options.ReplayLaunchOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task ExecuteReplayPlanAsync(
        ReplayRunnerOptions options,
        IReplayHost replayHost,
        EndpointReplayPlan initialPlan,
        IReadOnlyDictionary<string, OpenApiOperation> discoveredByKey,
        IReadOnlyDictionary<string, ReplayOverlay> overlays,
        ICollection<EndpointReplayResult> results,
        ICollection<EndpointReplayPlanItem> finalizedPlanItems,
        CancellationToken cancellationToken)
    {
        var captureCollector =
            replayHost.Services.GetService<ReplaySqlCaptureCollector>();

        var ordinal = 0;
        foreach (var planItem in initialPlan.Operations)
        {
            if (!string.Equals(planItem.Status, "planned", StringComparison.OrdinalIgnoreCase))
            {
                finalizedPlanItems.Add(planItem);
                continue;
            }

            ordinal++;
            var discoveredOperation = discoveredByKey[planItem.OperationKey];
            overlays.TryGetValue(planItem.OperationKey, out var overlay);
            var resolvedOperation = ReplayOperationResolver.Resolve(
                discoveredOperation,
                overlay);
            var artifactPath = _artifactWriter.GetOperationArtifactPath(
                options.ReplayArtifactDir,
                ordinal,
                planItem.OperationKey);

            EndpointReplayResult result;
            try
            {
                var preparedOperation = await replayHost
                    .PrepareOperationAsync(resolvedOperation, cancellationToken)
                    .ConfigureAwait(false);
                var request = _requestResolver.Resolve(
                    discoveredOperation,
                    resolvedOperation,
                    preparedOperation);
                result = await _requestExecutor
                    .ExecuteAsync(
                        replayHost.Client,
                        captureCollector,
                        request,
                        preparedOperation.AccessToken,
                        artifactPath,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                result = EndpointReplayResultFactory.CreateFailed(
                    planItem,
                    artifactPath,
                    exception.Message);
            }

            await _artifactWriter
                .WriteOperationResultAsync(result, cancellationToken)
                .ConfigureAwait(false);
            results.Add(result);
            finalizedPlanItems.Add(CreateFinalPlanItem(planItem, result));
        }
    }

    private static PipelineReport CreatePipeline(
        string replayArtifactDirectory,
        string summaryArtifactPath,
        IReadOnlyList<EndpointReplayResult> results)
    {
        var capturedCommandCount = results.Sum(static result => result.CapturedSqlCommands.Count);
        var correlationArtifactPath = ArtifactLayout.GetCorrelationPath(replayArtifactDirectory);
        var adviceArtifactPath = ArtifactLayout.GetReplayTuningAdvicePath(replayArtifactDirectory);

        return new PipelineReport
        {
            Stages =
            [
                new PipelineStageReport
                {
                    Name = PipelineStageNames.Observe,
                    Status = PipelineStageStatuses.Available,
                    Summary = "Capture a matching Query Store snapshot with --query-store before or after replay.",
                },
                new PipelineStageReport
                {
                    Name = PipelineStageNames.Replay,
                    Status = PipelineStageStatuses.Completed,
                    Summary = $"Replayed {results.Count} operation(s) through the active harness.",
                    ArtifactPath = summaryArtifactPath,
                },
                new PipelineStageReport
                {
                    Name = PipelineStageNames.Capture,
                    Status = PipelineStageStatuses.Completed,
                    Summary = $"Captured {capturedCommandCount} SQL command(s) during replay.",
                    ArtifactPath = replayArtifactDirectory,
                },
                new PipelineStageReport
                {
                    Name = PipelineStageNames.Correlate,
                    Status = PipelineStageStatuses.Available,
                    Summary = "Run --correlate to map captured SQL back to Query Store rows.",
                    ArtifactPath = correlationArtifactPath,
                },
                new PipelineStageReport
                {
                    Name = PipelineStageNames.Advise,
                    Status = PipelineStageStatuses.Available,
                    Summary = "Run --advise after correlation to emit operation-level tuning guidance.",
                    ArtifactPath = adviceArtifactPath,
                },
            ],
        };
    }

    private static EndpointReplayPlanItem CreateFinalPlanItem(
        EndpointReplayPlanItem planItem,
        EndpointReplayResult result)
    {
        return new EndpointReplayPlanItem
        {
            OperationKey = planItem.OperationKey,
            OperationId = planItem.OperationId,
            HttpMethod = planItem.HttpMethod,
            Route = planItem.Route,
            Persona = planItem.Persona,
            RequiresAuthentication = planItem.RequiresAuthentication,
            HasJsonRequestBody = planItem.HasJsonRequestBody,
            ReplaySafe = planItem.ReplaySafe,
            Status = result.Status,
            Reason = result.ErrorMessage,
            Notes = planItem.Notes,
        };
    }
}
