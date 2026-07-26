using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sqloom.Pipeline.Artifacts;
using Sqloom.Pipeline.Execution;
using Sqloom.Testing;

namespace Sqloom.Host;

/// <summary>
/// Parses and validates the Sqloom tune workflow arguments.
/// </summary>
internal static class TuneArgumentParser
{
    private static readonly HashSet<string> ObserveSwitches = new(StringComparer.OrdinalIgnoreCase)
    {
        "--lookback-hours",
        "--max-plans",
        "--max-waits",
        "--command-timeout-seconds",
        "--app-only",
        "--show-classification",
    };

    private static readonly HashSet<string> ReplaySwitches = new(StringComparer.OrdinalIgnoreCase)
    {
        "--app-project",
        "--sqlserver-dacpac-file",
        "--sqlserver-seed-sql-file",
        "--max-operations",
        "--target",
        "--replay-data-agent",
        "--replay-data-agent-model",
        "--openai-base-url",
        "--openai-api-key",
    };

    private static readonly HashSet<string> AdviceSwitches = new(StringComparer.OrdinalIgnoreCase)
    {
        "--model-provider",
        "--sqlserver-schema-file",
        "--sqlserver-dacpac-file",
        "--openai-model",
        "--openai-base-url",
        "--openai-api-key",
    };

    public static string? GetQueryStoreConnectionString(string[] args)
    {
        return CommandArgumentSupport.GetArgumentValue(args, "--read-only-connection-string");
    }

    public static ReplayLaunchOptions CreateReplayLaunchOptions(
        string[] args,
        string currentDirectory)
    {
        return ReplayArgumentParser.CreateReplayLaunchOptions(
            ExtractSwitchArguments(args, ReplaySwitches),
            currentDirectory,
            requireDacpacForSeed: false);
    }

    public static void ValidateBeforeSession(
        string[] args,
        SqloomApplicationManifest manifest,
        string currentDirectory)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        // Validate tune sub-arguments before replay/observe artifacts exist; placeholder paths stand in.
        CommandArgumentSupport.ValidateArguments(args, HostCommandKind.Tune);

        var validationPath = Path.Combine(
            currentDirectory,
            "sqloom-validation-placeholder.json");
        ReplayArgumentParser.ValidateReplayDataAgentOptions(
            ExtractSwitchArguments(args, ReplaySwitches));
        AdviseArgumentParser.CreateArguments(
            ExtractSwitchArguments(args, AdviceSwitches),
            currentDirectory,
            validationPath,
            validationPath,
            defaultDacpacPath: manifest.SqlServerDacpacPath,
            currentDirectory: currentDirectory,
            defaultReadOnlyConnectionString: GetQueryStoreConnectionString(args),
            allowMissingSchemaSource: true);
    }

    public static string? GetAppProjectPath(
        string[] args,
        string currentDirectory)
    {
        return ReplayArgumentParser.GetAppProjectPath(
            ExtractSwitchArguments(args, ReplaySwitches),
            currentDirectory);
    }

    public static TuneArguments Parse(
        string[] args,
        SqloomApplicationManifest manifest,
        IReplayHost replayHost,
        string readOnlyConnectionString,
        string currentDirectory,
        string? sourceProjectPathOverride = null,
        string? workflowArtifactDirOverride = null,
        ReplayLaunchOptions? replayLaunchOptionsOverride = null,
        string? adviceDacpacPathOverride = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(replayHost);

        CommandArgumentSupport.ValidateArguments(args, HostCommandKind.Tune);

        var workflowArtifactDir = workflowArtifactDirOverride
            ?? GetWorkflowArtifactDir(args, currentDirectory);
        var snapshotPath = ArtifactLayout.GetTuneQueryStoreSnapshotPath(workflowArtifactDir);
        var replayArtifactDirectory = ArtifactLayout.GetTuneReplayArtifactDir(workflowArtifactDir);
        var correlationPath = ArtifactLayout.GetCorrelationPath(replayArtifactDirectory);
        var advicePath = ArtifactLayout.GetReplayTuningAdvicePath(replayArtifactDirectory);

        var observeArguments = ObserveArgumentParser.Parse(
            AddSwitchValue(
                ExtractSwitchArguments(args, ObserveSwitches),
                "--json-output-file",
                snapshotPath),
            manifest,
            readOnlyConnectionString,
            currentDirectory);
        var replayArguments = ReplayArgumentParser.Parse(
            ExtractSwitchArguments(args, ReplaySwitches),
            manifest,
            replayHost,
            currentDirectory,
            replayArtifactDirectory,
            sourceProjectPathOverride,
            replayLaunchOptionsOverride);
        var adviseArguments = AdviseArgumentParser.CreateArguments(
            ExtractSwitchArguments(args, AdviceSwitches),
            replayArtifactDirectory,
            correlationPath,
            advicePath,
            defaultDacpacPath: adviceDacpacPathOverride
                ?? manifest.SqlServerDacpacPath,
            currentDirectory: currentDirectory,
            defaultReadOnlyConnectionString: readOnlyConnectionString);

        return new TuneArguments
        {
            WorkflowArtifactDir = workflowArtifactDir,
            ObserveArguments = observeArguments,
            ReplayArguments = replayArguments,
            CorrelateArguments = new CorrelateArguments
            {
                ConnectionString = readOnlyConnectionString,
                QueryStoreSnapshotPath = snapshotPath,
                ReplayArtifactDir = replayArtifactDirectory,
                JsonOutputPath = correlationPath,
            },
            AdviseArguments = adviseArguments,
        };
    }

    internal static string GetWorkflowArtifactDir(string[] args, string currentDirectory)
    {
        var artifactDirectory = CommandArgumentSupport.GetArgumentValue(args, "--artifact-dir");
        if (!string.IsNullOrWhiteSpace(artifactDirectory))
        {
            return Path.GetFullPath(
                artifactDirectory,
                currentDirectory);
        }

        var artifactRoot = DefaultArtifactRootLocator.GetPath(currentDirectory);
        return ArtifactLayout.GetTuneArtifactDir(
            artifactRoot,
            DateTimeOffset.UtcNow);
    }

    private static string[] AddSwitchValue(
        string[] args,
        string switchName,
        string value)
    {
        List<string> updated = [.. args, switchName, value];
        return [.. updated];
    }

    private static string[] ExtractSwitchArguments(
        string[] args,
        ISet<string> includedSwitches)
    {
        List<string> extracted = [];

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (index == 0 && string.Equals(argument, "tune", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!CommandArgumentSupport.IsSwitch(argument))
            {
                continue;
            }

            var hasValue = CommandCatalog.GetRequired(HostCommandKind.Tune).Options
                .Any(option => option.TakesValue
                    && string.Equals(option.Name, argument, StringComparison.OrdinalIgnoreCase));
            if (!includedSwitches.Contains(argument))
            {
                if (hasValue)
                {
                    index++;
                }

                continue;
            }

            extracted.Add(argument);
            if (hasValue)
            {
                extracted.Add(args[++index]);
            }
        }

        return [.. extracted];
    }
}
