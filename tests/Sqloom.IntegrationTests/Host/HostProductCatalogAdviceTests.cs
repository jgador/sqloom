using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sqloom.AspNetCore.Endpoints;
using Sqloom.Core.Artifacts;
using Sqloom.Core.Execution;
using Sqloom.Correlation.QueryStore;
using Sqloom.TestApp.IntegrationTests;
using Xunit;
using SqloomTestApp = global::Sqloom.TestApp;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises the seeded AdventureWorks sample path used for missing-index advice coverage.
/// </summary>
[Collection("ConsoleHostRuntime")]
public sealed class HostProductCatalogAdviceTests
{
    private const string DefaultConnectionKey = "ConnectionStrings:DefaultConnection";
    private static readonly JsonSerializerOptions _correlationSerializerOptions = CreateCorrelationSerializerOptions();

    [RequiresDockerFact]
    [Trait("Category", "Integration")]
    public async Task CreateAsync_WithSqlServerDacpac_SeedsProductsByCategoryEndpoint()
    {
        TestAppReplayHostFactory replayHostFactory = new();
        var replayHost = await replayHostFactory
            .CreateAsync(
                new ReplayLaunchOptions
                {
                    SqlServerDacpacPath = SqloomTestAppPaths.GetSqlServerDacpacPath(),
                })
            .ConfigureAwait(false);

        await using (replayHost.ConfigureAwait(false))
        {
            using var response = await replayHost.Client
                .GetAsync(TestAppProductCatalogScenario.CreateRequestPath())
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var products = await response.Content
                .ReadFromJsonAsync<List<SqloomTestApp.ProductByCategoryResponse>>()
                .ConfigureAwait(false);

            Assert.NotNull(products);
            Assert.True(products.Count > 300, "Expected the seeded hot category to return a large filtered result set.");
            Assert.All(
                products,
                product => Assert.StartsWith("HOT-", product.ProductNumber, StringComparison.Ordinal));
            Assert.All(
                products,
                product => Assert.True(
                    product.ListPrice >= TestAppProductCatalogScenario.ReplayMinPrice,
                    $"Expected all list prices to be >= {TestAppProductCatalogScenario.ReplayMinPrice}, but found {product.ListPrice}."));
            AssertSortedByListPriceDescending(products);
        }
    }

