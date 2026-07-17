using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Sqloom.Host.Replay;

/// <summary>
/// Resolves the ASP.NET Core source project used for endpoint discovery.
/// </summary>
internal sealed class EndpointSourceProjectResolver
{
    private const string FileAppProjectDirective = "#:project";

    public string Resolve(
        string[] args,
        HostStartupOptions startupOptions,
        string currentDirectory)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(startupOptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentDirectory);

        var explicitProject = CommandArgumentSupport.GetArgumentValue(args, "--app-project");
        if (!string.IsNullOrWhiteSpace(explicitProject))
        {
            return NormalizeAndValidateProjectPath(
                Path.GetFullPath(explicitProject, currentDirectory),
                "--app-project");
        }

        if (string.IsNullOrWhiteSpace(startupOptions.AppTargetPath))
        {
            throw new ArgumentException(
                "Endpoint discovery requires a target path or --app-project <path>.");
        }

        var targetPath = Path.GetFullPath(startupOptions.AppTargetPath);
        if (IsCSharpFile(targetPath))
        {
            return ResolveFromFileBasedHarness(targetPath);
        }

        if (IsCSharpProject(targetPath))
        {
            if (IsWebSdkProject(targetPath))
            {
                return NormalizeAndValidateProjectPath(targetPath, "target project");
            }

            return ResolveFromProjectBackedHarness(targetPath);
        }

        throw new ArgumentException(
            $"Sqloom could not infer an ASP.NET Core source project from '{targetPath}'. Supply --app-project <path>.");
    }

    private static string ResolveFromFileBasedHarness(string sourceFilePath)
    {
        var fullSourceFilePath = Path.GetFullPath(sourceFilePath);
        if (!File.Exists(fullSourceFilePath))
        {
            throw new ArgumentException($"The file-based harness '{fullSourceFilePath}' does not exist.");
        }

        var sourceDirectory = Path.GetDirectoryName(fullSourceFilePath)
            ?? throw new InvalidOperationException($"The file-based harness '{fullSourceFilePath}' has no parent directory.");
        var sourceProjectPaths = File.ReadLines(fullSourceFilePath)
            .Select(static line => line.Trim())
            .Where(static line => line.StartsWith(FileAppProjectDirective, StringComparison.OrdinalIgnoreCase))
            .Select(line => line[FileAppProjectDirective.Length..].Trim())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(value => NormalizeAndValidateProjectPath(
                Path.GetFullPath(value, sourceDirectory),
                FileAppProjectDirective))
            .Where(IsWebSdkProject)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return sourceProjectPaths.Length switch
        {
            1 => sourceProjectPaths[0],
            0 => throw new ArgumentException(
                $"The file-based harness '{fullSourceFilePath}' does not contain exactly one Web SDK #:project directive pointing at an ASP.NET Core source project. Supply --app-project <path>."),
            _ => throw new ArgumentException(
                $"The file-based harness '{fullSourceFilePath}' contains multiple ASP.NET Core source project directives: {string.Join(", ", sourceProjectPaths)}. Supply --app-project <path> to select one."),
        };
    }

    private static string ResolveFromProjectBackedHarness(string projectPath)
    {
        var fullProjectPath = NormalizeAndValidateProjectPath(projectPath, "target project");
        var projectDirectory = Path.GetDirectoryName(fullProjectPath)
            ?? throw new InvalidOperationException($"The project '{fullProjectPath}' has no parent directory.");
        var referencedWebProjects = ReadProjectReferences(fullProjectPath)
            .Select(path => Path.GetFullPath(path, projectDirectory))
            .Where(IsWebSdkProject)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return referencedWebProjects.Length switch
        {
            1 => referencedWebProjects[0],
            0 => throw new ArgumentException(
                $"The harness project '{fullProjectPath}' does not reference exactly one ASP.NET Core source project. Supply --app-project <path>."),
            _ => throw new ArgumentException(
                $"The harness project '{fullProjectPath}' references multiple ASP.NET Core source projects: {string.Join(", ", referencedWebProjects)}. Supply --app-project <path>."),
        };
    }

    private static IReadOnlyList<string> ReadProjectReferences(string projectPath)
    {
        var document = XDocument.Load(projectPath);
        return document
            .Descendants()
            .Where(static element => string.Equals(element.Name.LocalName, "ProjectReference", StringComparison.OrdinalIgnoreCase))
            .Select(static element => element.Attribute("Include")?.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToArray();
    }

    private static bool IsWebSdkProject(string projectPath)
    {
        if (!File.Exists(projectPath) || !IsCSharpProject(projectPath))
        {
            return false;
        }

        var document = XDocument.Load(projectPath);
        var sdk = document.Root?.Attribute("Sdk")?.Value;
        return sdk?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(static item => string.Equals(item, "Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase)) == true;
    }

    private static string NormalizeAndValidateProjectPath(
        string projectPath,
        string source)
    {
        var fullProjectPath = Path.GetFullPath(projectPath);
        if (!File.Exists(fullProjectPath))
        {
            throw new ArgumentException($"The source project from {source} does not exist: '{fullProjectPath}'.");
        }

        if (!IsCSharpProject(fullProjectPath))
        {
            throw new ArgumentException($"The source project from {source} must be a .csproj file: '{fullProjectPath}'.");
        }

        return fullProjectPath;
    }

    private static bool IsCSharpFile(string path)
    {
        return string.Equals(Path.GetExtension(path), ".cs", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCSharpProject(string path)
    {
        return string.Equals(Path.GetExtension(path), ".csproj", StringComparison.OrdinalIgnoreCase);
    }
}
