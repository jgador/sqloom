using System;
using Sqloom.Host.QueryStore;
using Sqloom.Pipeline.QueryStore;
using Xunit;

namespace Sqloom.Host.Tests.QueryStore;

/// <summary>
/// Exercises SQL Server Query Store collector.
/// </summary>
public sealed class SqlServerQueryStoreCollectorTests
{
    [Fact]
    public void ValidateOptions_RejectsNonPositiveValues()
    {
        QueryStoreOptions options = new()
        {
            LookbackWindow = TimeSpan.Zero,
            MaxPlans = 10,
            MaxWaits = 10,
            CommandTimeoutSeconds = 30,
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => SqlServerQueryStoreCollector.ValidateOptions(options));
    }

    [Fact]
    public void MapDatabaseOptions_MapsExpectedColumns()
    {
        QueryStoreDatabaseOptionsRow row = new()
        {
            DesiredState = "READ_WRITE",
            ActualState = "READ_ONLY",
            ReadOnlyReason = 65536L,
            CurrentStorageSizeMb = 128L,
            MaxStorageSizeMb = 1024L,
        };

        var options = SqlServerQueryStoreCollector.MapDatabaseOptions(row);

        Assert.Equal("READ_WRITE", options.DesiredState);
        Assert.Equal("READ_ONLY", options.ActualState);
        Assert.Equal(65536L, options.ReadOnlyReason);
        Assert.Equal(128d, options.CurrentStorageSizeMb);
        Assert.Equal(1024d, options.MaxStorageSizeMb);
    }

    [Fact]
    public void MapPlanRecord_MapsRuntimeMetricsAndOptionalColumns()
    {
        QueryStorePlanRow row = new()
        {
            QueryId = 42L,
            PlanId = 84L,
            QueryTextId = 21L,
            StatementSqlHandle = "0x010203040506",
            ObjectId = 7L,
            QueryParameterizationType = 2,
            ParamTypeDescription = "Simple",
            QueryHash = "0x000000000000002A",
            QueryText = "SELECT 1",
            ObjectName = "[dbo].[Expenses]",
            ExecutionCount = 12L,
            MeanDurationMicroseconds = 2500d,
            MeanCpuMicroseconds = 4200d,
            MeanLogicalReads = 77.5d,
            MaxDurationMicroseconds = 11000d,
            LastExecutionTimeUtc = new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero),
        };

        var record = SqlServerQueryStoreCollector.MapPlanRecord(row);

        Assert.Equal(42L, record.QueryId);
        Assert.Equal(84L, record.PlanId);
        Assert.Equal(21L, record.QueryTextId);
        Assert.Equal("0x010203040506", record.StatementSqlHandle);
        Assert.Equal(7L, record.ObjectId);
        Assert.Equal("0x000000000000002A", record.QueryHash);
        Assert.Equal("SELECT 1", record.QueryText);
        Assert.Equal("[dbo].[Expenses]", record.ObjectName);
        Assert.Equal(2, record.QueryParameterizationType);
        Assert.Equal("Simple", record.ParamTypeDescription);
        Assert.Equal(12L, record.ExecutionCount);
        Assert.Equal(TimeSpan.FromMilliseconds(2.5), record.MeanDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(11), record.MaxDuration);
        Assert.Equal(4.2d, record.MeanCpuMilliseconds);
        Assert.Equal(77.5d, record.MeanLogicalReads);
        Assert.Equal(
            new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero),
            record.LastExecutionTimeUtc);
    }

    [Fact]
    public void MapWaitStat_MapsWaitTotals()
    {
        QueryStoreWaitRow row = new()
        {
            QueryId = 42L,
            PlanId = 84L,
            WaitCategory = "Lock",
            TotalWaitMilliseconds = 87.5d,
            AvgWaitMs = 7.25d,
        };

        var wait = SqlServerQueryStoreCollector.MapWaitStat(row);

        Assert.Equal(42L, wait.QueryId);
        Assert.Equal(84L, wait.PlanId);
        Assert.Equal("Lock", wait.WaitCategory);
        Assert.Equal(87.5d, wait.TotalWaitMilliseconds);
        Assert.Equal(7.25d, wait.AvgWaitMs);
    }

}
