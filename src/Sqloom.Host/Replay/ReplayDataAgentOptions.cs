namespace Sqloom.Host.Replay;

/// <summary>
/// Carries replay data preparation options.
/// </summary>
public sealed class ReplayDataAgentOptions
{
    public ReplayDataAgentMode Mode { get; init; } = ReplayDataAgentMode.Off;

    public string? ModelName { get; init; }
}
