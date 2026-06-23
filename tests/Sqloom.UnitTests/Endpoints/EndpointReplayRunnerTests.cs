using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sqloom.AspNetCore.Endpoints;
using Sqloom.Core.Execution;
using Xunit;

namespace Sqloom.AspNetCore.Tests.Endpoints;

/// <summary>
/// Exercises endpoint replay runner.
/// </summary>
public sealed class EndpointReplayRunnerTests
{
    [Fact]
    public async Task RunAsync_MergesPreparedRequestValuesAndSendsAuthenticatedRequest()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "sqloom-runner-tests", Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);
        var documentPath = Path.Combine(tempDirectory, "openapi.json");
        await File.WriteAllTextAsync(
            documentPath,
            """
            {
              "openapi": "3.0.1",
              "security": [
                { "Bearer": [] }
              ],
              "paths": {
                "/api/items/{itemId}": {
                  "post": {
                    "parameters": [
                      {
                        "name": "itemId",
                        "in": "path",
                        "required": true,
                        "schema": { "type": "string" }
                      },
                      {
                        "name": "since",
                        "in": "query",
                        "required": true,
                        "schema": { "type": "string", "format": "date-time" }
                      },
                      {
                        "name": "x-trace",
                        "in": "header",
                        "required": true,
                        "schema": { "type": "string" }
                      }
                    ],
                    "requestBody": {
                      "required": true,
                      "content": {
                        "application/json": {
                          "example": {
                            "name": "example"
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """);

        using CapturingHandler handler = new();
        using var services = new ServiceCollection().BuildServiceProvider();
        using FakeReplayHost replayHost = new(
            new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = new Uri("http://localhost")
            },
            services,
            new PreparedReplayOperation
            {
                Persona = "test-user",
                AccessToken = "token-123",
                PathValues = new Dictionary<string, string>
                {
                    ["itemId"] = "42"
                },
                QueryValues = new Dictionary<string, string>
                {
                    ["since"] = "2026-05-04T09:41:00+08:00"
                },
                HeaderValues = new Dictionary<string, string>
                {
                    ["x-trace"] = "abc123"
                },
                RequestBodyJson = """{"name":"runtime"}"""
            });
        FakeReplayHostFactory hostFactory = new(
            replayHost);
        var replayLaunchOptions = new ReplayLaunchOptions
        {
            DacpacPath = Path.Combine("artifacts", "test.dacpac"),
            SeedSqlPath = Path.Combine("artifacts", "test.seed.sql"),
        };

        EndpointReplayRunner runner = new();
        var result = await runner.RunAsync(
            new ReplayRunnerOptions
            {
                AppName = "TestApp",
                OpenApiPath = documentPath,
                ReplayArtifactDir = tempDirectory,
                ReplayProfile = new ReplayProfile
                {
                    OperationOverlays =
                    [
                        new ReplayOverlay
                        {
                            OperationKey = "POST /api/items/{itemId}",
                            Persona = "test-user",
                            AllowNonGetReplay = true
                        }
                    ]
                },
                ReplayHostFactory = hostFactory,
                ReplayLaunchOptions = replayLaunchOptions,
            });

        var replay = Assert.Single(result.Results);
        Assert.Equal("POST /api/items/{itemId}", replay.OperationKey);
        Assert.Equal(200, replay.HttpStatusCode);
        Assert.Equal("/api/items/42?since=2026-05-04T09%3A41%3A00%2B08%3A00", replay.Request.RelativePathAndQuery);
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("token-123", handler.AuthorizationParameter);
        Assert.Equal("""{"name":"runtime"}""", handler.RequestBody);
        Assert.Equal("abc123", Assert.Single(handler.RequestHeaders["x-trace"]));
        Assert.Equal("TestApp", result.AppName);
        Assert.Equal(replayLaunchOptions.DacpacPath, hostFactory.ReceivedLaunchOptions?.DacpacPath);
        Assert.Equal(replayLaunchOptions.SeedSqlPath, hostFactory.ReceivedLaunchOptions?.SeedSqlPath);
        Assert.Contains(
            result.Pipeline.Stages,
            static stage =>
                stage.Name == PipelineStageNames.Capture
                && stage.Status == PipelineStageStatuses.Completed);
    }

    /// <summary>
    /// Provides a fake replay host factory for replay runner tests.
    /// </summary>
    private sealed class FakeReplayHostFactory : IReplayHostFactory
    {
        private readonly IReplayHost _host;

        public FakeReplayHostFactory(IReplayHost host)
        {
            _host = host;
        }

        public ReplayLaunchOptions? ReceivedLaunchOptions { get; private set; }

        public Task<IReplayHost> CreateAsync(
            ReplayLaunchOptions? launchOptions = null,
            CancellationToken cancellationToken = default)
        {
            ReceivedLaunchOptions = launchOptions;
            return Task.FromResult(_host);
        }
    }

    /// <summary>
    /// Provides a fake replay host for replay runner tests.
    /// </summary>
    private sealed class FakeReplayHost : IReplayHost, IDisposable
    {
        private readonly PreparedReplayOperation _preparedOperation;

        public FakeReplayHost(
            HttpClient client,
            IServiceProvider services,
            PreparedReplayOperation preparedOperation)
        {
            Client = client;
            Services = services;
            _preparedOperation = preparedOperation;
        }

        public HttpClient Client { get; }

        public IServiceProvider Services { get; }

        public ReplayBootstrapReport Bootstrap { get; } = new();

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
            Client.Dispose();
        }

        public Task<PreparedReplayOperation> PrepareOperationAsync(
            ResolvedReplayOperation operation,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_preparedOperation);
        }
    }

    /// <summary>
    /// Captures outgoing HTTP requests for replay runner tests.
    /// </summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? AuthorizationScheme { get; private set; }

        public string? AuthorizationParameter { get; private set; }

        public Dictionary<string, string[]> RequestHeaders { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            foreach (var header in request.Headers)
            {
                RequestHeaders[header.Key] = new List<string>(header.Value).ToArray();
            }

            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"ok":true}""", Encoding.UTF8, "application/json")
            };
        }
    }
}
