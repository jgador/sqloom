using Sqloom.Core.Execution;

namespace Sqloom.Host.Replay;

/// <summary>
/// Carries the context needed to prepare replay request data.
/// </summary>
public sealed class ReplayDataPreparationContext
{
    public required OpenApiOperation Operation { get; init; }

    public required ResolvedReplayOperation ResolvedOperation { get; init; }

    public required ReplayDataAgentMode Mode { get; init; }

    public string? ModelName { get; init; }
}
