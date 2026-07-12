namespace Sqloom.Core.Execution;

/// <summary>
/// Represents sql surface kind.
/// </summary>
public enum SqlSurfaceKind
{
    /// <summary>
    /// The operation uses an Entity Framework Core data-access surface.
    /// </summary>
    EntityFramework,
    /// <summary>
    /// The operation executes raw SQL directly.
    /// </summary>
    RawSql,
    /// <summary>
    /// The operation combines Entity Framework Core and raw SQL access.
    /// </summary>
    Mixed,
}
