namespace Sqloom.Showplan.Plans;

/// <summary>
/// Captures one warning extracted from a SQL Server SHOWPLAN fragment.
/// </summary>
public sealed class ShowplanWarning
{
    /// <summary>
    /// Gets the code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the message.
    /// </summary>
    public required string Message { get; init; }
}
