using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.QueryStore;

/// <summary>
/// Summarizes the matches produced by a correlation run.
/// </summary>
public sealed class QueryCorrelationSummary
{
    [JsonPropertyName("operationCount")]
    public int OperationCount { get; init; }

    [JsonPropertyName("capturedCommandCount")]
    public int CapturedCommandCount { get; init; }

    [JsonPropertyName("matchedCommandCount")]
    public int MatchedCommandCount { get; init; }

    [JsonPropertyName("handleExactCount")]
    public int HandleExactCount { get; init; }

    [JsonPropertyName("queryTextExactCount")]
    public int QueryTextExactCount { get; init; }

    [JsonPropertyName("fingerprintFallbackCount")]
    public int FingerprintFallbackCount { get; init; }

    [JsonPropertyName("unmatchedCount")]
    public int UnmatchedCount { get; init; }

    [JsonPropertyName("operations")]
    public IReadOnlyList<OperationCorrelationSummary> Operations { get; init; } =
        Array.Empty<OperationCorrelationSummary>();
}
