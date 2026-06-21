using System;
using System.IO;

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
            var gitPath = Path.Combine(currentDirectory.FullName, ".git");
            var backendPath = Path.Combine(currentDirectory.FullName, "backend");
            if ((Directory.Exists(gitPath) || File.Exists(gitPath)) && Directory.Exists(backendPath))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        return null;
    }
}
