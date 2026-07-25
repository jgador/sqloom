using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Host.Replay;
using Sqloom.Pipeline.Execution;
using Sqloom.Testing;

namespace Sqloom.Host;

/// <summary>
/// Runs the Sqloom replay stage against a resolved app harness.
/// </summary>
internal sealed class ReplayCommand
    : ICommandHandler
{
    public HostCommandKind CommandKind => HostCommandKind.Replay;

    public async Task<int> ExecuteAsync(CommandExecutionContext context)
    {
        var application = context.Application
            ?? throw new InvalidOperationException(
                "Sqloom replay requires one resolved app harness.");
        var launchOptions = ReplayArgumentParser.CreateReplayLaunchOptions(
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

        var replayArtifactDirectory = ReplayArgumentParser.GetReplayArtifactDir(
            context.Arguments,
            context.CurrentDirectory);
        var sourceProjectPath = EndpointSourceProjectResolver.Resolve(
            context.Arguments,
            context.StartupOptions,
            context.CurrentDirectory);

        await using var session = await application
            .StartAsync(applicationContext)
            .ConfigureAwait(false);
        var arguments = ReplayArgumentParser.Parse(
            context.Arguments,
            manifest,
            session.ReplayHost,
            context.CurrentDirectory,
            artifactDirectoryOverride: replayArtifactDirectory,
            sourceProjectPathOverride: sourceProjectPath);
        arguments.DebugWriter = context.DebugWriter;
        var result = await ExecuteAsync(arguments).ConfigureAwait(false);
        context.ConsoleWriter.PrintReplaySummary(
            result.ReplayResult,
            BuildRunReport(result.ReplayResult));
        return result.ExitCode;
    }

    internal async Task<(EndpointReplayRunResult ReplayResult, int ExitCode)> ExecuteAsync(
        ReplayArguments arguments,
        CancellationToken cancellationToken = default)
    {
        arguments.DebugWriter.PrintReplayRun(arguments);
        var replayResult = await EndpointReplayRunner
            .RunAsync(arguments.RunnerOptions, cancellationToken)
            .ConfigureAwait(false);
        return (
            replayResult,
            replayResult.Results.Any(result =>
                string.Equals(result.Status, "failed", StringComparison.OrdinalIgnoreCase))
                ? 1
                : 0);
    }

    private static RunReport BuildRunReport(EndpointReplayRunResult replayResult)
    {
        return new RunReport
        {
            AppName = replayResult.AppName,
            ArtifactRoot = replayResult.ReplayArtifactDir,
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
