using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.QueryStore.QueryStore;

/// <summary>
/// Captures the discovered database object catalog used for classification.
/// </summary>
public sealed class DiscoveredDatabaseObjectCatalog
{
    [JsonPropertyName("capturedAtUtc")]
    public required DateTimeOffset CapturedAtUtc { get; init; }

    [JsonPropertyName("sourceName")]
    public required string SourceName { get; init; }

    [JsonPropertyName("isComplete")]
    public required bool IsComplete { get; init; }

    [JsonPropertyName("warnings")]
    public required IReadOnlyList<string> Warnings { get; init; }

    [JsonPropertyName("objects")]
    public required IReadOnlyList<DiscoveredDatabaseObject> Objects { get; init; }
}
