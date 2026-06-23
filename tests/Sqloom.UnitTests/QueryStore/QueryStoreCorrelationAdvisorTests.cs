using System;
using System.IO;
using Sqloom.Core.Execution;
using Sqloom.Core.QueryStore;
using Sqloom.Host.QueryStore;
using Xunit;

namespace Sqloom.Host.Tests.QueryStore;

/// <summary>
/// Exercises Query Store correlation advisor.
/// </summary>
public sealed class QueryStoreCorrelationAdvisorTests
{
    private static string ReplayArtifactsPath =>
        Path.Combine("artifacts", "sqloom", "replay-20260609T000000000Z");

    private static string QueryStoreSnapshotPath =>
        Path.Combine("artifacts", "sqloom", "query-store-20260609T000000000Z.json");

    private static string OperationArtifactPath =>
        Path.Combine("artifacts", "sqloom", "operations", "01-expenses-dashboard.json");

    private static string QueryStoreCorrelationPath =>
        Path.Combine("artifacts", "sqloom", "query-store-correlation.json");

    private static string TuningAdvicePath =>
        Path.Combine("artifacts", "sqloom", "tuning-advice.json");

    [Fact]
    public void CreateReport_EmitsDeterministicRecommendationsForMatchedHotspots()
    {
        const string matchedHotspotSql = """
            SELECT [e].[ExpenseRecordId], [e].[UserId], [e].[OccurredAtUtc], [e].[Amount]
            FROM [ExpenseRecord] AS [e]
            WHERE [e].[UserId] = @__userId_0 AND [e].[OccurredAtUtc] >= @__fromUtc_1
            ORDER BY [e].[Amount] DESC
            """;

        QueryStoreCorrelationAdvisor advisor = new();
        QueryCorrelationReport correlationReport = new()
        {
            GeneratedAtUtc = new DateTimeOffset(2026, 6, 9, 0, 0, 0, TimeSpan.Zero),
            AppName = "TestHarness",
            ReplayArtifactDir = ReplayArtifactsPath,
            QueryStoreSnapshotPath = QueryStoreSnapshotPath,
            QueryStoreCapturedAtUtc = new DateTimeOffset(2026, 6, 9, 0, 0, 0, TimeSpan.Zero),
            Records =
            [
                new QueryCorrelationRecord
                {
                    OperationKey = "GET /api/expenses/dashboard",
                    HttpMethod = "GET",
                    Route = "/api/expenses/dashboard",
                    OperationArtifactPath = OperationArtifactPath,
                    CommandOrdinal = 1,
                    CapturedCommand = CreateCommand(matchedHotspotSql),
                    ComparableSqlText = matchedHotspotSql,
                    MatchKind = CorrelationMatchKind.FingerprintFallback,
                    Confidence = 0.40d,
                    MatchedPlans =
                    [
                        CreatePlan(10L, 20L, 450d, 175d, 8000d, matchedHotspotSql),
                        CreatePlan(10L, 21L, 220d, 110d, 4200d, matchedHotspotSql),
                    ],
                    Notes = ["Matched only by local fingerprint."],
                },
            ],
            Summary = new QueryCorrelationSummary
            {
                OperationCount = 1,
                CapturedCommandCount = 1,
                MatchedCommandCount = 1,
                HandleExactCount = 0,
                QueryTextExactCount = 0,
                FingerprintFallbackCount = 1,
                UnmatchedCount = 0,
                Operations =
                [
                    new OperationCorrelationSummary
                    {
                        OperationKey = "GET /api/expenses/dashboard",
                        HttpMethod = "GET",
                        Route = "/api/expenses/dashboard",
                        ReplayStatus = "replayed",
                        OperationArtifactPath = OperationArtifactPath,
                        CapturedCommandCount = 1,
                        MatchedCommandCount = 1,
                        HandleExactCount = 0,
                        QueryTextExactCount = 0,
                        FingerprintFallbackCount = 1,
                        UnmatchedCount = 0,
                        MatchedQueryIds = [10L],
                        MatchedPlanIds = [20L, 21L],
                    },
                ],
            },
            Pipeline = CreatePlaceholderPipeline(),
        };

        var report = advisor.CreateReport(
            correlationReport,
            QueryStoreCorrelationPath,
            TuningAdvicePath);

        Assert.Equal("TestHarness", report.AppName);
        Assert.Equal(1, report.Summary.OperationCount);
        Assert.Equal(3, report.Summary.RecommendationCount);
        Assert.Equal(
            Path.Combine(correlationReport.ReplayArtifactDir, "sql-tuning-proposal.json"),
            report.SqlProposalJsonPath);
        Assert.Equal(
            Path.Combine(correlationReport.ReplayArtifactDir, "sql-tuning-proposal.sql"),
            report.SqlProposalScriptPath);

        var operation = Assert.Single(report.Operations);
        Assert.Equal(operation.Proposals.Count, report.Summary.ProposalCount);
        Assert.Equal(0, report.Summary.ProposalCount);
        Assert.Contains(
            operation.Recommendations,
            static recommendation => recommendation.Title.Contains("Replace fingerprint fallback", StringComparison.Ordinal));
        Assert.Contains(
            operation.Recommendations,
            static recommendation => recommendation.Title.Contains("Reduce query cost", StringComparison.Ordinal));
        Assert.Contains(
            operation.Recommendations,
            static recommendation => recommendation.Title.Contains("Investigate plan instability", StringComparison.Ordinal));
        Assert.Contains(
            report.Pipeline.Stages,
            static stage =>
                stage.Name == PipelineStageNames.Advise
                && stage.Status == PipelineStageStatuses.Completed
                && string.Equals(stage.ArtifactPath, TuningAdvicePath, StringComparison.OrdinalIgnoreCase));
        Assert.Empty(operation.Proposals);
    }