    [RequiresDockerFact]
    [Trait("Category", "Integration")]
    [Trait("Category", "OpenAI")]
    public async Task HostRuntime_WithOpenAiAdviceFreeFormProposalKind_PersistsProposalForProductsByCategory()
    {
        var artifactDirectory = CreateTempDirectory();
        var dacpacPath = SqloomTestAppPaths.GetSqlServerDacpacPath();
        var schemaPath = SqloomTestAppPaths.GetSqlServerSchemaPath();
        var currentDirectory = Directory.GetCurrentDirectory();
        QueryStoreEnabledTestAppReplayHostFactory replayHostFactory = new(
            new ReplayLaunchOptions
            {
                SqlServerDacpacPath = dacpacPath,
            });

        await using (replayHostFactory.ConfigureAwait(false))
        {
            try
            {
                TestAppIntegration appIntegration = new();
                var replayProfile = appIntegration.GetReplayProfile();
                EndpointReplayRunner replayRunner = new();
                var replayResult = await replayRunner
                    .RunAsync(
                        new EndpointReplayRunnerOptions
                        {
                            AppName = appIntegration.AppName,
                            OpenApiDocumentPath = replayProfile.DefaultOpenApiDocumentPath,
                            ReplayArtifactDirectory = artifactDirectory,
                            ReplayProfile = replayProfile,
                            ReplayHostFactory = replayHostFactory,
                            ReplayLaunchOptions = new ReplayLaunchOptions
                            {
                                SqlServerDacpacPath = dacpacPath,
                            },
                            TargetFilter = TestAppProductCatalogScenario.OperationKey,
                        })
                    .ConfigureAwait(false);

                var replayOperation = Assert.Single(replayResult.Results);
                Assert.Equal("replayed", replayOperation.Status);
                Assert.Equal(200, replayOperation.HttpStatusCode);
                Assert.Contains(
                    replayOperation.CapturedSqlCommands,
                    command =>
                        command.SourceKind == CapturedSqlSourceKind.EntityFramework
                        && command.CommandText.Contains("[SalesLT].[Product]", StringComparison.OrdinalIgnoreCase)
                        && command.CommandText.Contains("[ProductCategoryID]", StringComparison.OrdinalIgnoreCase)
                        && command.CommandText.Contains("[ListPrice]", StringComparison.OrdinalIgnoreCase));

                var applicationConnectionString = replayHostFactory.ApplicationConnectionString
                    ?? throw new InvalidOperationException("The retained Sqloom replay host did not expose an application connection string.");

                await WarmQueryStoreAsync(replayHostFactory.Client).ConfigureAwait(false);

                var correlationReport = await CaptureCorrelationWithRetriesAsync(
                        currentDirectory,
                        artifactDirectory,
                        applicationConnectionString)
                    .ConfigureAwait(false);
                Assert.True(
                    HasMatchedProductCorrelation(correlationReport),
                    FormatCorrelationFailureMessage(correlationReport));

                var fakeOpenAiServer = await FakeOpenAiServer
                    .StartAsync(CreateOpenAiAdviceResponse())
                    .ConfigureAwait(false);
                await using (fakeOpenAiServer.ConfigureAwait(false))
                {
                    var adviseResult = await RunHostRuntimeAsync(
                            [
                                "advise",
                                "--replay-artifact-dir",
                                artifactDirectory,
                                "--model-provider",
                                "openai",
                                "--openai-api-key",
                                "sqloom-test-key",
                                "--sqlserver-schema-file",
                                schemaPath,
                                "--openai-base-url",
                                fakeOpenAiServer.BaseUrl.AbsoluteUri,
                            ],
                            currentDirectory)
                        .ConfigureAwait(false);
                    AssertCommandSucceeded("advise", adviseResult.ExitCode, adviseResult.StdOut, adviseResult.StdErr);
                    Assert.DoesNotContain(
                        "proposal kind 'nonclustered_index' is not locally validated",
                        adviseResult.StdErr,
                        StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain(
                        "Dropped SQL proposal",
                        adviseResult.StdErr,
                        StringComparison.OrdinalIgnoreCase);

                    var adviceReport = await ReadAdviceReportAsync(
                            ArtifactLayout.GetReplayTuningAdvicePath(artifactDirectory))
                        .ConfigureAwait(false);
                    var proposalReport = await ReadProposalReportAsync(
                            ArtifactLayout.GetReplaySqlTuningProposalPath(artifactDirectory))
                        .ConfigureAwait(false);
                    var proposalScript = await File
                        .ReadAllTextAsync(
                            ArtifactLayout.GetReplaySqlTuningProposalScriptPath(artifactDirectory))
                        .ConfigureAwait(false);
                    Assert.True(
                        string.Equals(adviceReport.ModelProvider, "openai", StringComparison.OrdinalIgnoreCase),
                        $"Expected an OpenAI advice report, but model provider was '{adviceReport.ModelProvider}'.");

                    var adviceOperation = Assert.Single(adviceReport.Operations);
                    var proposalOperation = Assert.Single(proposalReport.Operations);
                    Assert.Equal(TestAppProductCatalogScenario.OperationKey, adviceOperation.OperationKey);
                    Assert.True(
                        HasSurvivingProductProposal(adviceOperation),
                        FormatAdviceFailureMessage(adviceReport));
                    Assert.True(
                        HasSurvivingProductProposal(proposalOperation.Proposals),
                        FormatProposalFailureMessage(proposalReport, proposalScript));
                    Assert.Contains("-- Kind: nonclustered_index", proposalScript, StringComparison.Ordinal);
                    Assert.Contains("CREATE NONCLUSTERED INDEX", proposalScript, StringComparison.OrdinalIgnoreCase);
                    Assert.True(
                        ContainsProductTableReference(proposalScript),
                        $"Expected the proposal script to reference SalesLT.Product, but script was:{Environment.NewLine}{proposalScript}");
                    Assert.Contains("ProductCategoryID", proposalScript, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains("ListPrice", proposalScript, StringComparison.OrdinalIgnoreCase);
                    Assert.Equal("/v1/responses", Assert.Single(fakeOpenAiServer.RequestPaths));
                }
            }
            finally
            {
                DeleteDirectoryIfExists(artifactDirectory);
            }
        }
    }

    private static async Task<QueryStoreCorrelationReport> CaptureCorrelationWithRetriesAsync(
        string currentDirectory,
        string artifactDirectory,
        string applicationConnectionString)
    {
        var snapshotPath = Path.Combine(artifactDirectory, "query-store-snapshot.json");
        QueryStoreCorrelationReport? lastReport = null;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await FlushQueryStoreAsync(applicationConnectionString).ConfigureAwait(false);

            var observeResult = await RunHostRuntimeAsync(
                    [
                        "observe",
                        "--read-only-connection-string",
                        applicationConnectionString,
                        "--lookback-hours",
                        "1",
                        "--max-plans",
                        "50",
                        "--max-waits",
                        "10",
                        "--command-timeout-seconds",
                        "60",
                        "--json-output-file",
                        snapshotPath,
                    ],
                    currentDirectory)
                .ConfigureAwait(false);
            AssertCommandSucceeded("observe", observeResult.ExitCode, observeResult.StdOut, observeResult.StdErr);

            var correlateResult = await RunHostRuntimeAsync(
                    [
                        "correlate",
                        "--replay-artifact-dir",
                        artifactDirectory,
                        "--query-store-snapshot-file",
                        snapshotPath,
                        "--read-only-connection-string",
                        applicationConnectionString,
                    ],
                    currentDirectory)
                .ConfigureAwait(false);
            AssertCommandSucceeded("correlate", correlateResult.ExitCode, correlateResult.StdOut, correlateResult.StdErr);

            lastReport = await ReadCorrelationReportAsync(
                    ArtifactLayout.GetReplayQueryStoreCorrelationPath(artifactDirectory))
                .ConfigureAwait(false);
            if (HasMatchedProductCorrelation(lastReport))
            {
                return lastReport;
            }

            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }

        throw new Xunit.Sdk.XunitException(
            lastReport is null
                ? "Sqloom never produced a Query Store correlation artifact for the seeded product query."
                : FormatCorrelationFailureMessage(lastReport));
    }

    private static async Task WarmQueryStoreAsync(HttpClient client)
    {
        for (var iteration = 0; iteration < 6; iteration++)
        {
            using var response = await client
                .GetAsync(TestAppProductCatalogScenario.CreateRequestPath())
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
    }

    private static async Task<QueryStoreCorrelationReport> ReadCorrelationReportAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        return JsonSerializer.Deserialize<QueryStoreCorrelationReport>(
                json,
                _correlationSerializerOptions)
            ?? throw new InvalidOperationException($"Could not deserialize the Query Store correlation report at '{path}'.");
    }

    private static async Task<AdviceReport> ReadAdviceReportAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        return JsonSerializer.Deserialize<AdviceReport>(json)
            ?? throw new InvalidOperationException($"Could not deserialize the advice report at '{path}'.");
    }

