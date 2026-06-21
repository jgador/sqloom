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
        Path.Combine(RepositoryRoot, "backend", "artifacts", "sqloom");

    private static string TuneArtifactDirectory =>
        Path.Combine(ArtifactRoot, "tune", "tune-20260608T040506000Z");

    private static string ReplayArtifactDirectory =>
        Path.Combine(ArtifactRoot, "replay", "replay-20260608T040506000Z");

    [Fact]
    public void GetDefaultQueryStoreSnapshotPath_UsesQueryStoreFolderAndTimestampedFileName()
    {
        DateTimeOffset capturedAtUtc = new(2026, 6, 7, 13, 39, 58, TimeSpan.Zero);

        var path = ArtifactLayout.GetDefaultQueryStoreSnapshotPath(
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
    public void GetDefaultReplayArtifactDirectory_UsesReplayFolderAndTimestampedDirectory()
    {
        DateTimeOffset startedAtUtc = new(2026, 6, 8, 4, 5, 6, TimeSpan.Zero);

        var path = ArtifactLayout.GetDefaultReplayArtifactDirectory(
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
    public void GetDefaultTuneArtifactDirectory_UsesTuneFolderAndTimestampedDirectory()
    {
        DateTimeOffset startedAtUtc = new(2026, 6, 8, 4, 5, 6, TimeSpan.Zero);

        var path = ArtifactLayout.GetDefaultTuneArtifactDirectory(
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
    public void GetTuneReplayArtifactDirectory_UsesTuneArtifactDirectory()
    {
        var path = ArtifactLayout.GetTuneReplayArtifactDirectory(TuneArtifactDirectory);

        Assert.Equal(
            Path.Combine(
                TuneArtifactDirectory,
                "replay"),
            path);
    }

    [Fact]
    public void GetReplayQueryStoreCorrelationPath_UsesReplayArtifactDirectory()
    {
        var path = ArtifactLayout.GetReplayQueryStoreCorrelationPath(ReplayArtifactDirectory);

        Assert.Equal(
            Path.Combine(
                ReplayArtifactDirectory,
                "query-store-correlation.json"),
            path);
    }

    [Fact]
    public void GetReplayTuningAdvicePath_UsesReplayArtifactDirectory()
    {
        var path = ArtifactLayout.GetReplayTuningAdvicePath(ReplayArtifactDirectory);

        Assert.Equal(
            Path.Combine(
                ReplayArtifactDirectory,
                "tuning-advice.json"),
            path);
    }

    [Fact]
    public void GetReplaySqlTuningProposalPath_UsesReplayArtifactDirectory()
    {
        var path = ArtifactLayout.GetReplaySqlTuningProposalPath(ReplayArtifactDirectory);

        Assert.Equal(
            Path.Combine(
                ReplayArtifactDirectory,
                "sql-tuning-proposal.json"),
            path);
    }

    [Fact]
    public void GetReplaySqlTuningProposalScriptPath_UsesReplayArtifactDirectory()
    {
        var path = ArtifactLayout.GetReplaySqlTuningProposalScriptPath(ReplayArtifactDirectory);

        Assert.Equal(
            Path.Combine(
                ReplayArtifactDirectory,
                "sql-tuning-proposal.sql"),
            path);
    }
}
