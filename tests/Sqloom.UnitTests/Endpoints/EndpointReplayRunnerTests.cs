using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sqloom.Host.Replay;
using Sqloom.Pipeline.Execution;
using Xunit;

namespace Sqloom.Host.Tests.Replay;

/// <summary>
/// Exercises endpoint replay runner.
/// </summary>
public sealed class EndpointReplayRunnerTests
{
    [Fact]
    public async Task MergesPreparedValuesAndSendsAuthenticatedRequest()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "sqloom-runner-tests", Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);

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
        StaticReplayOperationCatalogLoader catalogLoader = new(
            CreateOperation(
                "POST",
                "/api/items/{itemId}",
                hasJsonRequestBody: true,
                requestBodyRequired: true,
                parameters:
                [
                    CreateParameter("itemId", "path", required: true, schemaType: "string"),
                    CreateParameter("since", "query", required: true, schemaType: "string", format: "date-time"),
                    CreateParameter("x-trace", "header", required: true, schemaType: "string"),
                ]));

        EndpointReplayRunner runner = new(catalogLoader);
        var result = await runner.RunAsync(
            new ReplayRunnerOptions
            {
                AppName = "TestApp",
                SourceProjectPath = "app.csproj",
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

    [Fact]
    public async Task WithReplayDataPreparer_FillsValuesAndWritesArtifact()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "sqloom-runner-tests", Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);

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
                AccessToken = "token-123"
            });
        StaticReplayOperationCatalogLoader catalogLoader = new(
            CreateOperation(
                "GET",
                "/api/items/{itemId}",
                parameters:
                [
                    CreateParameter("itemId", "path", required: true, schemaType: "integer"),
                    CreateParameter("since", "query", required: true, schemaType: "string", format: "date-time"),
                    CreateParameter("x-trace", "header", required: true, schemaType: "string"),
                ]));

        EndpointReplayRunner runner = new(catalogLoader);
        var result = await runner.RunAsync(
            new ReplayRunnerOptions
            {
                AppName = "TestApp",
                SourceProjectPath = "app.csproj",
                ReplayArtifactDir = tempDirectory,
                ReplayProfile = new ReplayProfile(),
                ReplayHost = replayHost,
                ReplayDataAgentOptions = new ReplayDataAgentOptions
                {
                    Mode = ReplayDataAgentMode.Auto,
                    ModelName = "gpt-test",
                },
                ReplayDataPreparer = new StubReplayDataPreparer(
                    new ReplayDataPreparationOperation
                    {
                        OperationKey = "GET /api/items/{itemId}",
                        Strategy = "test-preparer",
                        Status = "generated",
                        Confidence = 0.8,
                        PreparedData = new ReplayPreparedData
                        {
                            PathValues = new Dictionary<string, string>
                            {
                                ["itemId"] = "1",
                            },
                            QueryValues = new Dictionary<string, string>
                            {
                                ["since"] = "2026-01-01T00:00:00Z",
                            },
                            HeaderValues = new Dictionary<string, string>
                            {
                                ["x-trace"] = "sqloom",
                            },
                        },
                    }),
            });

        var replay = Assert.Single(result.Results);
        Assert.Equal("replayed", replay.Status);
        Assert.Equal("/api/items/1?since=2026-01-01T00%3A00%3A00Z", replay.Request.RelativePathAndQuery);
        Assert.Equal("sqloom", Assert.Single(handler.RequestHeaders["x-trace"]));
        Assert.NotNull(replayHost.LastResolvedOperation);
        Assert.Equal("1", replayHost.LastResolvedOperation!.PathValues["itemId"]);
        Assert.Equal("2026-01-01T00:00:00Z", replayHost.LastResolvedOperation.QueryValues["since"]);
        Assert.Equal("sqloom", replayHost.LastResolvedOperation.HeaderValues["x-trace"]);
        Assert.Equal(Path.Combine(tempDirectory, "replay-data-prep.json"), result.ReplayDataPreparationPath);
        Assert.True(File.Exists(result.ReplayDataPreparationPath));
        Assert.NotNull(result.ReplayDataPreparation);
        Assert.Equal("auto", result.ReplayDataPreparation!.Mode);
        Assert.Equal("gpt-test", result.ReplayDataPreparation.ModelName);

        var persistedReport = JsonSerializer.Deserialize<ReplayDataPreparationReport>(
            await File.ReadAllTextAsync(result.ReplayDataPreparationPath),
            JsonSerializerOptions.Web);
        Assert.NotNull(persistedReport);
        var preparedOperation = Assert.Single(persistedReport!.Operations);
        Assert.Equal("GET /api/items/{itemId}", preparedOperation.OperationKey);
        Assert.Equal("generated", preparedOperation.Status);
        Assert.Equal("1", preparedOperation.PreparedData.PathValues["itemId"]);
    }

    [Fact]
    public async Task WithReplayDataAgentOff_SkipsReplayDataAndArtifact()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "sqloom-runner-tests", Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);

        using CapturingHandler handler = new();
        using var services = new ServiceCollection().BuildServiceProvider();
        using FakeReplayHost replayHost = new(
            new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = new Uri("http://localhost")
            },
            services,
            new PreparedReplayOperation());
        StaticReplayOperationCatalogLoader catalogLoader = new(
            CreateOperation("GET", "/api/items"));

        EndpointReplayRunner runner = new(catalogLoader);
        var result = await runner.RunAsync(
            new ReplayRunnerOptions
            {
                AppName = "TestApp",
                SourceProjectPath = "app.csproj",
                ReplayArtifactDir = tempDirectory,
                ReplayProfile = new ReplayProfile(),
                ReplayHost = replayHost,
                ReplayDataAgentOptions = new ReplayDataAgentOptions
                {
                    Mode = ReplayDataAgentMode.Off,
                },
                ReplayDataPreparer = new ThrowingReplayDataPreparer(),
            });

        Assert.Null(result.ReplayDataPreparationPath);
        Assert.Null(result.ReplayDataPreparation);
        Assert.False(File.Exists(Path.Combine(tempDirectory, "replay-data-prep.json")));
        Assert.Single(result.Results);
    }

    [Fact]
    public async Task WhenReplayDataPreparerFails_WritesFailedArtifact()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "sqloom-runner-tests", Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);

        using CapturingHandler handler = new();
        using var services = new ServiceCollection().BuildServiceProvider();
        using FakeReplayHost replayHost = new(
            new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = new Uri("http://localhost")
            },
            services,
            new PreparedReplayOperation());
        StaticReplayOperationCatalogLoader catalogLoader = new(
            CreateOperation(
                "GET",
                "/api/items/{itemId}",
                parameters:
                [
                    CreateParameter("itemId", "path", required: true, schemaType: "integer"),
                ]));

        EndpointReplayRunner runner = new(catalogLoader);
        var result = await runner.RunAsync(
            new ReplayRunnerOptions
            {
                AppName = "TestApp",
                SourceProjectPath = "app.csproj",
                ReplayArtifactDir = tempDirectory,
                ReplayProfile = new ReplayProfile(),
                ReplayHost = replayHost,
                ReplayDataAgentOptions = new ReplayDataAgentOptions
                {
                    Mode = ReplayDataAgentMode.Auto,
                    ModelName = "gpt-test",
                },
                ReplayDataPreparer = new ThrowingReplayDataPreparer("prep failed"),
            });

        var replay = Assert.Single(result.Results);
        Assert.Equal("failed", replay.Status);
        Assert.Contains("prep failed", replay.ErrorMessage);
        Assert.Null(replayHost.LastResolvedOperation);
        Assert.Equal(Path.Combine(tempDirectory, "replay-data-prep.json"), result.ReplayDataPreparationPath);
        Assert.NotNull(result.ReplayDataPreparation);

        var preparedOperation = Assert.Single(result.ReplayDataPreparation!.Operations);
        Assert.Equal("GET /api/items/{itemId}", preparedOperation.OperationKey);
        Assert.Equal("failed", preparedOperation.Status);
        Assert.Equal("replay-data-agent", preparedOperation.Strategy);
        Assert.Contains("prep failed", preparedOperation.Warnings);
    }

    private static ReplayOperation CreateOperation(
        string httpMethod,
        string route,
        bool hasJsonRequestBody = false,
        bool requestBodyRequired = false,
        IReadOnlyList<ReplayParameter>? parameters = null)
    {
        return new ReplayOperation
        {
            StableOperationKey = $"{httpMethod} {route}",
            HttpMethod = httpMethod,
            Route = route,
            RequiresAuthentication = true,
            Parameters = parameters ?? [],
            HasJsonRequestBody = hasJsonRequestBody,
            RequestBodyRequired = requestBodyRequired,
        };
    }

    private static ReplayParameter CreateParameter(
        string name,
        string location,
        bool required,
        string? schemaType = null,
        string? format = null)
    {
        return new ReplayParameter
        {
            Name = name,
            Location = location,
            Required = required,
            SchemaType = schemaType,
            Format = format,
        };
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

    private sealed class StaticReplayOperationCatalogLoader : IReplayOperationCatalogLoader
    {
        private readonly IReadOnlyList<ReplayOperation> _operations;

        public StaticReplayOperationCatalogLoader(params ReplayOperation[] operations)
        {
            _operations = operations;
        }

        public string? ReceivedSourceProjectPath { get; private set; }

        public Task<IReadOnlyList<ReplayOperation>> LoadAsync(
            string sourceProjectPath,
            CancellationToken cancellationToken = default)
        {
            ReceivedSourceProjectPath = sourceProjectPath;
            return Task.FromResult(_operations);
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

        public ResolvedReplayOperation? LastResolvedOperation { get; private set; }

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
            LastResolvedOperation = operation;
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

    private sealed class ThrowingReplayDataPreparer : IReplayDataPreparer
    {
        private readonly string _message;

        public ThrowingReplayDataPreparer(string message = "Replay data preparer should not be called.")
        {
            _message = message;
        }

        public Task<ReplayDataPreparationOperation> PrepareAsync(
            ReplayDataPreparationContext context,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(_message);
        }
    }

    private sealed class StubReplayDataPreparer : IReplayDataPreparer
    {
        private readonly ReplayDataPreparationOperation _operation;

        public StubReplayDataPreparer(ReplayDataPreparationOperation operation)
        {
            _operation = operation;
        }

        public Task<ReplayDataPreparationOperation> PrepareAsync(
            ReplayDataPreparationContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_operation);
        }
    }
}
