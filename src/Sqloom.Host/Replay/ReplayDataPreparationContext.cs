using Sqloom.Pipeline.Execution;

namespace Sqloom.Host.Replay;

/// <summary>
/// Carries the context needed to prepare replay request data.
/// </summary>
public sealed class ReplayDataPreparationContext
{
    /// <summary>
    /// Gets the OpenAPI operation being prepared.
    /// </summary>
    public required OpenApiOperation Operation { get; init; }

    /// <summary>
    /// Gets the operation after profile and harness inputs have been resolved.
    /// </summary>
    public required ResolvedReplayOperation ResolvedOperation { get; init; }

    /// <summary>
    /// Gets the active agent-preparation policy.
    /// </summary>
    public required ReplayDataAgentMode Mode { get; init; }

    /// <summary>
    /// Gets the model selected for agent-assisted preparation.
    /// </summary>
    public string? ModelName { get; init; }
}
