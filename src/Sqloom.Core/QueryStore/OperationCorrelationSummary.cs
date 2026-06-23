using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.QueryStore;

/// <summary>
/// Summarizes Query Store matches for one replayed operation.
/// </summary>
public sealed class OperationCorrelationSummary
{
    [JsonPropertyName("operationKey")]
    public required string OperationKey { get; init; }

    [JsonPropertyName("httpMethod")]
    public required string HttpMethod { get; init; }

    [JsonPropertyName("route")]
    public required string Route { get; init; }

    [JsonPropertyName("persona")]
    public string? Persona { get; init; }

    [JsonPropertyName("replayStatus")]
    public required string ReplayStatus { get; init; }

    [JsonPropertyName("operationArtifactPath")]
    public required string OperationArtifactPath { get; init; }

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

    [JsonPropertyName("matchedQueryIds")]
    public IReadOnlyList<long> MatchedQueryIds { get; init; } =
        Array.Empty<long>();

    [JsonPropertyName("matchedPlanIds")]
    public IReadOnlyList<long> MatchedPlanIds { get; init; } =
        Array.Empty<long>();
}
