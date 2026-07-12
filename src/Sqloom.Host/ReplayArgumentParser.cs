using System;
using System.Collections.Generic;
using System.IO;
using Sqloom.Host.Replay;
using Sqloom.Core.Artifacts;
using Sqloom.Core.Execution;
using Sqloom.Testing;

namespace Sqloom.Host;

/// <summary>
/// Parses and validates the Sqloom replay command arguments.
/// </summary>
internal sealed class ReplayArgumentParser
{
    public ReplayArguments Parse(
        string[] args,
        SqloomApplicationManifest manifest,
        IReplayHost replayHost,
        string currentDirectory,
        string? artifactDirectoryOverride = null,
        string? openApiPathOverride = null,
        ReplayLaunchOptions? replayLaunchOptionsOverride = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(replayHost);

        CommandArgumentSupport.ValidateArguments(args, HostCommandKind.Replay);

        var replayProfile = manifest.ReplayProfile;
        var openApiPath = openApiPathOverride
            ?? GetOpenApiPath(
                args,
                manifest,
                currentDirectory);
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
                OpenApiPath = openApiPath,
                ReplayArtifactDir = replayArtifactDirectory,
                ReplayProfile = replayProfile,
                ReplayHost = replayHost,
                ReplayLaunchOptions = replayLaunchOptions,
                ReplayDataAgentOptions = replayDataAgentOptions,
                ReplayDataPreparer = CreateReplayDataPreparer(args, replayDataAgentOptions),
                MaxOperations = CommandArgumentSupport.GetIntArgumentValue(args, "--max-operations") ?? 25,
                TargetFilter = targetFilter,
            },
        };
    }

    public string GetOpenApiPath(
        string[] args,
        SqloomApplicationManifest manifest,
        string currentDirectory)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentDirectory);

        var openApiPath = CommandArgumentSupport.GetArgumentValue(args, "--openapi-file");
        if (!string.IsNullOrWhiteSpace(openApiPath))
        {
            return RequireOpenApiPath(
                Path.GetFullPath(openApiPath, currentDirectory),
                "--openapi-file");
        }

        if (string.IsNullOrWhiteSpace(manifest.OpenApiPath))
        {
            throw new ArgumentException(
                "The Sqloom application manifest must set OpenApiPath to the absolute path of the app-owned OpenAPI document.");
        }

        if (!Path.IsPathFullyQualified(manifest.OpenApiPath))
        {
            throw new ArgumentException(
                $"The Sqloom application manifest OpenApiPath must be absolute: '{manifest.OpenApiPath}'.");
        }

        return RequireOpenApiPath(
            Path.GetFullPath(manifest.OpenApiPath),
            "Sqloom application manifest OpenApiPath");
    }

    public string GetReplayArtifactDir(string[] args, string currentDirectory)
    {
        var artifactDirectory = CommandArgumentSupport.GetArgumentValue(args, "--artifact-dir");
        if (!string.IsNullOrWhiteSpace(artifactDirectory))
        {
            return Path.GetFullPath(
                artifactDirectory,
                currentDirectory);
        }

        var artifactRoot = ArtifactRootResolver.Resolve(currentDirectory);
        return ArtifactLayout.GetReplayArtifactDir(
            artifactRoot,
            DateTimeOffset.UtcNow);
    }

    private static string RequireOpenApiPath(
        string openApiPath,
        string source)
    {
        if (!File.Exists(openApiPath))
        {
            throw new ArgumentException(
                $"The OpenAPI document from {source} does not exist: '{openApiPath}'.");
        }

        return openApiPath;
    }

    internal ReplayLaunchOptions CreateReplayLaunchOptions(
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

    internal ReplayDataAgentOptions CreateReplayDataAgentOptions(string[] args)
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

    internal void ValidateReplayDataAgentOptions(string[] args)
    {
        var replayDataAgentOptions = CreateReplayDataAgentOptions(args);
        _ = CreateReplayDataPreparer(args, replayDataAgentOptions);
    }

    private static IReplayDataPreparer? CreateReplayDataPreparer(
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

        return new AgentFrameworkReplayDataPreparer(
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
