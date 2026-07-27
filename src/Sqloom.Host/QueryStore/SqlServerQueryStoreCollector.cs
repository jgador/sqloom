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
/// Collects Query Store evidence from SQL Server or Azure SQL using a readonly connection.
/// </summary>
internal static class SqlServerQueryStoreCollector
{
    // Reads Query Store state and storage so observe can report whether capture is usable.
    private const string QueryStoreOptionsSql = """
        SELECT
            desired_state_desc,
            actual_state_desc,
            CONVERT(bigint, readonly_reason) AS readonly_reason,
            CONVERT(bigint, current_storage_size_mb) AS current_storage_size_mb,
            CONVERT(bigint, max_storage_size_mb) AS max_storage_size_mb
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
            CONVERT(float, plan_runtime.max_duration_us) AS max_duration_us,
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
            MeanDurationMicroseconds DESC,
            MeanCpuMicroseconds DESC,
            ExecutionCount DESC,
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
            TotalWaitMilliseconds DESC,
            AvgWaitMs DESC,
            query_store_wait_stats.plan_id ASC;
        """;

    /// <summary>
    /// Captures Query Store options, plan metrics, and wait stats from a readonly SQL Server connection.
    /// </summary>
    public static async Task<QueryStoreSnapshot> CaptureAsync(
        string readOnlyConnectionString,
        QueryStoreOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(readOnlyConnectionString);
        ValidateOptions(options);
        SqlServerQueryTypeMaps.EnsureRegistered();

        var connection = await ReadOnlySqlConnectionFactory
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

    internal static void ValidateOptions(QueryStoreOptions options)
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

    internal static QueryStoreDatabaseOptions MapDatabaseOptions(QueryStoreDatabaseOptionsRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return new QueryStoreDatabaseOptions
        {
            DesiredState = row.DesiredState,
            ActualState = row.ActualState,
            ReadOnlyReason = row.ReadOnlyReason,
            CurrentStorageSizeMb = row.CurrentStorageSizeMb,
            MaxStorageSizeMb = row.MaxStorageSizeMb,
        };
    }

    internal static QueryStorePlanRecord MapPlanRecord(QueryStorePlanRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return new QueryStorePlanRecord
        {
            QueryId = row.QueryId,
            PlanId = row.PlanId,
            QueryTextId = row.QueryTextId,
            StatementSqlHandle = row.StatementSqlHandle,
            ObjectId = row.ObjectId,
            QueryHash = row.QueryHash,
            QueryText = row.QueryText,
            ObjectName = row.ObjectName,
            QueryParameterizationType = row.QueryParameterizationType,
            ParamTypeDescription = row.ParamTypeDescription,
            ExecutionCount = row.ExecutionCount,
            MeanDuration = FromMicroseconds(row.MeanDurationMicroseconds),
            MaxDuration = FromMicroseconds(row.MaxDurationMicroseconds),
            MeanCpuMilliseconds = row.MeanCpuMicroseconds / 1000d,
            MeanLogicalReads = row.MeanLogicalReads,
            LastExecutionTimeUtc = row.LastExecutionTimeUtc,
        };
    }

    internal static QueryStoreWaitStat MapWaitStat(QueryStoreWaitRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return new QueryStoreWaitStat
        {
            QueryId = row.QueryId,
            PlanId = row.PlanId,
            WaitCategory = row.WaitCategory,
            AvgWaitMs = row.AvgWaitMs,
            TotalWaitMilliseconds = row.TotalWaitMilliseconds,
        };
    }

    private static TimeSpan FromMicroseconds(double value)
    {
        return TimeSpan.FromMicroseconds(value);
    }

    private static async Task<QueryStoreDatabaseOptions> ReadDatabaseOptionsAsync(
        SqlConnection connection,
        QueryStoreOptions options,
        CancellationToken cancellationToken)
    {
        CommandDefinition command = new(
            QueryStoreOptionsSql,
            commandTimeout: options.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);
        var row = await connection.QueryFirstOrDefaultAsync<QueryStoreDatabaseOptionsRow>(command)
            .ConfigureAwait(false);
        if (row is null)
        {
            throw new InvalidOperationException("sys.database_query_store_options returned no rows.");
        }

        return MapDatabaseOptions(row);
    }

    private static async Task<IReadOnlyList<QueryStorePlanRecord>> ReadPlansAsync(
        SqlConnection connection,
        QueryStoreOptions options,
        DateTimeOffset startTimeUtc,
        CancellationToken cancellationToken)
    {
        CommandDefinition command = new(
            QueryStorePlansSql,
            new { StartTimeUtc = startTimeUtc, MaxPlans = options.MaxPlans },
            commandTimeout: options.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<QueryStorePlanRow>(command).ConfigureAwait(false);
        return rows.Select(MapPlanRecord).ToArray();
    }

    private static async Task<IReadOnlyList<QueryStoreWaitStat>> ReadWaitsAsync(
        SqlConnection connection,
        QueryStoreOptions options,
        DateTimeOffset startTimeUtc,
        CancellationToken cancellationToken)
    {
        CommandDefinition command = new(
            QueryStoreWaitsSql,
            new { StartTimeUtc = startTimeUtc, MaxWaits = options.MaxWaits },
            commandTimeout: options.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<QueryStoreWaitRow>(command).ConfigureAwait(false);
        return rows.Select(MapWaitStat).ToArray();
    }
}
