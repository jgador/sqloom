#:sdk Microsoft.NET.Sdk.Web
#:property TargetFramework=net10.0
#:property ManagePackageVersionsCentrally=false
#:property EnableTrimAnalyzer=false
#:package Microsoft.AspNetCore.TestHost@10.0.1
#:package Sqloom.Testing@0.4.0
#:project ../../../Sqloom.TestApp/Sqloom.TestApp.csproj

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sqloom.Pipeline.Execution;
using Sqloom.Pipeline.QueryStore;
using Sqloom.Testing;
using Sqloom.Testing.AspNetCore;
using SqloomTestApp = global::Sqloom.TestApp;

internal static class Program
{
    private static void Main()
    {
    }
}

/// <summary>
/// Supplies the durable Sqloom harness for the sample application.
/// </summary>
public sealed class SqloomTestApplication : ISqloomApplication
{
    private const string DefaultConnectionKey = "ConnectionStrings:DefaultConnection";
    private const string InMemoryDatabaseName = "SqloomTestApp";

    /// <inheritdoc />
    public SqloomApplicationManifest Describe(SqloomApplicationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new SqloomApplicationManifest
        {
            Name = "Sqloom Test App",
            OpenApiPath = Path.GetFullPath(
                Path.Combine(
                    ResolveRepositoryRoot(context),
                    "tests",
                    "Sqloom.TestApp",
                    "openapi.json")),
            ReplayProfile = new ReplayProfile(),
            WorkloadProfile = new WorkloadProfile
            {
                Name = "SqloomTestApp",
            },
        };
    }

    private static string ResolveRepositoryRoot(SqloomApplicationContext context)
    {
        return RepositoryRootLocator.TryFind(context.CurrentDirectory)
            ?? RepositoryRootLocator.TryFind(Directory.GetCurrentDirectory())
            ?? throw new InvalidOperationException("Could not locate the repository root for the Sqloom Test App harness.");
    }

    /// <inheritdoc />
    public async ValueTask<ISqloomApplicationSession> StartAsync(
        SqloomApplicationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var replayHost = await ReplayHost
            .CreateAsync(
                context.ApplicationConnectionString,
                cancellationToken)
            .ConfigureAwait(false);
        return new SqloomTestApplicationSession(replayHost);
    }

    private static async Task<WebApplication> CreateReplayAppAsync(
        string? applicationConnectionString,
        CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        if (!string.IsNullOrWhiteSpace(applicationConnectionString))
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [DefaultConnectionKey] = applicationConnectionString,
            });
        }

        builder.Services
            .AddControllers()
            .AddApplicationPart(typeof(SqloomTestApp.ProductsController).Assembly);
        builder.Services.AddScoped<SqloomTestApp.IProductCatalogService, SqloomTestApp.ProductCatalogService>();
        builder.Services.AddSingleton<ReplaySqlCaptureCollector>();
        builder.Services.AddSingleton<ReplaySqlCommandInterceptor>();
        builder.Services.AddSingleton<IStartupFilter, ReplaySqlCaptureStartupFilter>();
        builder.Services.AddSingleton<IInterceptor>(serviceProvider =>
            serviceProvider.GetRequiredService<ReplaySqlCommandInterceptor>());
        builder.Services.AddDbContext<SqloomTestApp.TestAppProductCatalogDbContext>((serviceProvider, options) =>
        {
            var interceptors = serviceProvider.GetServices<IInterceptor>().ToArray();
            if (!string.IsNullOrWhiteSpace(applicationConnectionString))
            {
                options.UseSqlServer(applicationConnectionString);
            }
            else
            {
                options.UseInMemoryDatabase(InMemoryDatabaseName);
            }

            if (interceptors.Length > 0)
            {
                options.AddInterceptors(interceptors);
            }
        });

        var app = builder.Build();
        app.MapControllers();
        await app.StartAsync(cancellationToken).ConfigureAwait(false);
        return app;
    }

    private sealed class SqloomTestApplicationSession : ISqloomApplicationSession
    {
        private readonly IReplayHost _replayHost;

        public SqloomTestApplicationSession(IReplayHost replayHost)
        {
            _replayHost = replayHost ?? throw new ArgumentNullException(nameof(replayHost));
        }

        public IReplayHost ReplayHost => _replayHost;

        public string? ReadOnlyConnection =>
            _replayHost is ReplayHost testAppReplayHost
                ? testAppReplayHost.ReadOnlyConnection
                : null;

        public ReplayBootstrapReport Bootstrap => _replayHost.Bootstrap;

        public async ValueTask DisposeAsync()
        {
            await _replayHost.DisposeAsync().ConfigureAwait(false);
        }
    }

    private sealed class ReplayHost : IReplayHost
    {
        private readonly WebApplication _application;
        private readonly ReplayBootstrapReport _bootstrap = new();
        private readonly HttpClient _client;
        private readonly string? _readOnlyConnectionString;

        private ReplayHost(
            WebApplication application,
            HttpClient client,
            string? readOnlyConnectionString)
        {
            _application = application;
            _client = client;
            _readOnlyConnectionString = readOnlyConnectionString;
        }

        public HttpClient Client => _client;

        public IServiceProvider Services => _application.Services;

        public ReplayBootstrapReport Bootstrap => _bootstrap;

        public string? ReadOnlyConnection => _readOnlyConnectionString;

        public static async Task<ReplayHost> CreateAsync(
            string? applicationConnectionString,
            CancellationToken cancellationToken)
        {
            var application = await CreateReplayAppAsync(
                    applicationConnectionString,
                    cancellationToken)
                .ConfigureAwait(false);
            var client = application.GetTestClient();
            client.BaseAddress = new Uri("http://localhost");

            return new ReplayHost(
                application,
                client,
                applicationConnectionString);
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
}
