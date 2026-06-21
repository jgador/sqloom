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

        var searchRoots = new[]
            {
                Path.Combine(repositoryRoot, "backend", "tools"),
                Path.Combine(repositoryRoot, "backend", "tests"),
            }
            .Where(Directory.Exists)
            .ToArray();
        if (searchRoots.Length == 0)
        {
            return Array.Empty<string>();
        }

        return searchRoots
            .SelectMany(
                static searchRoot => Directory.EnumerateFiles(
                    searchRoot,
                    "*.*proj",
                    SearchOption.AllDirectories))
            .Where(IsSupportedProjectPath)
            .Select(Path.GetFullPath)
            .Where(projectPath =>
                !string.Equals(
                    projectPath,
                    targetProjectPath,
                    StringComparison.OrdinalIgnoreCase));
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
}
