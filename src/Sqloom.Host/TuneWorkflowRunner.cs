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
        HostDebugWriter.PrintTuneRun(arguments.DebugEnabled, arguments);

        ReplayCommand replayCommand = new();
        HostDebugWriter.PrintTuneStageStarting(arguments.DebugEnabled, "replay");
        var replayResult = await replayCommand
            .ExecuteAsync(arguments.ReplayArguments, cancellationToken)
            .ConfigureAwait(false);
        HostDebugWriter.PrintTuneStageCompleted(arguments.DebugEnabled, "replay", replayResult.ReplayResult.SummaryArtifactPath);

        ObserveCommand observeCommand = new();
        HostDebugWriter.PrintTuneStageStarting(arguments.DebugEnabled, "observe");
        var observeResult = await observeCommand
            .ExecuteAsync(arguments.ObserveArguments, cancellationToken)
            .ConfigureAwait(false);
        HostDebugWriter.PrintTuneStageCompleted(arguments.DebugEnabled, "observe", observeResult.JsonOutputPath);

        CorrelateCommand correlateCommand = new();
        HostDebugWriter.PrintTuneStageStarting(arguments.DebugEnabled, "correlate");
        var correlateResult = await correlateCommand
            .ExecuteAsync(
                arguments.CorrelateArguments,
                observeResult.Snapshot,
                replayResult.ReplayResult,
                cancellationToken)
            .ConfigureAwait(false);
        HostDebugWriter.PrintTuneStageCompleted(arguments.DebugEnabled, "correlate", correlateResult.JsonOutputPath);

        AdviceCommand adviceCommand = new();
        HostDebugWriter.PrintTuneStageStarting(arguments.DebugEnabled, "advise");
        var adviceResult = await adviceCommand
            .ExecuteAsync(
                arguments.AdviseArguments,
                correlateResult.Report,
                cancellationToken)
            .ConfigureAwait(false);
        HostDebugWriter.PrintTuneStageCompleted(arguments.DebugEnabled, "advise", adviceResult.JsonOutputPath);

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
