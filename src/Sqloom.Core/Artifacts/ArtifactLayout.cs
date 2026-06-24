using System;
using System.IO;

namespace Sqloom.Core.Artifacts;

/// <summary>
/// Builds the default artifact layout used by Sqloom runs.
/// </summary>
public static class ArtifactLayout
{
    public static string GetDefaultArtifactRoot(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        return Path.Combine(repositoryRoot, "artifacts", "sqloom");
    }

    public static string GetQueryStoreSnapshotPath(string artifactRoot, DateTimeOffset capturedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactRoot);

        return Path.Combine(
            artifactRoot,
            "query-store",
            $"query-store-{capturedAtUtc.UtcDateTime:yyyyMMddTHHmmssfffZ}.json");
    }

    public static string GetReplayArtifactDir(string artifactRoot, DateTimeOffset startedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactRoot);

        return Path.Combine(
            artifactRoot,
            "replay",
            $"replay-{startedAtUtc.UtcDateTime:yyyyMMddTHHmmssfffZ}");
    }

    public static string GetTuneArtifactDir(string artifactRoot, DateTimeOffset startedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactRoot);

        return Path.Combine(
            artifactRoot,
            "tune",
            $"tune-{startedAtUtc.UtcDateTime:yyyyMMddTHHmmssfffZ}");
    }

    public static string GetTuneSummaryPath(string tuneArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tuneArtifactDirectory);

        return Path.Combine(tuneArtifactDirectory, "tune-summary.json");
    }

    public static string GetTuneQueryStoreSnapshotPath(string tuneArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tuneArtifactDirectory);

        return Path.Combine(tuneArtifactDirectory, "query-store-snapshot.json");
    }

    public static string GetTuneReplayArtifactDir(string tuneArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tuneArtifactDirectory);

        return Path.Combine(tuneArtifactDirectory, "replay");
    }

    public static string GetDiscoveredOpsPath(string replayArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        return Path.Combine(replayArtifactDirectory, "discovered-operations.json");
    }

    public static string GetReplayPlanPath(string replayArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        return Path.Combine(replayArtifactDirectory, "replay-plan.json");
    }

    public static string GetReplaySummaryPath(string replayArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        return Path.Combine(replayArtifactDirectory, "replay-summary.json");
    }

    public static string GetCorrelationPath(string replayArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        return Path.Combine(replayArtifactDirectory, "query-store-correlation.json");
    }

    public static string GetReplayTuningAdvicePath(string replayArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        return Path.Combine(replayArtifactDirectory, "tuning-advice.json");
    }

    public static string GetSqlServerSchemaPath(string replayArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        return Path.Combine(replayArtifactDirectory, "sqlserver-schema.sql");
    }

    public static string GetSqlProposalPath(string replayArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        return Path.Combine(replayArtifactDirectory, "sql-tuning-proposal.json");
    }

    public static string GetSqlProposalScriptPath(string replayArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        return Path.Combine(replayArtifactDirectory, "sql-tuning-proposal.sql");
    }

    public static string GetOperationArtifactPath(
        string replayArtifactDirectory,
        int ordinal,
        string operationKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationKey);

        return Path.Combine(
            replayArtifactDirectory,
            "operations",
            $"{ordinal:D2}-{SanitizeForFileName(operationKey)}.json");
    }

    private static string SanitizeForFileName(string value)
    {
        Span<char> invalidCharacters = stackalloc char[Path.GetInvalidFileNameChars().Length];
        Path.GetInvalidFileNameChars().CopyTo(invalidCharacters);

        var buffer = value.ToCharArray();
        for (var index = 0; index < buffer.Length; index++)
        {
            var character = buffer[index];
            if (char.IsWhiteSpace(character)
                || character == '/'
                || character == '\\'
                || character == ':')
            {
                buffer[index] = '-';
                continue;
            }

            if (invalidCharacters.Contains(character))
            {
                buffer[index] = '_';
            }
        }

        return new string(buffer).Trim('-');
    }
}
