using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.QueryStore;

/// <summary>
/// Captures the outcome of a statement_sql_handle resolution attempt.
/// </summary>
public sealed class SqlHandleResolution
{
    /// <summary>
    /// Gets the sql text.
    /// </summary>
    [JsonPropertyName("sqlText")]
    public required string SqlText { get; init; }

    /// <summary>
    /// Gets the comparable sql text.
    /// </summary>
    [JsonPropertyName("comparableSqlText")]
    public required string ComparableSqlText { get; init; }

    /// <summary>
    /// Gets the statement sql handle.
    /// </summary>
    [JsonPropertyName("statementSqlHandle")]
    public string? StatementSqlHandle { get; init; }

    /// <summary>
    /// Gets candidate statement_sql_handle matches; downstream correlation may still fall back to text or fingerprint matching.
    /// </summary>
    [JsonPropertyName("candidates")]
    public IReadOnlyList<SqlHandleCandidate> Candidates { get; init; } =
        Array.Empty<SqlHandleCandidate>();

    /// <summary>
    /// Gets the error message.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Describes one candidate returned during statement_sql_handle resolution.
/// </summary>
public sealed class SqlHandleCandidate
{
    /// <summary>
    /// Gets the query text shape.
    /// </summary>
    [JsonPropertyName("queryTextShape")]
    public string QueryTextShape { get; init; } = string.Empty;

    /// <summary>
    /// Gets the requested param type.
    /// </summary>
    [JsonPropertyName("requestedParamType")]
    public required string RequestedParamType { get; init; }

    /// <summary>
    /// Gets the query parameterization type.
    /// </summary>
    [JsonPropertyName("queryParameterizationType")]
    public int? QueryParameterizationType { get; init; }

    /// <summary>
    /// Gets the statement sql handle.
    /// </summary>
    [JsonPropertyName("statementSqlHandle")]
    public string? StatementSqlHandle { get; init; }
}
