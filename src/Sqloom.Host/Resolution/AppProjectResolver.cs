using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sqloom.Host;

/// <summary>
/// Resolves an app project and its companion Sqloom integration project.
/// </summary>
internal sealed class AppProjectResolver
{
    private readonly CompanionProjectLocator _companionProjectLocator = new();
    private readonly TargetPathResolver _targetPathResolver = new();

    public string ResolveAssemblyPath(
        string targetPath,
        bool noBuild,
        string dotNetCommand)
    {
        var projectSelection = ResolveProjectSelection(targetPath);
        return ResolveAssemblyPath(
            projectSelection,
            noBuild,
            dotNetCommand);
    }

    internal IReadOnlyList<ResolvedAssemblySelection> ResolveAssemblySelections(
        string targetPath,
        bool noBuild,
        string dotNetCommand)
    {
        return ResolveProjectSelections(targetPath)
            .Select(projectSelection => new ResolvedAssemblySelection(
                projectSelection,
                ResolveAssemblyPath(
                    projectSelection,
                    noBuild,
                    dotNetCommand)))
            .ToArray();
    }

    internal ResolvedProjectSelection ResolveProjectSelection(string targetPath)
    {
        var fullTargetPath = Path.GetFullPath(targetPath);
        var projectSelections = ResolveProjectSelections(fullTargetPath);
        return projectSelections.Count switch
        {
            1 => projectSelections[0],
            _ => throw BuildMultipleProjectSelectionException(fullTargetPath, projectSelections),
        };
    }

    internal IReadOnlyList<ResolvedProjectSelection> ResolveProjectSelections(string targetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);

        var fullTargetPath = Path.GetFullPath(targetPath);
        var fullProjectPaths = _targetPathResolver.ResolveProjectPaths(fullTargetPath);
        List<ResolvedProjectSelection> projectSelections = [];
        HashSet<string> resolvedIntegrationProjects = new(StringComparer.OrdinalIgnoreCase);

        foreach (var fullProjectPath in fullProjectPaths)
        {
            var validatedProjectPath = NormalizeAndValidateProjectPath(
                fullProjectPath,
                "app project");
            var projectDirectory = Path.GetDirectoryName(validatedProjectPath)
                ?? throw new InvalidOperationException($"The app project path '{validatedProjectPath}' has no parent directory.");
            var integrationProjectPath = ResolveIntegrationProjectPath(
                _companionProjectLocator,
                validatedProjectPath,
                projectDirectory);
            if (!resolvedIntegrationProjects.Add(integrationProjectPath))
            {
                continue;
            }

            projectSelections.Add(new ResolvedProjectSelection(
                fullTargetPath,
                validatedProjectPath,
                integrationProjectPath));
        }

        if (projectSelections.Count == 0)
        {
            throw new AppResolutionException(
                $"The Sqloom target '{fullTargetPath}' did not resolve to any Sqloom-capable projects. Use an explicit project path, place SqloomAppIntegrationType on the intended project, or add a companion integration project that points back through SqloomTargetProject.");
        }

