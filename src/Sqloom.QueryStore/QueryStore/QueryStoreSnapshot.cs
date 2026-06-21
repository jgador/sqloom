using System;
using System.Collections.Generic;

namespace Sqloom.QueryStore.QueryStore;

/// <summary>
/// Represents one captured Query Store observation window.
/// </summary>
public sealed class QueryStoreSnapshot
{
    public required DateTimeOffset CapturedAtUtc { get; init; }

    public required TimeSpan LookbackWindow { get; init; }

    public required QueryStoreDatabaseOptions DatabaseOptions { get; init; }

    public string? WorkloadProfileName { get; init; }

    public DiscoveredDatabaseObjectCatalog? DiscoveredObjectCatalog { get; init; }

    public required IReadOnlyList<QueryStorePlanRecord> Plans { get; init; }

    public required IReadOnlyList<QueryStoreWaitStat> Waits { get; init; }
}

/// <summary>
/// Captures the database-level Query Store state at snapshot time.
/// </summary>
public sealed class QueryStoreDatabaseOptions
{
    public required string DesiredState { get; init; }

    public required string ActualState { get; init; }

    public long ReadOnlyReason { get; init; }

    public double CurrentStorageSizeMb { get; init; }

    public double MaxStorageSizeMb { get; init; }
}

/// <summary>
/// Captures one Query Store wait slice aggregated for a plan.
/// </summary>
public sealed class QueryStoreWaitStat
{
    public required long QueryId { get; init; }

    public required long PlanId { get; init; }

    public required string WaitCategory { get; init; }

    public double AverageQueryWaitMilliseconds { get; init; }

    public double TotalWaitMilliseconds { get; init; }

    public QueryWorkloadClassification? Classification { get; init; }
}
