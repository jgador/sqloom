using System.IO;
using Sqloom.Core.Artifacts;
using Sqloom.Core.Execution;

namespace Sqloom.Host;

/// <summary>
/// Resolves the default artifact root for Sqloom host commands.
/// </summary>
internal static class ArtifactRootResolver
{
    public static string Resolve(string currentDirectory)
    {
        var repositoryRoot = RepositoryRootLocator.TryFind(currentDirectory);
        return repositoryRoot is null
            ? Path.Combine(currentDirectory, "artifacts", "sqloom")
            : ArtifactLayout.GetDefaultArtifactRoot(repositoryRoot);
    }
}
