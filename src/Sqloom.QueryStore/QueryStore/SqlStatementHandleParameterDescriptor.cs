namespace Sqloom.QueryStore.QueryStore;

/// <summary>
/// Describes one SQL parameter passed to statement_sql_handle resolution.
/// </summary>
public sealed class SqlStatementHandleParameterDescriptor
{
    public required string Name { get; init; }

    public string? DbType { get; init; }

    public int? Size { get; init; }

    public byte? Precision { get; init; }

    public byte? Scale { get; init; }
}
