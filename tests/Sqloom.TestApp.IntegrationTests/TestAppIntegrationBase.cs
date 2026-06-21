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
using Sqloom.Core.Contracts;
using Sqloom.Core.Execution;
using Sqloom.QueryStore.QueryStore;
using SqloomTestApp = global::Sqloom.TestApp;

namespace Sqloom.TestApp.IntegrationTests;

/// <summary>
/// Provides shared replay integration behavior for the sample Sqloom test app.
/// </summary>
public abstract class TestAppIntegrationBase : IAppIntegration, IQueryStoreAppIntegration
{
    private static readonly Lazy<string> _openApiDocumentPath = new(CreateOpenApiDocumentPath);

    public virtual string AppName => "Sqloom Test App";

    public QueryStoreWorkloadProfile GetQueryStoreWorkloadProfile()
    {
        return new QueryStoreWorkloadProfile
        {
            Name = "SqloomTestApp",
        };
    }

    public ReplayProfile GetReplayProfile()
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

    public IReplayHostFactory CreateReplayHostFactory()
    {
        return new TestAppReplayHostFactory();
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
