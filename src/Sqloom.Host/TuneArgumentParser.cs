using System;
using System.Collections.Generic;
using System.IO;
using Sqloom.Core.Artifacts;
using Sqloom.Core.Execution;
using Sqloom.Testing;

namespace Sqloom.Host;

/// <summary>
/// Parses and validates the Sqloom tune workflow arguments.
/// </summary>
internal sealed class TuneArgumentParser
{
    private static readonly HashSet<string> SupportedSwitches = new(StringComparer.OrdinalIgnoreCase)
    {
        "--read-only-connection-string",
        "--lookback-hours",
        "--max-plans",
        "--max-waits",
        "--command-timeout-seconds",
        "--app-only",
        "--show-classification",
        "--openapi-file",
        "--sqlserver-dacpac-file",
        "--sqlserver-seed-sql-file",
        "--artifact-dir",
        "--max-operations",
        "--target",
        "--model-provider",
        "--sqlserver-schema-file",
        "--openai-model",
        "--openai-base-url",
        "--openai-api-key",
    };

    private static readonly HashSet<string> ValueSwitches = new(StringComparer.OrdinalIgnoreCase)
    {
        "--read-only-connection-string",
        "--lookback-hours",
        "--max-plans",
        "--max-waits",
        "--command-timeout-seconds",
        "--openapi-file",
        "--sqlserver-dacpac-file",
        "--sqlserver-seed-sql-file",
        "--artifact-dir",
        "--max-operations",
        "--target",
        "--model-provider",
        "--sqlserver-schema-file",
        "--openai-model",
        "--openai-base-url",
        "--openai-api-key",
    };

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
        "--openapi-file",
        "--sqlserver-dacpac-file",
        "--sqlserver-seed-sql-file",
        "--max-operations",
        "--target",
    };

    private static readonly HashSet<string> AdviceSwitches = new(StringComparer.OrdinalIgnoreCase)
    {
        "--model-provider",
        "--sqlserver-schema-file",
        "--openai-model",
        "--openai-base-url",
        "--openai-api-key",
    };

    private readonly ObserveArgumentParser _observeArgumentParser = new();
    private readonly ReplayArgumentParser _replayArgumentParser = new();
    private readonly AdviseArgumentParser _adviseArgumentParser = new();

    public string? GetQueryStoreConnectionString(string[] args)
    {
        return CommandArgumentSupport.GetArgumentValue(args, "--read-only-connection-string");
    }

    public ReplayLaunchOptions CreateReplayLaunchOptions(
        string[] args,
        string currentDirectory)
    {
        return _replayArgumentParser.CreateReplayLaunchOptions(
            ExtractSwitchArguments(args, ReplaySwitches),
            currentDirectory);
    }

    public void ValidateBeforeSession(
        string[] args,
        SqloomApplicationManifest manifest,
        string currentDirectory)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        CommandArgumentSupport.ValidateArguments(
            args,
            HostCommandKind.Tune,
            SupportedSwitches,
            ValueSwitches);

        var validationPath = Path.Combine(
            currentDirectory,
            "sqloom-validation-placeholder.json");
        _adviseArgumentParser.CreateArguments(
            ExtractSwitchArguments(args, AdviceSwitches),
            currentDirectory,
            validationPath,
            validationPath,
            manifest.SqlServerSchemaPath);
    }

    public TuneArguments Parse(
        string[] args,
        SqloomApplicationManifest manifest,
        IReplayHost replayHost,
        string readOnlyConnectionString,
        string currentDirectory)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(replayHost);

        CommandArgumentSupport.ValidateArguments(
            args,
            HostCommandKind.Tune,
            SupportedSwitches,
            ValueSwitches);

        var workflowArtifactDirectory = GetWorkflowArtifactDirectory(args, currentDirectory);
        var snapshotPath = ArtifactLayout.GetTuneQueryStoreSnapshotPath(workflowArtifactDirectory);
        var replayArtifactDirectory = ArtifactLayout.GetTuneReplayArtifactDirectory(workflowArtifactDirectory);
        var correlationPath = ArtifactLayout.GetReplayQueryStoreCorrelationPath(replayArtifactDirectory);
        var advicePath = ArtifactLayout.GetReplayTuningAdvicePath(replayArtifactDirectory);

        var observeArguments = _observeArgumentParser.Parse(
            AddSwitchValue(
                ExtractSwitchArguments(args, ObserveSwitches),
                "--json-output-file",
                snapshotPath),
            manifest,
            readOnlyConnectionString,
            currentDirectory);
        var replayArguments = _replayArgumentParser.Parse(
            ExtractSwitchArguments(args, ReplaySwitches),
            manifest,
            replayHost,
            currentDirectory,
            replayArtifactDirectory);
        var adviseArguments = _adviseArgumentParser.CreateArguments(
            ExtractSwitchArguments(args, AdviceSwitches),
            replayArtifactDirectory,
            correlationPath,
            advicePath,
            manifest.SqlServerSchemaPath);

        return new TuneArguments
        {
            WorkflowArtifactDirectory = workflowArtifactDirectory,
            ObserveArguments = observeArguments,
            ReplayArguments = replayArguments,
            CorrelateArguments = new CorrelateArguments
            {
                ConnectionString = readOnlyConnectionString,
                QueryStoreSnapshotPath = snapshotPath,
                ReplayArtifactDirectory = replayArtifactDirectory,
                JsonOutputPath = correlationPath,
            },
            AdviseArguments = adviseArguments,
        };
    }

    internal string GetWorkflowArtifactDirectory(string[] args, string currentDirectory)
    {
        var artifactDirectory = CommandArgumentSupport.GetArgumentValue(args, "--artifact-dir");
        if (!string.IsNullOrWhiteSpace(artifactDirectory))
        {
            return Path.GetFullPath(
                artifactDirectory,
                currentDirectory);
        }

        var artifactRoot = ArtifactRootResolver.Resolve(currentDirectory);
        return ArtifactLayout.GetDefaultTuneArtifactDirectory(
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

            var hasValue = ValueSwitches.Contains(argument);
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
