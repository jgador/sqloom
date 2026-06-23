using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Sqloom.AspNetCore.Endpoints;
using Sqloom.Core.Artifacts;
using Sqloom.Core.Execution;
using Sqloom.Correlation.QueryStore;
using Sqloom.QueryStore.QueryStore;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises Sqloom advice command.
/// </summary>
public sealed class AdviceCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WithOpenAIModelProvider_WritesAdviceAndSqlProposalSidecars()
    {
        var replayArtifactDirectory = CreateTempDirectory();
        var correlationPath = ArtifactLayout.GetCorrelationPath(replayArtifactDirectory);
        var advicePath = ArtifactLayout.GetReplayTuningAdvicePath(replayArtifactDirectory);
        var proposalJsonPath = ArtifactLayout.GetSqlProposalPath(replayArtifactDirectory);
        var proposalScriptPath = ArtifactLayout.GetSqlProposalScriptPath(replayArtifactDirectory);
        var schemaPath = Path.Combine(replayArtifactDirectory, "schema.sql");
        var correlationReport = CreateCorrelationReport(replayArtifactDirectory);
        var expectedReport = CreateAdviceReport(
            replayArtifactDirectory,
            correlationPath,
            proposalJsonPath,
            proposalScriptPath);
        OpenAIAdviceOptions? resolvedOptions = null;
        string? resolvedSchemaPath = null;

        await JsonFileWriter.WriteAsync(
                correlationPath,
                correlationReport,
                static serializerOptions => serializerOptions.Converters.Add(new JsonStringEnumConverter()))
            ;
        await File.WriteAllTextAsync(
                schemaPath,
                """
                CREATE TABLE [dbo].[ExpenseRecord] (
                    [Id] INT NOT NULL,
                    [UserId] UNIQUEIDENTIFIER NOT NULL,
                    [OccurredAtLocal] DATETIME2 NOT NULL
                );
                GO
                """)
            ;

        AdviceCommand command = new(options =>
        {
            resolvedOptions = options;
            return new FakeAdviceReportGenerator(
                expectedReport,
                path => resolvedSchemaPath = path);
        });

        var result = await command
            .ExecuteAsync(
                new AdviseArguments
                {
                    ReplayArtifactDir = replayArtifactDirectory,
                    QueryStoreCorrelationPath = correlationPath,
                    SchemaPath = schemaPath,
                    JsonOutputPath = advicePath,
                    ModelProvider = ModelProviderKind.OpenAI,
                    OpenAIOptions = new OpenAIAdviceOptions
                    {
                        ApiKey = "sqloom-openai-key",
                        BaseUrl = "https://api.openai.com",
                        Model = "gpt-5.4-mini",
                    },
                })
            ;

        Assert.NotNull(resolvedOptions);
        Assert.Equal("sqloom-openai-key", resolvedOptions!.ApiKey);
        Assert.Equal("https://api.openai.com", resolvedOptions.BaseUrl);
        Assert.Equal("gpt-5.4-mini", resolvedOptions.Model);
        Assert.Equal(schemaPath, resolvedSchemaPath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(advicePath, result.JsonOutputPath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(proposalJsonPath, result.Report.SqlProposalJsonPath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(proposalScriptPath, result.Report.SqlProposalScriptPath, StringComparer.OrdinalIgnoreCase);

        Assert.True(File.Exists(advicePath), $"Expected advice artifact at '{advicePath}'.");
        Assert.True(File.Exists(proposalJsonPath), $"Expected SQL proposal artifact at '{proposalJsonPath}'.");
        Assert.True(File.Exists(proposalScriptPath), $"Expected SQL proposal script at '{proposalScriptPath}'.");

        var writtenAdviceReport = Assert.IsType<AdviceReport>(
            await JsonFileReader
                .ReadAsync<AdviceReport>(advicePath)
                );
        var writtenProposalReport = Assert.IsType<SqlTuningProposalReport>(
            await JsonFileReader
                .ReadAsync<SqlTuningProposalReport>(proposalJsonPath)
                );
        var proposalScript = await File
            .ReadAllTextAsync(proposalScriptPath)
            ;

        Assert.Equal(proposalJsonPath, writtenAdviceReport.SqlProposalJsonPath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(proposalScriptPath, writtenAdviceReport.SqlProposalScriptPath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("openai", writtenAdviceReport.ModelProvider);
        Assert.Equal("gpt-5.4-mini", writtenAdviceReport.ModelName);

        var adviceOperation = Assert.Single(writtenAdviceReport.Operations);
        Assert.NotEmpty(adviceOperation.Recommendations);
        Assert.NotEmpty(adviceOperation.Proposals);
        Assert.Equal(adviceOperation.Proposals.Count, writtenAdviceReport.Summary.ProposalCount);
        var adviceProposal = Assert.Single(adviceOperation.Proposals);
        Assert.Equal("maintenance_patch", adviceProposal.ProposalKind);
        Assert.Equal("[dbo].[ExpenseRecord]", adviceProposal.TargetObject);
        Assert.Contains(
            writtenAdviceReport.Warnings,
            warning => warning.Contains("did not include rollback SQL", StringComparison.OrdinalIgnoreCase));

        var proposalOperation = Assert.Single(writtenProposalReport.Operations);
        Assert.Equal(advicePath, writtenProposalReport.SourceAdvicePath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(proposalScriptPath, writtenProposalReport.SqlScriptPath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("openai", writtenProposalReport.ModelProvider);
        Assert.NotEmpty(proposalOperation.Proposals);
        Assert.Equal(proposalOperation.Proposals.Count, writtenProposalReport.Summary.ProposalCount);
        Assert.Equal("maintenance_patch", Assert.Single(proposalOperation.Proposals).ProposalKind);
        Assert.Contains(
            writtenProposalReport.Warnings,
            warning => warning.Contains("did not include rollback SQL", StringComparison.OrdinalIgnoreCase));

        Assert.Contains("Sqloom SQL proposals", proposalScript, StringComparison.Ordinal);
        Assert.Contains("Model provider: openai", proposalScript, StringComparison.Ordinal);
        Assert.Contains("-- Kind: maintenance_patch", proposalScript, StringComparison.Ordinal);
        Assert.Contains("UPDATE STATISTICS", proposalScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No rollback SQL was provided by the model.", proposalScript, StringComparison.Ordinal);
        Assert.Contains(adviceOperation.OperationKey, proposalScript, StringComparison.Ordinal);
    }

    private static AdviceReport CreateAdviceReport(
        string replayArtifactDirectory,
        string correlationPath,
        string proposalJsonPath,
        string proposalScriptPath)
    {
        return new AdviceReport
        {
            GeneratedAtUtc = new DateTimeOffset(2026, 6, 14, 0, 0, 0, TimeSpan.Zero),
            AppName = "SqloomTestApp",
            ReplayArtifactDir = replayArtifactDirectory,
            QueryStoreCorrelationPath = correlationPath,
            ModelProvider = "openai",
            ModelName = "gpt-5.4-mini",
            StrategyName = "openai-responses-structured-outputs",
            SqlProposalJsonPath = proposalJsonPath,
            SqlProposalScriptPath = proposalScriptPath,
            Pipeline = new PipelineReport
            {
                Stages =
                [
                    new PipelineStageReport
                    {
                        Name = PipelineStageNames.Advise,
                        Status = PipelineStageStatuses.Completed,
                        Summary = "placeholder",
                    },
                ],
            },
            Summary = new AdviceSummary
            {
                OperationCount = 1,
                RecommendationCount = 1,
                ProposalCount = 1,
            },
            Operations =
            [
                new AdviceOperationReport
                {
                    OperationKey = "GET /api/expenses/dashboard",
                    HttpMethod = "GET",
                    Route = "/api/expenses/dashboard",
                    ReplayStatus = "replayed",
                    CapturedCommandCount = 1,
                    MatchedCommandCount = 1,
                    Recommendations =
                    [
                        new SqlTuningRecommendation
                        {
                            Title = "Reduce dashboard logical reads",
                            RootCause = "Matched Query Store evidence shows elevated logical reads for the dashboard query.",
                            SuggestedChange = "Add a covering index for the dashboard filter shape.",
                            VerificationMetric = "Mean logical reads and duration drop on the next replay.",
                        },
                    ],
                    Proposals =
                    [
                        new SqlTuningProposal
                        {
                            Title = "Refresh ExpenseRecord statistics",
                            Diagnosis = "The dashboard query may benefit from refreshed statistics before deeper tuning.",
                            ProposalKind = "maintenance_patch",
                            TargetObject = "[dbo].[ExpenseRecord]",
                            SqlScript = "UPDATE STATISTICS [dbo].[ExpenseRecord] WITH FULLSCAN;",
                            RollbackSqlScript = string.Empty,
                            ExpectedBenefit = "Refresh cardinality estimates for the dashboard query.",
                            VerificationMetric = "Compare the matched plan shape and logical reads on the next replay.",
                            Confidence = 0.55d,
                            SourceCommandOrdinals = [1],
                            MatchedPlanIds = [20L],
                        },
                    ],
                },
            ],
            Warnings =
            [
                "Operation 'GET /api/expenses/dashboard': SQL proposal 'Refresh ExpenseRecord statistics' did not include rollback SQL. Sqloom persisted the proposal with an empty rollback script.",
            ],
        };
    }

    private static QueryCorrelationReport CreateCorrelationReport(string replayArtifactDirectory)
    {
        return new QueryCorrelationReport
        {
            GeneratedAtUtc = new DateTimeOffset(2026, 6, 14, 0, 0, 0, TimeSpan.Zero),
            AppName = "SqloomTestApp",
            ReplayArtifactDir = replayArtifactDirectory,
            QueryStoreSnapshotPath = Path.Combine(replayArtifactDirectory, "query-store-20260614T000000000Z.json"),
            QueryStoreCapturedAtUtc = new DateTimeOffset(2026, 6, 14, 0, 0, 0, TimeSpan.Zero),
            Records =
            [
                new QueryCorrelationRecord
                {
                    OperationKey = "GET /api/expenses/dashboard",
                    HttpMethod = "GET",
                    Route = "/api/expenses/dashboard",
                    OperationArtifactPath = Path.Combine(replayArtifactDirectory, "operations", "01-expenses-dashboard.json"),
                    CommandOrdinal = 1,
                    CapturedCommand = CreateCommand("SELECT TOP(@p) [e].[Id], [e].[CaptureId], [e].[SourceProposalId], [e].[Amount], [e].[CurrencyCode], [e].[OccurredAtLocal], [e].[Category], [e].[Subcategory], [e].[Merchant], [e].[Note], [e].[ConfirmedAtUtc], [e].[UpdatedAtUtc] FROM [ExpenseRecord] AS [e] WHERE [e].[UserId] = @userId ORDER BY [e].[OccurredAtLocal] DESC, [e].[Id] DESC"),
                    ComparableSqlText = "SELECT TOP(@p) [e].[Id], [e].[CaptureId], [e].[SourceProposalId], [e].[Amount], [e].[CurrencyCode], [e].[OccurredAtLocal], [e].[Category], [e].[Subcategory], [e].[Merchant], [e].[Note], [e].[ConfirmedAtUtc], [e].[UpdatedAtUtc] FROM [ExpenseRecord] AS [e] WHERE [e].[UserId] = @userId ORDER BY [e].[OccurredAtLocal] DESC, [e].[Id] DESC",
                    MatchKind = CorrelationMatchKind.StatementHandleExact,
                    Confidence = 1.0d,
                    MatchedPlans =
                    [
                        CreatePlan(),
                    ],
                    Notes = ["Matched Query Store statement_sql_handle exactly."],
                },
            ],
            Summary = new QueryCorrelationSummary
            {
                OperationCount = 1,
                CapturedCommandCount = 1,
                MatchedCommandCount = 1,
                HandleExactCount = 1,
                QueryTextExactCount = 0,
                FingerprintFallbackCount = 0,
                UnmatchedCount = 0,
                Operations =
                [
                    new OperationCorrelationSummary
                    {
                        OperationKey = "GET /api/expenses/dashboard",
                        HttpMethod = "GET",
                        Route = "/api/expenses/dashboard",
                        ReplayStatus = "replayed",
                        OperationArtifactPath = Path.Combine(replayArtifactDirectory, "operations", "01-expenses-dashboard.json"),
                        CapturedCommandCount = 1,
                        MatchedCommandCount = 1,
                        HandleExactCount = 1,
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

    private static QueryStorePlanRecord CreatePlan()
    {
        return new QueryStorePlanRecord
        {
            QueryId = 10L,
            PlanId = 20L,
            QueryTextId = 10L,
            StatementSqlHandle = "0xAAAA",
            QueryHash = "0x000000000000000A",
            QueryText = "SELECT TOP(@p) [e].[Id], [e].[CaptureId], [e].[SourceProposalId], [e].[Amount], [e].[CurrencyCode], [e].[OccurredAtLocal], [e].[Category], [e].[Subcategory], [e].[Merchant], [e].[Note], [e].[ConfirmedAtUtc], [e].[UpdatedAtUtc] FROM [ExpenseRecord] AS [e] WHERE [e].[UserId] = @userId ORDER BY [e].[OccurredAtLocal] DESC, [e].[Id] DESC",
            ObjectName = "[dbo].[ExpenseRecord]",
            QueryParameterizationType = 0,
            ParamTypeDescription = "None",
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
            Parameters = Array.Empty<CapturedSqlParameter>(),
            Duration = TimeSpan.FromMilliseconds(12),
        };
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "sqloom-advice-command-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private sealed class FakeAdviceReportGenerator(
        AdviceReport report,
        Action<string> captureSchemaPath) : IAdviceReportGenerator
    {
        public Task<AdviceReport> CreateReportAsync(
            QueryCorrelationReport correlationReport,
            string queryStoreCorrelationPath,
            string adviceOutputPath,
            string sqlServerSchemaPath,
            System.Threading.CancellationToken cancellationToken = default)
        {
            captureSchemaPath(sqlServerSchemaPath);
            return Task.FromResult(report);
        }

        public void Dispose()
        {
        }
    }
}
