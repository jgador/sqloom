using System;
using System.IO;
using Sqloom.Core.Artifacts;

namespace Sqloom.Core.Execution;

/// <summary>
/// Carries the shared filesystem and timestamp inputs for a Sqloom run.
/// </summary>
public sealed class RunOptions
{
    /// <summary>
    /// Gets the read only connection.
    /// </summary>
    public string? ReadOnlyConnection { get; init; }

    /// <summary>
    /// Gets the artifact root.
    /// </summary>
    public required string ArtifactRoot { get; init; }

    /// <summary>
    /// Gets the open ai api key.
    /// </summary>
    public string? OpenAIApiKey { get; init; }

    /// <summary>
    /// Gets the observe query store.
    /// </summary>
    public bool ObserveQueryStore { get; init; } = true;

    /// <summary>
    /// Gets the replay operations.
    /// </summary>
    public bool ReplayOperations { get; init; }

    /// <summary>
    /// Gets the capture sql during replay.
    /// </summary>
    public bool CaptureSqlDuringReplay { get; init; } = true;

    /// <summary>
    /// Gets the correlate replay.
    /// </summary>
    public bool CorrelateReplay { get; init; } = true;

    /// <summary>
    /// Gets the generate tuning advice.
    /// </summary>
    public bool GenerateTuningAdvice { get; init; } = true;

    /// <summary>
    /// Gets the max operations.
    /// </summary>
    public int MaxOperations { get; init; } = 25;

    /// <summary>
    /// Gets the open api path.
    /// </summary>
    public string? OpenApiPath { get; init; }

    /// <summary>
    /// Gets the target filter.
    /// </summary>
    public string? TargetFilter { get; init; }

    /// <summary>
    /// Creates run options using Sqloom's default artifact and execution settings.
    /// </summary>
    public static RunOptions CreateDefault(
        string currentDirectory,
        string? readOnlyConnectionString = null,
        string? openAIApiKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentDirectory);

        var repositoryRoot = RepositoryRootLocator.TryFind(currentDirectory);
        var artifactRoot = repositoryRoot is null
            ? Path.Combine(currentDirectory, "artifacts", "sqloom")
            : ArtifactLayout.GetDefaultArtifactRoot(repositoryRoot);

        return new RunOptions
        {
            ReadOnlyConnection = readOnlyConnectionString,
            ArtifactRoot = artifactRoot,
            OpenAIApiKey = openAIApiKey,
        };
    }
}
