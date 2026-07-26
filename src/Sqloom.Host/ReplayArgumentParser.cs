using System;
using System.Collections.Generic;
using System.IO;
using Sqloom.Host.Replay;
using Sqloom.Pipeline.Artifacts;
using Sqloom.Pipeline.Execution;
using Sqloom.Testing;

namespace Sqloom.Host;

/// <summary>
/// Parses and validates the Sqloom replay command arguments.
/// </summary>
internal static class ReplayArgumentParser
{
    public static ReplayArguments Parse(
        string[] args,
        SqloomApplicationManifest manifest,
        IReplayHost replayHost,
        string currentDirectory,
        string? artifactDirectoryOverride = null,
        string? sourceProjectPathOverride = null,
        ReplayLaunchOptions? replayLaunchOptionsOverride = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(replayHost);

        CommandArgumentSupport.ValidateArguments(args, HostCommandKind.Replay);

        var replayProfile = manifest.ReplayProfile;
        var sourceProjectPath = sourceProjectPathOverride
            ?? GetAppProjectPath(args, currentDirectory)
            ?? throw new ArgumentException(
                "Replay requires an ASP.NET Core source project. Supply --app-project <path> or use a file-based harness with exactly one Web SDK #:project directive.");
        var targetFilter = ReplayTargetSyntax.ValidateOperationKeyOrNull(
            CommandArgumentSupport.GetArgumentValue(args, "--target"));
        var replayArtifactDirectory = artifactDirectoryOverride
            ?? GetReplayArtifactDir(args, currentDirectory);
        var replayLaunchOptions = replayLaunchOptionsOverride
            ?? CreateReplayLaunchOptions(args, currentDirectory);
        var replayDataAgentOptions = CreateReplayDataAgentOptions(args);

        return new ReplayArguments
        {
            RunnerOptions = new ReplayRunnerOptions
            {
                AppName = manifest.Name,
                SourceProjectPath = sourceProjectPath,
                ReplayArtifactDir = replayArtifactDirectory,
                ReplayProfile = replayProfile,
                ReplayHost = replayHost,
                ReplayLaunchOptions = replayLaunchOptions,
                ReplayDataAgentOptions = replayDataAgentOptions,
                ReplayDataGenerator = CreateReplayDataGenerator(args, replayDataAgentOptions),
                MaxOperations = CommandArgumentSupport.GetIntArgumentValue(args, "--max-operations") ?? 25,
                TargetFilter = targetFilter,
            },
        };
    }

    public static string? GetAppProjectPath(
        string[] args,
        string currentDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentDirectory);

        var appProjectPath = CommandArgumentSupport.GetArgumentValue(args, "--app-project");
        if (string.IsNullOrWhiteSpace(appProjectPath))
        {
            return null;
        }

