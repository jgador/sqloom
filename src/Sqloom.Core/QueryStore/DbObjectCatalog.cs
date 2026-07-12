using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.QueryStore;

/// <summary>
/// Captures the discovered database object catalog used for classification.
/// </summary>
public sealed class DbObjectCatalog
{
    /// <summary>
    /// Gets the captured at utc.
    /// </summary>
    [JsonPropertyName("capturedAtUtc")]
    public required DateTimeOffset CapturedAtUtc { get; init; }

    /// <summary>
    /// Gets the source name.
    /// </summary>
    [JsonPropertyName("sourceName")]
    public required string SourceName { get; init; }

    /// <summary>
    /// Gets the is complete.
    /// </summary>
    [JsonPropertyName("isComplete")]
    public required bool IsComplete { get; init; }

    /// <summary>
    /// Gets the warnings.
    /// </summary>
    [JsonPropertyName("warnings")]
    public required IReadOnlyList<string> Warnings { get; init; }

    /// <summary>
    /// Gets the objects.
    /// </summary>
    [JsonPropertyName("objects")]
    public required IReadOnlyList<DiscoveredDatabaseObject> Objects { get; init; }
}
