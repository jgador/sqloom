using System.Collections.Generic;

namespace Sqloom.QueryStore.QueryStore;

/// <summary>
/// Captures the workload classification attached to a Query Store row.
/// </summary>
public sealed class QueryWorkloadClassification
{
    public required QueryWorkloadKind Kind { get; init; }

    public required double Confidence { get; init; }

    public required bool IncludeInAppOnly { get; init; }

    public required IReadOnlyList<string> Reasons { get; init; }
}
