using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Sqloom.Host.QueryStore;
using Sqloom.Pipeline.QueryStore;

namespace Sqloom.Host.QueryStore;

/// <summary>
/// Discovers user-defined database objects from SQL Server or Azure SQL using a readonly connection.
/// </summary>
internal static class SqlServerDiscoveredObjectCollector
{
    private const string UserTablesAndViewsSql = """
        SELECT
            schema_info.name AS SchemaName,
            objects.name AS ObjectName,
            CASE
                WHEN objects.type = N'U' THEN N'Table'
                WHEN objects.type = N'V' THEN N'View'
                ELSE N'Unknown'
            END AS ObjectKind
        FROM sys.objects AS objects
        INNER JOIN sys.schemas AS schema_info
            ON schema_info.schema_id = objects.schema_id
        WHERE objects.is_ms_shipped = 0
          AND objects.type IN (N'U', N'V')
        ORDER BY
            schema_info.name,
            objects.name,
            ObjectKind;
        """;

    private const string ViewDefinitionPermissionSql = """
        SELECT
            CAST(HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'VIEW DEFINITION') AS bit) AS HasViewDefinition;
        """;

    private const string UserModulesSql = """
        SELECT
            schema_info.name AS SchemaName,
            objects.name AS ObjectName,
            N'Module' AS ObjectKind
        FROM sys.objects AS objects
        INNER JOIN sys.schemas AS schema_info
            ON schema_info.schema_id = objects.schema_id
        WHERE objects.is_ms_shipped = 0
          AND objects.type IN (N'P', N'PC', N'FN', N'FS', N'FT', N'IF', N'TF')
        ORDER BY
            schema_info.name,
            objects.name,
            ObjectKind;
        """;

    /// <summary>
    /// Captures user-defined database object metadata from a readonly SQL Server connection.
    /// </summary>
    public static async Task<DbObjectCatalog> CaptureAsync(
        string readOnlyConnectionString,
        DbObjectScanOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(readOnlyConnectionString);
        ValidateOptions(options);

        var connection = await ReadOnlySqlConnectionFactory
            .CreateOpenConnectionAsync(readOnlyConnectionString, cancellationToken)
            .ConfigureAwait(false);

        await using (connection.ConfigureAwait(false))
        {
            List<DiscoveredDatabaseObject> objects = new();
            objects.AddRange(await ReadObjectsAsync(connection, UserTablesAndViewsSql, options.CommandTimeoutSeconds, cancellationToken).ConfigureAwait(false));

            var isComplete = true;
            List<string> warnings = new();

            var canReadModules = await ReadViewDefinitionPermissionAsync(connection, options.CommandTimeoutSeconds, cancellationToken)
                .ConfigureAwait(false);
            if (canReadModules)
            {
                try
                {
                    objects.AddRange(await ReadObjectsAsync(connection, UserModulesSql, options.CommandTimeoutSeconds, cancellationToken).ConfigureAwait(false));
                }
                catch (SqlException sqlException)
                {
                    isComplete = false;
                    warnings.Add($"Module discovery failed: {sqlException.Message}");
                }
            }
            else
            {
                isComplete = false;
                warnings.Add("Module discovery skipped because VIEW DEFINITION permission is unavailable.");
            }

            return FinalizeCatalog(connection.Database, objects, isComplete, warnings);
        }
    }

    internal static void ValidateOptions(DbObjectScanOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.CommandTimeoutSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.CommandTimeoutSeconds,
                "CommandTimeoutSeconds must be positive.");
        }
    }

    internal static DiscoveredDatabaseObject MapDiscoveredObject(DiscoveredDatabaseObjectRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return new DiscoveredDatabaseObject
        {
            SchemaName = row.SchemaName,
            ObjectName = row.ObjectName,
            FullyQualifiedName = $"[{row.SchemaName}].[{row.ObjectName}]",
            Kind = ParseKind(row.ObjectKind),
        };
    }

    internal static DbObjectCatalog FinalizeCatalog(
        string sourceName,
        IReadOnlyList<DiscoveredDatabaseObject> objects,
        bool isComplete,
        IReadOnlyList<string> warnings,
        DateTimeOffset? capturedAtUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(warnings);

        var orderedObjects = objects
            .OrderBy(static item => item.Kind)
            .ThenBy(static item => item.SchemaName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static item => item.ObjectName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new DbObjectCatalog
        {
            CapturedAtUtc = capturedAtUtc ?? DateTimeOffset.UtcNow,
            SourceName = sourceName,
            IsComplete = isComplete,
            Warnings = warnings.ToArray(),
            Objects = orderedObjects,
        };
    }

    private static DbObjectKind ParseKind(string objectKind)
    {
        return objectKind switch
        {
            "Table" => DbObjectKind.Table,
            "View" => DbObjectKind.View,
            "Module" => DbObjectKind.Module,
            _ => throw new InvalidOperationException($"Unsupported discovered object kind: {objectKind}."),
        };
    }

    private static async Task<bool> ReadViewDefinitionPermissionAsync(
        SqlConnection connection,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        CommandDefinition command = new(
            ViewDefinitionPermissionSql,
            commandTimeout: commandTimeoutSeconds,
            cancellationToken: cancellationToken);
        var row = await connection.QueryFirstOrDefaultAsync<ViewDefinitionPermissionRow>(command)
            .ConfigureAwait(false);
        if (row is null)
        {
            throw new InvalidOperationException("VIEW DEFINITION permission check returned no rows.");
        }

        return row.HasViewDefinition;
    }

    private static async Task<IReadOnlyList<DiscoveredDatabaseObject>> ReadObjectsAsync(
        SqlConnection connection,
        string commandText,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        CommandDefinition command = new(
            commandText,
            commandTimeout: commandTimeoutSeconds,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<DiscoveredDatabaseObjectRow>(command).ConfigureAwait(false);
        return rows.Select(MapDiscoveredObject).ToArray();
    }
}
