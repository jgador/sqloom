using System;
using System.Collections.Generic;
using System.IO;
using Sqloom.AspNetCore.Endpoints;
using Sqloom.Core.Artifacts;
using Sqloom.Core.Execution;
using Sqloom.Testing;

namespace Sqloom.Host;

/// <summary>
/// Parses and validates the Sqloom replay command arguments.
/// </summary>
internal sealed class ReplayArgumentParser
{
    private static readonly HashSet<string> SupportedSwitches = new(StringComparer.OrdinalIgnoreCase)
    {
        "--openapi-file",
        "--sqlserver-dacpac-file",
        "--sqlserver-seed-sql-file",
        "--artifact-dir",
        "--max-operations",
        "--target",
    };

    private static readonly HashSet<string> ValueSwitches = new(StringComparer.OrdinalIgnoreCase)
    {
        "--openapi-file",
        "--sqlserver-dacpac-file",
        "--sqlserver-seed-sql-file",
        "--artifact-dir",
        "--max-operations",
        "--target",
    };

    public ReplayArguments Parse(
        string[] args,
        SqloomApplicationManifest manifest,
        IReplayHost replayHost,
        string currentDirectory,
        string? artifactDirectoryOverride = null,
        string? openApiPathOverride = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(replayHost);

        CommandArgumentSupport.ValidateArguments(
            args,
            HostCommandKind.Replay,
            SupportedSwitches,
            ValueSwitches);

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
        var replayLaunchOptions = CreateReplayLaunchOptions(args, currentDirectory);

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
        string currentDirectory)
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
            throw new ArgumentException(
                "The post-DACPAC SQL seed script requires --sqlserver-dacpac-file <path>.");
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
            fullSeedSqlPath = Path.GetFullPath(seedSqlPath, currentDirectory);
            if (!File.Exists(fullSeedSqlPath))
            {
                throw new ArgumentException(
                    $"The SQL seed script '{fullSeedSqlPath}' does not exist.");
            }
        }

        return new ReplayLaunchOptions
        {
            DacpacPath = fullDacpacPath,
            SeedSqlPath = fullSeedSqlPath,
        };
    }
}
