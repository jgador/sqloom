using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Sqloom.Pipeline.Execution;

namespace Sqloom.TestApp.Harness;

/// <summary>
/// Creates replay hosts for the sample Sqloom test app harness.
/// </summary>
public sealed class ReplayHostFactory : IReplayHostFactory
{
    private readonly string? _applicationConnectionString;

    public ReplayHostFactory()
    {
    }

    public ReplayHostFactory(string? applicationConnectionString)
    {
        _applicationConnectionString = applicationConnectionString;
    }

    public async Task<IReplayHost> CreateAsync(
        ReplayLaunchOptions? launchOptions = null,
        CancellationToken cancellationToken = default)
    {
        return await CreateAsync(
                _applicationConnectionString,
                launchOptions,
                cancellationToken)
            .ConfigureAwait(false);
    }

    internal async Task<IReplayHost> CreateAsync(
        string? applicationConnectionString,
        ReplayLaunchOptions? launchOptions = null,
        CancellationToken cancellationToken = default)
    {
        return await ReplayHost
            .CreateAsync(
                applicationConnectionString,
                new ReplayBootstrapReport(),
                cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Owns the in-memory or externally supplied SQL Server-backed host used by the sample replay harness.
/// </summary>
internal sealed class ReplayHost : IReplayHost
{
    private readonly WebApplication _application;
    private readonly HttpClient _client;
    private readonly ReplayBootstrapReport _bootstrap;
    private readonly string? _readOnlyConnectionString;

    private ReplayHost(
        WebApplication application,
        HttpClient client,
        string? readOnlyConnectionString,
        ReplayBootstrapReport bootstrap)
    {
        _application = application;
        _client = client;
        _readOnlyConnectionString = readOnlyConnectionString;
        _bootstrap = bootstrap;
    }

    public HttpClient Client => _client;

    public IServiceProvider Services => _application.Services;

    public ReplayBootstrapReport Bootstrap => _bootstrap;

    public string? ReadOnlyConnection => _readOnlyConnectionString;

    public static async Task<ReplayHost> CreateAsync(
        string? applicationConnectionString,
        ReplayBootstrapReport bootstrap,
        CancellationToken cancellationToken)
    {
        var application = await SampleApplication
            .CreateReplayAppAsync(
                applicationConnectionString,
                cancellationToken)
            .ConfigureAwait(false);

        var client = application.GetTestClient();
        client.BaseAddress = new Uri("http://localhost");

        return new ReplayHost(
            application,
            client,
            applicationConnectionString,
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
    }
}
