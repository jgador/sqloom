using System;
using System.IO;

namespace Sqloom.Core.Artifacts;

/// <summary>
/// Builds the default artifact layout used by Sqloom runs.
/// </summary>
public static class ArtifactLayout
{
    /// <summary>
    /// Resolves the default Sqloom artifact root beneath a repository root.
    /// </summary>
    public static string GetDefaultArtifactRoot(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        return Path.Combine(repositoryRoot, "artifacts", "sqloom");
    }

    /// <summary>
    /// Builds the timestamped path for a standalone Query Store snapshot.
    /// </summary>
    public static string GetQueryStoreSnapshotPath(string artifactRoot, DateTimeOffset capturedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactRoot);

        return Path.Combine(
            artifactRoot,
            "query-store",
            $"query-store-{capturedAtUtc.UtcDateTime:yyyyMMddTHHmmssfffZ}.json");
    }

    /// <summary>
    /// Builds the timestamped artifact directory for a replay run.
    /// </summary>
    public static string GetReplayArtifactDir(string artifactRoot, DateTimeOffset startedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactRoot);

        return Path.Combine(
            artifactRoot,
            "replay",
            $"replay-{startedAtUtc.UtcDateTime:yyyyMMddTHHmmssfffZ}");
    }

    /// <summary>
    /// Builds the timestamped artifact directory for a tune workflow.
    /// </summary>
    public static string GetTuneArtifactDir(string artifactRoot, DateTimeOffset startedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactRoot);

        return Path.Combine(
            artifactRoot,
            "tune",
            $"tune-{startedAtUtc.UtcDateTime:yyyyMMddTHHmmssfffZ}");
    }

    /// <summary>
    /// Builds the tune workflow summary path.
    /// </summary>
    public static string GetTuneSummaryPath(string tuneArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tuneArtifactDirectory);

        return Path.Combine(tuneArtifactDirectory, "tune-summary.json");
    }

    /// <summary>
    /// Builds the Query Store snapshot path within a tune workflow.
    /// </summary>
    public static string GetTuneQueryStoreSnapshotPath(string tuneArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tuneArtifactDirectory);

        return Path.Combine(tuneArtifactDirectory, "query-store-snapshot.json");
    }

    /// <summary>
    /// Builds the replay artifact directory within a tune workflow.
    /// </summary>
    public static string GetTuneReplayArtifactDir(string tuneArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tuneArtifactDirectory);

        return Path.Combine(tuneArtifactDirectory, "replay");
    }

    /// <summary>
    /// Builds the discovered OpenAPI operations artifact path.
    /// </summary>
    public static string GetDiscoveredOpsPath(string replayArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        return Path.Combine(replayArtifactDirectory, "discovered-operations.json");
    }

    /// <summary>
    /// Builds the replay plan artifact path.
    /// </summary>
    public static string GetReplayPlanPath(string replayArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        return Path.Combine(replayArtifactDirectory, "replay-plan.json");
    }

    /// <summary>
    /// Builds the replay summary artifact path.
    /// </summary>
    public static string GetReplaySummaryPath(string replayArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        return Path.Combine(replayArtifactDirectory, "replay-summary.json");
    }

    /// <summary>
    /// Builds the replay data preparation artifact path.
    /// </summary>
    public static string GetReplayDataPreparationPath(string replayArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        return Path.Combine(replayArtifactDirectory, "replay-data-prep.json");
    }

    /// <summary>
    /// Builds the Query Store correlation artifact path.
    /// </summary>
    public static string GetCorrelationPath(string replayArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        return Path.Combine(replayArtifactDirectory, "query-store-correlation.json");
    }

    /// <summary>
    /// Builds the tuning advice artifact path for a replay run.
    /// </summary>
    public static string GetReplayTuningAdvicePath(string replayArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        return Path.Combine(replayArtifactDirectory, "tuning-advice.json");
    }

    /// <summary>
    /// Builds the normalized SQL Server schema artifact path.
    /// </summary>
    public static string GetSqlServerSchemaPath(string replayArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        return Path.Combine(replayArtifactDirectory, "sqlserver-schema.sql");
    }

    /// <summary>
    /// Builds the SQL Server DACPAC artifact path.
    /// </summary>
    public static string GetSqlServerDacpacPath(string replayArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        return Path.Combine(replayArtifactDirectory, "sqlserver-schema-source.dacpac");
    }

    /// <summary>
    /// Builds the directory for unpacked SQL Server DACPAC contents.
    /// </summary>
    public static string GetSqlServerDacpacExtractDir(string replayArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        return Path.Combine(replayArtifactDirectory, "sqlserver-dacpac-extract");
    }

    /// <summary>
    /// Builds the structured SQL tuning proposal path.
    /// </summary>
    public static string GetSqlProposalPath(string replayArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        return Path.Combine(replayArtifactDirectory, "sql-tuning-proposal.json");
    }

    /// <summary>
    /// Builds the executable SQL tuning proposal script path.
    /// </summary>
    public static string GetSqlProposalScriptPath(string replayArtifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        return Path.Combine(replayArtifactDirectory, "sql-tuning-proposal.sql");
    }

    /// <summary>
    /// Builds a stable artifact path for one replay operation.
    /// </summary>
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
