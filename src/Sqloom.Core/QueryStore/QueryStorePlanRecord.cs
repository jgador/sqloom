using System;
using System.Text.Json.Serialization;

namespace Sqloom.Core.QueryStore;

/// <summary>
/// Represents one hot query plan slice aggregated out of Query Store runtime data.
/// </summary>
public sealed class QueryStorePlanRecord
{
    [JsonPropertyName("queryId")]
    public required long QueryId { get; init; }

    [JsonPropertyName("planId")]
    public required long PlanId { get; init; }

    [JsonPropertyName("queryTextId")]
    public required long QueryTextId { get; init; }

    [JsonPropertyName("statementSqlHandle")]
    public string? StatementSqlHandle { get; init; }

    [JsonPropertyName("objectId")]
    public long? ObjectId { get; init; }

    [JsonPropertyName("queryHash")]
    public required string QueryHash { get; init; }

    [JsonPropertyName("queryText")]
    public required string QueryText { get; init; }

    [JsonPropertyName("objectName")]
    public string? ObjectName { get; init; }

    [JsonPropertyName("queryParameterizationType")]
    public int QueryParameterizationType { get; init; }

    [JsonPropertyName("paramTypeDescription")]
    public string ParamTypeDescription { get; init; } = string.Empty;

    [JsonPropertyName("executionCount")]
    public long ExecutionCount { get; init; }

    [JsonPropertyName("meanDuration")]
    public TimeSpan MeanDuration { get; init; }

    [JsonPropertyName("maxDuration")]
    public TimeSpan MaxDuration { get; init; }

    [JsonPropertyName("meanCpuMilliseconds")]
    public double MeanCpuMilliseconds { get; init; }

    [JsonPropertyName("meanLogicalReads")]
    public double MeanLogicalReads { get; init; }

    [JsonPropertyName("lastExecutionTimeUtc")]
    public DateTimeOffset? LastExecutionTimeUtc { get; init; }

    [JsonPropertyName("classification")]
    public QueryWorkloadClassification? Classification { get; init; }
}
