namespace Sqloom.Host.Replay;

/// <summary>
/// Carries replay data generation options.
/// </summary>
public sealed class ReplayDataAgentOptions
{
    /// <summary>
    /// Gets the policy controlling agent-assisted replay data generation.
    /// </summary>
    public ReplayDataAgentMode Mode { get; init; } = ReplayDataAgentMode.Off;

    /// <summary>
    /// Gets the model used for agent-assisted replay data generation.
    /// </summary>
    public string? ModelName { get; init; }
}
