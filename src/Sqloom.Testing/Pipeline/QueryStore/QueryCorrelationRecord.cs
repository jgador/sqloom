using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Sqloom.Pipeline.Execution;
using Sqloom.Pipeline.QueryStore;

namespace Sqloom.Pipeline.QueryStore;

/// <summary>
/// Captures one captured-SQL-to-Query Store correlation record.
/// </summary>
public sealed class QueryCorrelationRecord
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
    /// Gets the operation artifact path.
    /// </summary>
    [JsonPropertyName("operationArtifactPath")]
    public required string OperationArtifactPath { get; init; }

    /// <summary>
    /// Gets the command ordinal.
    /// </summary>
    [JsonPropertyName("commandOrdinal")]
    public int CommandOrdinal { get; init; }

    /// <summary>
    /// Gets the captured command.
    /// </summary>
    [JsonPropertyName("capturedCommand")]
    public required CapturedSqlCommand CapturedCommand { get; init; }

    /// <summary>
    /// Gets the normalized SQL text used for Query Store correlation, not the original command text.
    /// </summary>
    [JsonPropertyName("comparableSqlText")]
    public required string ComparableSqlText { get; init; }

    /// <summary>
    /// Gets the statement sql handle.
    /// </summary>
    [JsonPropertyName("statementSqlHandle")]
    public string? StatementSqlHandle { get; init; }

    /// <summary>
    /// Gets the ordered statement-handle resolution attempts used before text and fingerprint fallback.
    /// </summary>
    [JsonPropertyName("sqlHandleCandidates")]
    public IReadOnlyList<SqlHandleCandidate> SqlHandleCandidates { get; init; } =
        Array.Empty<SqlHandleCandidate>();

    /// <summary>
    /// Gets the match kind.
    /// </summary>
    [JsonPropertyName("matchKind")]
    public CorrelationMatchKind MatchKind { get; init; }

    /// <summary>
    /// Gets the confidence tier assigned from the selected correlation match strategy.
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    /// <summary>
    /// Gets the matched plans.
    /// </summary>
    [JsonPropertyName("matchedPlans")]
    public IReadOnlyList<QueryStorePlanRecord> MatchedPlans { get; init; } =
        Array.Empty<QueryStorePlanRecord>();

    /// <summary>
    /// Gets the notes.
    /// </summary>
    [JsonPropertyName("notes")]
    public IReadOnlyList<string> Notes { get; init; } =
        Array.Empty<string>();
}
