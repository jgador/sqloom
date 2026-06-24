using System;
using System.IO;
using Sqloom.Core.Artifacts;
using Xunit;

namespace Sqloom.Core.Tests.Artifacts;

/// <summary>
/// Exercises Sqloom artifact layout.
/// </summary>
public sealed class ArtifactLayoutTests
{
    private const string RepositoryRoot = "repo-root";

    private static string ArtifactRoot =>
        Path.Combine(RepositoryRoot, "artifacts", "sqloom");

    private static string TuneArtifactDirectory =>
        Path.Combine(ArtifactRoot, "tune", "tune-20260608T040506000Z");

    private static string ReplayArtifactDir =>
        Path.Combine(ArtifactRoot, "replay", "replay-20260608T040506000Z");

    [Fact]
    public void GetQueryStoreSnapshotPath_UsesQueryStoreFolderAndTimestampedFileName()
    {
        DateTimeOffset capturedAtUtc = new(2026, 6, 7, 13, 39, 58, TimeSpan.Zero);

        var path = ArtifactLayout.GetQueryStoreSnapshotPath(
            ArtifactRoot,
            capturedAtUtc);

        Assert.Equal(
            Path.Combine(
                ArtifactRoot,
                "query-store",
                "query-store-20260607T133958000Z.json"),
            path);
    }

    [Fact]
    public void GetReplayArtifactDir_UsesReplayFolderAndTimestampedDirectory()
    {
        DateTimeOffset startedAtUtc = new(2026, 6, 8, 4, 5, 6, TimeSpan.Zero);

        var path = ArtifactLayout.GetReplayArtifactDir(
            ArtifactRoot,
            startedAtUtc);

        Assert.Equal(
            Path.Combine(
                ArtifactRoot,
                "replay",
                "replay-20260608T040506000Z"),
            path);
    }

    [Fact]
    public void GetTuneArtifactDir_UsesTuneFolderAndTimestampedDirectory()
    {
        DateTimeOffset startedAtUtc = new(2026, 6, 8, 4, 5, 6, TimeSpan.Zero);

        var path = ArtifactLayout.GetTuneArtifactDir(
            ArtifactRoot,
            startedAtUtc);

        Assert.Equal(
            Path.Combine(
                ArtifactRoot,
                "tune",
                "tune-20260608T040506000Z"),
            path);
    }

    [Fact]
    public void GetTuneSummaryPath_UsesTuneArtifactDirectory()
    {
        var path = ArtifactLayout.GetTuneSummaryPath(TuneArtifactDirectory);

        Assert.Equal(
            Path.Combine(
                TuneArtifactDirectory,
                "tune-summary.json"),
            path);
    }

    [Fact]
    public void GetTuneReplayArtifactDir_UsesTuneArtifactDirectory()
    {
        var path = ArtifactLayout.GetTuneReplayArtifactDir(TuneArtifactDirectory);

        Assert.Equal(
            Path.Combine(
                TuneArtifactDirectory,
                "replay"),
            path);
    }

    [Fact]
    public void GetCorrelationPath_UsesReplayArtifactDir()
    {
        var path = ArtifactLayout.GetCorrelationPath(ReplayArtifactDir);

        Assert.Equal(
            Path.Combine(
                ReplayArtifactDir,
                "query-store-correlation.json"),
            path);
    }

    [Fact]
    public void GetReplayTuningAdvicePath_UsesReplayArtifactDir()
    {
        var path = ArtifactLayout.GetReplayTuningAdvicePath(ReplayArtifactDir);

        Assert.Equal(
            Path.Combine(
                ReplayArtifactDir,
                "tuning-advice.json"),
            path);
    }

    [Fact]
    public void GetSqlServerSchemaPath_UsesReplayArtifactDir()
    {
        var path = ArtifactLayout.GetSqlServerSchemaPath(ReplayArtifactDir);

        Assert.Equal(
            Path.Combine(
                ReplayArtifactDir,
                "sqlserver-schema.sql"),
            path);
    }

    [Fact]
    public void GetSqlProposalPath_UsesReplayArtifactDir()
    {
        var path = ArtifactLayout.GetSqlProposalPath(ReplayArtifactDir);

        Assert.Equal(
            Path.Combine(
                ReplayArtifactDir,
                "sql-tuning-proposal.json"),
            path);
    }

    [Fact]
    public void GetSqlProposalScriptPath_UsesReplayArtifactDir()
    {
        var path = ArtifactLayout.GetSqlProposalScriptPath(ReplayArtifactDir);

        Assert.Equal(
            Path.Combine(
                ReplayArtifactDir,
                "sql-tuning-proposal.sql"),
            path);
    }
}
