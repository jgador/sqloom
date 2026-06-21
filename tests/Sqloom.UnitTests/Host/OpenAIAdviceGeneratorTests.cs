using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.AspNetCore.Endpoints;
using Sqloom.Core.Artifacts;
using Sqloom.Core.Execution;
using Sqloom.Correlation.QueryStore;
using Sqloom.QueryStore.QueryStore;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises Sqloom OpenAI advice generator.
/// </summary>
public sealed class OpenAIAdviceGeneratorTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Fact]
    public async Task CreateReportAsync_UsesEvidenceAndSchemaWithoutBaselineHints()
    {
        var replayArtifactDirectory = CreateTempDirectory();
        var correlationPath = Path.Combine(replayArtifactDirectory, "query-store-correlation.json");
        var advicePath = Path.Combine(replayArtifactDirectory, "tuning-advice.json");
        var correlationReport = CreateCorrelationReport(replayArtifactDirectory);
        await WriteReplayEvidenceAsync(correlationReport);
        var schemaPath = CreateSchemaFile(
            replayArtifactDirectory,
            includeUserId: true);
        var capturedRequestBody = string.Empty;
        var responseBody = JsonSerializer.Serialize(new
        {
            output_text = JsonSerializer.Serialize(new
            {
                recommendations = new[]
                {
                    new
                    {
                        title = "Reduce dashboard logical reads",
                        rootCause = "Matched Query Store evidence shows the dashboard operation reads too many rows for the current predicate shape.",
                        suggestedChange = "Add a covering nonclustered index that matches the replayed filter and projected columns.",
                        verificationMetric = "Mean logical reads and mean duration drop for the matched plan on the next replay."
                    }
                },
                proposals = new[]
                {
                    new
                    {
                        title = "Add index on [dbo].[ExpenseRecord] for GET /api/expenses/dashboard",
                        diagnosis = "The matched dashboard query filters ExpenseRecord by UserId and the matched plan shows elevated reads.",
                        proposalKind = "nonclustered_index",
                        targetObject = "[dbo].[ExpenseRecord]",
                        sqlScript = "CREATE INDEX [IX_Sqloom_ExpenseRecord_UserId] ON [dbo].[ExpenseRecord] ([UserId] ASC) INCLUDE ([Id], [OccurredAtLocal]);",
                        rollbackSqlScript = "DROP INDEX [IX_Sqloom_ExpenseRecord_UserId] ON [dbo].[ExpenseRecord];",
                        expectedBenefit = "Reduce logical reads for the matched dashboard query.",
                        verificationMetric = "Mean logical reads drop for the matched plan on the next replay.",
                        confidence = 0.9d,
                        sourceCommandOrdinals = new[] { 1 },
                        matchedPlanIds = new[] { 20L },
                    },
                }
            })
        });
        using var handler = new FakeHttpMessageHandler(async request =>
        {
            capturedRequestBody = await request.Content!
                .ReadAsStringAsync()
                .ConfigureAwait(false);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    responseBody,
                    Encoding.UTF8,
                    "application/json"),
            };
        });
        using HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://api.openai.com/"),
        };

        using OpenAIAdviceGenerator generator = new(
            new OpenAIAdviceOptions
            {
                ApiKey = "sqloom-openai-key",
                BaseUrl = "https://api.openai.com",
                Model = "gpt-5.4-mini",
            },
            httpClient);

        var report = await generator
            .CreateReportAsync(
                correlationReport,
                correlationPath,
                advicePath,
                schemaPath);

        Assert.Equal("openai", report.ModelProvider);
        Assert.Equal("gpt-5.4-mini", report.ModelName);
        Assert.Equal("openai-responses-structured-outputs", report.StrategyName);
        Assert.Equal(
            ArtifactLayout.GetReplaySqlTuningProposalPath(correlationReport.ReplayArtifactDirectory),
            report.SqlProposalJsonPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            ArtifactLayout.GetReplaySqlTuningProposalScriptPath(correlationReport.ReplayArtifactDirectory),
            report.SqlProposalScriptPath,
            StringComparer.OrdinalIgnoreCase);

        var operation = Assert.Single(report.Operations);
        var recommendation = Assert.Single(operation.Recommendations);
        var proposal = Assert.Single(operation.Proposals);
        Assert.Equal("Reduce dashboard logical reads", recommendation.Title);
        Assert.Equal(operation.Recommendations.Count, report.Summary.RecommendationCount);
        Assert.Equal(operation.Proposals.Count, report.Summary.ProposalCount);
        Assert.Contains("Add index on [dbo].[ExpenseRecord]", proposal.Title, StringComparison.Ordinal);
        Assert.Equal("nonclustered_index", proposal.ProposalKind);
        Assert.Equal("[dbo].[ExpenseRecord]", proposal.TargetObject);
        Assert.Empty(report.Warnings);
        Assert.Contains("artifact_manifest_json:", capturedRequestBody, StringComparison.Ordinal);
        Assert.Contains("source_evidence_json:", capturedRequestBody, StringComparison.Ordinal);
        Assert.Contains("sql_server_schema_sql:", capturedRequestBody, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE [dbo].[ExpenseRecord]", capturedRequestBody, StringComparison.Ordinal);
        Assert.Contains("GET /api/expenses/dashboard", capturedRequestBody, StringComparison.Ordinal);
        AssertRequestSchemaRequiresNullableRollbackSqlScript(capturedRequestBody);
        Assert.DoesNotContain("set proposalKind to exactly", capturedRequestBody, StringComparison.Ordinal);
        Assert.DoesNotContain("\"enum\":[\"CreateIndex\"]", capturedRequestBody, StringComparison.Ordinal);
        Assert.DoesNotContain("Prefer nonclustered index proposals", capturedRequestBody, StringComparison.Ordinal);
        Assert.DoesNotContain("Baseline recommendations:", capturedRequestBody, StringComparison.Ordinal);
        Assert.DoesNotContain("Baseline SQL proposal candidates:", capturedRequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateReportAsync_PersistsFreeFormProposalWithoutRollbackAndAddsWarning()
    {
        var replayArtifactDirectory = CreateTempDirectory();
        var correlationPath = Path.Combine(replayArtifactDirectory, "query-store-correlation.json");
        var advicePath = Path.Combine(replayArtifactDirectory, "tuning-advice.json");
        var correlationReport = CreateCorrelationReport(replayArtifactDirectory);
        await WriteReplayEvidenceAsync(correlationReport);
        var schemaPath = CreateSchemaFile(
            replayArtifactDirectory,
            includeUserId: true);
        var responseBody = JsonSerializer.Serialize(new
        {
            output_text = JsonSerializer.Serialize(new
            {
                recommendations = new[]
                {
                    new
                    {
                        title = "Reduce dashboard logical reads",
                        rootCause = "Matched Query Store evidence shows the dashboard operation reads too many rows for the current predicate shape.",
                        suggestedChange = "Add a covering nonclustered index that matches the replayed filter and projected columns.",
                        verificationMetric = "Mean logical reads and mean duration drop for the matched plan on the next replay."
                    }
                },
                proposals = new[]
                {
                    new
                    {
                        title = "Refresh ExpenseRecord statistics",
                        diagnosis = "The matched dashboard query shows enough read variance to justify a manual statistics refresh proposal.",
                        proposalKind = "custom_sql_patch",
                        targetObject = "[dbo].[ExpenseRecord]",
                        sqlScript = "UPDATE STATISTICS [dbo].[ExpenseRecord] WITH FULLSCAN;",
                        rollbackSqlScript = (string?)null,
                        expectedBenefit = "Refresh cardinality estimates for the matched dashboard query.",
                        verificationMetric = "Compare the matched dashboard plan shape and logical reads after the next replay.",
                        confidence = 0.55d,
                        sourceCommandOrdinals = new[] { 1 },
                        matchedPlanIds = new[] { 20L },
                    },
                }
            })
        });
        using var handler = new FakeHttpMessageHandler(_ =>
        {
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        responseBody,
                        Encoding.UTF8,
                        "application/json"),
                });
        });
        using HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://api.openai.com/"),
        };

        using OpenAIAdviceGenerator generator = new(
            new OpenAIAdviceOptions
            {
                ApiKey = "sqloom-openai-key",
                BaseUrl = "https://api.openai.com",
                Model = "gpt-5.4-mini",
            },
            httpClient);

        var report = await generator
            .CreateReportAsync(
                correlationReport,
                correlationPath,
                advicePath,
                schemaPath);

        var operation = Assert.Single(report.Operations);
        var proposal = Assert.Single(operation.Proposals);
        Assert.Equal(1, report.Summary.ProposalCount);
        Assert.Equal("custom_sql_patch", proposal.ProposalKind);
        Assert.Equal("[dbo].[ExpenseRecord]", proposal.TargetObject);
        Assert.Equal(string.Empty, proposal.RollbackSqlScript);
        Assert.Contains(
            report.Warnings,
            warning => warning.Contains("did not include rollback SQL", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            report.Warnings,
            warning => warning.Contains("Dropped SQL proposal", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateReportAsync_WithDebugWriter_PrintsRedactedRequestAndResponse()
    {
        var replayArtifactDirectory = CreateTempDirectory();
        var correlationPath = Path.Combine(replayArtifactDirectory, "query-store-correlation.json");
        var advicePath = Path.Combine(replayArtifactDirectory, "tuning-advice.json");
        var correlationReport = CreateCorrelationReport(replayArtifactDirectory);
        await WriteReplayEvidenceAsync(correlationReport);
        var schemaPath = CreateSchemaFile(
            replayArtifactDirectory,
            includeUserId: true);
        var responseBody = JsonSerializer.Serialize(new
        {
            output_text = JsonSerializer.Serialize(new
            {
                recommendations = new[]
                {
                    new
                    {
                        title = "Reduce dashboard logical reads",
                        rootCause = "Matched Query Store evidence shows the dashboard operation reads too many rows for the current predicate shape.",
                        suggestedChange = "Review the matched dashboard query for missing covering indexes or unnecessary projected columns.",
                        verificationMetric = "Mean logical reads and mean duration drop for the matched plan on the next replay."
                    }
                },
                proposals = Array.Empty<object>(),
            })
        });
        using var handler = new FakeHttpMessageHandler(_ =>
        {
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        responseBody,
                        Encoding.UTF8,
                        "application/json"),
                });
        });
        using HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://api.openai.com/"),
        };
        HostDebugWriter debugWriter = new(isEnabled: true);

        using OpenAIAdviceGenerator generator = new(
            new OpenAIAdviceOptions
            {
                ApiKey = "openai-live-secret",
                BaseUrl = "https://api.openai.com",
                Model = "gpt-5.4-mini",
            },
            httpClient,
            debugWriter);

        var result = await CaptureStandardErrorAsync(async () =>
        {
            return await generator
                .CreateReportAsync(
                    correlationReport,
                    correlationPath,
                    advicePath,
                    schemaPath)
                .ConfigureAwait(true);
        });

        Assert.Equal("openai", result.Result.ModelProvider);
        Assert.Contains("[sqloom debug] [advise] OpenAI request", result.StandardError, StringComparison.Ordinal);
        Assert.Contains("[sqloom debug] [advise] OpenAI response", result.StandardError, StringComparison.Ordinal);
        Assert.Contains("authorization=Bearer ***REDACTED***", result.StandardError, StringComparison.Ordinal);
        Assert.Contains("\"model\": \"gpt-5.4-mini\"", result.StandardError, StringComparison.Ordinal);
        Assert.Contains("\"instructions\": |", result.StandardError, StringComparison.Ordinal);
        Assert.Contains("artifact_manifest_json", result.StandardError, StringComparison.Ordinal);
        Assert.Contains("\"output_text\":", result.StandardError, StringComparison.Ordinal);
        Assert.Contains("\"recommendations\":", result.StandardError, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u0022", result.StandardError, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u0027", result.StandardError, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u003e", result.StandardError, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\\r\\n", result.StandardError, StringComparison.Ordinal);
        Assert.DoesNotContain("openai-live-secret", result.StandardError, StringComparison.Ordinal);
    }

    private static QueryStoreCorrelationReport CreateCorrelationReport(string replayArtifactDirectory)
    {
        return new QueryStoreCorrelationReport
        {
            GeneratedAtUtc = new DateTimeOffset(2026, 6, 14, 0, 0, 0, TimeSpan.Zero),
            AppName = "Talio",
            ReplayArtifactDirectory = replayArtifactDirectory,
            QueryStoreSnapshotPath = Path.Combine(replayArtifactDirectory, "query-store-snapshot.json"),
            QueryStoreCapturedAtUtc = new DateTimeOffset(2026, 6, 14, 0, 0, 0, TimeSpan.Zero),
            Records =
            [
                new QueryStoreCorrelationRecord
                {
                    OperationKey = "GET /api/expenses/dashboard",
                    HttpMethod = "GET",
                    Route = "/api/expenses/dashboard",
                    OperationArtifactPath = Path.Combine(replayArtifactDirectory, "operations", "01-expenses-dashboard.json"),
                    CommandOrdinal = 1,
                    CapturedCommand = CreateCommand("SELECT [e].[Id], [e].[OccurredAtLocal] FROM [dbo].[ExpenseRecord] AS [e] WHERE [e].[UserId] = @p0 ORDER BY [e].[OccurredAtLocal] DESC"),
                    ComparableSqlText = "SELECT [e].[Id], [e].[OccurredAtLocal] FROM [dbo].[ExpenseRecord] AS [e] WHERE [e].[UserId] = @p0 ORDER BY [e].[OccurredAtLocal] DESC",
                    MatchKind = QueryStoreCorrelationMatchKind.StatementHandleExact,
                    Confidence = 1.0d,
                    MatchedPlans =
                    [
                        CreatePlan(),
                    ],
                    Notes = ["Matched Query Store statement_sql_handle exactly."],
                },
            ],
            Summary = new QueryStoreCorrelationSummary
            {
                OperationCount = 1,
                CapturedCommandCount = 1,
                MatchedCommandCount = 1,
                StatementHandleExactCount = 1,
                QueryTextExactCount = 0,
                FingerprintFallbackCount = 0,
                UnmatchedCount = 0,
                Operations =
                [
                    new QueryStoreCorrelationOperationSummary
                    {
                        OperationKey = "GET /api/expenses/dashboard",
                        HttpMethod = "GET",
                        Route = "/api/expenses/dashboard",
                        ReplayStatus = "replayed",
                        OperationArtifactPath = Path.Combine(replayArtifactDirectory, "operations", "01-expenses-dashboard.json"),
                        CapturedCommandCount = 1,
                        MatchedCommandCount = 1,
                        StatementHandleExactCount = 1,
                        QueryTextExactCount = 0,
                        FingerprintFallbackCount = 0,
                        UnmatchedCount = 0,
                        MatchedQueryIds = [10L],
                        MatchedPlanIds = [20L],
                    },
                ],
            },
            Pipeline = new PipelineReport
            {
                Stages =
                [
                    new PipelineStageReport
                    {
                        Name = PipelineStageNames.Correlate,
                        Status = PipelineStageStatuses.Completed,
                        Summary = "placeholder",
                    },
                ],
            },
        };
    }

    private static async Task WriteReplayEvidenceAsync(QueryStoreCorrelationReport correlationReport)
    {
        var operation = correlationReport.Summary.Operations[0];
        var operationDirectory = Path.GetDirectoryName(operation.OperationArtifactPath)
            ?? throw new InvalidOperationException("Missing operation artifact directory.");
        Directory.CreateDirectory(operationDirectory);

        await JsonFileWriter.WriteAsync(
                operation.OperationArtifactPath,
                new EndpointReplayResult
                {
                    OperationKey = operation.OperationKey,
                    HttpMethod = operation.HttpMethod,
                    Route = operation.Route,
                    Status = operation.ReplayStatus,
                    HttpStatusCode = 200,
                    DurationMilliseconds = 18.5d,
                    Request = new EndpointReplayRequest
                    {
                        OperationKey = operation.OperationKey,
                        HttpMethod = operation.HttpMethod,
                        Route = operation.Route,
                        RelativePathAndQuery = "/api/expenses/dashboard",
                    },
                    CapturedSqlCommands =
                    [
                        CreateCommand("SELECT [e].[Id], [e].[OccurredAtLocal] FROM [dbo].[ExpenseRecord] AS [e] WHERE [e].[UserId] = @p0 ORDER BY [e].[OccurredAtLocal] DESC"),
                    ],
                    ArtifactPath = operation.OperationArtifactPath,
                })
            .ConfigureAwait(false);
        await JsonFileWriter.WriteAsync(
                correlationReport.QueryStoreSnapshotPath!,
                new QueryStoreSnapshot
                {
                    CapturedAtUtc = correlationReport.QueryStoreCapturedAtUtc,
                    LookbackWindow = TimeSpan.FromHours(1),
                    DatabaseOptions = new QueryStoreDatabaseOptions
                    {
                        DesiredState = "READ_WRITE",
                        ActualState = "READ_WRITE",
                        ReadOnlyReason = 0,
                        CurrentStorageSizeMb = 32,
                        MaxStorageSizeMb = 1024,
                    },
                    WorkloadProfileName = "Talio",
                    DiscoveredObjectCatalog = new DiscoveredDatabaseObjectCatalog
                    {
                        CapturedAtUtc = correlationReport.QueryStoreCapturedAtUtc,
                        SourceName = "unit-test",
                        IsComplete = true,
                        Warnings = [],
                        Objects =
                        [
                            new DiscoveredDatabaseObject
                            {
                                SchemaName = "dbo",
                                ObjectName = "ExpenseRecord",
                                FullyQualifiedName = "[dbo].[ExpenseRecord]",
                                Kind = DiscoveredDatabaseObjectKind.Table,
                            },
                        ],
                    },
                    Plans =
                    [
                        CreatePlan(),
                    ],
                    Waits =
                    [
                        new QueryStoreWaitStat
                        {
                            QueryId = 10L,
                            PlanId = 20L,
                            WaitCategory = "CPU",
                            AverageQueryWaitMilliseconds = 3.5d,
                            TotalWaitMilliseconds = 14d,
                        },
                    ],
                },
                static serializerOptions => serializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .ConfigureAwait(false);
    }

    private static string CreateSchemaFile(
        string directory,
        bool includeUserId)
    {
        var userIdLine = includeUserId
            ? "    [UserId] UNIQUEIDENTIFIER NOT NULL,\r\n"
            : string.Empty;
        var path = Path.Combine(directory, "schema.sql");
        File.WriteAllText(
            path,
            $"""
            CREATE TABLE [dbo].[ExpenseRecord] (
                [Id] INT NOT NULL,
            {userIdLine}    [OccurredAtLocal] DATETIME2 NOT NULL
            );
            GO
            """);
        return path;
    }

    private static QueryStorePlanRecord CreatePlan()
    {
        return new QueryStorePlanRecord
        {
            QueryId = 10L,
            PlanId = 20L,
            QueryTextId = 10L,
            StatementSqlHandle = "0xAAAA",
            QueryHash = "0x000000000000000A",
            QueryText = "SELECT [e].[Id], [e].[OccurredAtLocal] FROM [dbo].[ExpenseRecord] AS [e] WHERE [e].[UserId] = @p0 ORDER BY [e].[OccurredAtLocal] DESC",
            ObjectName = "[dbo].[ExpenseRecord]",
            QueryParameterizationType = 0,
            QueryParameterizationTypeDescription = "None",
            ExecutionCount = 4,
            MeanDuration = TimeSpan.FromMilliseconds(240),
            MaxDuration = TimeSpan.FromMilliseconds(300),
            MeanCpuMilliseconds = 120,
            MeanLogicalReads = 2400,
            LastExecutionTimeUtc = new DateTimeOffset(2026, 6, 14, 0, 0, 0, TimeSpan.Zero),
        };
    }

    private static CapturedSqlCommand CreateCommand(string sqlText)
    {
        return new CapturedSqlCommand
        {
            SourceKind = CapturedSqlSourceKind.EntityFramework,
            Source = "EntityFrameworkCore",
            CommandText = sqlText,
            NormalizedCommandText = sqlText,
            Fingerprint = "fingerprint",
            Parameters =
            [
                new CapturedSqlParameter
                {
                    Name = "@p0",
                    DbType = "uniqueidentifier",
                    Value = "00000000-0000-0000-0000-000000000001",
                },
            ],
            Duration = TimeSpan.FromMilliseconds(12),
        };
    }

    /// <summary>
    /// Fakes OpenAI HTTP responses for advice-generator tests.
    /// </summary>
    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return handler(request);
        }
    }

    private static async Task<StandardErrorCaptureResult<T>> CaptureStandardErrorAsync<T>(Func<Task<T>> action)
    {
        await ConsoleGate.WaitAsync().ConfigureAwait(false);
        var originalError = Console.Error;
        using StringWriter standardError = new();

        try
        {
            Console.SetError(standardError);
            var result = await action().ConfigureAwait(false);
            return new StandardErrorCaptureResult<T>(
                result,
                standardError.ToString());
        }
        finally
        {
            Console.SetError(originalError);
            ConsoleGate.Release();
        }
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "sqloom-openai-advice-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void AssertRequestSchemaRequiresNullableRollbackSqlScript(string capturedRequestBody)
    {
        using JsonDocument document = JsonDocument.Parse(capturedRequestBody);
        var proposalItems = document.RootElement
            .GetProperty("text")
            .GetProperty("format")
            .GetProperty("schema")
            .GetProperty("properties")
            .GetProperty("proposals")
            .GetProperty("items");
        var rollbackSchema = proposalItems
            .GetProperty("properties")
            .GetProperty("rollbackSqlScript");
        var rollbackTypes = rollbackSchema
            .GetProperty("type")
            .EnumerateArray()
            .Select(static value => value.GetString())
            .ToArray();
        var requiredProperties = proposalItems
            .GetProperty("required")
            .EnumerateArray()
            .Select(static value => value.GetString())
            .ToArray();

        Assert.Contains("string", rollbackTypes, StringComparer.Ordinal);
        Assert.Contains("null", rollbackTypes, StringComparer.Ordinal);
        Assert.Contains("rollbackSqlScript", requiredProperties, StringComparer.Ordinal);
    }

    private sealed record StandardErrorCaptureResult<T>(
        T Result,
        string StandardError);
}
