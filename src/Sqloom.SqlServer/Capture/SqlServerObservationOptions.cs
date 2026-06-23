namespace Sqloom.SqlServer.Capture;

/// <summary>
/// Carries options for SQL Server observation.
/// </summary>
public sealed class SqlServerObservationOptions
{
    public required string ReadOnlyConnection { get; init; }

    public bool CaptureStatisticsXml { get; init; } = true;

    public bool CaptureStatisticsIo { get; init; } = true;

    public bool CaptureStatisticsTime { get; init; } = true;
}
