using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Sqloom.Core.Execution;
using Sqloom.QueryStore.QueryStore;

namespace Sqloom.Correlation.QueryStore;

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
