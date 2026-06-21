namespace Sqloom.AzureSql.Capture;

/// <summary>
/// Carries options for Azure SQL observation.
/// </summary>
public sealed class AzureSqlObservationOptions
{
    public required string ReadOnlyConnectionString { get; init; }

    public bool CaptureStatisticsXml { get; init; } = true;

    public bool CaptureStatisticsIo { get; init; } = true;

    public bool CaptureStatisticsTime { get; init; } = true;
}
