using System;
using Sqloom.QueryStore.QueryStore;
using Xunit;

namespace Sqloom.QueryStore.Tests.QueryStore;

/// <summary>
/// Exercises Query Store workload classifier.
/// </summary>
public sealed class QueryStoreWorkloadClassifierTests
{
    private static readonly QueryStoreWorkloadProfile _sampleProfile = new()
    {
        Name = "SampleApp",
        DiscoveredObjectCatalog = new DiscoveredDatabaseObjectCatalog
        {
            CapturedAtUtc = new DateTimeOffset(2026, 6, 8, 0, 0, 0, TimeSpan.Zero),
            SourceName = "sample-local",
            IsComplete = true,
            Warnings = Array.Empty<string>(),
            Objects =
            [
                CreateDiscoveredObject("dbo", "ExpenseRecord", DiscoveredDatabaseObjectKind.Table),
                CreateDiscoveredObject("reporting", "ExpenseSummary", DiscoveredDatabaseObjectKind.View),
                CreateDiscoveredObject("dbo", "RebuildExpenseCache", DiscoveredDatabaseObjectKind.Module),
            ],
        },
    };

    [Theory]
    [InlineData("SELECT [e].[Id] FROM [ExpenseRecord] AS [e] WHERE [e].[UserId] = @userId", QueryWorkloadKind.App)]
    [InlineData("SELECT [s].[Total] FROM [reporting].[ExpenseSummary] AS [s] WHERE [s].[Month] = @month", QueryWorkloadKind.App)]
    [InlineData("EXEC [dbo].[RebuildExpenseCache] @userId", QueryWorkloadKind.App)]
    [InlineData("SELECT SCHEMA_NAME(udf.schema_id) FROM sys.all_objects AS udf LEFT OUTER JOIN sys.sql_modules AS smudf ON smudf.object_id = udf.object_id WHERE OBJECTPROPERTYEX(udf.object_id, N'IsSchemaBound') = 0", QueryWorkloadKind.Tooling)]
    [InlineData("SELECT SCHEMA_NAME(v.schema_id) FROM sys.all_views AS v", QueryWorkloadKind.Tooling)]
    [InlineData("SELECT clmns.name FROM sys.tables AS tbl INNER JOIN sys.all_columns AS clmns ON clmns.object_id = tbl.object_id WHERE COLUMNPROPERTY(clmns.object_id, clmns.name, N'IsDeterministic') = 0", QueryWorkloadKind.Tooling)]
    [InlineData("insert into #dso select top 1 @databaseId, edition from sys.database_service_objectives", QueryWorkloadKind.Platform)]
    [InlineData("SELECT [backup_metadata_uuid] FROM [ea46cb77-a506-4ece-a72f-30121d3a6b18].[sys].[backup_metadata_store]", QueryWorkloadKind.Platform)]
    [InlineData("SELECT TOP 1 avg_cpu_percent FROM sys.dm_db_resource_stats order by end_time desc", QueryWorkloadKind.Platform)]
    public void ClassifyPlan_AssignsExpectedKindsForDiscoveredObjectsAndNoise(
        string queryText,
        QueryWorkloadKind expectedKind)
    {
        QueryStoreWorkloadClassifier classifier = new();

        var classification = classifier.ClassifyPlan(
            CreatePlan(queryText),
            _sampleProfile);

        Assert.Equal(expectedKind, classification.Kind);
        Assert.Equal(expectedKind == QueryWorkloadKind.App, classification.IncludeInAppOnly);
        Assert.NotEmpty(classification.Reasons);
    }

    [Fact]
    public void ClassifyPlan_LeavesAppLookingQueryUnknown_WhenCatalogIsMissing()
    {
        QueryStoreWorkloadClassifier classifier = new();

        var classification = classifier.ClassifyPlan(
            CreatePlan("SELECT [e].[Id] FROM [ExpenseRecord] AS [e] WHERE [e].[UserId] = @userId"),
            new QueryStoreWorkloadProfile
            {
                Name = "NoDiscovery",
            });

        Assert.Equal(QueryWorkloadKind.Unknown, classification.Kind);
        Assert.False(classification.IncludeInAppOnly);
        Assert.Contains("requires a discovered-object catalog", Assert.IsType<string[]>(classification.Reasons)[0]);
    }

    [Fact]
    public void ClassifyPlan_ReportsPartialCatalog_WhenNoRuleMatches()
    {
        QueryStoreWorkloadClassifier classifier = new();

        var classification = classifier.ClassifyPlan(
            CreatePlan("SELECT [x].[Id] FROM [dbo].[UnknownTable] AS [x]"),
            new QueryStoreWorkloadProfile
            {
                Name = "PartialDiscovery",
                DiscoveredObjectCatalog = new DiscoveredDatabaseObjectCatalog
                {
                    CapturedAtUtc = new DateTimeOffset(2026, 6, 8, 0, 0, 0, TimeSpan.Zero),
                    SourceName = "talio-local",
                    IsComplete = false,
                    Warnings =
                    [
                        "Module discovery skipped because VIEW DEFINITION permission is unavailable.",
                    ],
                    Objects =
                    [
                        CreateDiscoveredObject("dbo", "ExpenseRecord", DiscoveredDatabaseObjectKind.Table),
                    ],
                },
            });

        Assert.Equal(QueryWorkloadKind.Unknown, classification.Kind);
        Assert.Contains("catalog is partial", Assert.IsType<string[]>(classification.Reasons)[0]);
    }

