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
/// Resolves supported Sqloom target paths down to concrete project files.
/// </summary>
internal sealed class TargetPathResolver
{
    private static readonly Regex _solutionProjectLineRegex = new(
        "^Project\\(\"\\{[^}]+\\}\"\\)\\s*=\\s*\"[^\"]+\",\\s*\"(?<path>[^\"]+)\",\\s*\"\\{[^}]+\\}\"$",
        RegexOptions.Compiled);
    private readonly CompanionProjectLocator _companionProjectLocator = new();

    public string ResolveProjectPath(string targetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);

        var fullTargetPath = Path.GetFullPath(targetPath);
        var candidateProjectPaths = ResolveProjectPaths(fullTargetPath);
        return ResolveCandidateProjectPath(
            $"target '{fullTargetPath}'",
            candidateProjectPaths,
            "Place SqloomAppIntegrationType on the intended project, add a companion integration project that points back through SqloomTargetProject, or pass an explicit project path.");
    }

    public IReadOnlyCollection<string> ResolveProjectPaths(string targetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);

        var fullTargetPath = Path.GetFullPath(targetPath);
        if (Directory.Exists(fullTargetPath))
        {
            return ResolveProjectPathsFromDirectory(fullTargetPath);
        }

        if (File.Exists(fullTargetPath))
        {
            return ResolveProjectPathsFromFile(fullTargetPath);
        }

        if (IsSupportedTargetFilePath(fullTargetPath))
        {
            throw new AppResolutionException(
                $"The specified Sqloom target '{fullTargetPath}' does not exist.");
        }

        throw new AppResolutionException(
            $"The specified Sqloom target '{fullTargetPath}' is not supported. Use a directory, .sln, .slnx, .slnf, .csproj, .fsproj, or .vbproj path.");
    }

    private IReadOnlyCollection<string> ResolveProjectPathsFromDirectory(string directoryPath)
    {
        var directProjectPaths = Directory
            .EnumerateFiles(directoryPath, "*.*proj", SearchOption.TopDirectoryOnly)
            .Where(IsSupportedProjectPath)
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (directProjectPaths.Length == 1)
        {
            return directProjectPaths;
        }

        if (directProjectPaths.Length > 1)
        {
            return directProjectPaths
                .Where(_companionProjectLocator.IsSqloomCapableProject)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return Directory
            .EnumerateFiles(directoryPath, "*", SearchOption.TopDirectoryOnly)
            .Where(IsSupportedSolutionPath)
            .SelectMany(ResolveProjectsFromSolutionContainer)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyCollection<string> ResolveProjectPathsFromFile(string filePath)
    {
        if (IsSupportedProjectPath(filePath))
        {
            return [filePath];
        }

        if (IsSolutionPath(filePath))
        {
            return ResolveProjectsFromSolution(filePath);
        }

        if (IsSolutionFilterPath(filePath))
        {
            return ResolveProjectsFromSolutionFilter(filePath);
        }

        throw new AppResolutionException(
            $"The specified Sqloom target '{filePath}' is not supported. Use a directory, .sln, .slnx, .slnf, .csproj, .fsproj, or .vbproj path.");
    }

    private static string ResolveCandidateProjectPath(
        string sourceDescription,
        IReadOnlyCollection<string> candidateProjectPaths,
        string guidance)
    {
        return candidateProjectPaths.Count switch
        {
            0 => throw new AppResolutionException(
                $"The Sqloom {sourceDescription} did not resolve to any Sqloom-capable projects. {guidance}"),
            1 => candidateProjectPaths.First(),
            _ => throw new AppResolutionException(
                $"The Sqloom {sourceDescription} resolved to multiple Sqloom-capable projects: {string.Join(", ", candidateProjectPaths)}. Pass an explicit project path instead."),
        };
    }

    private IReadOnlyCollection<string> ResolveProjectsFromSolutionContainer(string path)
    {
        return IsSolutionPath(path)
            ? ResolveProjectsFromSolution(path)
            : ResolveProjectsFromSolutionFilter(path);
    }

    private IReadOnlyCollection<string> ResolveProjectsFromSolution(string solutionPath)
    {
        return IsXmlSolutionPath(solutionPath)
            ? ResolveProjectsFromXmlSolution(solutionPath)
            : ResolveProjectsFromClassicSolution(solutionPath);
    }

    private IReadOnlyCollection<string> ResolveProjectsFromClassicSolution(string solutionPath)
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
            .Where(_companionProjectLocator.IsSqloomCapableProject)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyCollection<string> ResolveProjectsFromXmlSolution(string solutionPath)
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
                .Select(relativeProjectPath => Path.GetFullPath(relativeProjectPath!, solutionDirectory))
                .Where(_companionProjectLocator.IsSqloomCapableProject)
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

    private IReadOnlyCollection<string> ResolveProjectsFromSolutionFilter(string solutionFilterPath)
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

                var fullProjectPath = Path.GetFullPath(
                    relativeProjectPath,
                    solutionFilterDirectory);
                if (_companionProjectLocator.IsSqloomCapableProject(fullProjectPath))
                {
                    projectPaths.Add(fullProjectPath);
                }
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

    private static bool IsSupportedTargetFilePath(string path)
    {
        return IsSupportedProjectPath(path)
            || IsSupportedSolutionPath(path);
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
}
