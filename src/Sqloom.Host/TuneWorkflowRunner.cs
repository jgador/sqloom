using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Pipeline.Artifacts;
using Sqloom.Pipeline.Execution;
using Sqloom.Pipeline.QueryStore;

namespace Sqloom.Host;

/// <summary>
/// Runs the observe, replay, correlate, and advise stages as one typed workflow.
/// </summary>
internal static class TuneWorkflowRunner
{
    public static async Task<(TuneWorkflowReport Report, string SummaryOutputPath, int ExitCode)> RunAsync(
        TuneArguments arguments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        arguments.DebugWriter.PrintTuneRun(arguments);

        ReplayCommand replayCommand = new();
        arguments.DebugWriter.PrintTuneStageStarting("replay");
        var replayResult = await replayCommand
            .ExecuteAsync(arguments.ReplayArguments, cancellationToken)
            .ConfigureAwait(false);
        arguments.DebugWriter.PrintTuneStageCompleted("replay", replayResult.ReplayResult.SummaryArtifactPath);

        ObserveCommand observeCommand = new();
        arguments.DebugWriter.PrintTuneStageStarting("observe");
        var observeResult = await observeCommand
            .ExecuteAsync(arguments.ObserveArguments, cancellationToken)
            .ConfigureAwait(false);
        arguments.DebugWriter.PrintTuneStageCompleted("observe", observeResult.JsonOutputPath);

        CorrelateCommand correlateCommand = new();
        arguments.DebugWriter.PrintTuneStageStarting("correlate");
        var correlateResult = await correlateCommand
            .ExecuteAsync(
                arguments.CorrelateArguments,
                observeResult.Snapshot,
                replayResult.ReplayResult,
                cancellationToken)
            .ConfigureAwait(false);
        arguments.DebugWriter.PrintTuneStageCompleted("correlate", correlateResult.JsonOutputPath);

        AdviceCommand adviceCommand = new();
        arguments.DebugWriter.PrintTuneStageStarting("advise");
        var adviceResult = await adviceCommand
            .ExecuteAsync(
                arguments.AdviseArguments,
                correlateResult.Report,
                cancellationToken)
            .ConfigureAwait(false);
        arguments.DebugWriter.PrintTuneStageCompleted("advise", adviceResult.JsonOutputPath);

        var report = CreateReport(
            arguments,
            observeResult,
            replayResult,
            correlateResult,
            adviceResult);
        var summaryOutputPath = ArtifactLayout.GetTuneSummaryPath(arguments.WorkflowArtifactDir);
        await JsonFileWriter.WriteAsync(
                summaryOutputPath,
                report,
                cancellationToken)
            .ConfigureAwait(false);

        return (report, summaryOutputPath, replayResult.ExitCode);
    }

    private static TuneWorkflowReport CreateReport(
        TuneArguments arguments,
        (QueryStoreSnapshot Snapshot, string JsonOutputPath) observeResult,
        (EndpointReplayRunResult ReplayResult, int ExitCode) replayResult,
        (QueryCorrelationReport Report, string JsonOutputPath) correlateResult,
        (AdviceReport Report, string JsonOutputPath) adviceResult)
    {
        HashSet<string> warnings = new(StringComparer.Ordinal);
        if (observeResult.Snapshot.DiscoveredObjectCatalog is { } discoveredObjectCatalog)
        {
            foreach (var warning in discoveredObjectCatalog.Warnings)
            {
                warnings.Add(warning);
            }
        }

        foreach (var warning in correlateResult.Report.Warnings)
        {
            warnings.Add(warning);
        }

        foreach (var warning in adviceResult.Report.Warnings)
        {
            warnings.Add(warning);
        }

        return new TuneWorkflowReport
        {
            GeneratedAtUtc = adviceResult.Report.GeneratedAtUtc,
            AppName = adviceResult.Report.AppName,
            WorkflowArtifactDir = arguments.WorkflowArtifactDir,
            QueryStoreSnapshotPath = observeResult.JsonOutputPath,
            ReplayArtifactDir = replayResult.ReplayResult.ReplayArtifactDir,
            ReplayDataGenerationPath = replayResult.ReplayResult.ReplayDataGenerationPath,
            QueryStoreCorrelationPath = correlateResult.JsonOutputPath,
            TuningAdvicePath = adviceResult.JsonOutputPath,
            SqlProposalJsonPath = adviceResult.Report.SqlProposalJsonPath,
            SqlProposalScriptPath = adviceResult.Report.SqlProposalScriptPath,
            ModelProvider = adviceResult.Report.ModelProvider,
            ModelName = adviceResult.Report.ModelName,
            Pipeline = adviceResult.Report.Pipeline,
            Summary = new TuneWorkflowSummary
            {
                QueryStorePlanCount = observeResult.Snapshot.Plans.Count,
                QueryStoreWaitCount = observeResult.Snapshot.Waits.Count,
                ReplayOperationCount = replayResult.ReplayResult.ReplayPlan.Operations.Count,
                ReplayedOperationCount = replayResult.ReplayResult.Results.Count(result =>
                    string.Equals(result.Status, "replayed", StringComparison.OrdinalIgnoreCase)),
                FailedOperationCount = replayResult.ReplayResult.Results.Count(result =>
                    string.Equals(result.Status, "failed", StringComparison.OrdinalIgnoreCase)),
                CapturedCommandCount = correlateResult.Report.Summary.CapturedCommandCount,
                MatchedCommandCount = correlateResult.Report.Summary.MatchedCommandCount,
                RecommendationCount = adviceResult.Report.Summary.RecommendationCount,
                ProposalCount = adviceResult.Report.Summary.ProposalCount,
            },
            Warnings = warnings.ToArray(),
        };
    }
}