        return projectSelections;
    }

    private static AppResolutionException BuildMultipleProjectSelectionException(
        string targetPath,
        IReadOnlyCollection<ResolvedProjectSelection> projectSelections)
    {
        var selectionDescriptions = projectSelections
            .Select(static projectSelection => projectSelection.UsesCompanionIntegrationProject
                ? $"{projectSelection.TargetProjectPath} -> {projectSelection.IntegrationProjectPath}"
                : projectSelection.TargetProjectPath);
        return new AppResolutionException(
            $"The Sqloom target '{targetPath}' resolved to multiple distinct app integrations: {string.Join(", ", selectionDescriptions)}. Pass an explicit project path instead.");
    }

    private string ResolveAssemblyPath(
        ResolvedProjectSelection projectSelection,
        bool noBuild,
        string dotNetCommand)
    {
        var effectiveProjectPath = projectSelection.IntegrationProjectPath;
        var projectDirectory = Path.GetDirectoryName(effectiveProjectPath)
            ?? throw new InvalidOperationException($"The app project path '{effectiveProjectPath}' has no parent directory.");
        var resolvedAssemblyPath = ResolveTargetPath(
            effectiveProjectPath,
            projectDirectory,
            dotNetCommand);

        if (!noBuild)
        {
            BuildProject(
                effectiveProjectPath,
                projectDirectory,
                dotNetCommand);
        }

        if (!File.Exists(resolvedAssemblyPath))
        {
            var missingArtifactMessage = noBuild
                ? BuildMissingNoBuildMessage(projectSelection, resolvedAssemblyPath)
                : BuildMissingBuildOutputMessage(projectSelection, resolvedAssemblyPath);
            throw new AppResolutionException(missingArtifactMessage);
        }

        return resolvedAssemblyPath;
    }

    private static string ResolveTargetPath(
        string projectPath,
        string workingDirectory,
        string dotNetCommand)
    {
        var result = ExecuteDotNetCommand(
            dotNetCommand,
            workingDirectory,
            "msbuild",
            projectPath,
            "-nologo",
            "-getProperty:TargetPath");
        if (result.ExitCode != 0)
        {
            throw new AppResolutionException(
                $"Failed to resolve the build output for Sqloom app project '{projectPath}'. {FormatCommandOutput(dotNetCommand, result)}");
        }

        var targetPath = result.StandardOutput
            .Split(
                ['\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Reverse()
            .FirstOrDefault(static line =>
                line.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                || line.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            throw new AppResolutionException(
                $"Failed to resolve the build output for Sqloom app project '{projectPath}'. dotnet msbuild did not report a TargetPath.");
        }

        return Path.GetFullPath(
            targetPath,
            workingDirectory);
    }

    private static string ResolveIntegrationProjectPath(
        CompanionProjectLocator companionProjectLocator,
        string projectPath,
        string workingDirectory)
    {
        var companionProjectPath = companionProjectLocator.TryResolveCompanionProjectPath(projectPath);
        if (string.IsNullOrWhiteSpace(companionProjectPath))
        {
            return projectPath;
        }

        return NormalizeAndValidateProjectPath(
            companionProjectPath,
            "Sqloom integration project",
            workingDirectory);
    }

    private static void BuildProject(
        string projectPath,
        string workingDirectory,
        string dotNetCommand)
    {
        var result = ExecuteDotNetCommand(
            dotNetCommand,
            workingDirectory,
            "build",
            projectPath,
            "--tl:off",
            "--nologo",
            "-clp:ErrorsOnly;NoSummary");
        if (result.ExitCode != 0)
        {
            throw new AppResolutionException(
                $"Failed to build Sqloom app project '{projectPath}'. {FormatCommandOutput(dotNetCommand, result)}");
        }
    }

    private static DotNetCommandResult ExecuteDotNetCommand(
        string dotNetCommand,
        string workingDirectory,
        params string[] arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dotNetCommand);
        ProcessStartInfo startInfo = new(dotNetCommand)
        {
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = new()
        {
            StartInfo = startInfo,
        };

        try
        {
            if (!process.Start())
            {
                throw new AppResolutionException(
                    $"Failed to start '{dotNetCommand}' while resolving a Sqloom app project.");
            }
        }
        catch (Exception exception) when (
            exception is InvalidOperationException
                or Win32Exception)
        {
            throw new AppResolutionException(
                $"Failed to start '{dotNetCommand}' while resolving a Sqloom app project: {exception.Message}",
                exception);
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        Task.WaitAll(
            standardOutputTask,
            standardErrorTask);

        return new DotNetCommandResult(
            process.ExitCode,
            standardOutputTask.Result,
            standardErrorTask.Result);
    }

    private static string FormatCommandOutput(
        string dotNetCommand,
        DotNetCommandResult result)
    {
        var output = string.Join(
            Environment.NewLine,
            new[]
            {
                result.StandardOutput.Trim(),
                result.StandardError.Trim(),
            }.Where(static item => !string.IsNullOrWhiteSpace(item)));

        return string.IsNullOrWhiteSpace(output)
            ? $"'{dotNetCommand}' exited with code {result.ExitCode}."
            : output;
    }

    private static string NormalizeAndValidateProjectPath(
        string projectPath,
        string projectDescription,
        string? basePath = null)
    {
        var fullProjectPath = basePath is null
            ? Path.GetFullPath(projectPath)
            : Path.GetFullPath(projectPath, basePath);
        if (!File.Exists(fullProjectPath))
        {
            throw new AppResolutionException(
                $"The specified {projectDescription} '{fullProjectPath}' does not exist.");
        }

        if (!IsSupportedProjectPath(fullProjectPath))
        {
            throw new AppResolutionException(
                $"The specified {projectDescription} '{fullProjectPath}' is not a supported MSBuild project file. Use a .csproj, .fsproj, or .vbproj file.");
        }

        return fullProjectPath;
    }

    private static string BuildMissingNoBuildMessage(
        ResolvedProjectSelection projectSelection,
        string targetPath)
    {
        if (!projectSelection.UsesResolvedTargetProject
            && !projectSelection.UsesCompanionIntegrationProject)
        {
            return $"The Sqloom app project '{projectSelection.TargetProjectPath}' resolved to '{targetPath}', but that assembly does not exist. Remove --no-build or build the project first.";
        }

        if (projectSelection.UsesResolvedTargetProject
            && !projectSelection.UsesCompanionIntegrationProject)
        {
            return $"The Sqloom target '{projectSelection.RequestedTargetPath}' resolved to app project '{projectSelection.TargetProjectPath}', but the resolved assembly '{targetPath}' does not exist. Remove --no-build or build the project first.";
        }

        if (!projectSelection.UsesResolvedTargetProject)
        {
            return $"The Sqloom target project '{projectSelection.TargetProjectPath}' maps to companion integration project '{projectSelection.IntegrationProjectPath}', but the resolved assembly '{targetPath}' does not exist. Remove --no-build or build the companion integration project first.";
        }

        return $"The Sqloom target '{projectSelection.RequestedTargetPath}' resolved to app project '{projectSelection.TargetProjectPath}' and companion integration project '{projectSelection.IntegrationProjectPath}', but the resolved assembly '{targetPath}' does not exist. Remove --no-build or build the companion integration project first.";
    }

    private static string BuildMissingBuildOutputMessage(
        ResolvedProjectSelection projectSelection,
        string targetPath)
    {
        if (!projectSelection.UsesResolvedTargetProject
            && !projectSelection.UsesCompanionIntegrationProject)
        {
            return $"The Sqloom app project '{projectSelection.TargetProjectPath}' resolved to '{targetPath}', but that assembly was not found after the build completed.";
        }

        if (projectSelection.UsesResolvedTargetProject
            && !projectSelection.UsesCompanionIntegrationProject)
        {
            return $"The Sqloom target '{projectSelection.RequestedTargetPath}' resolved to app project '{projectSelection.TargetProjectPath}', but the resolved assembly '{targetPath}' was not found after the build completed.";
        }

        if (!projectSelection.UsesResolvedTargetProject)
        {
            return $"The Sqloom target project '{projectSelection.TargetProjectPath}' maps to companion integration project '{projectSelection.IntegrationProjectPath}', but the resolved assembly '{targetPath}' was not found after the build completed.";
        }

        return $"The Sqloom target '{projectSelection.RequestedTargetPath}' resolved to app project '{projectSelection.TargetProjectPath}' and companion integration project '{projectSelection.IntegrationProjectPath}', but the resolved assembly '{targetPath}' was not found after the build completed.";
    }

    private static bool IsSupportedProjectPath(string projectPath)
    {
        return Path.GetExtension(projectPath).ToLowerInvariant() switch
        {
            ".csproj" => true,
            ".fsproj" => true,
            ".vbproj" => true,
            _ => false,
        };
    }

    private sealed record DotNetCommandResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}

internal sealed record ResolvedProjectSelection(
    string RequestedTargetPath,
    string TargetProjectPath,
    string IntegrationProjectPath)
{
    public bool UsesResolvedTargetProject =>
        !string.Equals(
            RequestedTargetPath,
            TargetProjectPath,
            StringComparison.OrdinalIgnoreCase);

    public bool UsesCompanionIntegrationProject =>
        !string.Equals(
            TargetProjectPath,
            IntegrationProjectPath,
            StringComparison.OrdinalIgnoreCase);
}

internal sealed record ResolvedAssemblySelection(
    ResolvedProjectSelection ProjectSelection,
    string AssemblyPath);
