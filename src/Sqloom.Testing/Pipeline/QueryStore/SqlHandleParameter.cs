namespace Sqloom.Pipeline.QueryStore;

/// <summary>
/// Describes one SQL parameter passed to statement_sql_handle resolution.
/// </summary>
public sealed class SqlHandleParameter
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the db type.
    /// </summary>
    public string? DbType { get; init; }

    /// <summary>
    /// Gets the size.
    /// </summary>
    public int? Size { get; init; }

    /// <summary>
    /// Gets the precision.
    /// </summary>
    public byte? Precision { get; init; }

    /// <summary>
    /// Gets the scale.
    /// </summary>
    public byte? Scale { get; init; }
}
