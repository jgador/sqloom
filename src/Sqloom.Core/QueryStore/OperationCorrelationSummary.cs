using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.QueryStore;

/// <summary>
/// Summarizes Query Store matches for one replayed operation.
/// </summary>
public sealed class OperationCorrelationSummary
{
    /// <summary>
    /// Gets the operation key.
    /// </summary>
    [JsonPropertyName("operationKey")]
    public required string OperationKey { get; init; }

    /// <summary>
    /// Gets the http method.
    /// </summary>
    [JsonPropertyName("httpMethod")]
    public required string HttpMethod { get; init; }

    /// <summary>
    /// Gets the route.
    /// </summary>
    [JsonPropertyName("route")]
    public required string Route { get; init; }

    /// <summary>
    /// Gets the persona.
    /// </summary>
    [JsonPropertyName("persona")]
    public string? Persona { get; init; }

    /// <summary>
    /// Gets the replay status.
    /// </summary>
    [JsonPropertyName("replayStatus")]
    public required string ReplayStatus { get; init; }

    /// <summary>
    /// Gets the operation artifact path.
    /// </summary>
    [JsonPropertyName("operationArtifactPath")]
    public required string OperationArtifactPath { get; init; }

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
    /// Gets the matched query ids.
    /// </summary>
    [JsonPropertyName("matchedQueryIds")]
    public IReadOnlyList<long> MatchedQueryIds { get; init; } =
        Array.Empty<long>();

    /// <summary>
    /// Gets the matched plan ids.
    /// </summary>
    [JsonPropertyName("matchedPlanIds")]
    public IReadOnlyList<long> MatchedPlanIds { get; init; } =
        Array.Empty<long>();
}
