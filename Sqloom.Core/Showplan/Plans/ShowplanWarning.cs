namespace Sqloom.Showplan.Plans;

/// <summary>
/// Captures one warning extracted from a SQL Server SHOWPLAN fragment.
/// </summary>
public sealed class ShowplanWarning
{
    public required string Code { get; init; }

    public required string Message { get; init; }
}
