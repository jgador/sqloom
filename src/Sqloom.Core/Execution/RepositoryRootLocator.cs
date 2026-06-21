using System;
using System.IO;
using System.Linq;

namespace Sqloom.Core.Execution;

/// <summary>
/// Locates the repository root used by Sqloom artifact helpers.
/// </summary>
public static class RepositoryRootLocator
{
    public static string? TryFind(string startPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startPath);

        var fullPath = Path.GetFullPath(startPath);
        var startDirectory = Directory.Exists(fullPath)
            ? fullPath
            : Path.GetDirectoryName(fullPath) ?? Environment.CurrentDirectory;

        DirectoryInfo? currentDirectory = new(startDirectory);
        while (currentDirectory is not null)
        {
            if (LooksLikeRepositoryRoot(currentDirectory.FullName))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        return null;
    }

    private static bool LooksLikeRepositoryRoot(string directoryPath)
    {
        var gitPath = Path.Combine(directoryPath, ".git");
        return (Directory.Exists(gitPath) || File.Exists(gitPath))
            && HasDotNetWorkspaceMarkers(directoryPath);
    }

    private static bool HasDotNetWorkspaceMarkers(string directoryPath)
    {
        return File.Exists(Path.Combine(directoryPath, "Directory.Build.props"))
            || File.Exists(Path.Combine(directoryPath, "global.json"))
            || Directory.EnumerateFiles(directoryPath, "*.sln", SearchOption.TopDirectoryOnly).Any()
            || Directory.EnumerateFiles(directoryPath, "*.slnf", SearchOption.TopDirectoryOnly).Any();
    }
}
