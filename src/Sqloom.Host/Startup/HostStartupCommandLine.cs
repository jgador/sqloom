using System;
using System.Collections.Generic;
using System.IO;

namespace Sqloom.Host;

/// <summary>
/// Parses and validates the Sqloom host startup command line.
/// </summary>
internal sealed class HostStartupCommandLine
{
    public HostStartupOptions Parse(string[] args, string currentDirectory)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentDirectory);

        List<string> applicationArguments = [];
        string? appTargetPath = null;
        string? dotNetCommand = null;
        var positionalTargetIndex = GetVerbScopedTargetIndex(args, currentDirectory);
        var noBuild = false;
        var debugEnabled = false;
        var showHelp = false;
        var showVersion = false;

        if (args.Length > 0)
        {
            EnsureSupportedLeadingArgument(args[0], currentDirectory);
        }

        for (var index = 0; index < args.Length; index++)
        {
            if (positionalTargetIndex == index)
            {
                appTargetPath = SetPathOnce(
                    appTargetPath,
                    args[index],
                    currentDirectory,
                    "Sqloom target path");
                continue;
            }

            var argument = args[index];
            if (string.Equals(argument, "--help", StringComparison.OrdinalIgnoreCase))
            {
                showHelp = true;
                continue;
            }

            if (string.Equals(argument, "--version", StringComparison.OrdinalIgnoreCase))
            {
                showVersion = true;
                continue;
            }

            if (string.Equals(argument, "--app-assembly", StringComparison.OrdinalIgnoreCase))
            {
                ThrowUnsupportedStartupSwitch(argument);
            }

            if (string.Equals(argument, "--app-assembly-file", StringComparison.OrdinalIgnoreCase))
            {
                ThrowUnsupportedStartupSwitch(argument);
            }

            if (string.Equals(argument, "--project", StringComparison.OrdinalIgnoreCase))
            {
                ThrowUnsupportedStartupSwitch(argument);
            }

            if (string.Equals(argument, "--dotnet-command", StringComparison.OrdinalIgnoreCase))
            {
                if (index + 1 >= args.Length
                    || IsSwitch(args[index + 1])
                    || string.IsNullOrWhiteSpace(args[index + 1]))
                {
                    throw new ArgumentException("Missing required value for --dotnet-command.");
                }

                if (!string.IsNullOrWhiteSpace(dotNetCommand))
                {
                    throw new ArgumentException(
                        "Sqloom received multiple values for --dotnet-command. Keep only one explicit dotnet command.");
                }

                dotNetCommand = args[++index];
                continue;
            }

            if (string.Equals(argument, "--no-build", StringComparison.OrdinalIgnoreCase))
            {
                noBuild = true;
                continue;
            }

            if (string.Equals(argument, "--debug", StringComparison.OrdinalIgnoreCase))
            {
                debugEnabled = true;
                continue;
            }

            applicationArguments.Add(argument);
        }

        return new HostStartupOptions
        {
            ApplicationArguments = applicationArguments.ToArray(),
            AppTargetPath = appTargetPath,
            DotNetCommand = dotNetCommand ?? "dotnet",
            NoBuild = noBuild,
            ShowHelp = showHelp,
            ShowVersion = showVersion,
            DebugEnabled = debugEnabled,
        };
    }

    private static bool IsSwitch(string value)
    {
        return value.StartsWith("--", StringComparison.Ordinal);
    }

    private static int? GetVerbScopedTargetIndex(
        string[] args,
        string currentDirectory)
    {
        if (args.Length < 2 || !IsVerb(args[0]))
        {
            return null;
        }

        return LooksLikeTargetPath(args[1], currentDirectory)
            ? 1
            : null;
    }

    private static bool IsVerb(string value)
    {
        return value.Equals("help", StringComparison.OrdinalIgnoreCase)
            || value.Equals("observe", StringComparison.OrdinalIgnoreCase)
            || value.Equals("tune", StringComparison.OrdinalIgnoreCase)
            || value.Equals("replay", StringComparison.OrdinalIgnoreCase)
            || value.Equals("correlate", StringComparison.OrdinalIgnoreCase)
            || value.Equals("advise", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeTargetPath(
        string value,
        string currentDirectory)
    {
        if (string.IsNullOrWhiteSpace(value) || IsSwitch(value))
        {
            return false;
        }

        var fullPath = Path.GetFullPath(
            value,
            currentDirectory);
        if (Directory.Exists(fullPath)
            || File.Exists(fullPath))
        {
            return true;
        }

        if (value == "."
            || value == ".."
            || value.Contains(Path.DirectorySeparatorChar)
            || value.Contains(Path.AltDirectorySeparatorChar))
        {
            return true;
        }

        return Path.GetExtension(value).ToLowerInvariant() switch
        {
            ".csproj" => true,
            ".fsproj" => true,
            ".vbproj" => true,
            ".sln" => true,
            ".slnx" => true,
            ".slnf" => true,
            _ => false,
        };
    }

    private static void EnsureSupportedLeadingArgument(
        string argument,
        string currentDirectory)
    {
        if (string.Equals(argument, "--app-assembly", StringComparison.OrdinalIgnoreCase)
            || string.Equals(argument, "--app-assembly-file", StringComparison.OrdinalIgnoreCase))
        {
            ThrowUnsupportedStartupSwitch(argument);
        }

        if (string.Equals(argument, "--project", StringComparison.OrdinalIgnoreCase))
        {
            ThrowUnsupportedStartupSwitch(argument);
        }

        if (LooksLikeTargetPath(argument, currentDirectory))
        {
            throw new ArgumentException(
                $"Sqloom now requires an explicit stage verb before the target path. Use 'tune {argument}', 'replay {argument}', or 'observe {argument}'.");
        }

        if (!IsSwitch(argument) && !IsVerb(argument))
        {
            throw new ArgumentException(
                $"Unknown Sqloom command '{argument}'. Use tune, observe, replay, correlate, advise, --help, or --version.");
        }
    }

    private static void ThrowUnsupportedStartupSwitch(string switchName)
    {
        throw new ArgumentException(
            $"Unsupported switch '{switchName}'. Sqloom now requires an explicit stage verb followed by a project, solution, solution filter, or directory path when runtime app selection is needed.");
    }

    private static string SetPathOnce(
        string? existingValue,
        string rawValue,
        string currentDirectory,
        string sourceName)
    {
        if (!string.IsNullOrWhiteSpace(existingValue))
        {
            throw new ArgumentException(
                $"Sqloom received multiple target path selections. Keep only one and remove the extra value from {sourceName}.");
        }

        return Path.GetFullPath(
            rawValue,
            currentDirectory);
    }
}
