namespace Sqloom.QueryStore.QueryStore;

/// <summary>
/// Describes one database object discovered for Query Store classification.
/// </summary>
public sealed class DiscoveredDatabaseObject
{
    public required string SchemaName { get; init; }

    public required string ObjectName { get; init; }

    public required string FullyQualifiedName { get; init; }

    public required DiscoveredDatabaseObjectKind Kind { get; init; }
}
