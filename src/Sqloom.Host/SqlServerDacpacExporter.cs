using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using Sqloom.Core.Artifacts;

namespace Sqloom.Host;

/// <summary>
/// Exports a SQL Server database into a DACPAC for schema extraction.
/// </summary>
internal interface ISqlServerDacpacExporter
{
    Task<string> ExportAsync(
        string readOnlyConnectionString,
        string replayArtifactDirectory,
        CancellationToken cancellationToken = default);
}

internal sealed class SqlServerDacpacExporter : ISqlServerDacpacExporter
{
    public Task<string> ExportAsync(
        string readOnlyConnectionString,
        string replayArtifactDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(readOnlyConnectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        SqlConnectionStringBuilder connectionBuilder = new(readOnlyConnectionString);
        if (string.IsNullOrWhiteSpace(connectionBuilder.ApplicationName))
        {
            connectionBuilder.ApplicationName = "Sqloom";
        }

        if (string.IsNullOrWhiteSpace(connectionBuilder.InitialCatalog))
        {
            throw new ArgumentException(
                "The read-only connection string must include a database name before Sqloom can export a SQL Server DACPAC.");
        }

        var dacpacPath = ArtifactLayout.GetSqlServerDacpacPath(replayArtifactDirectory);
        var dacpacDirectory = Path.GetDirectoryName(dacpacPath);
        if (!string.IsNullOrWhiteSpace(dacpacDirectory))
        {
            Directory.CreateDirectory(dacpacDirectory);
        }

        if (File.Exists(dacpacPath))
        {
            File.Delete(dacpacPath);
        }

        cancellationToken.ThrowIfCancellationRequested();
        DacServices dacServices = new(connectionBuilder.ConnectionString);
        dacServices.Extract(
            targetPath: dacpacPath,
            databaseName: connectionBuilder.InitialCatalog,
            applicationName: "Sqloom",
            applicationVersion: new Version(1, 0, 0),
            applicationDescription: "Extracted by Sqloom from the read-only SQL Server connection string.",
            tables: null,
            extractOptions: new DacExtractOptions
            {
                VerifyExtraction = true,
            },
            cancellationToken: cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(dacpacPath);
    }
}
