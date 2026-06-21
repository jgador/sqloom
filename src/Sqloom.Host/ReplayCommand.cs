using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.AspNetCore.Endpoints;
using Sqloom.Core.Contracts;
using Sqloom.Core.Execution;

namespace Sqloom.Host;

/// <summary>
/// Runs the Sqloom replay stage against resolved app integrations.
/// </summary>
internal sealed class ReplayCommand
    : ICommandHandler
{
    private readonly ReplayArgumentParser _argumentParser = new();
    private readonly EndpointReplayRunner _runner = new();

    public HostCommandKind CommandKind => HostCommandKind.Replay;

    public async Task<int> ExecuteAsync(CommandExecutionContext context)
    {
        IReadOnlyList<IAppIntegration> replayIntegrations = context.AppIntegrations;
        if (replayIntegrations.Count == 0)
        {
            throw new InvalidOperationException(
                "Sqloom replay requires one or more resolved app integrations.");
        }

        var replayArtifactDirectoryRoot = replayIntegrations.Count > 1
            ? _argumentParser.GetReplayArtifactDirectory(context.Arguments, context.CurrentDirectory)
            : null;
        var exitCode = 0;

        for (var replayIndex = 0; replayIndex < replayIntegrations.Count; replayIndex++)
        {
            if (replayIndex > 0)
            {
                Console.WriteLine();
            }

            var appIntegration = replayIntegrations[replayIndex];
            context.ConsoleWriter.PrintBanner(
                appIntegration.AppName,
                HostApplication.GetProjectNames(appIntegration));

            var arguments = _argumentParser.Parse(
                context.Arguments,
                appIntegration,
                context.CurrentDirectory,
                BuildReplayArtifactDirectoryOverride(
                    replayArtifactDirectoryRoot,
                    appIntegration,
                    replayIndex,
                    replayIntegrations.Count));
            arguments.DebugWriter = context.DebugWriter;
            var result = await ExecuteAsync(arguments).ConfigureAwait(false);
            context.ConsoleWriter.PrintReplaySummary(
                result.ReplayResult,
                result.RunReport);
            exitCode = Math.Max(exitCode, result.ExitCode);
        }

        return exitCode;
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

    private static string? BuildReplayArtifactDirectoryOverride(
        string? replayArtifactDirectoryRoot,
        IAppIntegration appIntegration,
        int replayIndex,
        int replayCount)
    {
        if (string.IsNullOrWhiteSpace(replayArtifactDirectoryRoot))
        {
            return null;
        }

        if (replayCount == 1)
        {
            return replayArtifactDirectoryRoot;
        }

        var assemblyName = appIntegration.GetType().Assembly.GetName().Name
            ?? appIntegration.AppName;
        return System.IO.Path.Combine(
            replayArtifactDirectoryRoot,
            $"{replayIndex + 1:D2}-{SanitizePathSegment(assemblyName)}");
    }

    private static string SanitizePathSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "sqloom-app";
        }

        var invalidCharacters = System.IO.Path.GetInvalidFileNameChars();
        var buffer = value.ToCharArray();
        for (var index = 0; index < buffer.Length; index++)
        {
            var character = buffer[index];
            if (char.IsWhiteSpace(character)
                || character == '/'
                || character == '\\'
                || character == ':')
            {
                buffer[index] = '-';
                continue;
            }

            if (Array.IndexOf(invalidCharacters, character) >= 0)
            {
                buffer[index] = '_';
            }
        }

        return new string(buffer).Trim('-');
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
