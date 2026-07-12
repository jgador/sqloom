namespace Sqloom.Core.Execution;

/// <summary>
/// Represents captured sql source kind.
/// </summary>
public enum CapturedSqlSourceKind
{
    /// <summary>
    /// The command was captured through Entity Framework Core interception.
    /// </summary>
    EntityFramework = 0,
    /// <summary>
    /// The command was captured through an ADO.NET integration.
    /// </summary>
    AdoNet = 1,
}
