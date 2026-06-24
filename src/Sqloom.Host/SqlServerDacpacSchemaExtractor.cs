using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac;
using Sqloom.Core.Artifacts;

namespace Sqloom.Host;

/// <summary>
/// Extracts SQL Server schema context from a DACPAC for Sqloom advice.
/// </summary>
internal interface ISqlServerDacpacSchemaExtractor
{
    Task<string> ExtractAsync(
        string dacpacPath,
        string replayArtifactDirectory,
        CancellationToken cancellationToken = default);
}

internal sealed class SqlServerDacpacSchemaExtractor : ISqlServerDacpacSchemaExtractor
{
    private const string DacpacModelSqlFileName = "model.sql";

    public Task<string> ExtractAsync(
        string dacpacPath,
        string replayArtifactDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dacpacPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        var fullDacpacPath = Path.GetFullPath(dacpacPath);
        if (!File.Exists(fullDacpacPath))
        {
            throw new ArgumentException(
                $"The SQL Server DACPAC '{fullDacpacPath}' does not exist.");
        }

        var schemaPath = ArtifactLayout.GetSqlServerSchemaPath(replayArtifactDirectory);
        var schemaDirectory = Path.GetDirectoryName(schemaPath);
        if (!string.IsNullOrWhiteSpace(schemaDirectory))
        {
            Directory.CreateDirectory(schemaDirectory);
        }

        var unpackDirectory = Path.Combine(
            Path.GetTempPath(),
            "sqloom-dacpac-schema",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(unpackDirectory);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var dacPackage = DacPackage.Load(fullDacpacPath);
            dacPackage.Unpack(unpackDirectory);
            cancellationToken.ThrowIfCancellationRequested();

            var modelSqlPath = Path.Combine(unpackDirectory, DacpacModelSqlFileName);
            if (!File.Exists(modelSqlPath))
            {
                throw new InvalidOperationException(
                    $"The SQL Server DACPAC '{fullDacpacPath}' did not contain '{DacpacModelSqlFileName}'.");
            }

            File.Copy(
                modelSqlPath,
                schemaPath,
                overwrite: true);
            return Task.FromResult(schemaPath);
        }
        finally
        {
            if (Directory.Exists(unpackDirectory))
            {
                Directory.Delete(
                    unpackDirectory,
                    recursive: true);
            }
        }
    }
}
