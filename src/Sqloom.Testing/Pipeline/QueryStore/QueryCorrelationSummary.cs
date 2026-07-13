using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Pipeline.QueryStore;

/// <summary>
/// Summarizes the matches produced by a correlation run.
/// </summary>
public sealed class QueryCorrelationSummary
{
    /// <summary>
    /// Gets the operation count.
    /// </summary>
    [JsonPropertyName("operationCount")]
    public int OperationCount { get; init; }

    /// <summary>
    /// Gets the captured command count.
    /// </summary>
    [JsonPropertyName("capturedCommandCount")]
    public int CapturedCommandCount { get; init; }

    /// <summary>
    /// Gets the matched command count.
    /// </summary>
    [JsonPropertyName("matchedCommandCount")]
    public int MatchedCommandCount { get; init; }

    /// <summary>
    /// Gets the handle exact count.
    /// </summary>
    [JsonPropertyName("handleExactCount")]
    public int HandleExactCount { get; init; }

    /// <summary>
    /// Gets the query text exact count.
    /// </summary>
    [JsonPropertyName("queryTextExactCount")]
    public int QueryTextExactCount { get; init; }

    /// <summary>
    /// Gets the fingerprint fallback count.
    /// </summary>
    [JsonPropertyName("fingerprintFallbackCount")]
    public int FingerprintFallbackCount { get; init; }

    /// <summary>
    /// Gets the unmatched count.
    /// </summary>
    [JsonPropertyName("unmatchedCount")]
    public int UnmatchedCount { get; init; }

    /// <summary>
    /// Gets the operations.
    /// </summary>
    [JsonPropertyName("operations")]
    public IReadOnlyList<OperationCorrelationSummary> Operations { get; init; } =
        Array.Empty<OperationCorrelationSummary>();
}
