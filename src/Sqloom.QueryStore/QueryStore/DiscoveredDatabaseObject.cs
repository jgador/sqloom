using System.Text.Json.Serialization;

namespace Sqloom.QueryStore.QueryStore;

/// <summary>
/// Describes one database object discovered for Query Store classification.
/// </summary>
public sealed class DiscoveredDatabaseObject
{
    [JsonPropertyName("schemaName")]
    public required string SchemaName { get; init; }

    [JsonPropertyName("objectName")]
    public required string ObjectName { get; init; }

    [JsonPropertyName("fullyQualifiedName")]
    public required string FullyQualifiedName { get; init; }

    [JsonPropertyName("kind")]
    public required DbObjectKind Kind { get; init; }
}