    private static async Task<SqlTuningProposalReport> ReadProposalReportAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        return JsonSerializer.Deserialize<SqlTuningProposalReport>(json)
            ?? throw new InvalidOperationException($"Could not deserialize the SQL proposal report at '{path}'.");
    }

    private static bool HasMatchedProductCorrelation(QueryStoreCorrelationReport report)
    {
        return report.Records.Any(record =>
            string.Equals(record.OperationKey, TestAppProductCatalogScenario.OperationKey, StringComparison.OrdinalIgnoreCase)
            && record.CapturedCommand.SourceKind == CapturedSqlSourceKind.EntityFramework
            && record.MatchKind != QueryStoreCorrelationMatchKind.Unmatched
            && record.MatchedPlans.Count > 0);
    }

    private static bool HasSurvivingProductProposal(AdviceOperationReport operation)
    {
        return HasSurvivingProductProposal(operation.Proposals);
    }

    private static bool HasSurvivingProductProposal(IReadOnlyList<SqlTuningProposal> proposals)
    {
        return proposals.Any(static proposal =>
            proposal.TargetObject.Contains("Product", StringComparison.OrdinalIgnoreCase)
            && string.Equals(
                proposal.ProposalKind,
                "nonclustered_index",
                StringComparison.Ordinal)
            && (proposal.SqlScript.Contains("CREATE NONCLUSTERED INDEX", StringComparison.OrdinalIgnoreCase)
                || proposal.SqlScript.Contains("CREATE INDEX", StringComparison.OrdinalIgnoreCase))
            && ContainsIndexColumnReference(proposal.SqlScript, "ProductCategoryID")
            && ContainsIndexColumnReference(proposal.SqlScript, "ListPrice"));
    }

    private static bool ContainsProductTableReference(string value)
    {
        return value.Contains("[SalesLT].[Product]", StringComparison.OrdinalIgnoreCase)
            || value.Contains("SalesLT.Product", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsIndexColumnReference(
        string sqlScript,
        string columnName)
    {
        return sqlScript.Contains($"[{columnName}]", StringComparison.OrdinalIgnoreCase)
            || sqlScript.Contains(columnName, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task FlushQueryStoreAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        SqlConnection connection = new(connectionString);
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            var command = connection.CreateCommand();
            await using (command.ConfigureAwait(false))
            {
                command.CommandText = "EXEC sys.sp_query_store_flush_db;";
                command.CommandTimeout = 60;
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task EnableQueryStoreAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        const string enableQueryStoreSql = """
            ALTER DATABASE CURRENT SET QUERY_STORE = ON
            (
                OPERATION_MODE = READ_WRITE,
                QUERY_CAPTURE_MODE = ALL
            );
            """;

        SqlConnection connection = new(connectionString);
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            var command = connection.CreateCommand();
            await using (command.ConfigureAwait(false))
            {
                command.CommandText = enableQueryStoreSql;
                command.CommandTimeout = 60;
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static string CreateOpenAiAdviceResponse()
    {
        return JsonSerializer.Serialize(new
        {
            output_text = JsonSerializer.Serialize(new
            {
                recommendations = new[]
                {
                    new
                    {
                        title = "Recover stronger ownership evidence for the product query",
                        rootCause = "The matched plan was tied back through fingerprint fallback instead of exact Query Store ownership.",
                        suggestedChange = "Capture a fresh Query Store snapshot and confirm exact statement ownership while also applying the aligned index proposal below if the same plan remains hot.",
                        verificationMetric = "Exact statement correlation increases and the matched plan reads drop on the next replay."
                    },
                    new
                    {
                        title = "Add a covering Product index for category-and-price filtering",
                        rootCause = "The replayed Product query filters by ProductCategoryID, ranges on ListPrice, sorts by ListPrice DESC, and projects ProductID, Name, and ProductNumber.",
                        suggestedChange = "Create a nonclustered covering index on SalesLT.Product keyed by ProductCategoryID and ListPrice DESC with the projected columns included.",
                        verificationMetric = "The matched Product plan shows fewer logical reads and lower mean duration after replay."
                    }
                },
                proposals = new[]
                {
                    new
                    {
                        title = "Create covering Product index for by-category price query",
                        diagnosis = "The replayed Product query filters by ProductCategoryID, ranges on ListPrice, sorts by ListPrice DESC, and projects ProductID, Name, and ProductNumber.",
                        proposalKind = "nonclustered_index",
                        targetObject = "SalesLT.Product",
                        sqlScript = "CREATE NONCLUSTERED INDEX IX_Product_Category_ListPrice ON SalesLT.Product (ProductCategoryID ASC, ListPrice DESC) INCLUDE (ProductID, Name, ProductNumber);",
                        rollbackSqlScript = "DROP INDEX IX_Product_Category_ListPrice ON SalesLT.Product;",
                        expectedBenefit = "Reduce logical reads and sort work for the matched Product query plan.",
                        verificationMetric = "Logical reads and mean duration drop for the matched Product query plan on the next replay.",
                        confidence = 0.84d,
                        sourceCommandOrdinals = new[] { 1 },
                        matchedPlanIds = new[] { 89L },
                    },
                }
            })
        });
    }

    private static async Task<HostRuntimeCommandResult> RunHostRuntimeAsync(
        string[] args,
        string currentDirectory)
    {
        return await CaptureConsoleAsync(static async state =>
        {
            var exitCode = await HostRuntime
                .RunAsync(
                    new StandaloneTestAppIntegration(),
                    state.Args,
                    state.CurrentDirectory)
                .ConfigureAwait(false);
            return new HostRuntimeCommandResult(
                exitCode,
                string.Empty,
                string.Empty);
        }, (Args: args, CurrentDirectory: currentDirectory)).ConfigureAwait(false);
    }

    private static JsonSerializerOptions CreateCorrelationSerializerOptions()
    {
        JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = false,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static async Task<HostRuntimeCommandResult> CaptureConsoleAsync<TState>(
        Func<TState, Task<HostRuntimeCommandResult>> action,
        TState state)
    {
        await ConsoleHostRuntimeCollection.Gate.WaitAsync().ConfigureAwait(false);
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var stdOut = new StringWriter();
        using var stdErr = new StringWriter();

        try
        {
            Console.SetOut(stdOut);
            Console.SetError(stdErr);
            var result = await action(state).ConfigureAwait(false);
            return result with
            {
                StdOut = stdOut.ToString(),
                StdErr = stdErr.ToString(),
            };
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            ConsoleHostRuntimeCollection.Gate.Release();
        }
    }

    private static void AssertSortedByListPriceDescending(IReadOnlyList<SqloomTestApp.ProductByCategoryResponse> products)
    {
        for (var index = 1; index < products.Count; index++)
        {
            Assert.True(
                products[index - 1].ListPrice >= products[index].ListPrice,
                $"Expected descending list prices, but row {index - 1} had {products[index - 1].ListPrice} before {products[index].ListPrice}.");
        }
    }

    private static void AssertCommandSucceeded(
        string stageName,
        int exitCode,
        string standardOutput,
        string standardError)
    {
        Assert.True(
            exitCode == 0,
            $"Sqloom {stageName} failed.{Environment.NewLine}ExitCode: {exitCode}{Environment.NewLine}StdOut:{Environment.NewLine}{standardOutput}{Environment.NewLine}StdErr:{Environment.NewLine}{standardError}");
    }

    private static string FormatCorrelationFailureMessage(QueryStoreCorrelationReport report)
    {
        var summaries = report.Records
            .Select(record =>
                $"{record.OperationKey}:{record.MatchKind}:{record.MatchedPlans.Count}:{Truncate(record.ComparableSqlText)}")
            .ToArray();
        return
            $"Expected a matched Query Store record for '{TestAppProductCatalogScenario.OperationKey}', but found: {string.Join(" | ", summaries)}.";
    }

    private static string FormatAdviceFailureMessage(AdviceReport report)
    {
        var operation = report.Operations.Single();
        var recommendationSummaries = operation.Recommendations
            .Select(recommendation => $"{recommendation.Title} => {recommendation.SuggestedChange}")
            .ToArray();
        var proposalSummaries = operation.Proposals
            .Select(proposal => $"{proposal.ProposalKind}:{proposal.TargetObject}:{Truncate(proposal.SqlScript)}")
            .ToArray();

        return
            $"Expected the OpenAI advice report to suggest an index for the seeded product query, but recommendations were [{string.Join(" | ", recommendationSummaries)}] and proposals were [{string.Join(" | ", proposalSummaries)}].";
    }

    private static string FormatProposalFailureMessage(
        SqlTuningProposalReport report,
        string proposalScript)
    {
        var operation = report.Operations.Single();
        var proposalSummaries = operation.Proposals
            .Select(proposal => $"{proposal.ProposalKind}:{proposal.TargetObject}:{Truncate(proposal.SqlScript)}")
            .ToArray();

        return
            $"Expected the SQL proposal sidecars to keep an index proposal for '{TestAppProductCatalogScenario.OperationKey}', but proposals were [{string.Join(" | ", proposalSummaries)}] and script was [{Truncate(proposalScript)}].";
    }

    private static string Truncate(string value)
    {
        return value.Length <= 180
            ? value
            : value[..177] + "...";
    }

    private static string CreateTempDirectory()
    {
        var directoryPath = Path.Combine(
            Path.GetTempPath(),
            "sqloom-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
        return directoryPath;
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(
                path,
                recursive: true);
        }
    }

    /// <summary>
    /// Retains the inner SQL-backed replay host so Query Store stages can run against the same container.
    /// </summary>
    private sealed class QueryStoreEnabledTestAppReplayHostFactory : IReplayHostFactory, IAsyncDisposable
    {
        private readonly TestAppReplayHostFactory _inner = new();
        private readonly ReplayLaunchOptions _launchOptions;
        private RetainedReplayHost? _retainedHost;

        public QueryStoreEnabledTestAppReplayHostFactory(ReplayLaunchOptions launchOptions)
        {
            _launchOptions = launchOptions;
        }

        public string? ApplicationConnectionString { get; private set; }

        public HttpClient Client => _retainedHost?.Client
            ?? throw new InvalidOperationException("The retained Sqloom replay host is not available.");

        public async Task<IReplayHost> CreateAsync(
            ReplayLaunchOptions? launchOptions = null,
            CancellationToken cancellationToken = default)
        {
            var effectiveLaunchOptions = launchOptions is null
                || string.IsNullOrWhiteSpace(launchOptions.SqlServerDacpacPath)
                    ? _launchOptions
                    : launchOptions;
            var replayHost = await _inner
                .CreateAsync(effectiveLaunchOptions, cancellationToken)
                .ConfigureAwait(false);
            var configuration = replayHost.Services.GetRequiredService<IConfiguration>();
            ApplicationConnectionString = configuration[DefaultConnectionKey]
                ?? throw new InvalidOperationException("Missing sample replay application connection string.");
            await EnableQueryStoreAsync(ApplicationConnectionString, cancellationToken).ConfigureAwait(false);
            _retainedHost = new RetainedReplayHost(replayHost);
            return _retainedHost;
        }

        public async ValueTask DisposeAsync()
        {
            if (_retainedHost is not null)
            {
                await _retainedHost.DisposeInnerAsync().ConfigureAwait(false);
                _retainedHost = null;
            }
        }

        private sealed class RetainedReplayHost : IReplayHost
        {
            private readonly IReplayHost _inner;

            public RetainedReplayHost(IReplayHost inner)
            {
                _inner = inner;
            }

            public HttpClient Client => _inner.Client;

            public IServiceProvider Services => _inner.Services;

            public ReplayBootstrapReport Bootstrap => _inner.Bootstrap;

            public Task<PreparedReplayOperation> PrepareOperationAsync(
                ResolvedReplayOperation operation,
                CancellationToken cancellationToken = default)
            {
                return _inner.PrepareOperationAsync(operation, cancellationToken);
            }

            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }

            public ValueTask DisposeInnerAsync()
            {
                return _inner.DisposeAsync();
            }
        }
    }

    private sealed class FakeOpenAiServer : IAsyncDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly string _responseBody;
        private readonly Task _serverTask;
        private readonly TcpListener _tcpListener;
        private readonly List<string> _requestPaths = [];

        private FakeOpenAiServer(
            TcpListener tcpListener,
            string responseBody)
        {
            _tcpListener = tcpListener;
            _responseBody = responseBody;
            _serverTask = RunAsync(_cancellationTokenSource.Token);
        }

        public Uri BaseUrl { get; private init; } = null!;

        public IReadOnlyList<string> RequestPaths => _requestPaths;

        public static Task<FakeOpenAiServer> StartAsync(string responseBody)
        {
            TcpListener tcpListener = new(IPAddress.Loopback, 0);
            tcpListener.Start();
            var endpoint = (IPEndPoint)tcpListener.LocalEndpoint;
            return Task.FromResult(new FakeOpenAiServer(tcpListener, responseBody)
            {
                BaseUrl = new Uri($"http://127.0.0.1:{endpoint.Port}/", UriKind.Absolute),
            });
        }

        public async ValueTask DisposeAsync()
        {
            _cancellationTokenSource.Cancel();
            _tcpListener.Stop();

            try
            {
                await _serverTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _cancellationTokenSource.Dispose();
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _tcpListener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                using (client)
                {
                    await HandleClientAsync(client, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task HandleClientAsync(
            TcpClient client,
            CancellationToken cancellationToken)
        {
            var stream = client.GetStream();
            var request = await ReadRequestAsync(stream, cancellationToken).ConfigureAwait(false);
            lock (_requestPaths)
            {
                _requestPaths.Add(request.Path);
            }

            var responseBytes = Encoding.UTF8.GetBytes(_responseBody);
            var responseHeader = string.Join(
                "\r\n",
                "HTTP/1.1 200 OK",
                "Content-Type: application/json",
                $"Content-Length: {responseBytes.Length}",
                "Connection: close",
                string.Empty,
                string.Empty);
            var responseHeaderBytes = Encoding.ASCII.GetBytes(responseHeader);
            await stream.WriteAsync(responseHeaderBytes, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(responseBytes, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task<FakeHttpRequest> ReadRequestAsync(
            NetworkStream stream,
            CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[4096];
            using MemoryStream requestBytes = new();
            var headerEndIndex = -1;

            while (headerEndIndex < 0)
            {
                var read = await stream
                    .ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                    .ConfigureAwait(false);
                if (read == 0)
                {
                    throw new InvalidOperationException("The fake OpenAI server received an incomplete HTTP request.");
                }

                requestBytes.Write(buffer, 0, read);
                headerEndIndex = FindHeaderEndIndex(requestBytes.GetBuffer(), (int)requestBytes.Length);
            }

            var requestBuffer = requestBytes.GetBuffer();
            var headerText = Encoding.ASCII.GetString(requestBuffer, 0, headerEndIndex);
            var requestLine = headerText
                .Split(["\r\n"], StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("The fake OpenAI server received an HTTP request without a request line.");
            var requestLineParts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var path = requestLineParts.Length >= 2
                ? requestLineParts[1]
                : string.Empty;
            var contentLength = ParseContentLength(headerText);
            var bodyOffset = headerEndIndex;
            var bodyBytesRemaining = contentLength - ((int)requestBytes.Length - bodyOffset);
            while (bodyBytesRemaining > 0)
            {
                var read = await stream
                    .ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, bodyBytesRemaining)), cancellationToken)
                    .ConfigureAwait(false);
                if (read == 0)
                {
                    throw new InvalidOperationException("The fake OpenAI server received a truncated HTTP request body.");
                }

                requestBytes.Write(buffer, 0, read);
                bodyBytesRemaining -= read;
            }

            return new FakeHttpRequest(path);
        }

        private static int FindHeaderEndIndex(
            byte[] buffer,
            int length)
        {
            for (var index = 3; index < length; index++)
            {
                if (buffer[index - 3] == '\r'
                    && buffer[index - 2] == '\n'
                    && buffer[index - 1] == '\r'
                    && buffer[index] == '\n')
                {
                    return index + 1;
                }
            }

            return -1;
        }

        private static int ParseContentLength(string headerText)
        {
            foreach (var line in headerText.Split(["\r\n"], StringSplitOptions.RemoveEmptyEntries))
            {
                const string contentLengthPrefix = "Content-Length:";
                if (!line.StartsWith(contentLengthPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (int.TryParse(line[contentLengthPrefix.Length..].Trim(), out var contentLength))
                {
                    return contentLength;
                }

                break;
            }

            return 0;
        }

        private sealed record FakeHttpRequest(string Path);
    }

    private sealed record HostRuntimeCommandResult(
        int ExitCode,
        string StdOut,
        string StdErr);
}
