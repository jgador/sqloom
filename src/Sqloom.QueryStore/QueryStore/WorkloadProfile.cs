namespace Sqloom.QueryStore.QueryStore;

/// <summary>
/// Describes the Query Store object catalog and classification hints for an app.
/// </summary>
public sealed class WorkloadProfile
{
    public static WorkloadProfile Empty { get; } = new();

    public string Name { get; init; } = "Default";

    public DbObjectCatalog? DiscoveredObjectCatalog { get; init; }

    public WorkloadProfile WithDiscoveredObjectCatalog(DbObjectCatalog? discoveredObjectCatalog)
    {
        return new WorkloadProfile
        {
            Name = Name,
            DiscoveredObjectCatalog = discoveredObjectCatalog,
        };
    }
}
