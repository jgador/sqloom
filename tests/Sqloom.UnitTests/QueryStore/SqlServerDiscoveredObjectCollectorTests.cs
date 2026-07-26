using System;
using Sqloom.Host.QueryStore;
using Sqloom.Pipeline.QueryStore;
using Xunit;

namespace Sqloom.Host.Tests.QueryStore;

/// <summary>
/// Exercises SQL Server discovered object collector.
/// </summary>
public sealed class SqlServerDiscoveredObjectCollectorTests
{
    [Fact]
    public void ValidateOptions_RejectsNonPositiveCommandTimeout()
    {
        DbObjectScanOptions options = new()
        {
            CommandTimeoutSeconds = 0,
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => SqlServerDiscoveredObjectCollector.ValidateOptions(options));
    }

    [Theory]
    [InlineData("dbo", "ExpenseRecord", "Table", DbObjectKind.Table)]
    [InlineData("reporting", "ExpenseSummary", "View", DbObjectKind.View)]
    [InlineData("dbo", "RebuildExpenseCache", "Module", DbObjectKind.Module)]
    public void MapDiscoveredObject_MapsExpectedColumns(
        string schemaName,
        string objectName,
        string objectKind,
        DbObjectKind expectedKind)
    {
        DiscoveredDatabaseObjectRow row = new()
        {
            SchemaName = schemaName,
            ObjectName = objectName,
            ObjectKind = objectKind,
        };

        var record = SqlServerDiscoveredObjectCollector.MapDiscoveredObject(row);

        Assert.Equal(schemaName, record.SchemaName);
        Assert.Equal(objectName, record.ObjectName);
        Assert.Equal($"[{schemaName}].[{objectName}]", record.FullyQualifiedName);
        Assert.Equal(expectedKind, record.Kind);
    }

    [Fact]
    public void FinalizeCatalog_WarnsWhenModuleDiscoveryIsIncomplete()
    {
        var catalog = SqlServerDiscoveredObjectCollector.FinalizeCatalog(
            "sqloom-local",
            [
                new DiscoveredDatabaseObject
                {
                    SchemaName = "dbo",
                    ObjectName = "ExpenseSummary",
                    FullyQualifiedName = "[dbo].[ExpenseSummary]",
                    Kind = DbObjectKind.View,
                },
                new DiscoveredDatabaseObject
                {
                    SchemaName = "dbo",
                    ObjectName = "ExpenseRecord",
                    FullyQualifiedName = "[dbo].[ExpenseRecord]",
                    Kind = DbObjectKind.Table,
                },
            ],
            isComplete: false,
            warnings:
            [
                "Module discovery skipped because VIEW DEFINITION permission is unavailable.",
            ],
            capturedAtUtc: new DateTimeOffset(2026, 6, 8, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal("sqloom-local", catalog.SourceName);
        Assert.False(catalog.IsComplete);
        Assert.Single(catalog.Warnings);
        Assert.Equal("ExpenseRecord", catalog.Objects[0].ObjectName);
        Assert.Equal(DbObjectKind.Table, catalog.Objects[0].Kind);
        Assert.Equal("ExpenseSummary", catalog.Objects[1].ObjectName);
        Assert.Equal(DbObjectKind.View, catalog.Objects[1].Kind);
    }

}
