using System;
using System.Data;
using Sqloom.Host.QueryStore;
using Sqloom.Core.QueryStore;
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
    public void ReadDatabaseOptions_MapsExpectedColumns()
    {
        using var table = CreateDatabaseOptionsTable();
        using var reader = table.CreateDataReader();
        Assert.True(reader.Read());

        var options = SqlServerQueryStoreCollector.ReadDatabaseOptions(reader);

        Assert.Equal("READ_WRITE", options.DesiredState);
        Assert.Equal("READ_ONLY", options.ActualState);
        Assert.Equal(65536L, options.ReadOnlyReason);
        Assert.Equal(128.5d, options.CurrentStorageSizeMb);
        Assert.Equal(1024d, options.MaxStorageSizeMb);
    }

    [Fact]
    public void ReadPlanRecord_MapsRuntimeMetricsAndOptionalColumns()
    {
        using var table = CreatePlanTable();
        using var reader = table.CreateDataReader();
        Assert.True(reader.Read());

        var record = SqlServerQueryStoreCollector.ReadPlanRecord(reader);

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
    public void ReadWaitStat_MapsWaitTotals()
    {
        using var table = CreateWaitTable();
        using var reader = table.CreateDataReader();
        Assert.True(reader.Read());

        var wait = SqlServerQueryStoreCollector.ReadWaitStat(reader);

        Assert.Equal(42L, wait.QueryId);
        Assert.Equal(84L, wait.PlanId);
        Assert.Equal("Lock", wait.WaitCategory);
        Assert.Equal(87.5d, wait.TotalWaitMilliseconds);
        Assert.Equal(7.25d, wait.AvgWaitMs);
    }

    private static DataTable CreateDatabaseOptionsTable()
    {
        DataTable table = new();
        table.Columns.Add("desired_state_desc", typeof(string));
        table.Columns.Add("actual_state_desc", typeof(string));
        table.Columns.Add("readonly_reason", typeof(int));
        table.Columns.Add("current_storage_size_mb", typeof(decimal));
        table.Columns.Add("max_storage_size_mb", typeof(decimal));
        table.Rows.Add("READ_WRITE", "READ_ONLY", 65536, 128.5m, 1024m);
        return table;
    }

    private static DataTable CreatePlanTable()
    {
        DataTable table = new();
        table.Columns.Add("query_id", typeof(long));
        table.Columns.Add("plan_id", typeof(long));
        table.Columns.Add("query_text_id", typeof(long));
        table.Columns.Add("statement_sql_handle", typeof(string));
        table.Columns.Add("object_id", typeof(long));
        table.Columns.Add("query_parameterization_type", typeof(int));
        table.Columns.Add("query_parameterization_type_desc", typeof(string));
        table.Columns.Add("query_hash", typeof(string));
        table.Columns.Add("query_sql_text", typeof(string));
        table.Columns.Add("object_name", typeof(string));
        table.Columns.Add("execution_count", typeof(long));
        table.Columns.Add("mean_duration_us", typeof(double));
        table.Columns.Add("mean_cpu_us", typeof(double));
        table.Columns.Add("mean_logical_reads", typeof(double));
        table.Columns.Add("max_duration_us", typeof(double));
        table.Columns.Add("last_execution_time", typeof(DateTimeOffset));
        table.Rows.Add(
            42L,
            84L,
            21L,
            "0x010203040506",
            7L,
            2,
            "Simple",
            "0x000000000000002A",
            "SELECT 1",
            "[dbo].[Expenses]",
            12L,
            2500d,
            4200d,
            77.5d,
            11000d,
            new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero));
        return table;
    }

    private static DataTable CreateWaitTable()
    {
        DataTable table = new();
        table.Columns.Add("query_id", typeof(long));
        table.Columns.Add("plan_id", typeof(long));
        table.Columns.Add("wait_category_desc", typeof(string));
        table.Columns.Add("total_query_wait_time_ms", typeof(decimal));
        table.Columns.Add("avg_query_wait_time_ms", typeof(decimal));
        table.Rows.Add(42L, 84L, "Lock", 87.5m, 7.25m);
        return table;
    }
}
