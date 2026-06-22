using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sqloom.Host;

/// <summary>
/// Resolves harness target projects and assemblies into loadable assembly paths.
/// </summary>
internal sealed class AppProjectResolver
{
    private readonly TargetPathResolver _targetPathResolver = new();

    public string ResolveAssemblyPath(
        string targetPath,
        bool noBuild,
        string dotNetCommand)
    {
        var assemblySelections = ResolveAssemblySelections(
            targetPath,
            noBuild,
            dotNetCommand);
        return assemblySelections.Count switch
        {
            1 => assemblySelections[0].AssemblyPath,
            _ => throw BuildMultipleAssemblySelectionException(targetPath, assemblySelections),
        };
    }

    internal IReadOnlyList<ResolvedAssemblySelection> ResolveAssemblySelections(
        string targetPath,
        bool noBuild,
        string dotNetCommand)
    {
        return _targetPathResolver
            .ResolveTargetSelections(targetPath)
            .Select(selection => new ResolvedAssemblySelection(
                selection,
                ResolveAssemblyPath(
                    selection,
                    noBuild,
                    dotNetCommand)))
            .DistinctBy(static selection => selection.AssemblyPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static AppResolutionException BuildMultipleAssemblySelectionException(
        string targetPath,
        IReadOnlyCollection<ResolvedAssemblySelection> assemblySelections)
    {
        return new AppResolutionException(
            $"The Sqloom target '{Path.GetFullPath(targetPath)}' resolved to multiple harness assembly candidates: {string.Join(", ", assemblySelections.Select(static selection => selection.AssemblyPath))}. Pass a narrower target in v1.");
    }

    private string ResolveAssemblyPath(
        ResolvedTargetSelection targetSelection,
        bool noBuild,
        string dotNetCommand)
    {
        if (targetSelection.Kind == ResolvedTargetKind.Assembly)
        {
            return NormalizeAndValidateAssemblyPath(targetSelection.TargetPath);
        }

        var projectPath = NormalizeAndValidateProjectPath(
            targetSelection.TargetPath,
            "harness project");
        var projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException($"The harness project path '{projectPath}' has no parent directory.");
        var resolvedAssemblyPath = ResolveTargetPath(
            projectPath,
            projectDirectory,
            dotNetCommand);

        if (!noBuild)
        {
            BuildProject(
                projectPath,
                projectDirectory,
                dotNetCommand);
        }

        if (!File.Exists(resolvedAssemblyPath))
        {
            var missingArtifactMessage = noBuild
                ? $"The Sqloom harness project '{projectPath}' resolved to '{resolvedAssemblyPath}', but that assembly does not exist. Remove --no-build or build the project first."
                : $"The Sqloom harness project '{projectPath}' resolved to '{resolvedAssemblyPath}', but that assembly was not found after the build completed.";
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
                $"Failed to resolve the build output for Sqloom harness project '{projectPath}'. {FormatCommandOutput(dotNetCommand, result)}");
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
                $"Failed to resolve the build output for Sqloom harness project '{projectPath}'. dotnet msbuild did not report a TargetPath.");
        }

        return Path.GetFullPath(
            targetPath,
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
                $"Failed to build Sqloom harness project '{projectPath}'. {FormatCommandOutput(dotNetCommand, result)}");
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
                    $"Failed to start '{dotNetCommand}' while resolving a Sqloom harness project.");
            }
        }
        catch (Exception exception) when (
            exception is InvalidOperationException
                or Win32Exception)
        {
            throw new AppResolutionException(
                $"Failed to start '{dotNetCommand}' while resolving a Sqloom harness project: {exception.Message}",
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
        string projectDescription)
    {
        var fullProjectPath = Path.GetFullPath(projectPath);
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

    private static string NormalizeAndValidateAssemblyPath(string assemblyPath)
    {
        var fullAssemblyPath = Path.GetFullPath(assemblyPath);
        if (!File.Exists(fullAssemblyPath))
        {
            throw new AppResolutionException(
                $"The specified harness assembly '{fullAssemblyPath}' does not exist.");
        }

        if (!IsSupportedAssemblyPath(fullAssemblyPath))
        {
            throw new AppResolutionException(
                $"The specified harness assembly '{fullAssemblyPath}' is not supported. Use a .dll or .exe file.");
        }

        return fullAssemblyPath;
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

    private static bool IsSupportedAssemblyPath(string assemblyPath)
    {
        return Path.GetExtension(assemblyPath).ToLowerInvariant() switch
        {
            ".dll" => true,
            ".exe" => true,
            _ => false,
        };
    }

    private sealed record DotNetCommandResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}

internal sealed record ResolvedAssemblySelection(
    ResolvedTargetSelection TargetSelection,
    string AssemblyPath);
