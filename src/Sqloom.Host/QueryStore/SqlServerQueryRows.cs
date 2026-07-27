using System;

namespace Sqloom.Host.QueryStore;

/// <summary>
/// Represents one row returned from SQL Server Query Store database options.
/// </summary>
internal sealed class QueryStoreDatabaseOptionsRow
{
    /// <summary>
    /// The requested Query Store state from <c>sys.database_query_store_options.desired_state_desc</c>.
    /// </summary>
    public string DesiredState { get; set; } = string.Empty;

    /// <summary>
    /// The effective Query Store state from <c>sys.database_query_store_options.actual_state_desc</c>.
    /// </summary>
    public string ActualState { get; set; } = string.Empty;

    /// <summary>
    /// The Query Store readonly reason bitmask from <c>sys.database_query_store_options.readonly_reason</c>.
    /// </summary>
    public long ReadOnlyReason { get; set; }

    /// <summary>
    /// The current Query Store storage consumption in megabytes from <c>current_storage_size_mb</c>.
    /// </summary>
    public long CurrentStorageSizeMb { get; set; }

    /// <summary>
    /// The configured Query Store storage limit in megabytes from <c>max_storage_size_mb</c>.
    /// </summary>
    public long MaxStorageSizeMb { get; set; }
}

/// <summary>
/// Represents one aggregated SQL Server Query Store plan row.
/// </summary>
internal sealed class QueryStorePlanRow
{
    /// <summary>
    /// The Query Store query identifier from <c>sys.query_store_query.query_id</c>.
    /// </summary>
    public long QueryId { get; set; }

    /// <summary>
    /// The Query Store plan identifier from <c>sys.query_store_plan.plan_id</c>.
    /// </summary>
    public long PlanId { get; set; }

    /// <summary>
    /// The Query Store query-text identifier from <c>sys.query_store_query.query_text_id</c>.
    /// </summary>
    public long QueryTextId { get; set; }

    /// <summary>
    /// The hexadecimal statement SQL handle, or <see langword="null"/> when SQL Server has no handle.
    /// </summary>
    public string? StatementSqlHandle { get; set; }

    /// <summary>
    /// The owning database object identifier, or <see langword="null"/> for an ad hoc query.
    /// </summary>
    public long? ObjectId { get; set; }

    /// <summary>
    /// The numeric parameterization mode from <c>sys.query_store_query.query_parameterization_type</c>.
    /// </summary>
    public int QueryParameterizationType { get; set; }

    /// <summary>
    /// The parameterization mode description from <c>query_parameterization_type_desc</c>.
    /// </summary>
    public string ParamTypeDescription { get; set; } = string.Empty;

    /// <summary>
    /// The hexadecimal query hash converted from <c>sys.query_store_query.query_hash</c>.
    /// </summary>
    public string QueryHash { get; set; } = string.Empty;

    /// <summary>
    /// The captured SQL text from <c>sys.query_store_query_text.query_sql_text</c>.
    /// </summary>
    public string QueryText { get; set; } = string.Empty;

    /// <summary>
    /// The bracketed schema-qualified object name, or <see langword="null"/> for an ad hoc query.
    /// </summary>
    public string? ObjectName { get; set; }

    /// <summary>
    /// The aggregated execution count returned as <c>execution_count</c>.
    /// </summary>
    public long ExecutionCount { get; set; }

    /// <summary>
    /// The mean execution duration in microseconds returned as <c>mean_duration_us</c>.
    /// </summary>
    public double MeanDurationMicroseconds { get; set; }

    /// <summary>
    /// The maximum execution duration in microseconds returned as <c>max_duration_us</c>.
    /// </summary>
    public double MaxDurationMicroseconds { get; set; }

    /// <summary>
    /// The mean CPU consumption in microseconds returned as <c>mean_cpu_us</c>.
    /// </summary>
    public double MeanCpuMicroseconds { get; set; }

    /// <summary>
    /// The mean logical-read count returned as <c>mean_logical_reads</c>.
    /// </summary>
    public double MeanLogicalReads { get; set; }

    /// <summary>
    /// The latest execution timestamp, or <see langword="null"/> when Query Store has no value.
    /// </summary>
    public DateTimeOffset? LastExecutionTimeUtc { get; set; }
}

/// <summary>
/// Represents one aggregated SQL Server Query Store wait-statistics row.
/// </summary>
internal sealed class QueryStoreWaitRow
{
    /// <summary>
    /// The Query Store query identifier from <c>sys.query_store_query.query_id</c>.
    /// </summary>
    public long QueryId { get; set; }

    /// <summary>
    /// The Query Store plan identifier from <c>sys.query_store_wait_stats.plan_id</c>.
    /// </summary>
    public long PlanId { get; set; }

    /// <summary>
    /// The wait-category description from <c>wait_category_desc</c>.
    /// </summary>
    public string WaitCategory { get; set; } = string.Empty;

    /// <summary>
    /// The average wait time in milliseconds returned as <c>avg_query_wait_time_ms</c>.
    /// </summary>
    public double AvgWaitMs { get; set; }

    /// <summary>
    /// The total wait time in milliseconds returned as <c>total_query_wait_time_ms</c>.
    /// </summary>
    public double TotalWaitMilliseconds { get; set; }
}

/// <summary>
/// Represents one user-defined SQL Server object discovered from the system catalog.
/// </summary>
internal sealed class DiscoveredDatabaseObjectRow
{
    /// <summary>
    /// The containing schema name from <c>sys.schemas.name</c>.
    /// </summary>
    public string SchemaName { get; set; } = string.Empty;

    /// <summary>
    /// The database object name from <c>sys.objects.name</c>.
    /// </summary>
    public string ObjectName { get; set; } = string.Empty;

    /// <summary>
    /// The normalized object-kind label returned as <c>object_kind</c>.
    /// </summary>
    public string ObjectKind { get; set; } = string.Empty;
}

/// <summary>
/// Represents the SQL Server VIEW DEFINITION permission probe result.
/// </summary>
internal sealed class ViewDefinitionPermissionRow
{
    /// <summary>
    /// Whether <c>HAS_PERMS_BY_NAME</c> reported VIEW DEFINITION permission.
    /// </summary>
    public bool HasViewDefinition { get; set; }
}

/// <summary>
/// Represents one SQL Server statement-handle resolution result.
/// </summary>
internal sealed class SqlStatementHandleRow
{
    /// <summary>
    /// The resolved parameterization mode, or <see langword="null"/> when no statement matched.
    /// </summary>
    public int? QueryParameterizationType { get; set; }

    /// <summary>
    /// The hexadecimal statement SQL handle, or <see langword="null"/> when no statement matched.
    /// </summary>
    public string? StatementSqlHandle { get; set; }
}
