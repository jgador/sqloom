namespace Sqloom.Pipeline.QueryStore;

/// <summary>
/// Carries options for discovered database object collection.
/// </summary>
public sealed class DbObjectScanOptions
{
    /// <summary>
    /// Gets the command timeout seconds.
    /// </summary>
    public int CommandTimeoutSeconds { get; init; } = 30;
}
