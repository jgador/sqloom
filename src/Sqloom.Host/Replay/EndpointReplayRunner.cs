using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sqloom.Pipeline.Artifacts;
using Sqloom.Pipeline.Execution;
using Sqloom.Testing.AspNetCore;

namespace Sqloom.Host.Replay;

/// <summary>
/// Executes source-discovered replay operations against a Sqloom app harness.
/// </summary>
public sealed class EndpointReplayRunner
{
    private readonly EndpointCatalogLoader _catalogLoader = new();
    private readonly ReplayArtifactWriter _artifactWriter = new();
    private readonly ReplayPlanBuilder _planBuilder = new();
    private readonly ReplayRequestExecutor _requestExecutor = new();
    private readonly ReplayRequestResolver _requestResolver = new();

    /// <summary>
    /// Discovers, prepares, executes, and records the configured endpoint replay operations.
    /// </summary>
    public async Task<EndpointReplayRunResult> RunAsync(
        ReplayRunnerOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var discoveredOperations = await _catalogLoader
            .LoadAsync(options.SourceProjectPath, cancellationToken)
            .ConfigureAwait(false);
        return await RunAsync(options, discoveredOperations, cancellationToken)
            .ConfigureAwait(false);
    }

    internal async Task<EndpointReplayRunResult> RunAsync(
        ReplayRunnerOptions options,
        IReadOnlyList<ReplayOperation> discoveredOperations,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(discoveredOperations);

        var endpointCatalogPath = _artifactWriter.GetEndpointCatalogPath(options.ReplayArtifactDir);
        await _artifactWriter
            .WriteDiscoveredOpsAsync(
                endpointCatalogPath,
                discoveredOperations,
                cancellationToken)
            .ConfigureAwait(false);

        var legacyDiscoveredOperationsPath = _artifactWriter.GetDiscoveredOpsPath(options.ReplayArtifactDir);
        await _artifactWriter
            .WriteDiscoveredOpsAsync(
                legacyDiscoveredOperationsPath,
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
        var replayDataGenerationOperations = new List<ReplayDataGenerationOperation>();
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
                    replayDataGenerationOperations,
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
            SourceProjectPath = initialPlan.SourceProjectPath,
            PlannedAtUtc = initialPlan.PlannedAtUtc,
            Operations = finalizedPlanItems,
        };
        await _artifactWriter
            .WritePlanAsync(replayPlanPath, finalPlan, cancellationToken)
            .ConfigureAwait(false);

        var summaryPath = _artifactWriter.GetSummaryPath(options.ReplayArtifactDir);
        var replayDataGenerationPath =
            options.ReplayDataAgentOptions.Mode == ReplayDataAgentMode.Off
                ? null
                : _artifactWriter.GetReplayDataGenerationPath(options.ReplayArtifactDir);
        var replayDataGeneration = replayDataGenerationPath is null
            ? null
            : CreateReplayDataGenerationReport(
                options,
                replayDataGenerationOperations);
        if (replayDataGenerationPath is not null
            && replayDataGeneration is not null)
        {
            await _artifactWriter
                .WriteReplayDataGenerationAsync(
                    replayDataGenerationPath,
                    replayDataGeneration,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var runResult = new EndpointReplayRunResult()
        {
            AppName = options.AppName,
            ReplayArtifactDir = options.ReplayArtifactDir,
            SourceProjectPath = options.SourceProjectPath,
            DiscoveredOpsPath = endpointCatalogPath,
            ReplayPlanArtifactPath = replayPlanPath,
            SummaryArtifactPath = summaryPath,
            ReplayDataGenerationPath = replayDataGenerationPath,
            ReplayDataGeneration = replayDataGeneration,
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
        IReadOnlyDictionary<string, ReplayOperation> discoveredByKey,
        IReadOnlyDictionary<string, ReplayOverlay> overlays,
        ICollection<EndpointReplayResult> results,
        ICollection<EndpointReplayPlanItem> finalizedPlanItems,
        ICollection<ReplayDataGenerationOperation> replayDataGenerationOperations,
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
                var replayInputOperation = await GenerateReplayDataAsync(
                        options,
                        discoveredOperation,
                        resolvedOperation,
                        replayDataGenerationOperations,
                        cancellationToken)
                    .ConfigureAwait(false);
                var preparedOperation = await replayHost
                    .PrepareOperationAsync(
                        replayInputOperation,
                        cancellationToken)
                    .ConfigureAwait(false);
                var request = _requestResolver.Resolve(
                    discoveredOperation,
                    replayInputOperation,
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

    private static async Task<ResolvedReplayOperation> GenerateReplayDataAsync(
        ReplayRunnerOptions options,
        ReplayOperation discoveredOperation,
        ResolvedReplayOperation resolvedOperation,
        ICollection<ReplayDataGenerationOperation> replayDataGenerationOperations,
        CancellationToken cancellationToken)
    {
        if (options.ReplayDataAgentOptions.Mode == ReplayDataAgentMode.Off
            || options.ReplayDataGenerator is null)
        {
            return resolvedOperation;
        }

        ReplayDataGenerationOperation preparedData;
        try
        {
            preparedData = await options.ReplayDataGenerator
                .GenerateAsync(
                    new ReplayDataGenerationContext
                    {
                        Operation = discoveredOperation,
                        ResolvedOperation = resolvedOperation,
                        Mode = options.ReplayDataAgentOptions.Mode,
                        ModelName = options.ReplayDataAgentOptions.ModelName,
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            replayDataGenerationOperations.Add(new ReplayDataGenerationOperation
            {
                OperationKey = discoveredOperation.StableOperationKey,
                Strategy = "replay-data-agent",
                Status = "failed",
                Confidence = 0,
                Warnings =
                [
                    exception.Message,
                ],
                SourcesUsed =
                [
                    "replay-data-agent",
                ],
                Notes = "Replay data generation failed before the operation could be replayed.",
            });
            throw;
        }

        replayDataGenerationOperations.Add(preparedData);
        return ApplyPreparedData(resolvedOperation, preparedData.PreparedData);
    }

    private static ResolvedReplayOperation ApplyPreparedData(
        ResolvedReplayOperation resolvedOperation,
        ReplayPreparedData preparedData)
    {
        // Agent-prepared data is backfill only; explicit resolved operation values always win.
        return new ResolvedReplayOperation
        {
            OperationKey = resolvedOperation.OperationKey,
            OperationId = resolvedOperation.OperationId,
            HttpMethod = resolvedOperation.HttpMethod,
            Route = resolvedOperation.Route,
            Persona = resolvedOperation.Persona ?? preparedData.Persona,
            RequestBodyJson = resolvedOperation.RequestBodyJson ?? preparedData.RequestBodyJson,
            PathValues = MergeMissingValues(resolvedOperation.PathValues, preparedData.PathValues),
            QueryValues = MergeMissingValues(resolvedOperation.QueryValues, preparedData.QueryValues),
            HeaderValues = MergeMissingValues(resolvedOperation.HeaderValues, preparedData.HeaderValues),
            Notes = resolvedOperation.Notes,
        };
    }

    private static IReadOnlyDictionary<string, string> MergeMissingValues(
        IReadOnlyDictionary<string, string> primary,
        IReadOnlyDictionary<string, string> fallback)
    {
        Dictionary<string, string> merged = new(primary, StringComparer.OrdinalIgnoreCase);
        foreach ((var key, var value) in fallback)
        {
            if (!merged.ContainsKey(key))
            {
                merged[key] = value;
            }
        }

        return merged;
    }

    private static ReplayDataGenerationReport CreateReplayDataGenerationReport(
        ReplayRunnerOptions options,
        IReadOnlyList<ReplayDataGenerationOperation> operations)
    {
        return new ReplayDataGenerationReport
        {
            AppName = options.AppName,
            Mode = options.ReplayDataAgentOptions.Mode.ToString().ToLowerInvariant(),
            ModelName = options.ReplayDataAgentOptions.ModelName,
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            Operations = operations,
            Warnings = operations
                .SelectMany(static operation => operation.Warnings)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
        };
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
