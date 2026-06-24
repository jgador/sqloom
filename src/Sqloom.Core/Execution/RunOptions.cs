using System;
using System.IO;
using Sqloom.Core.Artifacts;

namespace Sqloom.Core.Execution;

/// <summary>
/// Carries the shared filesystem and timestamp inputs for a Sqloom run.
/// </summary>
public sealed class RunOptions
{
    public string? ReadOnlyConnection { get; init; }

    public required string ArtifactRoot { get; init; }

    public string? OpenAIApiKey { get; init; }

    public bool ObserveQueryStore { get; init; } = true;

    public bool ReplayOperations { get; init; }

    public bool CaptureSqlDuringReplay { get; init; } = true;

    public bool CorrelateReplay { get; init; } = true;

    public bool GenerateTuningAdvice { get; init; } = true;

    public int MaxOperations { get; init; } = 25;

    public string? OpenApiPath { get; init; }

    public string? TargetFilter { get; init; }

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
