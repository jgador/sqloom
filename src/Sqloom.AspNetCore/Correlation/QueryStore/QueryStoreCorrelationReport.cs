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
public sealed class QueryStoreCorrelationReport
{
    [JsonPropertyName("generatedAtUtc")]
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    [JsonPropertyName("appName")]
    public string? AppName { get; init; }

    [JsonPropertyName("replayArtifactDirectory")]
    public required string ReplayArtifactDirectory { get; init; }

    [JsonPropertyName("queryStoreSnapshotPath")]
    public string? QueryStoreSnapshotPath { get; init; }

    [JsonPropertyName("queryStoreCapturedAtUtc")]
    public required DateTimeOffset QueryStoreCapturedAtUtc { get; init; }

    [JsonPropertyName("records")]
    public IReadOnlyList<QueryStoreCorrelationRecord> Records { get; init; } =
        Array.Empty<QueryStoreCorrelationRecord>();

    [JsonPropertyName("summary")]
    public required QueryStoreCorrelationSummary Summary { get; init; }

    [JsonPropertyName("pipeline")]
    public required PipelineReport Pipeline { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } =
        Array.Empty<string>();
}

/// <summary>
/// Captures one captured-SQL-to-Query Store correlation record.
/// </summary>
public sealed class QueryStoreCorrelationRecord
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

    [JsonPropertyName("statementSqlHandleCandidates")]
    public IReadOnlyList<SqlStatementHandleCandidate> StatementSqlHandleCandidates { get; init; } =
        Array.Empty<SqlStatementHandleCandidate>();

    [JsonPropertyName("matchKind")]
    public QueryStoreCorrelationMatchKind MatchKind { get; init; }

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
public sealed class QueryStoreCorrelationSummary
{
    [JsonPropertyName("operationCount")]
    public int OperationCount { get; init; }

    [JsonPropertyName("capturedCommandCount")]
    public int CapturedCommandCount { get; init; }

    [JsonPropertyName("matchedCommandCount")]
    public int MatchedCommandCount { get; init; }

    [JsonPropertyName("statementHandleExactCount")]
    public int StatementHandleExactCount { get; init; }

    [JsonPropertyName("queryTextExactCount")]
    public int QueryTextExactCount { get; init; }

    [JsonPropertyName("fingerprintFallbackCount")]
    public int FingerprintFallbackCount { get; init; }

    [JsonPropertyName("unmatchedCount")]
    public int UnmatchedCount { get; init; }

    [JsonPropertyName("operations")]
    public IReadOnlyList<QueryStoreCorrelationOperationSummary> Operations { get; init; } =
        Array.Empty<QueryStoreCorrelationOperationSummary>();
}

/// <summary>
/// Summarizes Query Store matches for one replayed operation.
/// </summary>
public sealed class QueryStoreCorrelationOperationSummary
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

    [JsonPropertyName("statementHandleExactCount")]
    public int StatementHandleExactCount { get; init; }

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
