using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.AspNetCore.Endpoints;
using Sqloom.Core.Execution;
using Sqloom.Testing;

namespace Sqloom.Host;

/// <summary>
/// Runs the Sqloom replay stage against a resolved app harness.
/// </summary>
internal sealed class ReplayCommand
    : ICommandHandler
{
    private readonly ReplayArgumentParser _argumentParser = new();
    private readonly EndpointReplayRunner _runner = new();

    public HostCommandKind CommandKind => HostCommandKind.Replay;

    public async Task<int> ExecuteAsync(CommandExecutionContext context)
    {
        var application = context.Application
            ?? throw new InvalidOperationException(
                "Sqloom replay requires one resolved app harness.");
        var launchOptions = _argumentParser.CreateReplayLaunchOptions(
            context.Arguments,
            context.CurrentDirectory);
        var applicationContext = new SqloomApplicationContext
        {
            CurrentDirectory = context.CurrentDirectory,
            ReplayLaunchOptions = launchOptions,
        };
        var manifest = application.Describe(applicationContext);

        context.ConsoleWriter.PrintBanner(
            manifest.Name,
            HostApplication.GetProjectNames(application));

        await using var session = await application
            .StartAsync(applicationContext)
            .ConfigureAwait(false);
        var arguments = _argumentParser.Parse(
            context.Arguments,
            manifest,
            session.ReplayHost,
            context.CurrentDirectory,
            artifactDirectoryOverride: null);
        arguments.DebugWriter = context.DebugWriter;
        var result = await ExecuteAsync(arguments).ConfigureAwait(false);
        context.ConsoleWriter.PrintReplaySummary(
            result.ReplayResult,
            result.RunReport);
        return result.ExitCode;
    }

    public async Task<ReplayCommandResult> ExecuteAsync(
        ReplayArguments arguments,
        CancellationToken cancellationToken = default)
    {
        arguments.DebugWriter.PrintReplayRun(arguments);
        var replayResult = await _runner
            .RunAsync(arguments.RunnerOptions, cancellationToken)
            .ConfigureAwait(false);
        var runReport = BuildRunReport(replayResult);

        return new ReplayCommandResult
        {
            ReplayResult = replayResult,
            RunReport = runReport,
            ExitCode = replayResult.Results.Any(result =>
                string.Equals(result.Status, "failed", StringComparison.OrdinalIgnoreCase))
                ? 1
                : 0,
        };
    }

    private static RunReport BuildRunReport(EndpointReplayRunResult replayResult)
    {
        return new RunReport
        {
            AppName = replayResult.AppName,
            ArtifactRoot = replayResult.ReplayArtifactDirectory,
            DiscoveredOperationCount = replayResult.DiscoveredOperations.Count,
            PlannedOperationCount = replayResult.ReplayPlan.Operations.Count(item =>
                !string.Equals(item.Status, "skipped", StringComparison.OrdinalIgnoreCase)),
            ReplayBootstrap = replayResult.ReplayBootstrap,
            Pipeline = replayResult.Pipeline,
            Operations = replayResult.ReplayPlan.Operations.Select(planItem =>
            {
                var result = replayResult.Results.FirstOrDefault(item =>
                    string.Equals(item.OperationKey, planItem.OperationKey, StringComparison.OrdinalIgnoreCase));
                return new EndpointOperationResult
                {
                    OperationKey = planItem.OperationKey,
                    HttpMethod = planItem.HttpMethod,
                    Route = planItem.Route,
                    Status = result?.Status ?? planItem.Status,
                    SkipReason = planItem.Reason ?? result?.ErrorMessage,
                    HttpStatusCode = result?.HttpStatusCode,
                    DurationMilliseconds = result?.DurationMilliseconds,
                    CapturedSqlCommandCount = result?.CapturedSqlCommands.Count ?? 0,
                    ArtifactPaths = result is null
                        ? Array.Empty<string>()
                        : [result.ArtifactPath],
                };
            }).ToArray(),
        };
    }
}

/// <summary>
/// Carries the result of the Sqloom replay command.
/// </summary>
internal sealed class ReplayCommandResult
{
    public required EndpointReplayRunResult ReplayResult { get; init; }

    public required RunReport RunReport { get; init; }

    public int ExitCode { get; init; }
}
