namespace Sqloom.Host.Replay;

/// <summary>
/// Controls replay data generation.
/// </summary>
public enum ReplayDataAgentMode
{
    /// <summary>
    /// Disables agent-assisted replay data generation.
    /// </summary>
    Off,

    /// <summary>
    /// Uses agent-assisted generation when it is available and applicable.
    /// </summary>
    Auto,

    /// <summary>
    /// Requires agent-assisted generation to succeed.
    /// </summary>
    Required,
}
