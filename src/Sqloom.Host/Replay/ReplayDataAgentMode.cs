namespace Sqloom.Host.Replay;

/// <summary>
/// Controls replay data preparation.
/// </summary>
public enum ReplayDataAgentMode
{
    /// <summary>
    /// Disables agent-assisted replay data preparation.
    /// </summary>
    Off,

    /// <summary>
    /// Uses agent-assisted preparation when it is available and applicable.
    /// </summary>
    Auto,

    /// <summary>
    /// Requires agent-assisted preparation to succeed.
    /// </summary>
    Required,
}
