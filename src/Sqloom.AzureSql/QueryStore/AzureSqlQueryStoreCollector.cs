using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Sqloom.AzureSql.Capture;
using Sqloom.QueryStore.QueryStore;

namespace Sqloom.AzureSql.QueryStore;

/// <summary>
/// Collects Query Store evidence from Azure SQL or SQL Server using a readonly connection.
/// </summary>
public sealed class AzureSqlQueryStoreCollector : IQueryStoreCollector
{
    // Reads Query Store state and storage so observe can report whether capture is usable.
    private const string QueryStoreOptionsSql = """
        SELECT
            desired_state_desc,
            actual_state_desc,
            readonly_reason,
            current_storage_size_mb,
            max_storage_size_mb
        FROM sys.database_query_store_options;
        """;

    // Aggregates the hottest successful plans in the lookback window for snapshot triage output.
    private const string QueryStorePlansSql = """
        WITH filtered_runtime_stats AS (
            SELECT
                runtime_stats.plan_id,
                runtime_stats.runtime_stats_interval_id,
                CAST(SUM(runtime_stats.count_executions) AS bigint) AS execution_count,
                SUM(runtime_stats.avg_duration * runtime_stats.count_executions) AS total_duration_us,
                SUM(runtime_stats.avg_cpu_time * runtime_stats.count_executions) AS total_cpu_us,
                SUM(runtime_stats.avg_logical_io_reads * runtime_stats.count_executions) AS total_logical_reads,
                MAX(runtime_stats.max_duration) AS max_duration_us,
                MAX(runtime_stats.last_execution_time) AS last_execution_time
            FROM sys.query_store_runtime_stats AS runtime_stats
            INNER JOIN sys.query_store_runtime_stats_interval AS runtime_interval
                ON runtime_interval.runtime_stats_interval_id = runtime_stats.runtime_stats_interval_id
            WHERE runtime_stats.execution_type = 0
              AND runtime_interval.end_time >= @StartTimeUtc
            GROUP BY
                runtime_stats.plan_id,
                runtime_stats.runtime_stats_interval_id
        ),
        plan_runtime AS (
            SELECT
                filtered_runtime_stats.plan_id,
                CAST(SUM(filtered_runtime_stats.execution_count) AS bigint) AS execution_count,
                SUM(filtered_runtime_stats.total_duration_us) AS total_duration_us,
                SUM(filtered_runtime_stats.total_cpu_us) AS total_cpu_us,
                SUM(filtered_runtime_stats.total_logical_reads) AS total_logical_reads,
                MAX(filtered_runtime_stats.max_duration_us) AS max_duration_us,
                MAX(filtered_runtime_stats.last_execution_time) AS last_execution_time
            FROM filtered_runtime_stats
            GROUP BY filtered_runtime_stats.plan_id
        )
        SELECT TOP (@MaxPlans)
            query_store_query.query_id,
            query_store_plan.plan_id,
            query_store_query.query_text_id,
            CONVERT(varchar(130), query_store_query_text.statement_sql_handle, 1) AS statement_sql_handle,
            NULLIF(CONVERT(bigint, query_store_query.object_id), 0) AS object_id,
            CONVERT(int, query_store_query.query_parameterization_type) AS query_parameterization_type,
            query_store_query.query_parameterization_type_desc,
            CONVERT(varchar(18), query_store_query.query_hash, 1) AS query_hash,
            query_store_query_text.query_sql_text,
            CASE
                WHEN query_store_query.object_id = 0 THEN NULL
                ELSE QUOTENAME(OBJECT_SCHEMA_NAME(query_store_query.object_id)) + N'.' + QUOTENAME(OBJECT_NAME(query_store_query.object_id))
            END AS object_name,
            plan_runtime.execution_count,
            CASE
                WHEN plan_runtime.execution_count = 0 THEN 0
                ELSE plan_runtime.total_duration_us / CAST(plan_runtime.execution_count AS float)
            END AS mean_duration_us,
            CASE
                WHEN plan_runtime.execution_count = 0 THEN 0
                ELSE plan_runtime.total_cpu_us / CAST(plan_runtime.execution_count AS float)
            END AS mean_cpu_us,
            CASE
                WHEN plan_runtime.execution_count = 0 THEN 0
                ELSE plan_runtime.total_logical_reads / CAST(plan_runtime.execution_count AS float)
            END AS mean_logical_reads,
            plan_runtime.max_duration_us,
            plan_runtime.last_execution_time
        FROM plan_runtime
        INNER JOIN sys.query_store_plan AS query_store_plan
            ON query_store_plan.plan_id = plan_runtime.plan_id
        INNER JOIN sys.query_store_query AS query_store_query
            ON query_store_query.query_id = query_store_plan.query_id
        INNER JOIN sys.query_store_query_text AS query_store_query_text
            ON query_store_query_text.query_text_id = query_store_query.query_text_id
        WHERE query_store_query.is_internal_query = 0
        ORDER BY
            mean_duration_us DESC,
            mean_cpu_us DESC,
            execution_count DESC,
            query_store_plan.plan_id ASC;
        """;

