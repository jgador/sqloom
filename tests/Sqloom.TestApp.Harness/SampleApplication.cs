using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sqloom.AspNetCore.Capture;
using Sqloom.Core.Execution;
using Sqloom.QueryStore.QueryStore;
using Sqloom.Testing;
using SqloomTestApp = global::Sqloom.TestApp;

namespace Sqloom.TestApp.Harness;

/// <summary>
/// Supplies the sample Sqloom test app harness.
/// </summary>
public sealed class SampleApplication : ISqloomApplication
{
    public SqloomApplicationManifest Describe(SqloomApplicationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new SqloomApplicationManifest
        {
            Name = "Sqloom Test App",
            OpenApiPath = OpenApiDoc.FindRequired(
                ResolveTestAppDirectory()),
            ReplayProfile = CreateReplayProfile(),
            WorkloadProfile = new WorkloadProfile
            {
                Name = "SqloomTestApp",
            },
            SchemaPath = ResolveHarnessFilePath(ReplayConstants.SchemaFileName),
        };
    }

    public async ValueTask<ISqloomApplicationSession> StartAsync(
        SqloomApplicationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        ReplayHostFactory replayHostFactory = new();
        var replayHost = await replayHostFactory
            .CreateAsync(
                ResolveLaunchOptions(context.ReplayLaunchOptions),
                cancellationToken)
            .ConfigureAwait(false);
        return new SampleSession(replayHost);
    }

    private static ReplayProfile CreateReplayProfile()
    {
        return new ReplayProfile
        {
            Personas =
            [
                new ReplayPersonaDefinition
                {
                    Name = "sqloom-test-user",
                },
            ],
            OperationOverlays =
            [
                new ReplayOverlay
                {
                    OperationKey = CatalogScenario.OperationKey,
                    Persona = "sqloom-test-user",
                    QueryValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["categoryId"] = CatalogScenario.HotCategoryId.ToString(),
                        ["minPrice"] = CatalogScenario.MinPriceText,
                    },
                    Notes = "AdventureWorks product query intentionally seeded without a supporting nonclustered index for tuning advice coverage.",
                },
            ],
        };
    }

    private static ReplayLaunchOptions ResolveLaunchOptions(ReplayLaunchOptions requestedOptions)
    {
        return new ReplayLaunchOptions
        {
            DacpacPath = requestedOptions.DacpacPath
                ?? ResolveHarnessFilePath(ReplayConstants.DacpacFileName),
            SeedSqlPath = requestedOptions.SeedSqlPath
                ?? ResolveHarnessFilePath(ReplayConstants.SeedSqlFileName),
        };
    }

    private static string ResolveHarnessFilePath(string fileName)
    {
        var repositoryRoot = RepositoryRootLocator.TryFind(AppContext.BaseDirectory)
            ?? RepositoryRootLocator.TryFind(Directory.GetCurrentDirectory())
            ?? throw new InvalidOperationException("Could not locate the repository root for the Sqloom Test App harness.");
        return Path.Combine(
            repositoryRoot,
            "tests",
            "Sqloom.TestApp.Harness",
            fileName);
    }

    private static string ResolveTestAppDirectory()
    {
        var repositoryRoot = RepositoryRootLocator.TryFind(AppContext.BaseDirectory)
            ?? RepositoryRootLocator.TryFind(Directory.GetCurrentDirectory())
            ?? throw new InvalidOperationException("Could not locate the repository root for the Sqloom Test App harness.");
        return Path.Combine(
            repositoryRoot,
            "tests",
            "Sqloom.TestApp");
    }

    private sealed class SampleSession : ISqloomApplicationSession
    {
        private readonly IReplayHost _replayHost;

        public SampleSession(IReplayHost replayHost)
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

    internal static async Task<WebApplication> CreateReplayAppAsync(
        string? applicationConnectionString,
        CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        if (!string.IsNullOrWhiteSpace(applicationConnectionString))
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ConfigKeys.DefaultConnectionKey] = applicationConnectionString,
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
                options.UseInMemoryDatabase(ConfigKeys.InMemoryDbName);
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
}
