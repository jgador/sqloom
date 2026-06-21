using System;
using System.Collections.Generic;
using System.IO;
using Sqloom.AspNetCore.Endpoints;
using Sqloom.Core.Artifacts;
using Sqloom.Core.Contracts;
using Sqloom.Core.Execution;

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
        IAppIntegration appIntegration,
        string currentDirectory,
        string? artifactDirectoryOverride = null)
    {
        ArgumentNullException.ThrowIfNull(appIntegration);

        CommandArgumentSupport.ValidateArguments(
            args,
            HostCommandKind.Replay,
            SupportedSwitches,
            ValueSwitches);

        var replayProfile = appIntegration.GetReplayProfile();
        var openApiDocumentPath = Path.GetFullPath(
            CommandArgumentSupport.GetArgumentValue(args, "--openapi-file")
            ?? replayProfile.DefaultOpenApiDocumentPath);
        var targetFilter = EndpointReplayTargetSyntax.ValidateOperationKeyOrNull(
            CommandArgumentSupport.GetArgumentValue(args, "--target"));
        var replayArtifactDirectory = artifactDirectoryOverride
            ?? GetReplayArtifactDirectory(args, currentDirectory);
        var replayLaunchOptions = CreateReplayLaunchOptions(args, currentDirectory);

        return new ReplayArguments
        {
            RunnerOptions = new EndpointReplayRunnerOptions
            {
                AppName = appIntegration.AppName,
                OpenApiDocumentPath = openApiDocumentPath,
                ReplayArtifactDirectory = replayArtifactDirectory,
                ReplayProfile = replayProfile,
                ReplayHostFactory = appIntegration.CreateReplayHostFactory(),
                ReplayLaunchOptions = replayLaunchOptions,
                MaxOperations = CommandArgumentSupport.GetIntArgumentValue(args, "--max-operations") ?? 25,
                TargetFilter = targetFilter,
            },
        };
    }

    public string GetReplayArtifactDirectory(string[] args, string currentDirectory)
    {
        var artifactDirectory = CommandArgumentSupport.GetArgumentValue(args, "--artifact-dir");
        if (!string.IsNullOrWhiteSpace(artifactDirectory))
        {
            return Path.GetFullPath(
                artifactDirectory,
                currentDirectory);
        }

        var artifactRoot = ArtifactRootResolver.Resolve(currentDirectory);
        return ArtifactLayout.GetDefaultReplayArtifactDirectory(
            artifactRoot,
            DateTimeOffset.UtcNow);
    }

    private static ReplayLaunchOptions CreateReplayLaunchOptions(
        string[] args,
        string currentDirectory)
    {
        var sqlServerDacpacPath = CommandArgumentSupport.GetArgumentValue(args, "--sqlserver-dacpac-file");
        var sqlServerSeedSqlPath = CommandArgumentSupport.GetArgumentValue(args, "--sqlserver-seed-sql-file");

        if (string.IsNullOrWhiteSpace(sqlServerDacpacPath)
            && string.IsNullOrWhiteSpace(sqlServerSeedSqlPath))
        {
            return new ReplayLaunchOptions();
        }

        if (string.IsNullOrWhiteSpace(sqlServerDacpacPath)
            && !string.IsNullOrWhiteSpace(sqlServerSeedSqlPath))
        {
            throw new ArgumentException(
                "The post-DACPAC SQL seed script requires --sqlserver-dacpac-file <path>.");
        }

        var fullDacpacPath = Path.GetFullPath(sqlServerDacpacPath!, currentDirectory);
        if (!File.Exists(fullDacpacPath))
        {
            throw new ArgumentException(
                $"The SQL Server DACPAC '{fullDacpacPath}' does not exist.");
        }

        string? fullSeedSqlPath = null;
        if (!string.IsNullOrWhiteSpace(sqlServerSeedSqlPath))
        {
            fullSeedSqlPath = Path.GetFullPath(sqlServerSeedSqlPath, currentDirectory);
            if (!File.Exists(fullSeedSqlPath))
            {
                throw new ArgumentException(
                    $"The SQL seed script '{fullSeedSqlPath}' does not exist.");
            }
        }

        return new ReplayLaunchOptions
        {
            SqlServerDacpacPath = fullDacpacPath,
            SqlServerSeedSqlPath = fullSeedSqlPath,
        };
    }
}
