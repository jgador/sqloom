namespace Sqloom.Core.QueryStore;

/// <summary>
/// Represents db object kind.
/// </summary>
public enum DbObjectKind
{
    /// <summary>
    /// A database table.
    /// </summary>
    Table = 0,
    /// <summary>
    /// A database view.
    /// </summary>
    View = 1,
    /// <summary>
    /// A programmable database module such as a procedure or function.
    /// </summary>
    Module = 2,
}