    // Aggregates the heaviest wait categories in the same window so waits can be triaged beside plans.
    private const string QueryStoreWaitsSql = """
        WITH filtered_runtime_counts AS (
            SELECT
                runtime_stats.plan_id,
                runtime_stats.runtime_stats_interval_id,
                CAST(SUM(runtime_stats.count_executions) AS bigint) AS execution_count
            FROM sys.query_store_runtime_stats AS runtime_stats
            INNER JOIN sys.query_store_runtime_stats_interval AS runtime_interval
                ON runtime_interval.runtime_stats_interval_id = runtime_stats.runtime_stats_interval_id
            WHERE runtime_stats.execution_type = 0
              AND runtime_interval.end_time >= @StartTimeUtc
            GROUP BY
                runtime_stats.plan_id,
                runtime_stats.runtime_stats_interval_id
        )
        SELECT TOP (@MaxWaits)
            query_store_query.query_id,
            query_store_wait_stats.plan_id,
            query_store_wait_stats.wait_category_desc,
            SUM(query_store_wait_stats.total_query_wait_time_ms) AS total_query_wait_time_ms,
            CASE
                WHEN SUM(filtered_runtime_counts.execution_count) = 0 THEN 0
                ELSE SUM(query_store_wait_stats.total_query_wait_time_ms) / CAST(SUM(filtered_runtime_counts.execution_count) AS float)
            END AS avg_query_wait_time_ms
        FROM sys.query_store_wait_stats AS query_store_wait_stats
        INNER JOIN filtered_runtime_counts
            ON filtered_runtime_counts.plan_id = query_store_wait_stats.plan_id
           AND filtered_runtime_counts.runtime_stats_interval_id = query_store_wait_stats.runtime_stats_interval_id
        INNER JOIN sys.query_store_plan AS query_store_plan
            ON query_store_plan.plan_id = query_store_wait_stats.plan_id
        INNER JOIN sys.query_store_query AS query_store_query
            ON query_store_query.query_id = query_store_plan.query_id
        WHERE query_store_wait_stats.execution_type = 0
          AND query_store_query.is_internal_query = 0
        GROUP BY
            query_store_query.query_id,
            query_store_wait_stats.plan_id,
            query_store_wait_stats.wait_category_desc
        ORDER BY
            total_query_wait_time_ms DESC,
            avg_query_wait_time_ms DESC,
            query_store_wait_stats.plan_id ASC;
        """;

    private readonly ReadOnlySqlConnectionFactory _connectionFactory;

    public AzureSqlQueryStoreCollector()
        : this(new ReadOnlySqlConnectionFactory())
    {
    }

