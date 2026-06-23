using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.QueryStore.QueryStore;

/// <summary>
/// Represents one captured Query Store observation window.
/// </summary>
public sealed class QueryStoreSnapshot
{
    [JsonPropertyName("capturedAtUtc")]
    public required DateTimeOffset CapturedAtUtc { get; init; }

    [JsonPropertyName("lookbackWindow")]
    public required TimeSpan LookbackWindow { get; init; }

    [JsonPropertyName("databaseOptions")]
    public required QueryStoreDatabaseOptions DatabaseOptions { get; init; }

    [JsonPropertyName("workloadProfileName")]
    public string? WorkloadProfileName { get; init; }

    [JsonPropertyName("discoveredObjectCatalog")]
    public DbObjectCatalog? DiscoveredObjectCatalog { get; init; }

    [JsonPropertyName("plans")]
    public required IReadOnlyList<QueryStorePlanRecord> Plans { get; init; }

    [JsonPropertyName("waits")]
    public required IReadOnlyList<QueryStoreWaitStat> Waits { get; init; }
}

/// <summary>
/// Captures the database-level Query Store state at snapshot time.
/// </summary>
public sealed class QueryStoreDatabaseOptions
{
    [JsonPropertyName("desiredState")]
    public required string DesiredState { get; init; }

    [JsonPropertyName("actualState")]
    public required string ActualState { get; init; }

    [JsonPropertyName("readOnlyReason")]
    public long ReadOnlyReason { get; init; }

    [JsonPropertyName("currentStorageSizeMb")]
    public double CurrentStorageSizeMb { get; init; }

    [JsonPropertyName("maxStorageSizeMb")]
    public double MaxStorageSizeMb { get; init; }
}

/// <summary>
/// Captures one Query Store wait slice aggregated for a plan.
/// </summary>
public sealed class QueryStoreWaitStat
{
    [JsonPropertyName("queryId")]
    public required long QueryId { get; init; }

    [JsonPropertyName("planId")]
    public required long PlanId { get; init; }

    [JsonPropertyName("waitCategory")]
    public required string WaitCategory { get; init; }

    [JsonPropertyName("avgWaitMs")]
    public double AvgWaitMs { get; init; }

    [JsonPropertyName("totalWaitMilliseconds")]
    public double TotalWaitMilliseconds { get; init; }

    [JsonPropertyName("classification")]
    public QueryWorkloadClassification? Classification { get; init; }
}
