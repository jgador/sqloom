using System;
using System.Text.Json.Serialization;

namespace Sqloom.Pipeline.QueryStore;

/// <summary>
/// Represents one hot query plan slice aggregated out of Query Store runtime data.
/// </summary>
public sealed class QueryStorePlanRecord
{
    /// <summary>
    /// Gets the query id.
    /// </summary>
    [JsonPropertyName("queryId")]
    public required long QueryId { get; init; }

    /// <summary>
    /// Gets the plan id.
    /// </summary>
    [JsonPropertyName("planId")]
    public required long PlanId { get; init; }

    /// <summary>
    /// Gets the query text id.
    /// </summary>
    [JsonPropertyName("queryTextId")]
    public required long QueryTextId { get; init; }

    /// <summary>
    /// Gets the normalized statement_sql_handle used as the highest-confidence correlation key.
    /// </summary>
    [JsonPropertyName("statementSqlHandle")]
    public string? StatementSqlHandle { get; init; }

    /// <summary>
    /// Gets the object id.
    /// </summary>
    [JsonPropertyName("objectId")]
    public long? ObjectId { get; init; }

    /// <summary>
    /// Gets the query hash.
    /// </summary>
    [JsonPropertyName("queryHash")]
    public required string QueryHash { get; init; }

    /// <summary>
    /// Gets the query text.
    /// </summary>
    [JsonPropertyName("queryText")]
    public required string QueryText { get; init; }

    /// <summary>
    /// Gets the object name.
    /// </summary>
    [JsonPropertyName("objectName")]
    public string? ObjectName { get; init; }

    /// <summary>
    /// Gets the raw Query Store parameterization mode code preserved for handle-resolution evidence.
    /// </summary>
    [JsonPropertyName("queryParameterizationType")]
    public int QueryParameterizationType { get; init; }

    /// <summary>
    /// Gets the param type description.
    /// </summary>
    [JsonPropertyName("paramTypeDescription")]
    public string ParamTypeDescription { get; init; } = string.Empty;

    /// <summary>
    /// Gets the execution count.
    /// </summary>
    [JsonPropertyName("executionCount")]
    public long ExecutionCount { get; init; }

    /// <summary>
    /// Gets the mean duration.
    /// </summary>
    [JsonPropertyName("meanDuration")]
    public TimeSpan MeanDuration { get; init; }

    /// <summary>
    /// Gets the max duration.
    /// </summary>
    [JsonPropertyName("maxDuration")]
    public TimeSpan MaxDuration { get; init; }

    /// <summary>
    /// Gets the mean cpu milliseconds.
    /// </summary>
    [JsonPropertyName("meanCpuMilliseconds")]
    public double MeanCpuMilliseconds { get; init; }

    /// <summary>
    /// Gets the mean logical reads.
    /// </summary>
    [JsonPropertyName("meanLogicalReads")]
    public double MeanLogicalReads { get; init; }

    /// <summary>
    /// Gets the last execution time utc.
    /// </summary>
    [JsonPropertyName("lastExecutionTimeUtc")]
    public DateTimeOffset? LastExecutionTimeUtc { get; init; }

    /// <summary>
    /// Gets the classification.
    /// </summary>
    [JsonPropertyName("classification")]
    public QueryWorkloadClassification? Classification { get; init; }
}