    [Fact]
    public void ClassifyPlan_DoesNotTreatSeparatedSchemaAndObjectTokensAsQualifiedReference()
    {
        QueryStoreWorkloadClassifier classifier = new();
        var classification = classifier.ClassifyPlan(
            CreatePlan("SELECT @schema = N'dbo', @target = N'ExpenseRecord'; SELECT @schema, @target;"),
            _sampleProfile);

        Assert.Equal(QueryWorkloadKind.Unknown, classification.Kind);
    }

    [Fact]
    public void ApplyClassification_AnnotatesPlansSnapshotMetadataAndInheritedWaits()
    {
        QueryStoreWorkloadClassifier classifier = new();
        QueryStoreSnapshot snapshot = new()
        {
            CapturedAtUtc = new DateTimeOffset(2026, 6, 7, 14, 3, 52, TimeSpan.Zero),
            LookbackWindow = TimeSpan.FromHours(24),
            DatabaseOptions = new QueryStoreDatabaseOptions
            {
                DesiredState = "READ_WRITE",
                ActualState = "READ_WRITE",
                ReadOnlyReason = 0,
                CurrentStorageSizeMb = 5,
                MaxStorageSizeMb = 100,
            },
            Plans =
            [
                CreatePlan("SELECT [e].[Id] FROM [ExpenseRecord] AS [e] WHERE [e].[UserId] = @userId", queryId: 142L, planId: 14L),
                CreatePlan("SELECT SCHEMA_NAME(udf.schema_id) FROM sys.all_objects AS udf", queryId: 20L, planId: 2L),
            ],
            Waits =
            [
                new QueryStoreWaitStat
                {
                    QueryId = 142L,
                    PlanId = 14L,
                    WaitCategory = "CPU",
                    AverageQueryWaitMilliseconds = 1.2d,
                    TotalWaitMilliseconds = 4.8d,
                },
                new QueryStoreWaitStat
                {
                    QueryId = 20L,
                    PlanId = 2L,
                    WaitCategory = "Unknown",
                    AverageQueryWaitMilliseconds = 45d,
                    TotalWaitMilliseconds = 90d,
                },
            ],
        };

        var classifiedSnapshot = classifier.ApplyClassification(snapshot, _sampleProfile);

        var appPlan = Assert.Single(classifiedSnapshot.Plans, plan => plan.QueryId == 142L);
        var toolingPlan = Assert.Single(classifiedSnapshot.Plans, plan => plan.QueryId == 20L);
        var appWait = Assert.Single(classifiedSnapshot.Waits, wait => wait.QueryId == 142L);
        var toolingWait = Assert.Single(classifiedSnapshot.Waits, wait => wait.QueryId == 20L);

        Assert.Equal("SampleApp", classifiedSnapshot.WorkloadProfileName);
        Assert.NotNull(classifiedSnapshot.DiscoveredObjectCatalog);
        Assert.Equal(QueryWorkloadKind.App, appPlan.Classification?.Kind);
        Assert.Equal(QueryWorkloadKind.Tooling, toolingPlan.Classification?.Kind);
        Assert.Equal(QueryWorkloadKind.App, appWait.Classification?.Kind);
        Assert.Equal(QueryWorkloadKind.Tooling, toolingWait.Classification?.Kind);
        Assert.True(appWait.Classification?.IncludeInAppOnly);
        Assert.False(toolingWait.Classification?.IncludeInAppOnly);
        Assert.Contains("Inherited from matching plan record", Assert.IsType<string[]>(appWait.Classification?.Reasons)[0]);
    }

    private static DiscoveredDatabaseObject CreateDiscoveredObject(
        string schemaName,
        string objectName,
        DiscoveredDatabaseObjectKind kind)
    {
        return new DiscoveredDatabaseObject
        {
            SchemaName = schemaName,
            ObjectName = objectName,
            FullyQualifiedName = $"[{schemaName}].[{objectName}]",
            Kind = kind,
        };
    }

    private static QueryStorePlanRecord CreatePlan(string queryText, long queryId = 1L, long planId = 1L)
    {
        return new QueryStorePlanRecord
        {
            QueryId = queryId,
            PlanId = planId,
            QueryTextId = queryId,
            StatementSqlHandle = $"0x{queryId:X32}",
            ObjectId = null,
            QueryHash = $"0x{queryId:X16}",
            QueryText = queryText,
            ObjectName = null,
            QueryParameterizationType = 0,
            QueryParameterizationTypeDescription = "None",
            ExecutionCount = 1,
            MeanDuration = TimeSpan.FromMilliseconds(1),
            MaxDuration = TimeSpan.FromMilliseconds(1),
            MeanCpuMilliseconds = 1,
            MeanLogicalReads = 1,
            LastExecutionTimeUtc = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero),
        };
    }
}
