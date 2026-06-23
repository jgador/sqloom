using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.QueryStore.QueryStore;

/// <summary>
/// Captures the outcome of a statement_sql_handle resolution attempt.
/// </summary>
public sealed class SqlHandleResolution
{
    [JsonPropertyName("sqlText")]
    public required string SqlText { get; init; }

    [JsonPropertyName("comparableSqlText")]
    public required string ComparableSqlText { get; init; }

    [JsonPropertyName("statementSqlHandle")]
    public string? StatementSqlHandle { get; init; }

    [JsonPropertyName("candidates")]
    public IReadOnlyList<SqlHandleCandidate> Candidates { get; init; } =
        Array.Empty<SqlHandleCandidate>();

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Describes one candidate returned during statement_sql_handle resolution.
/// </summary>
public sealed class SqlHandleCandidate
{
    [JsonPropertyName("queryTextShape")]
    public string QueryTextShape { get; init; } = string.Empty;

    [JsonPropertyName("requestedParamType")]
    public required string RequestedParamType { get; init; }

    [JsonPropertyName("queryParameterizationType")]
    public int? QueryParameterizationType { get; init; }

    [JsonPropertyName("statementSqlHandle")]
    public string? StatementSqlHandle { get; init; }
}
