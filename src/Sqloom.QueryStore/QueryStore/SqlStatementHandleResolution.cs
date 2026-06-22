using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.QueryStore.QueryStore;

/// <summary>
/// Captures the outcome of a statement_sql_handle resolution attempt.
/// </summary>
public sealed class SqlStatementHandleResolution
{
    [JsonPropertyName("sqlText")]
    public required string SqlText { get; init; }

    [JsonPropertyName("comparableSqlText")]
    public required string ComparableSqlText { get; init; }

    [JsonPropertyName("statementSqlHandle")]
    public string? StatementSqlHandle { get; init; }

    [JsonPropertyName("candidates")]
    public IReadOnlyList<SqlStatementHandleCandidate> Candidates { get; init; } =
        Array.Empty<SqlStatementHandleCandidate>();

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Describes one candidate returned during statement_sql_handle resolution.
/// </summary>
public sealed class SqlStatementHandleCandidate
{
    [JsonPropertyName("queryTextShape")]
    public string QueryTextShape { get; init; } = string.Empty;

    [JsonPropertyName("requestedQueryParameterizationType")]
    public required string RequestedQueryParameterizationType { get; init; }

    [JsonPropertyName("queryParameterizationType")]
    public int? QueryParameterizationType { get; init; }

    [JsonPropertyName("statementSqlHandle")]
    public string? StatementSqlHandle { get; init; }
}
