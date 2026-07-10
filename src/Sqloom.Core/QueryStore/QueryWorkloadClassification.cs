using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.QueryStore;

/// <summary>
/// Captures the workload classification attached to a Query Store row.
/// </summary>
public sealed class QueryWorkloadClassification
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    [JsonPropertyName("kind")]
    public required QueryWorkloadKind Kind { get; init; }

    /// <summary>
    /// Gets the confidence.
    /// </summary>
    [JsonPropertyName("confidence")]
    public required double Confidence { get; init; }

    /// <summary>
    /// Gets the include in app only.
    /// </summary>
    [JsonPropertyName("includeInAppOnly")]
    public required bool IncludeInAppOnly { get; init; }

    /// <summary>
    /// Gets the reasons.
    /// </summary>
    [JsonPropertyName("reasons")]
    public required IReadOnlyList<string> Reasons { get; init; }
}
