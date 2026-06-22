using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using Sqloom.Core.Execution;

namespace Sqloom.TestApp.Harness;

/// <summary>
/// Publishes the sample app DACPAC into a disposable SQL Server replay database.
/// </summary>
internal sealed class TestAppReplaySqlServerDacpacPublisher
{
    public async Task<SqlServerDacpacArtifact> PublishAsync(
        string connectionString,
        string dacpacPath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(dacpacPath);

        var fullPath = Path.GetFullPath(dacpacPath);
        if (!File.Exists(fullPath))
        {
            throw new ArgumentException(
                $"The SQL Server DACPAC '{fullPath}' does not exist.");
        }

        var fileName = Path.GetFileName(fullPath);
        var sha256 = await ComputeSha256Async(fullPath, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var targetDatabaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        DacServices dacServices = new(connectionString);
        using var dacpac = DacPackage.Load(fullPath);
        dacServices.Deploy(
            dacpac,
            targetDatabaseName,
            upgradeExisting: true);

        return new SqlServerDacpacArtifact
        {
            SourcePath = fullPath,
            FileName = fileName,
            Sha256 = sha256,
        };
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
