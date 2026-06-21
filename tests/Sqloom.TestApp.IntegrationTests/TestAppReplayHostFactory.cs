using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Sqloom.Core.Execution;
using Testcontainers.MsSql;

namespace Sqloom.TestApp.IntegrationTests;

/// <summary>
/// Creates replay hosts for the sample Sqloom test app integration.
/// </summary>
public sealed class TestAppReplayHostFactory : IReplayHostFactory
{
    private readonly TestAppReplayDatabaseBootstrapper _databaseBootstrapper = new();

    public async Task<IReplayHost> CreateAsync(
        ReplayLaunchOptions? launchOptions = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(launchOptions?.SqlServerDacpacPath)
            && !string.IsNullOrWhiteSpace(launchOptions?.SqlServerSeedSqlPath))
        {
            throw new ArgumentException(
                "Sqloom Test App replay requires --sqlserver-dacpac-file <path> when --sqlserver-seed-sql-file <path> is supplied.");
        }

        if (string.IsNullOrWhiteSpace(launchOptions?.SqlServerDacpacPath))
        {
            return await TestAppReplayHost
                .CreateAsync(
                    sqlServer: null,
                    applicationConnectionString: null,
                    new ReplayBootstrapReport(),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var sqlServerDacpacPath = TestAppReplaySqlServerDacpacPathResolver.ResolveRequiredPath(launchOptions);
        if (!string.Equals(
            Path.GetFileName(sqlServerDacpacPath),
            TestAppReplayConstants.SqlServerDacpacFileName,
            StringComparison.OrdinalIgnoreCase))
        {
            // The sample harness only provisions SQL when callers point it at the committed
            // AdventureWorks DACPAC. Other apps can still replay this endpoint through the
            // in-memory fallback when they share a generic host invocation.
            return await TestAppReplayHost
                .CreateAsync(
                    sqlServer: null,
                    applicationConnectionString: null,
                    new ReplayBootstrapReport(),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var sqlServer = new MsSqlBuilder(TestAppReplayConstants.SqlServerImage)
            .WithPassword(TestAppReplayConstants.SqlServerPassword)
            .Build();
        await sqlServer.StartAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var replayBootstrap = await _databaseBootstrapper
                .BootstrapAsync(
                    sqlServer,
                    launchOptions,
                    cancellationToken)
                .ConfigureAwait(false);

            return await TestAppReplayHost
                .CreateAsync(
                    sqlServer,
                    replayBootstrap.ApplicationConnectionString,
                    replayBootstrap.Bootstrap,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            await sqlServer.DisposeAsync().AsTask().ConfigureAwait(false);
            throw;
        }
    }
}

/// <summary>
/// Owns the in-memory host and optional SQL Server container used by the sample replay harness.
/// </summary>
internal sealed class TestAppReplayHost : IReplayHost
{
    private readonly MsSqlContainer? _sqlServer;
    private readonly WebApplication _application;
    private readonly HttpClient _client;
    private readonly ReplayBootstrapReport _bootstrap;

    private TestAppReplayHost(
        MsSqlContainer? sqlServer,
        WebApplication application,
        HttpClient client,
        ReplayBootstrapReport bootstrap)
    {
        _sqlServer = sqlServer;
        _application = application;
        _client = client;
        _bootstrap = bootstrap;
    }

    public HttpClient Client => _client;

    public IServiceProvider Services => _application.Services;

    public ReplayBootstrapReport Bootstrap => _bootstrap;

    public static async Task<TestAppReplayHost> CreateAsync(
        MsSqlContainer? sqlServer,
        string? applicationConnectionString,
        ReplayBootstrapReport bootstrap,
        CancellationToken cancellationToken)
    {
        var application = await TestAppIntegrationBase
            .CreateReplayApplicationAsync(
                applicationConnectionString,
                cancellationToken)
            .ConfigureAwait(false);

        var client = application.GetTestClient();
        client.BaseAddress = new Uri("http://localhost");

        return new TestAppReplayHost(
            sqlServer,
            application,
            client,
            bootstrap);
    }

    public Task<PreparedReplayOperation> PrepareOperationAsync(
        ResolvedReplayOperation operation,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PreparedReplayOperation
        {
            Persona = operation.Persona,
            AccessToken = "sqloom-test-app-token",
            PathValues = operation.PathValues,
            QueryValues = operation.QueryValues,
            HeaderValues = operation.HeaderValues,
            RequestBodyJson = operation.RequestBodyJson,
            Notes = operation.Notes,
        });
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _application.DisposeAsync().ConfigureAwait(false);
        if (_sqlServer is not null)
        {
            await _sqlServer.DisposeAsync().AsTask().ConfigureAwait(false);
        }
    }
}
