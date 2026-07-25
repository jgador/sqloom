using Sqloom.Pipeline.Execution;

namespace Sqloom.Host.Replay;

/// <summary>
/// Carries the context needed to prepare replay request data.
/// </summary>
internal sealed class ReplayDataGenerationContext
{
    /// <summary>
    /// Gets the endpoint operation being prepared.
    /// </summary>
    public required ReplayOperation Operation { get; init; }

    /// <summary>
    /// Gets the operation after profile and harness inputs have been resolved.
    /// </summary>
    public required ResolvedReplayOperation ResolvedOperation { get; init; }

    /// <summary>
    /// Gets the active agent-generation policy.
    /// </summary>
    public required ReplayDataAgentMode Mode { get; init; }

    /// <summary>
    /// Gets the model selected for agent-assisted generation.
    /// </summary>
    public string? ModelName { get; init; }
}