        return RequireAppProjectPath(
            Path.GetFullPath(appProjectPath, currentDirectory),
            "--app-project");
    }

    public static string GetReplayArtifactDir(string[] args, string currentDirectory)
    {
        var artifactDirectory = CommandArgumentSupport.GetArgumentValue(args, "--artifact-dir");
        if (!string.IsNullOrWhiteSpace(artifactDirectory))
        {
            return Path.GetFullPath(
                artifactDirectory,
                currentDirectory);
        }

        var artifactRoot = DefaultArtifactRootLocator.GetPath(currentDirectory);
        return ArtifactLayout.GetReplayArtifactDir(
            artifactRoot,
            DateTimeOffset.UtcNow);
    }

    private static string RequireAppProjectPath(
        string appProjectPath,
        string source)
    {
        if (!File.Exists(appProjectPath))
        {
            throw new ArgumentException(
                $"The ASP.NET Core source project from {source} does not exist: '{appProjectPath}'.");
        }

        if (!string.Equals(Path.GetExtension(appProjectPath), ".csproj", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"The ASP.NET Core source project from {source} must be a .csproj file: '{appProjectPath}'.");
        }

        return appProjectPath;
    }

    internal static ReplayLaunchOptions CreateReplayLaunchOptions(
        string[] args,
        string currentDirectory,
        bool requireDacpacForSeed = true)
    {
        var dacpacPath = CommandArgumentSupport.GetArgumentValue(args, "--sqlserver-dacpac-file");
        var seedSqlPath = CommandArgumentSupport.GetArgumentValue(args, "--sqlserver-seed-sql-file");

        if (string.IsNullOrWhiteSpace(dacpacPath)
            && string.IsNullOrWhiteSpace(seedSqlPath))
        {
            return new ReplayLaunchOptions();
        }

        if (string.IsNullOrWhiteSpace(dacpacPath)
            && !string.IsNullOrWhiteSpace(seedSqlPath))
        {
            var seedOnlySqlPath = ResolveSeedSqlPath(seedSqlPath, currentDirectory);
            if (requireDacpacForSeed)
            {
                throw new ArgumentException(
                    "The post-DACPAC SQL seed script requires --sqlserver-dacpac-file <path>.");
            }

            return new ReplayLaunchOptions
            {
                SeedSqlPath = seedOnlySqlPath,
            };
        }

        var fullDacpacPath = Path.GetFullPath(dacpacPath!, currentDirectory);
        if (!File.Exists(fullDacpacPath))
        {
            throw new ArgumentException(
                $"The SQL Server DACPAC '{fullDacpacPath}' does not exist.");
        }

        string? fullSeedSqlPath = null;
        if (!string.IsNullOrWhiteSpace(seedSqlPath))
        {
            fullSeedSqlPath = ResolveSeedSqlPath(seedSqlPath, currentDirectory);
        }

        return new ReplayLaunchOptions
        {
            DacpacPath = fullDacpacPath,
            SeedSqlPath = fullSeedSqlPath,
        };
    }

    private static string ResolveSeedSqlPath(
        string seedSqlPath,
        string currentDirectory)
    {
        var fullSeedSqlPath = Path.GetFullPath(seedSqlPath, currentDirectory);
        if (!File.Exists(fullSeedSqlPath))
        {
            throw new ArgumentException(
                $"The SQL seed script '{fullSeedSqlPath}' does not exist.");
        }

        return fullSeedSqlPath;
    }

    internal static ReplayDataAgentOptions CreateReplayDataAgentOptions(string[] args)
    {
        var mode = ParseReplayDataAgentMode(
            CommandArgumentSupport.GetArgumentValue(args, "--replay-data-agent"));
        if (mode == ReplayDataAgentMode.Off)
        {
            return new ReplayDataAgentOptions
            {
                Mode = ReplayDataAgentMode.Off,
            };
        }

        return new ReplayDataAgentOptions
        {
            Mode = mode,
            ModelName = CommandArgumentSupport.GetArgumentValue(args, "--replay-data-agent-model")
                ?? "gpt-5.4-mini",
        };
    }

    internal static void ValidateReplayDataAgentOptions(string[] args)
    {
        var replayDataAgentOptions = CreateReplayDataAgentOptions(args);
        _ = CreateReplayDataGenerator(args, replayDataAgentOptions);
    }

    private static IReplayDataGenerator? CreateReplayDataGenerator(
        string[] args,
        ReplayDataAgentOptions options)
    {
        if (options.Mode == ReplayDataAgentMode.Off)
        {
            return null;
        }

        var apiKey = CommandArgumentSupport.GetArgumentValue(args, "--openai-api-key");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException(
                "Sqloom replay data agent requires --openai-api-key unless --replay-data-agent off is supplied.");
        }

        return new AgentFrameworkReplayDataGenerator(
            new OpenAIAdviceOptions
            {
                ApiKey = apiKey,
                BaseUrl = CommandArgumentSupport.GetArgumentValue(args, "--openai-base-url")
                    ?? "https://api.openai.com",
                Model = options.ModelName ?? "gpt-5.4-mini",
            });
    }

    private static ReplayDataAgentMode ParseReplayDataAgentMode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ReplayDataAgentMode.Required;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "off" => ReplayDataAgentMode.Off,
            "auto" => ReplayDataAgentMode.Auto,
            "required" => ReplayDataAgentMode.Required,
            _ => throw new ArgumentException(
                "The value for --replay-data-agent must be 'off', 'auto', or 'required'."),
        };
    }
}
