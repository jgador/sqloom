using System;
using System.Data;
using System.Linq;
using Dapper;
using Sqloom.Host.QueryStore;
using Xunit;

namespace Sqloom.Host.Tests.QueryStore;

/// <summary>
/// Exercises SQL Server query row mappings through Dapper's data-reader materializer.
/// </summary>
public sealed class SqlServerQueryTypeMapsTests
{
    [Fact]
    public void QueryStoreDatabaseOptionsRow_HydratesSqlColumnNames()
    {
        var row = ParseRow<QueryStoreDatabaseOptionsRow>(
            ("desired_state_desc", typeof(string), "READ_WRITE"),
            ("actual_state_desc", typeof(string), "READ_ONLY"),
            ("readonly_reason", typeof(long), 4L),
            ("current_storage_size_mb", typeof(long), 128L),
            ("max_storage_size_mb", typeof(long), 1024L));

        Assert.Equal("READ_WRITE", row.DesiredState);
        Assert.Equal("READ_ONLY", row.ActualState);
        Assert.Equal(4L, row.ReadOnlyReason);
        Assert.Equal(128L, row.CurrentStorageSizeMb);
        Assert.Equal(1024L, row.MaxStorageSizeMb);
    }

    [Fact]
    public void QueryStorePlanRow_HydratesSqlColumnNames()
    {
        var lastExecutionTime = new DateTimeOffset(2026, 7, 28, 12, 34, 56, TimeSpan.Zero);
        var row = ParseRow<QueryStorePlanRow>(
            ("query_id", typeof(long), 11L),
            ("plan_id", typeof(long), 12L),
            ("query_text_id", typeof(long), 13L),
            ("statement_sql_handle", typeof(string), "0x0102"),
            ("object_id", typeof(long), 14L),
            ("query_parameterization_type", typeof(int), 2),
            ("query_parameterization_type_desc", typeof(string), "User"),
            ("query_hash", typeof(string), "0x0304"),
            ("query_sql_text", typeof(string), "SELECT 1"),
            ("object_name", typeof(string), "[dbo].[Items]"),
            ("execution_count", typeof(long), 15L),
            ("mean_duration_us", typeof(double), 16.5d),
            ("max_duration_us", typeof(double), 17.5d),
            ("mean_cpu_us", typeof(double), 18.5d),
            ("mean_logical_reads", typeof(double), 19.5d),
            ("last_execution_time", typeof(DateTimeOffset), lastExecutionTime));

        Assert.Equal(11L, row.QueryId);
        Assert.Equal(12L, row.PlanId);
        Assert.Equal(13L, row.QueryTextId);
        Assert.Equal("0x0102", row.StatementSqlHandle);
        Assert.Equal(14L, row.ObjectId);
        Assert.Equal(2, row.QueryParameterizationType);
        Assert.Equal("User", row.ParamTypeDescription);
        Assert.Equal("0x0304", row.QueryHash);
        Assert.Equal("SELECT 1", row.QueryText);
        Assert.Equal("[dbo].[Items]", row.ObjectName);
        Assert.Equal(15L, row.ExecutionCount);
        Assert.Equal(16.5d, row.MeanDurationMicroseconds);
        Assert.Equal(17.5d, row.MaxDurationMicroseconds);
        Assert.Equal(18.5d, row.MeanCpuMicroseconds);
        Assert.Equal(19.5d, row.MeanLogicalReads);
        Assert.Equal(lastExecutionTime, row.LastExecutionTimeUtc);
    }

    [Fact]
    public void QueryStoreWaitRow_HydratesSqlColumnNames()
    {
        var row = ParseRow<QueryStoreWaitRow>(
            ("query_id", typeof(long), 21L),
            ("plan_id", typeof(long), 22L),
            ("wait_category_desc", typeof(string), "CPU"),
            ("avg_query_wait_time_ms", typeof(double), 23.5d),
            ("total_query_wait_time_ms", typeof(double), 24.5d));

        Assert.Equal(21L, row.QueryId);
        Assert.Equal(22L, row.PlanId);
        Assert.Equal("CPU", row.WaitCategory);
        Assert.Equal(23.5d, row.AvgWaitMs);
        Assert.Equal(24.5d, row.TotalWaitMilliseconds);
    }

    [Fact]
    public void DiscoveredDatabaseObjectRow_HydratesSqlColumnNames()
    {
        var row = ParseRow<DiscoveredDatabaseObjectRow>(
            ("schema_name", typeof(string), "dbo"),
            ("object_name", typeof(string), "Items"),
            ("object_kind", typeof(string), "Table"));

        Assert.Equal("dbo", row.SchemaName);
        Assert.Equal("Items", row.ObjectName);
        Assert.Equal("Table", row.ObjectKind);
    }

    [Fact]
    public void ViewDefinitionPermissionRow_HydratesSqlColumnNames()
    {
        var row = ParseRow<ViewDefinitionPermissionRow>(
            ("has_view_definition", typeof(bool), true));

        Assert.True(row.HasViewDefinition);
    }

    [Fact]
    public void SqlStatementHandleRow_HydratesSqlColumnNames()
    {
        var row = ParseRow<SqlStatementHandleRow>(
            ("query_parameterization_type", typeof(int), 1),
            ("statement_sql_handle", typeof(string), "0x0506"));

        Assert.Equal(1, row.QueryParameterizationType);
        Assert.Equal("0x0506", row.StatementSqlHandle);
    }

    private static T ParseRow<T>(
        params (string ColumnName, Type ColumnType, object? Value)[] values)
    {
        SqlServerQueryTypeMaps.EnsureRegistered();

        using DataTable table = new();
        foreach (var value in values)
        {
            table.Columns.Add(value.ColumnName, value.ColumnType);
        }

        table.Rows.Add(values.Select(value => value.Value ?? DBNull.Value).ToArray());

        using DataTableReader reader = table.CreateDataReader();
        return reader.Parse<T>().Single();
    }
}
