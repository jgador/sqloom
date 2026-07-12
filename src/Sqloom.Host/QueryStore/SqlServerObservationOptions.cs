namespace Sqloom.Host.QueryStore;

/// <summary>
/// Carries options for SQL Server observation.
/// </summary>
public sealed class SqlServerObservationOptions
{
    /// <summary>
    /// Gets the readonly SQL Server connection used for observation.
    /// </summary>
    public required string ReadOnlyConnection { get; init; }

    /// <summary>
    /// Gets whether execution plans are captured with statistics XML.
    /// </summary>
    public bool CaptureStatisticsXml { get; init; } = true;

    /// <summary>
    /// Gets whether SQL Server I/O statistics are captured.
    /// </summary>
    public bool CaptureStatisticsIo { get; init; } = true;

    /// <summary>
    /// Gets whether SQL Server timing statistics are captured.
    /// </summary>
    public bool CaptureStatisticsTime { get; init; } = true;
}
