using Sqloom.Core.Execution;

namespace Sqloom.Host.Replay;

/// <summary>
/// Carries the inputs required to execute an endpoint replay run.
/// </summary>
public sealed class ReplayRunnerOptions
{
    /// <summary>
    /// Gets the application name written to replay artifacts.
    /// </summary>
    public required string AppName { get; init; }

    /// <summary>
    /// Gets the OpenAPI document used to discover replay operations.
    /// </summary>
    public required string OpenApiPath { get; init; }

    /// <summary>
    /// Gets the directory where replay artifacts are written.
    /// </summary>
    public required string ReplayArtifactDir { get; init; }

    /// <summary>
    /// Gets the replay profile that supplies operation inputs and personas.
    /// </summary>
    public required ReplayProfile ReplayProfile { get; init; }

    /// <summary>
    /// Gets the factory used to create a replay host when no host instance is supplied.
    /// </summary>
    public IReplayHostFactory? ReplayHostFactory { get; init; }

    /// <summary>
    /// Gets an existing replay host supplied by the application harness.
    /// </summary>
    public IReplayHost? ReplayHost { get; init; }

    /// <summary>
    /// Gets the launch inputs passed to a newly created replay host.
    /// </summary>
    public ReplayLaunchOptions ReplayLaunchOptions { get; init; } = new();

    /// <summary>
    /// Gets the agent-assisted replay data preparation options.
    /// </summary>
    public ReplayDataAgentOptions ReplayDataAgentOptions { get; init; } = new();

    /// <summary>
    /// Gets the replay data preparer used before harness operation preparation.
    /// </summary>
    public IReplayDataPreparer? ReplayDataPreparer { get; init; }

    /// <summary>
    /// Gets the maximum number of discovered operations to replay.
    /// </summary>
    public int MaxOperations { get; init; } = 25;

    /// <summary>
    /// Gets the optional operation key or route filter applied during replay planning.
    /// </summary>
    public string? TargetFilter { get; init; }
}
