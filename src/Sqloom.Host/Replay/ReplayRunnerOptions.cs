using Sqloom.Core.Execution;

namespace Sqloom.Host.Replay;

/// <summary>
/// Carries the inputs required to execute an endpoint replay run.
/// </summary>
public sealed class ReplayRunnerOptions
{
    public required string AppName { get; init; }

    public required string OpenApiPath { get; init; }

    public required string ReplayArtifactDir { get; init; }

    public required ReplayProfile ReplayProfile { get; init; }

    public IReplayHostFactory? ReplayHostFactory { get; init; }

    public IReplayHost? ReplayHost { get; init; }

    public ReplayLaunchOptions ReplayLaunchOptions { get; init; } = new();

    public int MaxOperations { get; init; } = 25;

    public string? TargetFilter { get; init; }
}
