using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.AspNetCore.Capture;
using Sqloom.AspNetCore.Endpoints;
using Sqloom.Core.Execution;
using Sqloom.Correlation.QueryStore;
using Sqloom.QueryStore.QueryStore;
using Xunit;

namespace Sqloom.Correlation.Tests.QueryStore;

/// <summary>
/// Exercises Query Store correlator.
/// </summary>
public sealed class QueryStoreCorrelatorTests
{
    private static string ArtifactRoot =>
        Path.Combine("artifacts", "sqloom");

    private static string OperationArtifactPath =>
        Path.Combine(ArtifactRoot, "operations", "01-expenses-dashboard.json");

    [Fact]
    public async Task CorrelateAsync_PrefersStatementHandleMatchesOverTextAndFingerprintFallback()
    {
        var snapshot = CreateSnapshot(
            CreatePlan(
                queryId: 10L,
                planId: 20L,
                queryText: "SELECT 1",
                statementSqlHandle: "0xAAAA"),
            CreatePlan(
                queryId: 11L,
                planId: 21L,
                queryText: "SELECT 1",
                statementSqlHandle: "0xBBBB"));
        var replayResult = CreateReplayResult(
            CreateCommand("SELECT 1"));
        FakeStatementSqlHandleResolver resolver = new(
            new SqlHandleResolution
            {
                SqlText = "SELECT 1",
                ComparableSqlText = "SELECT 1",
                StatementSqlHandle = "0xBBBB",
                Candidates =
                [
                    new SqlHandleCandidate
                    {
                        RequestedParamType = "None",
                        QueryParameterizationType = 0,
                        StatementSqlHandle = "0xBBBB",
                    },
                ],
            });
        QueryStoreCorrelator correlator = new(resolver);

        var report = await correlator
            .CorrelateAsync(snapshot, [replayResult], ArtifactRoot);

        var record = Assert.Single(report.Records);
        Assert.Equal(CorrelationMatchKind.StatementHandleExact, record.MatchKind);
        Assert.Equal(1.0d, record.Confidence);
        Assert.Single(record.MatchedPlans);
        Assert.Equal(11L, record.MatchedPlans[0].QueryId);
        Assert.Contains(
            report.Pipeline.Stages,
            static stage =>
                stage.Name == PipelineStageNames.Correlate
                && stage.Status == PipelineStageStatuses.Completed);
        Assert.Contains(
            report.Pipeline.Stages,
            static stage =>
                stage.Name == PipelineStageNames.Advise
                && stage.Status == PipelineStageStatuses.Available);
    }

    [Fact]
    public async Task CorrelateAsync_UsesTrimmedOuterNoiseForExactQueryTextMatches()
    {
        var snapshot = CreateSnapshot(
            CreatePlan(
                queryId: 42L,
                planId: 84L,
                queryText: "SELECT [e].[Id] FROM [dbo].[ExpenseRecord] AS [e]",
                statementSqlHandle: "0x4242"));
        var replayResult = CreateReplayResult(
            CreateCommand(
                """
                -- outer comment
                SELECT [e].[Id] FROM [dbo].[ExpenseRecord] AS [e]
                /* trailing comment */
                """));
        FakeStatementSqlHandleResolver resolver = new(
            new SqlHandleResolution
            {
                SqlText = replayResult.CapturedSqlCommands[0].CommandText,
                ComparableSqlText = "SELECT [e].[Id] FROM [dbo].[ExpenseRecord] AS [e]",
                Candidates = Array.Empty<SqlHandleCandidate>(),
            });
        QueryStoreCorrelator correlator = new(resolver);

        var report = await correlator
            .CorrelateAsync(snapshot, [replayResult], ArtifactRoot);

        var record = Assert.Single(report.Records);
        Assert.Equal(CorrelationMatchKind.QueryTextExact, record.MatchKind);
        Assert.Single(record.MatchedPlans);
        Assert.Equal(42L, record.MatchedPlans[0].QueryId);
    }

