namespace Sqloom.Pipeline.QueryStore;

/// <summary>
/// Normalizes Query Store statement_sql_handle values.
/// </summary>
public static class QueryStoreStatementHandle
{
    /// <summary>
    /// Normalizes a SQL statement handle to its canonical hexadecimal form.
    /// </summary>
    public static string Normalize(string? value)
    {
        return value?.Trim().ToUpperInvariant() ?? string.Empty;
    }
}
