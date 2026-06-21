using System;
using System.Collections.Generic;

namespace Sqloom.QueryStore.QueryStore;

/// <summary>
/// Captures the outcome of a statement_sql_handle resolution attempt.
/// </summary>
public sealed class SqlStatementHandleResolution
{
    public required string SqlText { get; init; }

    public required string ComparableSqlText { get; init; }

    public string? StatementSqlHandle { get; init; }

    public IReadOnlyList<SqlStatementHandleCandidate> Candidates { get; init; } =
        Array.Empty<SqlStatementHandleCandidate>();

    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Describes one candidate returned during statement_sql_handle resolution.
/// </summary>
public sealed class SqlStatementHandleCandidate
{
    public string QueryTextShape { get; init; } = string.Empty;

    public required string RequestedQueryParameterizationType { get; init; }

    public int? QueryParameterizationType { get; init; }

    public string? StatementSqlHandle { get; init; }
}
