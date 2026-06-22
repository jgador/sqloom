using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Sqloom.Host;

/// <summary>
/// Resolves supported Sqloom harness targets down to concrete projects or assemblies.
/// </summary>
internal sealed class TargetPathResolver
{
    private static readonly Regex _solutionProjectLineRegex = new(
        "^Project\\(\"\\{[^}]+\\}\"\\)\\s*=\\s*\"[^\"]+\",\\s*\"(?<path>[^\"]+)\",\\s*\"\\{[^}]+\\}\"$",
        RegexOptions.Compiled);

    public IReadOnlyList<ResolvedTargetSelection> ResolveTargetSelections(string targetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);

        var fullTargetPath = Path.GetFullPath(targetPath);
        var selections = Directory.Exists(fullTargetPath)
            ? ResolveTargetSelectionsFromDirectory(fullTargetPath)
            : File.Exists(fullTargetPath)
                ? ResolveTargetSelectionsFromFile(fullTargetPath)
                : throw BuildUnsupportedOrMissingTargetException(fullTargetPath);

        var distinctSelections = selections
            .DistinctBy(static selection => selection.TargetPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (distinctSelections.Length == 0)
        {
            throw new AppResolutionException(
                $"The Sqloom target '{fullTargetPath}' did not resolve to any harness project or assembly.");
        }

        return distinctSelections;
    }

    private static IReadOnlyList<ResolvedTargetSelection> ResolveTargetSelectionsFromDirectory(string directoryPath)
    {
        var directProjectSelections = Directory
            .EnumerateFiles(directoryPath, "*.*proj", SearchOption.TopDirectoryOnly)
            .Where(IsSupportedProjectPath)
            .Select(projectPath => ResolvedTargetSelection.Project(
                directoryPath,
                Path.GetFullPath(projectPath)))
            .ToArray();
        if (directProjectSelections.Length > 0)
        {
            return directProjectSelections;
        }

        var solutionSelections = Directory
            .EnumerateFiles(directoryPath, "*", SearchOption.TopDirectoryOnly)
            .Where(IsSupportedSolutionPath)
            .SelectMany(ResolveProjectsFromSolutionContainer)
            .Select(projectPath => ResolvedTargetSelection.Project(
                directoryPath,
                projectPath))
            .ToArray();
        if (solutionSelections.Length > 0)
        {
            return solutionSelections;
        }

        return Directory
            .EnumerateFiles(directoryPath, "*", SearchOption.TopDirectoryOnly)
            .Where(IsSupportedAssemblyPath)
            .Select(assemblyPath => ResolvedTargetSelection.Assembly(
                directoryPath,
                Path.GetFullPath(assemblyPath)))
            .ToArray();
    }

    private static IReadOnlyList<ResolvedTargetSelection> ResolveTargetSelectionsFromFile(string filePath)
    {
        if (IsSupportedProjectPath(filePath))
        {
            return [ResolvedTargetSelection.Project(filePath, filePath)];
        }

        if (IsSupportedAssemblyPath(filePath))
        {
            return [ResolvedTargetSelection.Assembly(filePath, filePath)];
        }

        if (IsSolutionPath(filePath))
        {
            return ResolveProjectsFromSolution(filePath)
                .Select(projectPath => ResolvedTargetSelection.Project(
                    filePath,
                    projectPath))
                .ToArray();
        }

        if (IsSolutionFilterPath(filePath))
        {
            return ResolveProjectsFromSolutionFilter(filePath)
                .Select(projectPath => ResolvedTargetSelection.Project(
                    filePath,
                    projectPath))
                .ToArray();
        }

        throw new AppResolutionException(
            $"The specified Sqloom target '{filePath}' is not supported. Use a directory, harness assembly, .sln, .slnx, .slnf, .csproj, .fsproj, or .vbproj path.");
    }

    private static IReadOnlyList<string> ResolveProjectsFromSolutionContainer(string path)
    {
        return IsSolutionPath(path)
            ? ResolveProjectsFromSolution(path)
            : ResolveProjectsFromSolutionFilter(path);
    }

    private static IReadOnlyList<string> ResolveProjectsFromSolution(string solutionPath)
    {
        return IsXmlSolutionPath(solutionPath)
            ? ResolveProjectsFromXmlSolution(solutionPath)
            : ResolveProjectsFromClassicSolution(solutionPath);
    }

