using System;
using System.Collections.Generic;
using System.IO;
using Sqloom.Pipeline.Artifacts;

namespace Sqloom.Host;

/// <summary>
/// Parses and validates the Sqloom correlate command arguments.
/// </summary>
internal sealed class CorrelateArgumentParser
{
    public string? GetQueryStoreConnectionString(string[] args)
    {
        return CommandArgumentSupport.GetArgumentValue(args, "--read-only-connection-string");
    }

    public CorrelateArguments Parse(
        string[] args,
        string connectionString)
    {
        CommandArgumentSupport.ValidateArguments(args, HostCommandKind.Correlate);

        var replayArtifactDirectory = Path.GetFullPath(
            CommandArgumentSupport.GetRequiredArgumentValue(args, "--replay-artifact-dir"));
        if (!Directory.Exists(replayArtifactDirectory))
        {
            throw new ArgumentException(
                $"The replay artifact directory '{replayArtifactDirectory}' does not exist.");
        }

        var queryStoreSnapshotPath = Path.GetFullPath(
            CommandArgumentSupport.GetRequiredArgumentValue(args, "--query-store-snapshot-file"));
        if (!File.Exists(queryStoreSnapshotPath))
        {
            throw new ArgumentException(
                $"The Query Store snapshot '{queryStoreSnapshotPath}' does not exist.");
        }

        var jsonOutputPath = CommandArgumentSupport.GetArgumentValue(args, "--json-output-file") is { } jsonOutputPathOverride
            ? Path.GetFullPath(jsonOutputPathOverride)
            : ArtifactLayout.GetCorrelationPath(replayArtifactDirectory);

        return new CorrelateArguments
        {
            ConnectionString = connectionString,
            ReplayArtifactDir = replayArtifactDirectory,
            QueryStoreSnapshotPath = queryStoreSnapshotPath,
            JsonOutputPath = jsonOutputPath,
        };
    }
}
