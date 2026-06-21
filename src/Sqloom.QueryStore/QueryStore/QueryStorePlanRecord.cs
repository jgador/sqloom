using System;

namespace Sqloom.QueryStore.QueryStore;

/// <summary>
/// Represents one hot query plan slice aggregated out of Query Store runtime data.
/// </summary>
public sealed class QueryStorePlanRecord
{
    public required long QueryId { get; init; }

    public required long PlanId { get; init; }

    public required long QueryTextId { get; init; }

    public string? StatementSqlHandle { get; init; }

    public long? ObjectId { get; init; }

    public required string QueryHash { get; init; }

    public required string QueryText { get; init; }

    public string? ObjectName { get; init; }

    public int QueryParameterizationType { get; init; }

    public string QueryParameterizationTypeDescription { get; init; } = string.Empty;

    public long ExecutionCount { get; init; }

    public TimeSpan MeanDuration { get; init; }

    public TimeSpan MaxDuration { get; init; }

    public double MeanCpuMilliseconds { get; init; }

    public double MeanLogicalReads { get; init; }

    public DateTimeOffset? LastExecutionTimeUtc { get; init; }

    public QueryWorkloadClassification? Classification { get; init; }
}
