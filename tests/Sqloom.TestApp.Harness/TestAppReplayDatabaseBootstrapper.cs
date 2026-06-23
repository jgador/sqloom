using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Sqloom.Core.Execution;
using Testcontainers.MsSql;

namespace Sqloom.TestApp.Harness;

/// <summary>
/// Bootstraps the disposable SQL Server database used for the sample Sqloom replay harness.
/// </summary>
internal sealed class TestAppReplayDatabaseBootstrapper
{
    private readonly TestAppReplaySqlServerDacpacPublisher _dacpacPublisher = new();
    private readonly TestAppReplaySqlServerSeedScriptExecutor _seedScriptExecutor = new();
    private readonly TestAppReplayDatabaseSeeder _databaseSeeder = new();

    public async Task<TestAppReplayDatabaseBootstrapResult> BootstrapAsync(
        MsSqlContainer sqlServer,
        ReplayLaunchOptions? launchOptions,
        CancellationToken cancellationToken)
    {
        var fullDacpacPath = TestAppReplayDacpacPathResolver.ResolveRequiredPath(launchOptions);
        var fullSeedSqlPath = TestAppReplaySeedSqlPathResolver.ResolvePathOrNull(launchOptions);
        var applicationConnectionString = await CreateDatabaseAsync(sqlServer, cancellationToken).ConfigureAwait(false);
        var sqlServerDacpac = await _dacpacPublisher
            .PublishAsync(
                applicationConnectionString,
                fullDacpacPath,
                cancellationToken)
            .ConfigureAwait(false);
        SqlServerSeedSqlArtifact? sqlServerSeedSql = null;
        if (!string.IsNullOrWhiteSpace(fullSeedSqlPath))
        {
            sqlServerSeedSql = await _seedScriptExecutor
                .ExecuteAsync(
                    applicationConnectionString,
                    fullSeedSqlPath,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            await _databaseSeeder
                .SeedAsync(
                    applicationConnectionString,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return new TestAppReplayDatabaseBootstrapResult
        {
            ApplicationConnectionString = applicationConnectionString,
            Bootstrap = new ReplayBootstrapReport
            {
                SqlServerDacpac = sqlServerDacpac,
                SqlServerSeedSql = sqlServerSeedSql,
            },
        };
    }

    private static async Task<string> CreateDatabaseAsync(
        MsSqlContainer sqlServer,
        CancellationToken cancellationToken)
    {
        var databaseName = $"{TestAppReplayConstants.DatabaseNamePrefix}_{Guid.NewGuid():N}"[..32];
        var masterConnectionString = new SqlConnectionStringBuilder(sqlServer.GetConnectionString())
        {
            InitialCatalog = TestAppReplayConstants.MasterDatabaseName,
            MultipleActiveResultSets = true,
            TrustServerCertificate = true,
        }.ConnectionString;

        SqlConnection connection = new(masterConnectionString);
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            var command = connection.CreateCommand();
            await using (command.ConfigureAwait(false))
            {
                command.CommandText = $"CREATE DATABASE [{databaseName}];";
                command.CommandTimeout = TestAppReplayConstants.CommandTimeoutSeconds;
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        return new SqlConnectionStringBuilder(masterConnectionString)
        {
            InitialCatalog = databaseName,
        }.ConnectionString;
    }
}
