namespace Sqloom.Core.QueryStore;

/// <summary>
/// Describes the Query Store object catalog and classification hints for an app.
/// </summary>
public sealed class WorkloadProfile
{
    /// <summary>
    /// Gets the empty.
    /// </summary>
    public static WorkloadProfile Empty { get; } = new();

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name { get; init; } = "Default";

    /// <summary>
    /// Gets the discovered object catalog.
    /// </summary>
    public DbObjectCatalog? DiscoveredObjectCatalog { get; init; }

    /// <summary>
    /// Returns a profile associated with the supplied discovered object catalog.
    /// </summary>
    public WorkloadProfile WithDiscoveredObjectCatalog(DbObjectCatalog? discoveredObjectCatalog)
    {
        return new WorkloadProfile
        {
            Name = Name,
            DiscoveredObjectCatalog = discoveredObjectCatalog,
        };
    }
}
