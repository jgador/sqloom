namespace Sqloom.Host;

/// <summary>
/// Carries arguments for the Sqloom correlate command.
/// </summary>
internal sealed class CorrelateArguments
{
    public required string ConnectionString { get; init; }

    public required string QueryStoreSnapshotPath { get; init; }

    public required string ReplayArtifactDirectory { get; init; }

    public required string JsonOutputPath { get; init; }

    public HostDebugWriter DebugWriter { get; set; } = HostDebugWriter.Disabled;
}