    [Fact]
    public void CreateReport_UsesRecoveryAdviceWhenReplayEvidenceIsIncomplete()
    {
        QueryStoreCorrelationAdvisor advisor = new();
        QueryCorrelationReport correlationReport = new()
        {
            GeneratedAtUtc = new DateTimeOffset(2026, 6, 9, 0, 0, 0, TimeSpan.Zero),
            AppName = "TestHarness",
            ReplayArtifactDir = ReplayArtifactsPath,
            QueryStoreSnapshotPath = QueryStoreSnapshotPath,
            QueryStoreCapturedAtUtc = new DateTimeOffset(2026, 6, 9, 0, 0, 0, TimeSpan.Zero),
            Records = Array.Empty<QueryCorrelationRecord>(),
            Summary = new QueryCorrelationSummary
            {
                OperationCount = 1,
                CapturedCommandCount = 0,
                MatchedCommandCount = 0,
                UnmatchedCount = 0,
                Operations =
                [
                    new OperationCorrelationSummary
                    {
                        OperationKey = "GET /api/expenses/dashboard",
                        HttpMethod = "GET",
                        Route = "/api/expenses/dashboard",
                        ReplayStatus = "failed",
                        OperationArtifactPath = OperationArtifactPath,
                        CapturedCommandCount = 0,
                        MatchedCommandCount = 0,
                        HandleExactCount = 0,
                        QueryTextExactCount = 0,
                        FingerprintFallbackCount = 0,
                        UnmatchedCount = 0,
                    },
                ],
            },
            Pipeline = CreatePlaceholderPipeline(),
        };

        var report = advisor.CreateReport(
            correlationReport,
            QueryStoreCorrelationPath,
            TuningAdvicePath);

        var operation = Assert.Single(report.Operations);
        Assert.Equal(0, report.Summary.ProposalCount);
        Assert.Empty(operation.Proposals);
        var recommendation = Assert.Single(operation.Recommendations);
        Assert.Contains("Stabilize replay before tuning", recommendation.Title);
    }

    private static PipelineReport CreatePlaceholderPipeline()
    {
        return new PipelineReport
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
        };
    }

    private static QueryStorePlanRecord CreatePlan(
        long queryId,
        long planId,
        double meanDurationMilliseconds,
        double meanCpuMilliseconds,
        double meanLogicalReads,
        string queryText)
    {
        return new QueryStorePlanRecord
        {
            QueryId = queryId,
            PlanId = planId,
            QueryTextId = queryId,
            StatementSqlHandle = $"0x{planId:X4}",
            QueryHash = $"0x{queryId:X16}",
            QueryText = queryText,
            ObjectName = "[dbo].[ExpenseRecord]",
            QueryParameterizationType = 0,
            ParamTypeDescription = "None",
            ExecutionCount = 3,
            MeanDuration = TimeSpan.FromMilliseconds(meanDurationMilliseconds),
            MaxDuration = TimeSpan.FromMilliseconds(meanDurationMilliseconds * 1.2d),
            MeanCpuMilliseconds = meanCpuMilliseconds,
            MeanLogicalReads = meanLogicalReads,
            LastExecutionTimeUtc = new DateTimeOffset(2026, 6, 9, 0, 0, 0, TimeSpan.Zero),
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
}
