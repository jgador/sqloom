using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Host.Replay;
using Sqloom.Core.QueryStore;
using Sqloom.Host.QueryStore;
using Sqloom.Core.Artifacts;
using Sqloom.Core.Execution;

namespace Sqloom.Host;

/// <summary>
/// Runs the Sqloom correlate stage against replay artifacts and Query Store.
/// </summary>
internal sealed class CorrelateCommand
    : ICommandHandler
{
    private readonly CorrelateArgumentParser _argumentParser = new();

    public HostCommandKind CommandKind => HostCommandKind.Correlate;

    public async Task<int> ExecuteAsync(CommandExecutionContext context)
    {
        context.ConsoleWriter.PrintBanner(
            null,
            HostApplication.GetProjectNames(context.Application));

        var readOnlyConnectionString = _argumentParser.GetQueryStoreConnectionString(context.Arguments);
        if (string.IsNullOrWhiteSpace(readOnlyConnectionString))
        {
            Console.Error.WriteLine(
                "Query Store correlation requires --read-only-connection-string.");
            return 1;
        }

        var arguments = _argumentParser.Parse(
            context.Arguments,
            readOnlyConnectionString);
        arguments.DebugWriter = context.DebugWriter;
        var result = await ExecuteAsync(arguments).ConfigureAwait(false);
        context.ConsoleWriter.PrintCorrelationSummary(
            result.Report,
            result.JsonOutputPath);
        return 0;
    }

    public async Task<CorrelateCommandResult> ExecuteAsync(
        CorrelateArguments arguments,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await JsonFileReader
            .ReadAsync<QueryStoreSnapshot>(
                arguments.QueryStoreSnapshotPath,
                static serializerOptions => serializerOptions.Converters.Add(new JsonStringEnumConverter()),
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Could not deserialize Query Store snapshot at '{arguments.QueryStoreSnapshotPath}'.");

        var replaySummaryPath = ArtifactLayout.GetReplaySummaryPath(arguments.ReplayArtifactDir);
        var replayRunResult = await JsonFileReader
            .ReadAsync<EndpointReplayRunResult>(
                replaySummaryPath,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Could not deserialize replay summary at '{replaySummaryPath}'.");

        return await ExecuteAsync(
                arguments,
                snapshot,
                replayRunResult,
                cancellationToken)
            .ConfigureAwait(false);
    }

    internal async Task<CorrelateCommandResult> ExecuteAsync(
        CorrelateArguments arguments,
        QueryStoreSnapshot snapshot,
        EndpointReplayRunResult replayRunResult,
        CancellationToken cancellationToken = default)
    {
        QueryStoreCorrelator correlator = new(new SqlStatementHandleResolver(arguments.ConnectionString));
        var rawReport = await correlator
            .CorrelateAsync(
                snapshot,
                replayRunResult.Results,
                arguments.ReplayArtifactDir,
                replayRunResult.AppName,
                arguments.QueryStoreSnapshotPath,
                cancellationToken)
            .ConfigureAwait(false);
        var report = CreateFinalReport(
            rawReport,
            replayRunResult.AppName,
            arguments.JsonOutputPath);
        arguments.DebugWriter.PrintCorrelationRun(arguments, report);

        await JsonFileWriter.WriteAsync(
                arguments.JsonOutputPath,
                report,
                static serializerOptions => serializerOptions.Converters.Add(new JsonStringEnumConverter()),
                cancellationToken)
            .ConfigureAwait(false);

        return new CorrelateCommandResult
        {
            Report = report,
            JsonOutputPath = arguments.JsonOutputPath,
        };
    }

    private static QueryCorrelationReport CreateFinalReport(
        QueryCorrelationReport rawReport,
        string appName,
        string jsonOutputPath)
    {
        var replaySummaryPath = ArtifactLayout.GetReplaySummaryPath(rawReport.ReplayArtifactDir);
        var adviceArtifactPath = ArtifactLayout.GetReplayTuningAdvicePath(rawReport.ReplayArtifactDir);

        return new QueryCorrelationReport
        {
            GeneratedAtUtc = rawReport.GeneratedAtUtc,
            AppName = appName,
            ReplayArtifactDir = rawReport.ReplayArtifactDir,
            QueryStoreSnapshotPath = rawReport.QueryStoreSnapshotPath,
            QueryStoreCapturedAtUtc = rawReport.QueryStoreCapturedAtUtc,
            Records = rawReport.Records,
            Summary = rawReport.Summary,
            Pipeline = new PipelineReport
            {
                Stages =
                [
                    new PipelineStageReport
                    {
                        Name = PipelineStageNames.Observe,
                        Status = PipelineStageStatuses.Completed,
                        Summary = "Captured a Query Store snapshot for correlation.",
                        ArtifactPath = rawReport.QueryStoreSnapshotPath,
                    },
                    new PipelineStageReport
                    {
                        Name = PipelineStageNames.Replay,
                        Status = PipelineStageStatuses.Completed,
                        Summary = "Replay artifacts were available for correlation.",
                        ArtifactPath = replaySummaryPath,
                    },
                    new PipelineStageReport
                    {
                        Name = PipelineStageNames.Capture,
                        Status = PipelineStageStatuses.Completed,
                        Summary = "Captured replay SQL was available for correlation.",
                        ArtifactPath = rawReport.ReplayArtifactDir,
                    },
                    new PipelineStageReport
                    {
                        Name = PipelineStageNames.Correlate,
                        Status = PipelineStageStatuses.Completed,
                        Summary = "Captured SQL was mapped back to Query Store rows.",
                        ArtifactPath = jsonOutputPath,
                    },
                    new PipelineStageReport
                    {
                        Name = PipelineStageNames.Advise,
                        Status = PipelineStageStatuses.Available,
                        Summary = "Run --advise to turn the correlation artifact into operation-level tuning guidance.",
                        ArtifactPath = adviceArtifactPath,
                    },
                ],
            },
            Warnings = rawReport.Warnings,
        };
    }
}

/// <summary>
/// Carries the result of the Sqloom correlate command.
/// </summary>
internal sealed class CorrelateCommandResult
{
    public required QueryCorrelationReport Report { get; init; }

    public required string JsonOutputPath { get; init; }
}
