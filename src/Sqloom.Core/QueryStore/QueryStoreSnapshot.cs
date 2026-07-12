using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.QueryStore;

/// <summary>
/// Represents one captured Query Store observation window.
/// </summary>
public sealed class QueryStoreSnapshot
{
    /// <summary>
    /// Gets the captured at utc.
    /// </summary>
    [JsonPropertyName("capturedAtUtc")]
    public required DateTimeOffset CapturedAtUtc { get; init; }

    /// <summary>
    /// Gets the lookback window.
    /// </summary>
    [JsonPropertyName("lookbackWindow")]
    public required TimeSpan LookbackWindow { get; init; }

    /// <summary>
    /// Gets the database options.
    /// </summary>
    [JsonPropertyName("databaseOptions")]
    public required QueryStoreDatabaseOptions DatabaseOptions { get; init; }

    /// <summary>
    /// Gets the workload profile name.
    /// </summary>
    [JsonPropertyName("workloadProfileName")]
    public string? WorkloadProfileName { get; init; }

    /// <summary>
    /// Gets the discovered object catalog.
    /// </summary>
    [JsonPropertyName("discoveredObjectCatalog")]
    public DbObjectCatalog? DiscoveredObjectCatalog { get; init; }

    /// <summary>
    /// Gets the plans.
    /// </summary>
    [JsonPropertyName("plans")]
    public required IReadOnlyList<QueryStorePlanRecord> Plans { get; init; }

    /// <summary>
    /// Gets the waits.
    /// </summary>
    [JsonPropertyName("waits")]
    public required IReadOnlyList<QueryStoreWaitStat> Waits { get; init; }
}

/// <summary>
/// Captures the database-level Query Store state at snapshot time.
/// </summary>
public sealed class QueryStoreDatabaseOptions
{
    /// <summary>
    /// Gets the desired state.
    /// </summary>
    [JsonPropertyName("desiredState")]
    public required string DesiredState { get; init; }

    /// <summary>
    /// Gets the actual state.
    /// </summary>
    [JsonPropertyName("actualState")]
    public required string ActualState { get; init; }

    /// <summary>
    /// Gets the raw SQL Server Query Store readonly_reason code captured at snapshot time.
    /// </summary>
    [JsonPropertyName("readOnlyReason")]
    public long ReadOnlyReason { get; init; }

    /// <summary>
    /// Gets the current storage size mb.
    /// </summary>
    [JsonPropertyName("currentStorageSizeMb")]
    public double CurrentStorageSizeMb { get; init; }

    /// <summary>
    /// Gets the max storage size mb.
    /// </summary>
    [JsonPropertyName("maxStorageSizeMb")]
    public double MaxStorageSizeMb { get; init; }
}

/// <summary>
/// Captures one Query Store wait slice aggregated for a plan.
/// </summary>
public sealed class QueryStoreWaitStat
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
    /// Gets the wait category.
    /// </summary>
    [JsonPropertyName("waitCategory")]
    public required string WaitCategory { get; init; }

    /// <summary>
    /// Gets the avg wait ms.
    /// </summary>
    [JsonPropertyName("avgWaitMs")]
    public double AvgWaitMs { get; init; }

    /// <summary>
    /// Gets the total wait milliseconds.
    /// </summary>
    [JsonPropertyName("totalWaitMilliseconds")]
    public double TotalWaitMilliseconds { get; init; }

    /// <summary>
    /// Gets the classification.
    /// </summary>
    [JsonPropertyName("classification")]
    public QueryWorkloadClassification? Classification { get; init; }
}
