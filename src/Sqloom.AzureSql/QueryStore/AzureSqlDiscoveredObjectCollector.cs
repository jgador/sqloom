using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Sqloom.AzureSql.Capture;
using Sqloom.QueryStore.QueryStore;

namespace Sqloom.AzureSql.QueryStore;

/// <summary>
/// Discovers user-defined database objects from Azure SQL or SQL Server using a readonly connection.
/// </summary>
public sealed class AzureSqlDiscoveredObjectCollector : IDiscoveredDatabaseObjectCollector
{
    private const string UserTablesAndViewsSql = """
        SELECT
            schema_info.name AS schema_name,
            objects.name AS object_name,
            CASE
                WHEN objects.type = N'U' THEN N'Table'
                WHEN objects.type = N'V' THEN N'View'
                ELSE N'Unknown'
            END AS object_kind
        FROM sys.objects AS objects
        INNER JOIN sys.schemas AS schema_info
            ON schema_info.schema_id = objects.schema_id
        WHERE objects.is_ms_shipped = 0
          AND objects.type IN (N'U', N'V')
        ORDER BY
            schema_info.name,
            objects.name,
            object_kind;
        """;

    private const string ViewDefinitionPermissionSql = """
        SELECT
            CAST(HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'VIEW DEFINITION') AS bit) AS has_view_definition;
        """;

    private const string UserModulesSql = """
        SELECT
            schema_info.name AS schema_name,
            objects.name AS object_name,
            N'Module' AS object_kind
        FROM sys.objects AS objects
        INNER JOIN sys.schemas AS schema_info
            ON schema_info.schema_id = objects.schema_id
        WHERE objects.is_ms_shipped = 0
          AND objects.type IN (N'P', N'PC', N'FN', N'FS', N'FT', N'IF', N'TF')
        ORDER BY
            schema_info.name,
            objects.name,
            object_kind;
        """;

    private readonly ReadOnlySqlConnectionFactory _connectionFactory;

    public AzureSqlDiscoveredObjectCollector()
        : this(new ReadOnlySqlConnectionFactory())
    {
    }

    public AzureSqlDiscoveredObjectCollector(ReadOnlySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<DiscoveredDatabaseObjectCatalog> CaptureAsync(
        string readOnlyConnectionString,
        DiscoveredDatabaseObjectObservationOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(readOnlyConnectionString);
        ValidateOptions(options);

        var connection = await _connectionFactory
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

    internal static void ValidateOptions(DiscoveredDatabaseObjectObservationOptions options)
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

    internal static DiscoveredDatabaseObject ReadDiscoveredObjectRecord(DbDataReader reader)
    {
        var schemaName = reader.GetString(reader.GetOrdinal("schema_name"));
        var objectName = reader.GetString(reader.GetOrdinal("object_name"));
        var objectKind = reader.GetString(reader.GetOrdinal("object_kind"));

        return new DiscoveredDatabaseObject
        {
            SchemaName = schemaName,
            ObjectName = objectName,
            FullyQualifiedName = $"[{schemaName}].[{objectName}]",
            Kind = ParseKind(objectKind),
        };
    }

    internal static bool ReadViewDefinitionPermission(DbDataReader reader)
    {
        return reader.GetBoolean(reader.GetOrdinal("has_view_definition"));
    }

    internal static DiscoveredDatabaseObjectCatalog FinalizeCatalog(
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

        return new DiscoveredDatabaseObjectCatalog
        {
            CapturedAtUtc = capturedAtUtc ?? DateTimeOffset.UtcNow,
            SourceName = sourceName,
            IsComplete = isComplete,
            Warnings = warnings.ToArray(),
            Objects = orderedObjects,
        };
    }

    private static DiscoveredDatabaseObjectKind ParseKind(string objectKind)
    {
        return objectKind switch
        {
            "Table" => DiscoveredDatabaseObjectKind.Table,
            "View" => DiscoveredDatabaseObjectKind.View,
            "Module" => DiscoveredDatabaseObjectKind.Module,
            _ => throw new InvalidOperationException($"Unsupported discovered object kind: {objectKind}."),
        };
    }

    private static async Task<bool> ReadViewDefinitionPermissionAsync(
        SqlConnection connection,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        using var command = CreateCommand(connection, ViewDefinitionPermissionSql, commandTimeoutSeconds);
        DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await using (reader.ConfigureAwait(false))
        {
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("VIEW DEFINITION permission check returned no rows.");
            }

            return ReadViewDefinitionPermission(reader);
        }
    }

    private static async Task<IReadOnlyList<DiscoveredDatabaseObject>> ReadObjectsAsync(
        SqlConnection connection,
        string commandText,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        using var command = CreateCommand(connection, commandText, commandTimeoutSeconds);
        List<DiscoveredDatabaseObject> objects = new();
        DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await using (reader.ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                objects.Add(ReadDiscoveredObjectRecord(reader));
            }
        }

        return objects;
    }

    private static SqlCommand CreateCommand(SqlConnection connection, string commandText, int commandTimeoutSeconds)
    {
        var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.CommandTimeout = commandTimeoutSeconds;
        return command;
    }
}
