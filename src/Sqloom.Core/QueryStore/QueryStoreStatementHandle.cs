namespace Sqloom.Core.QueryStore;

/// <summary>
/// Normalizes Query Store statement_sql_handle values.
/// </summary>
public static class QueryStoreStatementHandle
{
    public static string Normalize(string? value)
    {
        return value?.Trim().ToUpperInvariant() ?? string.Empty;
    }
}
