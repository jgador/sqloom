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
        SqloomApplicationDescriptor descriptor,
        IReplayHost replayHost,
        string currentDirectory,
        string? artifactDirectoryOverride = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(replayHost);

        CommandArgumentSupport.ValidateArguments(
            args,
            HostCommandKind.Replay,
            SupportedSwitches,
            ValueSwitches);

        var replayProfile = descriptor.ReplayProfile;
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
                AppName = descriptor.Name,
                OpenApiDocumentPath = openApiDocumentPath,
                ReplayArtifactDirectory = replayArtifactDirectory,
                ReplayProfile = replayProfile,
                ReplayHost = replayHost,
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

    internal ReplayLaunchOptions CreateReplayLaunchOptions(
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
