using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Sqloom.Core.Execution;

namespace Sqloom.TestApp.Harness;

/// <summary>
/// Executes a post-DACPAC SQL seed script against the sample replay database.
/// </summary>
internal sealed class TestAppReplaySqlServerSeedScriptExecutor
{
    public async Task<SqlServerSeedSqlArtifact> ExecuteAsync(
        string connectionString,
        string seedSqlPath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(seedSqlPath);

        var fullPath = Path.GetFullPath(seedSqlPath);
        if (!File.Exists(fullPath))
        {
            throw new ArgumentException(
                $"The SQL seed script '{fullPath}' does not exist.");
        }

        var sqlText = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
        var sha256 = await ComputeSha256Async(fullPath, cancellationToken).ConfigureAwait(false);
        var batches = SplitBatches(sqlText);

        SqlConnection connection = new(connectionString);
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            foreach (var batch in batches)
            {
                if (string.IsNullOrWhiteSpace(batch))
                {
                    continue;
                }

                var command = connection.CreateCommand();
                await using (command.ConfigureAwait(false))
                {
                    command.CommandText = batch;
                    command.CommandTimeout = TestAppReplayConstants.CommandTimeoutSeconds;
                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        return new SqlServerSeedSqlArtifact
        {
            SourcePath = fullPath,
            FileName = Path.GetFileName(fullPath),
            Sha256 = sha256,
        };
    }

    internal static IReadOnlyList<string> SplitBatches(string sqlText)
    {
        ArgumentNullException.ThrowIfNull(sqlText);

        List<string> batches = [];
        StringBuilder currentBatch = new();

        using StringReader reader = new(sqlText);
        while (reader.ReadLine() is { } line)
        {
            if (string.Equals(line.Trim(), "GO", StringComparison.OrdinalIgnoreCase))
            {
                AddBatchIfNotBlank(batches, currentBatch);
                currentBatch.Clear();
                continue;
            }

            currentBatch.AppendLine(line);
        }

        AddBatchIfNotBlank(batches, currentBatch);
        return batches;
    }

    private static void AddBatchIfNotBlank(
        List<string> batches,
        StringBuilder currentBatch)
    {
        if (currentBatch.Length == 0)
        {
            return;
        }

        var batchText = currentBatch.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(batchText))
        {
            batches.Add(batchText);
        }
    }

    private static async Task<string> ComputeSha256Async(
        string path,
        CancellationToken cancellationToken)
    {
        using var stream = File.OpenRead(path);
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hashBytes);
    }
}