    [Fact]
    public async Task CorrelateAsync_UsesExactQueryTextForStatementInsideCapturedBatch()
    {
        var snapshot = CreateSnapshot(
            CreatePlan(
                queryId: 42L,
                planId: 84L,
                queryText: "SELECT [e].[Id] FROM [dbo].[ExpenseRecord] AS [e] WHERE [e].[UserId] = @UserId",
                statementSqlHandle: "0x4242"));
        var replayResult = CreateReplayResult(
            CreateCommand(
                """
                SET NOCOUNT ON;
                SELECT [e].[Id] FROM [dbo].[ExpenseRecord] AS [e] WHERE [e].[UserId] = @UserId;
                SELECT 1;
                """));
        FakeStatementSqlHandleResolver resolver = new(
            new SqlHandleResolution
            {
                SqlText = replayResult.CapturedSqlCommands[0].CommandText,
                ComparableSqlText = replayResult.CapturedSqlCommands[0].CommandText,
                Candidates = Array.Empty<SqlHandleCandidate>(),
            });
        QueryStoreCorrelator correlator = new(resolver);

        var report = await correlator
            .CorrelateAsync(snapshot, [replayResult], ArtifactRoot);

        var record = Assert.Single(report.Records);
        Assert.Equal(CorrelationMatchKind.QueryTextExact, record.MatchKind);
        Assert.Single(record.MatchedPlans);
        Assert.Equal(42L, record.MatchedPlans[0].QueryId);
    }

