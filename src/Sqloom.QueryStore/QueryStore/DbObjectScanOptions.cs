namespace Sqloom.QueryStore.QueryStore;

/// <summary>
/// Carries options for discovered database object collection.
/// </summary>
public sealed class DbObjectScanOptions
{
    public int CommandTimeoutSeconds { get; init; } = 30;
}
