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
        string? openApiDocumentPathOverride = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(replayHost);

        CommandArgumentSupport.ValidateArguments(
            args,
            HostCommandKind.Replay,
            SupportedSwitches,
            ValueSwitches);

        var replayProfile = manifest.ReplayProfile;
        var openApiDocumentPath = openApiDocumentPathOverride
            ?? GetOpenApiDocumentPath(
                args,
                manifest,
                currentDirectory);
        var targetFilter = EndpointReplayTargetSyntax.ValidateOperationKeyOrNull(
            CommandArgumentSupport.GetArgumentValue(args, "--target"));
        var replayArtifactDirectory = artifactDirectoryOverride
            ?? GetReplayArtifactDirectory(args, currentDirectory);
        var replayLaunchOptions = CreateReplayLaunchOptions(args, currentDirectory);

        return new ReplayArguments
        {
            RunnerOptions = new EndpointReplayRunnerOptions
            {
                AppName = manifest.Name,
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

    public string GetOpenApiDocumentPath(
        string[] args,
        SqloomApplicationManifest manifest,
        string currentDirectory)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentDirectory);

        var openApiDocumentPath = CommandArgumentSupport.GetArgumentValue(args, "--openapi-file");
        if (!string.IsNullOrWhiteSpace(openApiDocumentPath))
        {
            return RequireOpenApiDocumentPath(
                Path.GetFullPath(openApiDocumentPath, currentDirectory),
                "--openapi-file");
        }

        if (string.IsNullOrWhiteSpace(manifest.OpenApiDocumentPath))
        {
            throw new ArgumentException(
                "The Sqloom application manifest must set OpenApiDocumentPath to the absolute path of the app-owned OpenAPI document.");
        }

        if (!Path.IsPathFullyQualified(manifest.OpenApiDocumentPath))
        {
            throw new ArgumentException(
                $"The Sqloom application manifest OpenApiDocumentPath must be absolute: '{manifest.OpenApiDocumentPath}'.");
        }

        return RequireOpenApiDocumentPath(
            Path.GetFullPath(manifest.OpenApiDocumentPath),
            "Sqloom application manifest OpenApiDocumentPath");
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

    private static string RequireOpenApiDocumentPath(
        string openApiDocumentPath,
        string source)
    {
        if (!File.Exists(openApiDocumentPath))
        {
            throw new ArgumentException(
                $"The OpenAPI document from {source} does not exist: '{openApiDocumentPath}'.");
        }

        return openApiDocumentPath;
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