    [Fact]
    public async Task CorrelateAsync_UsesFingerprintFallbackForDiagnostics_WhenNoSafeExactMatchExists()
    {
        const string snapshotText = "SELECT * FROM [dbo].[ExpenseRecord] WHERE [Id] = @userId";
        const string capturedText = "SELECT * FROM [dbo].[ExpenseRecord] WHERE [Id] = @p0";

        var snapshot = CreateSnapshot(
            CreatePlan(
                queryId: 5L,
                planId: 6L,
                queryText: snapshotText,
                statementSqlHandle: "0x1111"));
        var replayResult = CreateReplayResult(CreateCommand(capturedText));
        FakeStatementSqlHandleResolver resolver = new(
            new SqlHandleResolution
            {
                SqlText = capturedText,
                ComparableSqlText = capturedText,
                Candidates = Array.Empty<SqlHandleCandidate>(),
            });
        QueryStoreCorrelator correlator = new(resolver);

        var report = await correlator
            .CorrelateAsync(snapshot, [replayResult], ArtifactRoot);

        var record = Assert.Single(report.Records);
        Assert.Equal(CorrelationMatchKind.FingerprintFallback, record.MatchKind);
        Assert.Contains(record.Notes, static note => note.Contains("diagnostic", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CorrelateAsync_LeavesUnmatchedSqlExplicitlyUnmatched()
    {
        var snapshot = CreateSnapshot(
            CreatePlan(
                queryId: 1L,
                planId: 2L,
                queryText: "SELECT 1",
                statementSqlHandle: "0xAAAA"));
        var replayResult = CreateReplayResult(
            CreateCommand("SELECT 2"));
        FakeStatementSqlHandleResolver resolver = new(
            new SqlHandleResolution
            {
                SqlText = "SELECT 2",
                ComparableSqlText = "SELECT 2",
                Candidates = Array.Empty<SqlHandleCandidate>(),
            });
        QueryStoreCorrelator correlator = new(resolver);

        var report = await correlator
            .CorrelateAsync(snapshot, [replayResult], ArtifactRoot);

        var record = Assert.Single(report.Records);
        Assert.Equal(CorrelationMatchKind.Unmatched, record.MatchKind);
        Assert.Empty(record.MatchedPlans);
    }

    [Fact]
    public async Task CorrelateAsync_PreservesStoredProcedureObjectContext()
    {
        var snapshot = CreateSnapshot(
            CreatePlan(
                queryId: 90L,
                planId: 91L,
                queryText: "EXEC [dbo].[RebuildExpenseCache] @userId",
                statementSqlHandle: "0xC0FFEE",
                objectId: 512L,
                objectName: "[dbo].[RebuildExpenseCache]"));
        var replayResult = CreateReplayResult(
            CreateCommand("EXEC [dbo].[RebuildExpenseCache] @userId"));
        FakeStatementSqlHandleResolver resolver = new(
            new SqlHandleResolution
            {
                SqlText = "EXEC [dbo].[RebuildExpenseCache] @userId",
                ComparableSqlText = "EXEC [dbo].[RebuildExpenseCache] @userId",
                StatementSqlHandle = "0xC0FFEE",
                Candidates =
                [
                    new SqlHandleCandidate
                    {
                        RequestedParamType = "None",
                        QueryParameterizationType = 0,
                        StatementSqlHandle = "0xC0FFEE",
                    },
                ],
            });
        QueryStoreCorrelator correlator = new(resolver);

        var report = await correlator
            .CorrelateAsync(snapshot, [replayResult], ArtifactRoot);

        var record = Assert.Single(report.Records);
        var matchedPlan = Assert.Single(record.MatchedPlans);
        Assert.Equal(512L, matchedPlan.ObjectId);
        Assert.Equal("[dbo].[RebuildExpenseCache]", matchedPlan.ObjectName);
    }

    private static QueryStoreSnapshot CreateSnapshot(params QueryStorePlanRecord[] plans)
    {
        return new QueryStoreSnapshot
        {
            CapturedAtUtc = new DateTimeOffset(2026, 6, 8, 0, 0, 0, TimeSpan.Zero),
            LookbackWindow = TimeSpan.FromHours(1),
            DatabaseOptions = new QueryStoreDatabaseOptions
            {
                DesiredState = "READ_WRITE",
                ActualState = "READ_WRITE",
                ReadOnlyReason = 0,
                CurrentStorageSizeMb = 1,
                MaxStorageSizeMb = 100,
            },
            Plans = plans,
            Waits = Array.Empty<QueryStoreWaitStat>(),
        };
    }

    private static QueryStorePlanRecord CreatePlan(
        long queryId,
        long planId,
        string queryText,
        string statementSqlHandle,
        long? objectId = null,
        string? objectName = null)
    {
        return new QueryStorePlanRecord
        {
            QueryId = queryId,
            PlanId = planId,
            QueryTextId = queryId,
            StatementSqlHandle = statementSqlHandle,
            ObjectId = objectId,
            QueryHash = $"0x{queryId:X16}",
            QueryText = queryText,
            ObjectName = objectName,
            QueryParameterizationType = 0,
            ParamTypeDescription = "None",
            ExecutionCount = 1,
            MeanDuration = TimeSpan.FromMilliseconds(1),
            MaxDuration = TimeSpan.FromMilliseconds(1),
            MeanCpuMilliseconds = 1,
            MeanLogicalReads = 1,
            LastExecutionTimeUtc = new DateTimeOffset(2026, 6, 8, 0, 0, 0, TimeSpan.Zero),
        };
    }

    private static EndpointReplayResult CreateReplayResult(params CapturedSqlCommand[] commands)
    {
        return new EndpointReplayResult
        {
            OperationKey = "GET /api/expenses/dashboard",
            HttpMethod = "GET",
            Route = "/api/expenses/dashboard",
            Persona = "subscribed-user",
            Status = "replayed",
            HttpStatusCode = 200,
            DurationMilliseconds = 12,
            Request = new EndpointReplayRequest
            {
                OperationKey = "GET /api/expenses/dashboard",
                HttpMethod = "GET",
                Route = "/api/expenses/dashboard",
                RelativePathAndQuery = "/api/expenses/dashboard",
            },
            CapturedSqlCommands = commands,
            ArtifactPath = OperationArtifactPath,
        };
    }

    private static CapturedSqlCommand CreateCommand(string sqlText)
    {
        return new CapturedSqlCommand
        {
            SourceKind = CapturedSqlSourceKind.EntityFramework,
            Source = "EntityFrameworkCore",
            CommandText = sqlText,
            NormalizedCommandText = ReplaySqlTextNormalizer.Normalize(sqlText),
            Fingerprint = ReplaySqlTextNormalizer.ComputeFingerprint(sqlText),
            Parameters = Array.Empty<CapturedSqlParameter>(),
            Duration = TimeSpan.FromMilliseconds(2),
        };
    }

    /// <summary>
    /// Resolves fake statement SQL handle.
    /// </summary>
    private sealed class FakeStatementSqlHandleResolver : ISqlHandleResolver
    {
        private readonly SqlHandleResolution _resolution;

        public FakeStatementSqlHandleResolver(SqlHandleResolution resolution)
        {
            _resolution = resolution;
        }

        public Task<SqlHandleResolution> ResolveAsync(
            string sqlText,
            IReadOnlyList<SqlHandleParameter> parameters,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_resolution);
        }
    }
}
