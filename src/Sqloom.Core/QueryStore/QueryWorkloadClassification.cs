using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.QueryStore;

/// <summary>
/// Captures the workload classification attached to a Query Store row.
/// </summary>
public sealed class QueryWorkloadClassification
{
    [JsonPropertyName("kind")]
    public required QueryWorkloadKind Kind { get; init; }

    [JsonPropertyName("confidence")]
    public required double Confidence { get; init; }

    [JsonPropertyName("includeInAppOnly")]
    public required bool IncludeInAppOnly { get; init; }

    [JsonPropertyName("reasons")]
    public required IReadOnlyList<string> Reasons { get; init; }
}
