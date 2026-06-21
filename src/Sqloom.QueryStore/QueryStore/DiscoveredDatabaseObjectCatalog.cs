using System;
using System.Collections.Generic;

namespace Sqloom.QueryStore.QueryStore;

/// <summary>
/// Captures the discovered database object catalog used for classification.
/// </summary>
public sealed class DiscoveredDatabaseObjectCatalog
{
    public required DateTimeOffset CapturedAtUtc { get; init; }

    public required string SourceName { get; init; }

    public required bool IsComplete { get; init; }

    public required IReadOnlyList<string> Warnings { get; init; }

    public required IReadOnlyList<DiscoveredDatabaseObject> Objects { get; init; }
}
