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
public sealed class TestAppApplication : ISqloomApplication
{
    private static readonly Lazy<string> _openApiDocumentPath = new(CreateOpenApiDocumentPath);

    public SqloomApplicationDescriptor Describe(SqloomApplicationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new SqloomApplicationDescriptor
        {
            Name = "Sqloom Test App",
            ReplayProfile = CreateReplayProfile(),
            QueryStoreWorkloadProfile = new QueryStoreWorkloadProfile
            {
                Name = "SqloomTestApp",
            },
            SqlServerSchemaPath = ResolveHarnessFilePath(TestAppReplayConstants.SqlServerSchemaFileName),
        };
    }

    public async ValueTask<ISqloomApplicationSession> StartAsync(
        SqloomApplicationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        TestAppReplayHostFactory replayHostFactory = new();
        var replayHost = await replayHostFactory
            .CreateAsync(
                CreateEffectiveLaunchOptions(context.ReplayLaunchOptions),
                cancellationToken)
            .ConfigureAwait(false);
        return new TestAppApplicationSession(replayHost);
    }

    private static ReplayProfile CreateReplayProfile()
    {
        return new ReplayProfile
        {
            DefaultOpenApiDocumentPath = _openApiDocumentPath.Value,
            Personas =
            [
                new ReplayPersonaDefinition
                {
                    Name = "sqloom-test-user",
                },
            ],
            OperationOverlays =
            [
                new ReplayOperationOverlayDefinition
                {
                    OperationKey = TestAppProductCatalogScenario.OperationKey,
                    Persona = "sqloom-test-user",
                    QueryValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["categoryId"] = TestAppProductCatalogScenario.HotCategoryId.ToString(),
                        ["minPrice"] = TestAppProductCatalogScenario.ReplayMinPriceText,
                    },
                    Notes = "AdventureWorks product query intentionally seeded without a supporting nonclustered index for tuning advice coverage.",
                },
            ],
        };
    }

    private static string CreateOpenApiDocumentPath()
    {
        var directoryPath = Path.Combine(
            Path.GetTempPath(),
            "sqloom-tests",
            "openapi");
        Directory.CreateDirectory(directoryPath);

        var documentPath = Path.Combine(directoryPath, "sqloom-test-app-openapi.json");
        File.WriteAllText(
            documentPath,
            """
            {
              "openapi": "3.0.1",
              "security": [
                {
                  "Bearer": []
                }
              ],
              "paths": {
                "/api/products/by-category": {
                  "get": {
                    "operationId": "GetProductsByCategory",
                    "parameters": [
                      {
                        "name": "categoryId",
                        "in": "query",
                        "required": true,
                        "schema": {
                          "type": "integer",
                          "format": "int32"
                        }
                      },
                      {
                        "name": "minPrice",
                        "in": "query",
                        "required": true,
                        "schema": {
                          "type": "number",
                          "format": "decimal"
                        }
                      }
                    ],
                    "responses": {
                      "200": {
                        "description": "OK"
                      }
                    }
                  }
                }
              }
            }
            """);

        return documentPath;
    }

    private static ReplayLaunchOptions CreateEffectiveLaunchOptions(ReplayLaunchOptions requestedOptions)
    {
        return new ReplayLaunchOptions
        {
            SqlServerDacpacPath = requestedOptions.SqlServerDacpacPath
                ?? ResolveHarnessFilePath(TestAppReplayConstants.SqlServerDacpacFileName),
            SqlServerSeedSqlPath = requestedOptions.SqlServerSeedSqlPath
                ?? ResolveHarnessFilePath(TestAppReplayConstants.SqlServerSeedSqlFileName),
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

    private sealed class TestAppApplicationSession : ISqloomApplicationSession
    {
        private readonly IReplayHost _replayHost;

        public TestAppApplicationSession(IReplayHost replayHost)
        {
            _replayHost = replayHost ?? throw new ArgumentNullException(nameof(replayHost));
        }

        public IReplayHost ReplayHost => _replayHost;

        public string? ReadOnlyConnectionString =>
            _replayHost is TestAppReplayHost testAppReplayHost
                ? testAppReplayHost.ReadOnlyConnectionString
                : null;

        public ReplayBootstrapReport Bootstrap => _replayHost.Bootstrap;

        public async ValueTask DisposeAsync()
        {
            await _replayHost.DisposeAsync().ConfigureAwait(false);
        }
    }

    internal static async Task<WebApplication> CreateReplayApplicationAsync(
        string? applicationConnectionString,
        CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        if (!string.IsNullOrWhiteSpace(applicationConnectionString))
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [TestAppConfigurationKeys.DefaultConnectionString] = applicationConnectionString,
            });
        }

        builder.Services
            .AddControllers()
            .AddApplicationPart(typeof(SqloomTestApp.TestAppProductsController).Assembly);
        builder.Services.AddScoped<SqloomTestApp.ITestAppProductCatalogService, SqloomTestApp.TestAppProductCatalogService>();
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
                options.UseInMemoryDatabase(TestAppConfigurationKeys.InMemoryDatabaseName);
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
