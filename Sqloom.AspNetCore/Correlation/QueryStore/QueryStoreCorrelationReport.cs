using System;
using System.Collections.Generic;
using Sqloom.AspNetCore.Endpoints;
using Sqloom.Core.Execution;
using Sqloom.QueryStore.QueryStore;

namespace Sqloom.Correlation.QueryStore;

/// <summary>
/// Captures the output of a replay-to-Query Store correlation run.
/// </summary>
public sealed class QueryStoreCorrelationReport
{
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    public string? AppName { get; init; }

    public required string ReplayArtifactDirectory { get; init; }

    public string? QueryStoreSnapshotPath { get; init; }

    public required DateTimeOffset QueryStoreCapturedAtUtc { get; init; }

    public IReadOnlyList<QueryStoreCorrelationRecord> Records { get; init; } =
        Array.Empty<QueryStoreCorrelationRecord>();

    public required QueryStoreCorrelationSummary Summary { get; init; }

    public required PipelineReport Pipeline { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } =
        Array.Empty<string>();
}

/// <summary>
/// Captures one captured-SQL-to-Query Store correlation record.
/// </summary>
public sealed class QueryStoreCorrelationRecord
{
    public required string OperationKey { get; init; }

    public required string HttpMethod { get; init; }

    public required string Route { get; init; }

    public string? Persona { get; init; }

    public required string OperationArtifactPath { get; init; }

    public int CommandOrdinal { get; init; }

    public required CapturedSqlCommand CapturedCommand { get; init; }

    public required string ComparableSqlText { get; init; }

    public string? StatementSqlHandle { get; init; }

    public IReadOnlyList<SqlStatementHandleCandidate> StatementSqlHandleCandidates { get; init; } =
        Array.Empty<SqlStatementHandleCandidate>();

    public QueryStoreCorrelationMatchKind MatchKind { get; init; }

    public double Confidence { get; init; }

    public IReadOnlyList<QueryStorePlanRecord> MatchedPlans { get; init; } =
        Array.Empty<QueryStorePlanRecord>();

    public IReadOnlyList<string> Notes { get; init; } =
        Array.Empty<string>();
}

/// <summary>
/// Summarizes the matches produced by a correlation run.
/// </summary>
public sealed class QueryStoreCorrelationSummary
{
    public int OperationCount { get; init; }

    public int CapturedCommandCount { get; init; }

    public int MatchedCommandCount { get; init; }

    public int StatementHandleExactCount { get; init; }

    public int QueryTextExactCount { get; init; }

    public int FingerprintFallbackCount { get; init; }

    public int UnmatchedCount { get; init; }

    public IReadOnlyList<QueryStoreCorrelationOperationSummary> Operations { get; init; } =
        Array.Empty<QueryStoreCorrelationOperationSummary>();
}

/// <summary>
/// Summarizes Query Store matches for one replayed operation.
/// </summary>
public sealed class QueryStoreCorrelationOperationSummary
{
    public required string OperationKey { get; init; }

    public required string HttpMethod { get; init; }

    public required string Route { get; init; }

    public string? Persona { get; init; }

    public required string ReplayStatus { get; init; }

    public required string OperationArtifactPath { get; init; }

    public int CapturedCommandCount { get; init; }

    public int MatchedCommandCount { get; init; }

    public int StatementHandleExactCount { get; init; }

    public int QueryTextExactCount { get; init; }

    public int FingerprintFallbackCount { get; init; }

    public int UnmatchedCount { get; init; }

    public IReadOnlyList<long> MatchedQueryIds { get; init; } =
        Array.Empty<long>();

    public IReadOnlyList<long> MatchedPlanIds { get; init; } =
        Array.Empty<long>();
}
