using System;
using System.Data;
using Sqloom.AzureSql.QueryStore;
using Sqloom.QueryStore.QueryStore;
using Xunit;

namespace Sqloom.AzureSql.Tests.QueryStore;

/// <summary>
/// Exercises Azure SQL discovered object collector.
/// </summary>
public sealed class AzureSqlDiscoveredObjectCollectorTests
{
    [Fact]
    public void ValidateOptions_RejectsNonPositiveCommandTimeout()
    {
        DiscoveredDatabaseObjectObservationOptions options = new()
        {
            CommandTimeoutSeconds = 0,
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => AzureSqlDiscoveredObjectCollector.ValidateOptions(options));
    }

    [Theory]
    [InlineData("dbo", "ExpenseRecord", "Table", DiscoveredDatabaseObjectKind.Table)]
    [InlineData("reporting", "ExpenseSummary", "View", DiscoveredDatabaseObjectKind.View)]
    [InlineData("dbo", "RebuildExpenseCache", "Module", DiscoveredDatabaseObjectKind.Module)]
    public void ReadDiscoveredObjectRecord_MapsExpectedColumns(
        string schemaName,
        string objectName,
        string objectKind,
        DiscoveredDatabaseObjectKind expectedKind)
    {
        using var table = CreateDiscoveredObjectTable();
        table.Rows.Add(schemaName, objectName, objectKind);
        using var reader = table.CreateDataReader();
        Assert.True(reader.Read());

        var record = AzureSqlDiscoveredObjectCollector.ReadDiscoveredObjectRecord(reader);

        Assert.Equal(schemaName, record.SchemaName);
        Assert.Equal(objectName, record.ObjectName);
        Assert.Equal($"[{schemaName}].[{objectName}]", record.FullyQualifiedName);
        Assert.Equal(expectedKind, record.Kind);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadViewDefinitionPermission_MapsPermissionFlag(bool hasViewDefinition)
    {
        using DataTable table = new();
        table.Columns.Add("has_view_definition", typeof(bool));
        table.Rows.Add(hasViewDefinition);
        using var reader = table.CreateDataReader();
        Assert.True(reader.Read());

        var actual = AzureSqlDiscoveredObjectCollector.ReadViewDefinitionPermission(reader);

        Assert.Equal(hasViewDefinition, actual);
    }

    [Fact]
    public void FinalizeCatalog_CreatesPartialCatalogWithWarningWhenModuleDiscoveryIsIncomplete()
    {
        var catalog = AzureSqlDiscoveredObjectCollector.FinalizeCatalog(
            "talio-local",
            [
                new DiscoveredDatabaseObject
                {
                    SchemaName = "dbo",
                    ObjectName = "ExpenseSummary",
                    FullyQualifiedName = "[dbo].[ExpenseSummary]",
                    Kind = DiscoveredDatabaseObjectKind.View,
                },
                new DiscoveredDatabaseObject
                {
                    SchemaName = "dbo",
                    ObjectName = "ExpenseRecord",
                    FullyQualifiedName = "[dbo].[ExpenseRecord]",
                    Kind = DiscoveredDatabaseObjectKind.Table,
                },
            ],
            isComplete: false,
            warnings:
            [
                "Module discovery skipped because VIEW DEFINITION permission is unavailable.",
            ],
            capturedAtUtc: new DateTimeOffset(2026, 6, 8, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal("talio-local", catalog.SourceName);
        Assert.False(catalog.IsComplete);
        Assert.Single(catalog.Warnings);
        Assert.Equal("ExpenseRecord", catalog.Objects[0].ObjectName);
        Assert.Equal(DiscoveredDatabaseObjectKind.Table, catalog.Objects[0].Kind);
        Assert.Equal("ExpenseSummary", catalog.Objects[1].ObjectName);
        Assert.Equal(DiscoveredDatabaseObjectKind.View, catalog.Objects[1].Kind);
    }

    private static DataTable CreateDiscoveredObjectTable()
    {
        DataTable table = new();
        table.Columns.Add("schema_name", typeof(string));
        table.Columns.Add("object_name", typeof(string));
        table.Columns.Add("object_kind", typeof(string));
        return table;
    }
}
