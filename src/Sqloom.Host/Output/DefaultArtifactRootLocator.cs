using System.IO;
using Sqloom.Pipeline.Artifacts;
using Sqloom.Pipeline.Execution;

namespace Sqloom.Host;

/// <summary>
/// Chooses the default artifact root from the nearest repository root or the current directory.
/// </summary>
internal static class DefaultArtifactRootLocator
{
    public static string GetPath(string currentDirectory)
    {
        var repositoryRoot = RepositoryRootLocator.TryFind(currentDirectory);
        return repositoryRoot is null
            ? Path.Combine(currentDirectory, "artifacts", "sqloom")
            : ArtifactLayout.GetDefaultArtifactRoot(repositoryRoot);
    }
}
