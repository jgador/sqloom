namespace Sqloom.QueryStore.QueryStore;

/// <summary>
/// Describes the Query Store object catalog and classification hints for an app.
/// </summary>
public sealed class QueryStoreWorkloadProfile
{
    public static QueryStoreWorkloadProfile Empty { get; } = new();

    public string Name { get; init; } = "Default";

    public DiscoveredDatabaseObjectCatalog? DiscoveredObjectCatalog { get; init; }

    public QueryStoreWorkloadProfile WithDiscoveredObjectCatalog(DiscoveredDatabaseObjectCatalog? discoveredObjectCatalog)
    {
        return new QueryStoreWorkloadProfile
        {
            Name = Name,
            DiscoveredObjectCatalog = discoveredObjectCatalog,
        };
    }
}
