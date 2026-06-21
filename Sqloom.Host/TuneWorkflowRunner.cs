using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Core.Artifacts;

namespace Sqloom.Host;

/// <summary>
/// Runs the observe, replay, correlate, and advise stages as one typed workflow.
/// </summary>
internal sealed class TuneWorkflowRunner
{
    private readonly AdviceCommand _adviceCommand;
    private readonly CorrelateCommand _correlateCommand;
    private readonly ObserveCommand _observeCommand;
    private readonly ReplayCommand _replayCommand;

    public TuneWorkflowRunner()
        : this(
            new ObserveCommand(),
            new ReplayCommand(),
            new CorrelateCommand(),
            new AdviceCommand())
    {
    }

    internal TuneWorkflowRunner(
        ObserveCommand observeCommand,
        ReplayCommand replayCommand,
        CorrelateCommand correlateCommand,
        AdviceCommand adviceCommand)
    {
        _observeCommand = observeCommand;
        _replayCommand = replayCommand;
        _correlateCommand = correlateCommand;
        _adviceCommand = adviceCommand;
    }

    public async Task<TuneWorkflowResult> RunAsync(
        TuneArguments arguments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        arguments.DebugWriter.PrintTuneRun(arguments);

        arguments.DebugWriter.PrintTuneStageStarting("observe");
        var observeResult = await _observeCommand
            .ExecuteAsync(arguments.ObserveArguments, cancellationToken)
            .ConfigureAwait(false);
        arguments.DebugWriter.PrintTuneStageCompleted("observe", observeResult.JsonOutputPath);
        arguments.DebugWriter.PrintTuneStageStarting("replay");
        var replayResult = await _replayCommand
            .ExecuteAsync(arguments.ReplayArguments, cancellationToken)
            .ConfigureAwait(false);
        arguments.DebugWriter.PrintTuneStageCompleted("replay", replayResult.ReplayResult.SummaryArtifactPath);
        arguments.DebugWriter.PrintTuneStageStarting("correlate");
        var correlateResult = await _correlateCommand
            .ExecuteAsync(
                arguments.CorrelateArguments,
                observeResult.Snapshot,
                replayResult.ReplayResult,
                cancellationToken)
            .ConfigureAwait(false);
        arguments.DebugWriter.PrintTuneStageCompleted("correlate", correlateResult.JsonOutputPath);
        arguments.DebugWriter.PrintTuneStageStarting("advise");
        var adviceResult = await _adviceCommand
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
        var summaryOutputPath = ArtifactLayout.GetTuneSummaryPath(arguments.WorkflowArtifactDirectory);
        await JsonFileWriter.WriteAsync(
                summaryOutputPath,
                report,
                cancellationToken)
            .ConfigureAwait(false);

        return new TuneWorkflowResult
        {
            ObserveResult = observeResult,
            ReplayResult = replayResult,
            CorrelateResult = correlateResult,
            AdviceResult = adviceResult,
            Report = report,
            SummaryOutputPath = summaryOutputPath,
            ExitCode = replayResult.ExitCode,
        };
    }

    private static TuneWorkflowReport CreateReport(
        TuneArguments arguments,
        ObserveCommandResult observeResult,
        ReplayCommandResult replayResult,
        CorrelateCommandResult correlateResult,
        AdviceCommandResult adviceResult)
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
            WorkflowArtifactDirectory = arguments.WorkflowArtifactDirectory,
            QueryStoreSnapshotPath = observeResult.JsonOutputPath,
            ReplayArtifactDirectory = replayResult.ReplayResult.ReplayArtifactDirectory,
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