    public AzureSqlQueryStoreCollector(ReadOnlySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<QueryStoreSnapshot> CaptureAsync(
        string readOnlyConnectionString,
        QueryStoreObservationOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(readOnlyConnectionString);
        ValidateOptions(options);

        var connection = await _connectionFactory
            .CreateOpenConnectionAsync(readOnlyConnectionString, cancellationToken)
            .ConfigureAwait(false);

        await using (connection.ConfigureAwait(false))
        {
            var databaseOptions = await ReadDatabaseOptionsAsync(connection, options, cancellationToken)
                .ConfigureAwait(false);

            if (string.Equals(databaseOptions.ActualState, "OFF", StringComparison.OrdinalIgnoreCase)
                || string.Equals(databaseOptions.ActualState, "ERROR", StringComparison.OrdinalIgnoreCase))
            {
                return new QueryStoreSnapshot
                {
                    CapturedAtUtc = DateTimeOffset.UtcNow,
                    LookbackWindow = options.LookbackWindow,
                    DatabaseOptions = databaseOptions,
                    Plans = Array.Empty<QueryStorePlanRecord>(),
                    Waits = Array.Empty<QueryStoreWaitStat>(),
                };
            }

            var startTimeUtc = DateTimeOffset.UtcNow - options.LookbackWindow;
            var plans = await ReadPlansAsync(connection, options, startTimeUtc, cancellationToken)
                .ConfigureAwait(false);
            var waits = await ReadWaitsAsync(connection, options, startTimeUtc, cancellationToken)
                .ConfigureAwait(false);

            return new QueryStoreSnapshot
            {
                CapturedAtUtc = DateTimeOffset.UtcNow,
                LookbackWindow = options.LookbackWindow,
                DatabaseOptions = databaseOptions,
                Plans = plans,
                Waits = waits,
            };
        }
    }

    internal static void ValidateOptions(QueryStoreObservationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.LookbackWindow <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.LookbackWindow, "LookbackWindow must be positive.");
        }

