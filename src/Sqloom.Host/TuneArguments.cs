namespace Sqloom.Host;

/// <summary>
/// Carries arguments for the Sqloom tune workflow.
/// </summary>
internal sealed class TuneArguments
{
    public required string WorkflowArtifactDirectory { get; init; }

    public required ObserveArguments ObserveArguments { get; init; }

    public required ReplayArguments ReplayArguments { get; init; }

    public required CorrelateArguments CorrelateArguments { get; init; }

    public required AdviseArguments AdviseArguments { get; init; }

    public HostDebugWriter DebugWriter { get; set; } = HostDebugWriter.Disabled;
}
