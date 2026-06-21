using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Sqloom.Core.Execution;

namespace Sqloom.Host;

/// <summary>
/// Finds companion Sqloom projects that belong to an app target.
/// </summary>
internal sealed class CompanionProjectLocator
{
    private const string SqloomAppIntegrationTypePropertyName = "SqloomAppIntegrationType";
    private const string SqloomTargetProjectPropertyName = "SqloomTargetProject";

    public bool IsSqloomCapableProject(string projectPath)
    {
        return HasNonEmptyProperty(
                projectPath,
                SqloomAppIntegrationTypePropertyName)
            || TryResolveCompanionProjectPath(projectPath) is not null;
    }

    public string? TryResolveCompanionProjectPath(string targetProjectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetProjectPath);

        var fullTargetProjectPath = Path.GetFullPath(targetProjectPath);
        var companionProjectPaths = EnumerateCompanionProjectPaths(fullTargetProjectPath)
            .Where(companionProjectPath =>
            {
                var targetedProjectPath = TryGetNormalizedPropertyPath(
                    companionProjectPath,
                    SqloomTargetProjectPropertyName);
                return string.Equals(
                    targetedProjectPath,
                    fullTargetProjectPath,
                    StringComparison.OrdinalIgnoreCase);
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return companionProjectPaths.Length switch
        {
            0 => null,
            1 => companionProjectPaths[0],
            _ => throw new AppResolutionException(
                $"The Sqloom target project '{fullTargetProjectPath}' matches multiple companion integration projects: {string.Join(", ", companionProjectPaths)}. Keep only one SqloomTargetProject mapping."),
        };
    }

    private static IEnumerable<string> EnumerateCompanionProjectPaths(string targetProjectPath)
    {
        var repositoryRoot = RepositoryRootLocator.TryFind(targetProjectPath);
        if (repositoryRoot is null)
        {
            return Array.Empty<string>();
        }

        var searchRoots = GetSearchRoots(repositoryRoot).ToArray();
        if (searchRoots.Length == 0)
        {
            return Array.Empty<string>();
        }

        return searchRoots
            .SelectMany(EnumerateProjectFiles)
            .Where(IsSupportedProjectPath)
            .Select(Path.GetFullPath)
            .Where(projectPath =>
                !string.Equals(
                    projectPath,
                    targetProjectPath,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> GetSearchRoots(string repositoryRoot)
    {
        var searchRoots = new[]
            {
                Path.Combine(repositoryRoot, "src"),
                Path.Combine(repositoryRoot, "tests"),
            }
            .Where(Directory.Exists)
            .ToArray();

        return searchRoots.Length > 0
            ? searchRoots
            : [repositoryRoot];
    }

    private static IEnumerable<string> EnumerateProjectFiles(string searchRoot)
    {
        Stack<string> pendingDirectories = new();
        pendingDirectories.Push(searchRoot);

        while (pendingDirectories.Count > 0)
        {
            var currentDirectory = pendingDirectories.Pop();

            IEnumerable<string> projectPaths;
            try
            {
                projectPaths = Directory.EnumerateFiles(
                    currentDirectory,
                    "*.*proj",
                    SearchOption.TopDirectoryOnly);
            }
            catch (Exception exception) when (
                exception is DirectoryNotFoundException
                    or IOException
                    or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var projectPath in projectPaths)
            {
                yield return projectPath;
            }

            IEnumerable<string> childDirectories;
            try
            {
                childDirectories = Directory.EnumerateDirectories(
                    currentDirectory,
                    "*",
                    SearchOption.TopDirectoryOnly);
            }
            catch (Exception exception) when (
                exception is DirectoryNotFoundException
                    or IOException
                    or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var childDirectory in childDirectories)
            {
                if (ShouldSkipDirectory(childDirectory))
                {
                    continue;
                }

                pendingDirectories.Push(childDirectory);
            }
        }
    }

    private static bool HasNonEmptyProperty(
        string projectPath,
        string propertyName)
    {
        try
        {
            var document = XDocument.Load(projectPath);
            var projectNamespace = document.Root?.Name.Namespace ?? XNamespace.None;
            return document
                .Descendants(projectNamespace + propertyName)
                .Any(static element => !string.IsNullOrWhiteSpace(element.Value));
        }
        catch (Exception exception) when (
            exception is IOException
                or InvalidOperationException
                or UnauthorizedAccessException
                or XmlException)
        {
            return false;
        }
    }

    private static string? TryGetNormalizedPropertyPath(
        string projectPath,
        string propertyName)
    {
        try
        {
            var document = XDocument.Load(projectPath);
            var projectNamespace = document.Root?.Name.Namespace ?? XNamespace.None;
            var propertyValue = document
                .Descendants(projectNamespace + propertyName)
                .Select(static element => element.Value)
                .LastOrDefault(static value => !string.IsNullOrWhiteSpace(value));
            if (string.IsNullOrWhiteSpace(propertyValue))
            {
                return null;
            }

            var projectDirectory = Path.GetDirectoryName(projectPath)
                ?? throw new InvalidOperationException($"The project path '{projectPath}' has no parent directory.");
            return Path.GetFullPath(
                propertyValue,
                projectDirectory);
        }
        catch (Exception exception) when (
            exception is IOException
                or InvalidOperationException
                or UnauthorizedAccessException
                or XmlException)
        {
            return null;
        }
    }

    private static bool IsSupportedProjectPath(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".csproj" => true,
            ".fsproj" => true,
            ".vbproj" => true,
            _ => false,
        };
    }

    private static bool ShouldSkipDirectory(string directoryPath)
    {
        var directoryName = Path.GetFileName(directoryPath);
        return string.Equals(directoryName, ".git", StringComparison.OrdinalIgnoreCase)
            || string.Equals(directoryName, ".idea", StringComparison.OrdinalIgnoreCase)
            || string.Equals(directoryName, ".synapse", StringComparison.OrdinalIgnoreCase)
            || string.Equals(directoryName, ".tools", StringComparison.OrdinalIgnoreCase)
            || string.Equals(directoryName, ".vs", StringComparison.OrdinalIgnoreCase)
            || string.Equals(directoryName, "artifacts", StringComparison.OrdinalIgnoreCase)
            || string.Equals(directoryName, "bin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(directoryName, "node_modules", StringComparison.OrdinalIgnoreCase)
            || string.Equals(directoryName, "obj", StringComparison.OrdinalIgnoreCase);
    }
}