        if (options.MaxPlans <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.MaxPlans, "MaxPlans must be positive.");
        }

        if (options.MaxWaits <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.MaxWaits, "MaxWaits must be positive.");
        }

        if (options.CommandTimeoutSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.CommandTimeoutSeconds,
                "CommandTimeoutSeconds must be positive.");
        }
    }

    internal static QueryStoreDatabaseOptions ReadDatabaseOptions(DbDataReader reader)
    {
        return new QueryStoreDatabaseOptions
        {
            DesiredState = reader.GetString(reader.GetOrdinal("desired_state_desc")),
            ActualState = reader.GetString(reader.GetOrdinal("actual_state_desc")),
            ReadOnlyReason = Convert.ToInt64(reader.GetValue(reader.GetOrdinal("readonly_reason"))),
            CurrentStorageSizeMb = Convert.ToDouble(reader.GetValue(reader.GetOrdinal("current_storage_size_mb"))),
            MaxStorageSizeMb = Convert.ToDouble(reader.GetValue(reader.GetOrdinal("max_storage_size_mb"))),
        };
    }

    internal static QueryStorePlanRecord ReadPlanRecord(DbDataReader reader)
    {
        return new QueryStorePlanRecord
        {
            QueryId = reader.GetInt64(reader.GetOrdinal("query_id")),
            PlanId = reader.GetInt64(reader.GetOrdinal("plan_id")),
            QueryTextId = reader.GetInt64(reader.GetOrdinal("query_text_id")),
            StatementSqlHandle = GetNullableString(reader, "statement_sql_handle"),
            ObjectId = GetNullableInt64(reader, "object_id"),
            QueryHash = reader.GetString(reader.GetOrdinal("query_hash")),
            QueryText = reader.GetString(reader.GetOrdinal("query_sql_text")),
            ObjectName = GetNullableString(reader, "object_name"),
            QueryParameterizationType = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("query_parameterization_type"))),
            QueryParameterizationTypeDescription = reader.GetString(reader.GetOrdinal("query_parameterization_type_desc")),
            ExecutionCount = reader.GetInt64(reader.GetOrdinal("execution_count")),
            MeanDuration = FromMicroseconds(Convert.ToDouble(reader.GetValue(reader.GetOrdinal("mean_duration_us")))),
            MaxDuration = FromMicroseconds(Convert.ToDouble(reader.GetValue(reader.GetOrdinal("max_duration_us")))),
            MeanCpuMilliseconds = Convert.ToDouble(reader.GetValue(reader.GetOrdinal("mean_cpu_us"))) / 1000d,
            MeanLogicalReads = Convert.ToDouble(reader.GetValue(reader.GetOrdinal("mean_logical_reads"))),
            LastExecutionTimeUtc = GetNullableDateTimeOffset(reader, "last_execution_time"),
        };
    }

    internal static QueryStoreWaitStat ReadWaitStat(DbDataReader reader)
    {
        return new QueryStoreWaitStat
        {
            QueryId = reader.GetInt64(reader.GetOrdinal("query_id")),
            PlanId = reader.GetInt64(reader.GetOrdinal("plan_id")),
            WaitCategory = reader.GetString(reader.GetOrdinal("wait_category_desc")),
            AverageQueryWaitMilliseconds = Convert.ToDouble(reader.GetValue(reader.GetOrdinal("avg_query_wait_time_ms"))),
            TotalWaitMilliseconds = Convert.ToDouble(reader.GetValue(reader.GetOrdinal("total_query_wait_time_ms"))),
        };
    }

    private static TimeSpan FromMicroseconds(double value)
    {
        return TimeSpan.FromMicroseconds(value);
    }

    private static string? GetNullableString(DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTimeOffset? GetNullableDateTimeOffset(DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<DateTimeOffset>(ordinal);
    }

    private static long? GetNullableInt64(DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : Convert.ToInt64(reader.GetValue(ordinal));
    }

    private static async Task<QueryStoreDatabaseOptions> ReadDatabaseOptionsAsync(
        SqlConnection connection,
        QueryStoreObservationOptions options,
        CancellationToken cancellationToken)
    {
        using var command = CreateCommand(connection, QueryStoreOptionsSql, options.CommandTimeoutSeconds);
        DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await using (reader.ConfigureAwait(false))
        {
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("sys.database_query_store_options returned no rows.");
            }

            return ReadDatabaseOptions(reader);
        }
    }

    private static async Task<IReadOnlyList<QueryStorePlanRecord>> ReadPlansAsync(
        SqlConnection connection,
        QueryStoreObservationOptions options,
        DateTimeOffset startTimeUtc,
        CancellationToken cancellationToken)
    {
        using var command = CreateCommand(connection, QueryStorePlansSql, options.CommandTimeoutSeconds);
        command.Parameters.Add(new SqlParameter("@StartTimeUtc", System.Data.SqlDbType.DateTimeOffset) { Value = startTimeUtc });
        command.Parameters.Add(new SqlParameter("@MaxPlans", System.Data.SqlDbType.Int) { Value = options.MaxPlans });

        return await ReadRecordsAsync(command, ReadPlanRecord, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<QueryStoreWaitStat>> ReadWaitsAsync(
        SqlConnection connection,
        QueryStoreObservationOptions options,
        DateTimeOffset startTimeUtc,
        CancellationToken cancellationToken)
    {
        using var command = CreateCommand(connection, QueryStoreWaitsSql, options.CommandTimeoutSeconds);
        command.Parameters.Add(new SqlParameter("@StartTimeUtc", System.Data.SqlDbType.DateTimeOffset) { Value = startTimeUtc });
        command.Parameters.Add(new SqlParameter("@MaxWaits", System.Data.SqlDbType.Int) { Value = options.MaxWaits });

        return await ReadRecordsAsync(command, ReadWaitStat, cancellationToken).ConfigureAwait(false);
    }

    private static SqlCommand CreateCommand(SqlConnection connection, string commandText, int commandTimeoutSeconds)
    {
        var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.CommandTimeout = commandTimeoutSeconds;
        return command;
    }

    private static async Task<IReadOnlyList<T>> ReadRecordsAsync<T>(
        SqlCommand command,
        Func<DbDataReader, T> map,
        CancellationToken cancellationToken)
    {
        List<T> records = new();
        DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await using (reader.ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                records.Add(map(reader));
            }

            return records;
        }
    }
}
