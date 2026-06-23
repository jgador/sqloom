using System;

namespace Sqloom.Core.QueryStore;

/// <summary>
/// Controls how much Query Store history is pulled into a snapshot.
/// </summary>
public sealed class QueryStoreOptions
{
    public TimeSpan LookbackWindow { get; init; } = TimeSpan.FromDays(7);

    public int MaxPlans { get; init; } = 25;

    public int MaxWaits { get; init; } = 10;

    public int CommandTimeoutSeconds { get; init; } = 30;
}
