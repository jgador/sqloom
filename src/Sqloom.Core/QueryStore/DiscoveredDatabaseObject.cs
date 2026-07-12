using System.Text.Json.Serialization;

namespace Sqloom.Core.QueryStore;

/// <summary>
/// Describes one database object discovered for Query Store classification.
/// </summary>
public sealed class DiscoveredDatabaseObject
{
    /// <summary>
    /// Gets the schema name.
    /// </summary>
    [JsonPropertyName("schemaName")]
    public required string SchemaName { get; init; }

    /// <summary>
    /// Gets the object name.
    /// </summary>
    [JsonPropertyName("objectName")]
    public required string ObjectName { get; init; }

    /// <summary>
    /// Gets the fully qualified name.
    /// </summary>
    [JsonPropertyName("fullyQualifiedName")]
    public required string FullyQualifiedName { get; init; }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    [JsonPropertyName("kind")]
    public required DbObjectKind Kind { get; init; }
}