    private static IReadOnlyList<string> ResolveProjectsFromClassicSolution(string solutionPath)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionPath)
            ?? throw new InvalidOperationException($"The solution path '{solutionPath}' has no parent directory.");

        return File
            .ReadLines(solutionPath)
            .Select(static line => _solutionProjectLineRegex.Match(line))
            .Where(static match => match.Success)
            .Select(static match => match.Groups["path"].Value)
            .Where(IsSupportedProjectPath)
            .Select(relativeProjectPath => Path.GetFullPath(relativeProjectPath, solutionDirectory))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> ResolveProjectsFromXmlSolution(string solutionPath)
    {
        try
        {
            var solutionDirectory = Path.GetDirectoryName(solutionPath)
                ?? throw new InvalidOperationException($"The solution path '{solutionPath}' has no parent directory.");
            var document = XDocument.Load(solutionPath);

            return document
                .Descendants("Project")
                .Select(static element => element.Attribute("Path")?.Value)
                .OfType<string>()
                .Where(static relativeProjectPath => !string.IsNullOrWhiteSpace(relativeProjectPath))
                .Where(IsSupportedProjectPath)
                .Select(relativeProjectPath => Path.GetFullPath(relativeProjectPath, solutionDirectory))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception exception) when (
            exception is IOException
                or UnauthorizedAccessException
                or XmlException)
        {
            throw new AppResolutionException(
                $"The solution '{solutionPath}' is not valid .slnx XML: {exception.Message}",
                exception);
        }
    }

    private static IReadOnlyList<string> ResolveProjectsFromSolutionFilter(string solutionFilterPath)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(solutionFilterPath));
            if (!document.RootElement.TryGetProperty("solution", out var solutionElement))
            {
                throw new AppResolutionException(
                    $"The solution filter '{solutionFilterPath}' is missing the required 'solution' object.");
            }

            if (!solutionElement.TryGetProperty("projects", out var projectsElement)
                || projectsElement.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<string>();
            }

            var solutionFilterDirectory = Path.GetDirectoryName(solutionFilterPath)
                ?? throw new InvalidOperationException($"The solution filter path '{solutionFilterPath}' has no parent directory.");

            List<string> projectPaths = [];
            foreach (var projectElement in projectsElement.EnumerateArray())
            {
                if (projectElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var relativeProjectPath = projectElement.GetString();
                if (string.IsNullOrWhiteSpace(relativeProjectPath)
                    || !IsSupportedProjectPath(relativeProjectPath))
                {
                    continue;
                }

                projectPaths.Add(Path.GetFullPath(
                    relativeProjectPath,
                    solutionFilterDirectory));
            }

            return projectPaths
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (JsonException exception)
        {
            throw new AppResolutionException(
                $"The solution filter '{solutionFilterPath}' is not valid JSON: {exception.Message}",
                exception);
        }
    }

    private static AppResolutionException BuildUnsupportedOrMissingTargetException(string targetPath)
    {
        if (IsSupportedTargetFilePath(targetPath))
        {
            return new AppResolutionException(
                $"The specified Sqloom target '{targetPath}' does not exist.");
        }

        return new AppResolutionException(
            $"The specified Sqloom target '{targetPath}' is not supported. Use a directory, harness assembly, .sln, .slnx, .slnf, .csproj, .fsproj, or .vbproj path.");
    }

    private static bool IsSupportedTargetFilePath(string path)
    {
        return IsSupportedProjectPath(path)
            || IsSupportedSolutionPath(path)
            || IsSupportedAssemblyPath(path);
    }

    private static bool IsSupportedSolutionPath(string path)
    {
        return IsSolutionPath(path)
            || IsSolutionFilterPath(path);
    }

    private static bool IsSolutionPath(string path)
    {
        return IsClassicSolutionPath(path)
            || IsXmlSolutionPath(path);
    }

    private static bool IsClassicSolutionPath(string path)
    {
        return string.Equals(
            Path.GetExtension(path),
            ".sln",
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsXmlSolutionPath(string path)
    {
        return string.Equals(
            Path.GetExtension(path),
            ".slnx",
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSolutionFilterPath(string path)
    {
        return string.Equals(
            Path.GetExtension(path),
            ".slnf",
            StringComparison.OrdinalIgnoreCase);
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

    private static bool IsSupportedAssemblyPath(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".dll" => true,
            ".exe" => true,
            _ => false,
        };
    }
}

internal enum ResolvedTargetKind
{
    Project,
    Assembly,
}

internal sealed record ResolvedTargetSelection(
    string RequestedTargetPath,
    string TargetPath,
    ResolvedTargetKind Kind)
{
    public static ResolvedTargetSelection Project(
        string requestedTargetPath,
        string projectPath)
    {
        return new ResolvedTargetSelection(
            requestedTargetPath,
            projectPath,
            ResolvedTargetKind.Project);
    }

    public static ResolvedTargetSelection Assembly(
        string requestedTargetPath,
        string assemblyPath)
    {
        return new ResolvedTargetSelection(
            requestedTargetPath,
            assemblyPath,
            ResolvedTargetKind.Assembly);
    }
}
