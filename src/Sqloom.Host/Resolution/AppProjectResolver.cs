using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sqloom.Host;

/// <summary>
/// Resolves harness target projects and assemblies into loadable assembly paths.
/// </summary>
internal sealed class AppProjectResolver
{
    private const int MinimumFileBasedAppSdkMajorVersion = 10;
    private readonly TargetPathResolver _targetPathResolver = new();

    public async Task<string> ResolveAssemblyPathAsync(
        string targetPath,
        bool noBuild,
        string dotNetCommand,
        CancellationToken cancellationToken = default)
    {
        var assemblySelections = await ResolveAssemblySelectionsAsync(
                targetPath,
                noBuild,
                dotNetCommand,
                cancellationToken)
            .ConfigureAwait(false);
        return assemblySelections.Count switch
        {
            1 => assemblySelections[0].AssemblyPath,
            _ => throw BuildMultipleAssemblySelectionException(targetPath, assemblySelections),
        };
    }

    internal async Task<IReadOnlyList<ResolvedAssemblySelection>> ResolveAssemblySelectionsAsync(
        string targetPath,
        bool noBuild,
        string dotNetCommand,
        CancellationToken cancellationToken = default)
    {
        List<ResolvedAssemblySelection> assemblySelections = [];
        foreach (var selection in _targetPathResolver.ResolveTargetSelections(targetPath))
        {
            assemblySelections.Add(new ResolvedAssemblySelection(
                selection,
                await ResolveAssemblyPathAsync(
                        selection,
                        noBuild,
                        dotNetCommand,
                        cancellationToken)
                    .ConfigureAwait(false)));
        }

        return assemblySelections
            .DistinctBy(static selection => selection.AssemblyPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static AppResolutionException BuildMultipleAssemblySelectionException(
        string targetPath,
        IReadOnlyCollection<ResolvedAssemblySelection> assemblySelections)
    {
        return new AppResolutionException(
            $"The Sqloom target '{Path.GetFullPath(targetPath)}' resolved to multiple harness assembly candidates: {string.Join(", ", assemblySelections.Select(static selection => selection.AssemblyPath))}. Pass a narrower target.");
    }

    private async Task<string> ResolveAssemblyPathAsync(
        ResolvedTargetSelection targetSelection,
        bool noBuild,
        string dotNetCommand,
        CancellationToken cancellationToken)
    {
        if (targetSelection.Kind == ResolvedTargetKind.Assembly)
        {
            return NormalizeAndValidateAssemblyPath(targetSelection.TargetPath);
        }

        if (targetSelection.Kind == ResolvedTargetKind.CSharpFile)
        {
            if (noBuild)
            {
                throw new AppResolutionException(
                    "C# file-based Sqloom harness targets are always built. Remove --no-build and try again.");
            }

            return await BuildCSharpFileAsync(
                    targetSelection.TargetPath,
                    dotNetCommand,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var projectPath = NormalizeAndValidateProjectPath(
            targetSelection.TargetPath,
            "harness project");
        var projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException($"The harness project path '{projectPath}' has no parent directory.");
        var resolvedAssemblyPath = await ResolveTargetPathAsync(
                projectPath,
                projectDirectory,
                dotNetCommand,
                cancellationToken)
            .ConfigureAwait(false);

        if (!noBuild)
        {
            await BuildProjectAsync(
                    projectPath,
                    projectDirectory,
                    dotNetCommand,
                    cancellationToken)
                .ConfigureAwait(false);
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

    private static async Task<string> BuildCSharpFileAsync(
        string sourceFilePath,
        string dotNetCommand,
        CancellationToken cancellationToken)
    {
        var fullSourceFilePath = NormalizeAndValidateCSharpFilePath(sourceFilePath);
        var sourceDirectory = Path.GetDirectoryName(fullSourceFilePath)
            ?? throw new InvalidOperationException($"The C# file-based harness path '{fullSourceFilePath}' has no parent directory.");

        await EnsureFileBasedAppsSupportedAsync(
                dotNetCommand,
                sourceDirectory,
                cancellationToken)
            .ConfigureAwait(false);

        var buildId = Guid.NewGuid().ToString("N");
        var assemblyName = ResolveCSharpFileAssemblyName(fullSourceFilePath);
        var outputDirectory = Path.Combine(
            Path.GetTempPath(),
            "sqloom",
            "file-harnesses",
            buildId);
        var assemblyPath = Path.Combine(
            outputDirectory,
            $"{assemblyName}.dll");
        var dependencyManifestPath = Path.Combine(
            outputDirectory,
            $"{assemblyName}.deps.json");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await ExecuteDotNetCommandAsync(
                    dotNetCommand,
                    sourceDirectory,
                    cancellationToken,
                    "build",
                    Path.GetFileName(fullSourceFilePath),
                    "--configuration",
                    "Debug",
                    "--artifacts-path",
                    Path.Combine(outputDirectory, "build"),
                    "--output",
                    outputDirectory,
                    "--nologo")
                .ConfigureAwait(false);
            if (result.ExitCode != 0)
            {
                throw new AppResolutionException(
                    $"Failed to build C# file-based Sqloom harness '{fullSourceFilePath}'. {FormatCommandOutput(dotNetCommand, result)}");
            }

            if (!File.Exists(assemblyPath)
                || !File.Exists(dependencyManifestPath))
            {
                throw new AppResolutionException(
                    $"The C# file-based Sqloom harness '{fullSourceFilePath}' built successfully, but its expected assembly and dependency manifest were not found in '{outputDirectory}'.");
            }

            return assemblyPath;
        }
        catch
        {
            DeleteDirectoryIfExists(outputDirectory);
            throw;
        }
    }

    private static string ResolveCSharpFileAssemblyName(string sourceFilePath)
    {
        var assemblyName = Path.GetFileNameWithoutExtension(sourceFilePath);
        foreach (var line in File.ReadLines(sourceFilePath))
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.Length == 0
                || trimmedLine.StartsWith("#!", StringComparison.Ordinal))
            {
                continue;
            }

            if (!trimmedLine.StartsWith("#:", StringComparison.Ordinal))
            {
                break;
            }

            const string propertyPrefix = "#:property";
            if (!trimmedLine.StartsWith(
                    propertyPrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var assignment = trimmedLine[propertyPrefix.Length..].Trim();
            var separatorIndex = assignment.IndexOf('=');
            if (separatorIndex <= 0
                || !string.Equals(
                    assignment[..separatorIndex].Trim(),
                    "AssemblyName",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var configuredAssemblyName = assignment[(separatorIndex + 1)..].Trim();
            if (configuredAssemblyName.Contains("$(", StringComparison.Ordinal))
            {
                throw new AppResolutionException(
                    $"The C# file-based Sqloom harness '{sourceFilePath}' uses an AssemblyName expression. Use a literal AssemblyName value so Sqloom can locate the build output.");
            }

            if (string.IsNullOrWhiteSpace(configuredAssemblyName)
                || configuredAssemblyName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new AppResolutionException(
                    $"The C# file-based Sqloom harness '{sourceFilePath}' has an invalid AssemblyName value.");
            }

            assemblyName = configuredAssemblyName;
        }

        return assemblyName;
    }

    private static async Task EnsureFileBasedAppsSupportedAsync(
        string dotNetCommand,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteDotNetCommandAsync(
                dotNetCommand,
                workingDirectory,
                cancellationToken,
                "--version")
            .ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new AppResolutionException(
                $"Failed to determine whether '{dotNetCommand}' supports C# file-based apps. {FormatCommandOutput(dotNetCommand, result)}");
        }

        var reportedVersion = result.StandardOutput.Trim();
        ValidateFileBasedAppSdkVersion(
            dotNetCommand,
            reportedVersion);
    }

    internal static void ValidateFileBasedAppSdkVersion(
        string dotNetCommand,
        string reportedVersion)
    {
        if (!TryGetFileBasedAppSdkMajorVersion(
                reportedVersion,
                out var majorVersion)
            || majorVersion < MinimumFileBasedAppSdkMajorVersion)
        {
            throw new AppResolutionException(
                $"C# file-based Sqloom harness targets require .NET SDK {MinimumFileBasedAppSdkMajorVersion} or later, but '{dotNetCommand}' reported '{reportedVersion}'.");
        }
    }

    internal static bool TryGetFileBasedAppSdkMajorVersion(
        string value,
        out int majorVersion)
    {
        majorVersion = 0;
        foreach (var line in value.Split(
                     ['\r', '\n'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var versionText = line.Split('-', '+')[0];
            if (!Version.TryParse(
                    versionText,
                    out var version))
            {
                continue;
            }

            majorVersion = version.Major;
            return true;
        }

        return false;
    }

    private static async Task<string> ResolveTargetPathAsync(
        string projectPath,
        string workingDirectory,
        string dotNetCommand,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteDotNetCommandAsync(
                dotNetCommand,
                workingDirectory,
                cancellationToken,
                "msbuild",
                projectPath,
                "-nologo",
                "-getProperty:TargetPath")
            .ConfigureAwait(false);
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

    private static async Task BuildProjectAsync(
        string projectPath,
        string workingDirectory,
        string dotNetCommand,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteDotNetCommandAsync(
                dotNetCommand,
                workingDirectory,
                cancellationToken,
                "build",
                projectPath,
                "--tl:off",
                "--nologo",
                "-clp:ErrorsOnly;NoSummary")
            .ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new AppResolutionException(
                $"Failed to build Sqloom harness project '{projectPath}'. {FormatCommandOutput(dotNetCommand, result)}");
        }
    }

    private static async Task<DotNetCommandResult> ExecuteDotNetCommandAsync(
        string dotNetCommand,
        string workingDirectory,
        CancellationToken cancellationToken,
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
                    $"Failed to start '{dotNetCommand}' while resolving a Sqloom harness target.");
            }
        }
        catch (Exception exception) when (
            exception is InvalidOperationException
                or Win32Exception)
        {
            throw new AppResolutionException(
                $"Failed to start '{dotNetCommand}' while resolving a Sqloom harness target: {exception.Message}",
                exception);
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        try
        {
            await process
                .WaitForExitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKillProcess(process);
            throw;
        }

        var standardOutput = await standardOutputTask.ConfigureAwait(false);
        var standardError = await standardErrorTask.ConfigureAwait(false);

        return new DotNetCommandResult(
            process.ExitCode,
            standardOutput,
            standardError);
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (Win32Exception)
        {
        }
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

    private static string NormalizeAndValidateCSharpFilePath(string sourceFilePath)
    {
        var fullSourceFilePath = Path.GetFullPath(sourceFilePath);
        if (!File.Exists(fullSourceFilePath))
        {
            throw new AppResolutionException(
                $"The specified C# file-based Sqloom harness '{fullSourceFilePath}' does not exist.");
        }

        if (!string.Equals(
                Path.GetExtension(fullSourceFilePath),
                ".cs",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new AppResolutionException(
                $"The specified C# file-based Sqloom harness '{fullSourceFilePath}' is not a .cs file.");
        }

        return fullSourceFilePath;
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

    private static void DeleteDirectoryIfExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        try
        {
            Directory.Delete(
                directoryPath,
                recursive: true);
        }
        catch (Exception exception) when (
            exception is IOException
                or UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// Captures dotnet process output so build and TargetPath failures can preserve stderr context.
    /// </summary>
    private sealed record DotNetCommandResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}

/// <summary>
/// Links a resolved target selection to the assembly path that the host will load.
/// </summary>
internal sealed record ResolvedAssemblySelection(
    ResolvedTargetSelection TargetSelection,
    string AssemblyPath);
