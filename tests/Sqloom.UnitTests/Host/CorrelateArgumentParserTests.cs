using System;
using System.IO;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises Sqloom correlate argument parsing.
/// </summary>
public sealed class CorrelateArgumentParserTests
{
    [Fact]
    public void Parse_ThrowsWhenReplayArtifactDirectoryIsMissing()
    {
        CorrelateArgumentParser parser = new();
        var missingReplayDirectory = Path.Combine(
            Path.GetTempPath(),
            "sqloom-host-command-line-tests",
            Guid.NewGuid().ToString("N"));
        var snapshotPath = CreateTempFile();

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "--replay-artifact-dir",
                    missingReplayDirectory,
                    "--query-store-snapshot-file",
                    snapshotPath,
                ],
                "Server=localhost;Database=Sqloom;Trusted_Connection=True;"));

        Assert.Contains("replay artifact directory", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_ThrowsWhenQueryStoreSnapshotIsMissing()
    {
        CorrelateArgumentParser parser = new();
        var replayDirectory = CreateTempDirectory();
        var missingSnapshotPath = Path.Combine(replayDirectory, "missing-query-store.json");

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "--replay-artifact-dir",
                    replayDirectory,
                    "--query-store-snapshot-file",
                    missingSnapshotPath,
                ],
                "Server=localhost;Database=Sqloom;Trusted_Connection=True;"));

        Assert.Contains("Query Store snapshot", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_RejectsLegacyQueryStoreSnapshotSwitch()
    {
        CorrelateArgumentParser parser = new();
        var replayDirectory = CreateTempDirectory();
        var snapshotPath = CreateTempFile();

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "--replay-artifact-dir",
                    replayDirectory,
                    "--query-store-snapshot",
                    snapshotPath,
                ],
                "Server=localhost;Database=Sqloom;Trusted_Connection=True;"));

        Assert.Contains("Unsupported switch", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--query-store-snapshot", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "sqloom-host-command-line-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string CreateTempFile()
    {
        var filePath = Path.Combine(
            CreateTempDirectory(),
            "query-store-snapshot.json");
        File.WriteAllText(filePath, "{}");
        return filePath;
    }
}
