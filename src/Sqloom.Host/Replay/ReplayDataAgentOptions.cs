namespace Sqloom.Host.Replay;

/// <summary>
/// Carries replay data preparation options.
/// </summary>
public sealed class ReplayDataAgentOptions
{
    /// <summary>
    /// Gets the policy controlling agent-assisted replay data preparation.
    /// </summary>
    public ReplayDataAgentMode Mode { get; init; } = ReplayDataAgentMode.Off;

    /// <summary>
    /// Gets the model used for agent-assisted replay data preparation.
    /// </summary>
    public string? ModelName { get; init; }
}
