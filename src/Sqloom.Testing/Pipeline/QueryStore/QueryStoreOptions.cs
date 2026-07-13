using System;

namespace Sqloom.Pipeline.QueryStore;

/// <summary>
/// Controls how much Query Store history is pulled into a snapshot.
/// </summary>
public sealed class QueryStoreOptions
{
    /// <summary>
    /// Gets the lookback window.
    /// </summary>
    public TimeSpan LookbackWindow { get; init; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Gets the max plans.
    /// </summary>
    public int MaxPlans { get; init; } = 25;

    /// <summary>
    /// Gets the max waits.
    /// </summary>
    public int MaxWaits { get; init; } = 10;

    /// <summary>
    /// Gets the command timeout seconds.
    /// </summary>
    public int CommandTimeoutSeconds { get; init; } = 30;
}
