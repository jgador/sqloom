using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Sqloom.AspNetCore.Endpoints;
using Sqloom.Core.Execution;
using Sqloom.QueryStore.QueryStore;

namespace Sqloom.Correlation.QueryStore;

/// <summary>
/// Captures the output of a replay-to-Query Store correlation run.
/// </summary>
public sealed class QueryCorrelationReport
{
    [JsonPropertyName("generatedAtUtc")]
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    [JsonPropertyName("appName")]
    public string? AppName { get; init; }

    [JsonPropertyName("replayArtifactDir")]
    public required string ReplayArtifactDir { get; init; }

    [JsonPropertyName("queryStoreSnapshotPath")]
    public string? QueryStoreSnapshotPath { get; init; }

    [JsonPropertyName("queryStoreCapturedAtUtc")]
    public required DateTimeOffset QueryStoreCapturedAtUtc { get; init; }

    [JsonPropertyName("records")]
    public IReadOnlyList<QueryCorrelationRecord> Records { get; init; } =
        Array.Empty<QueryCorrelationRecord>();

    [JsonPropertyName("summary")]
    public required QueryCorrelationSummary Summary { get; init; }

    [JsonPropertyName("pipeline")]
    public required PipelineReport Pipeline { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } =
        Array.Empty<string>();
}

/// <summary>
/// Captures one captured-SQL-to-Query Store correlation record.
/// </summary>
public sealed class QueryCorrelationRecord
{
    [JsonPropertyName("operationKey")]
    public required string OperationKey { get; init; }

    [JsonPropertyName("httpMethod")]
    public required string HttpMethod { get; init; }

    [JsonPropertyName("route")]
    public required string Route { get; init; }

    [JsonPropertyName("persona")]
    public string? Persona { get; init; }

    [JsonPropertyName("operationArtifactPath")]
    public required string OperationArtifactPath { get; init; }

    [JsonPropertyName("commandOrdinal")]
    public int CommandOrdinal { get; init; }

    [JsonPropertyName("capturedCommand")]
    public required CapturedSqlCommand CapturedCommand { get; init; }

    [JsonPropertyName("comparableSqlText")]
    public required string ComparableSqlText { get; init; }

    [JsonPropertyName("statementSqlHandle")]
    public string? StatementSqlHandle { get; init; }

    [JsonPropertyName("sqlHandleCandidates")]
    public IReadOnlyList<SqlHandleCandidate> SqlHandleCandidates { get; init; } =
        Array.Empty<SqlHandleCandidate>();

    [JsonPropertyName("matchKind")]
    public CorrelationMatchKind MatchKind { get; init; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    [JsonPropertyName("matchedPlans")]
    public IReadOnlyList<QueryStorePlanRecord> MatchedPlans { get; init; } =
        Array.Empty<QueryStorePlanRecord>();

    [JsonPropertyName("notes")]
    public IReadOnlyList<string> Notes { get; init; } =
        Array.Empty<string>();
}

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
