using System;
using System.Reflection;
using System.Threading.Tasks;
using Sqloom.Core.Contracts;

namespace Sqloom.Host;

/// <summary>
/// Runs the Sqloom host with injected console and process abstractions.
/// </summary>
public static class HostRuntime
{
    public static Task<int> RunAsync(string[] args)
    {
        return RunAsync(args, Environment.CurrentDirectory);
    }

    public static async Task<int> RunAsync(
        string[] args,
        string currentDirectory)
    {
        HostConsoleWriter consoleWriter = new();
        HostStartupCommandLine startupCommandLine = new();

        try
        {
            var startupOptions = startupCommandLine.Parse(args, currentDirectory);
            if (startupOptions.ShowVersion)
            {
                consoleWriter.PrintVersion(GetDisplayVersion());
                return 0;
            }

            if (startupOptions.ShowHelp)
            {
                consoleWriter.PrintUsage();
                return 0;
            }

            HostApplication application = new(
                new AppResolver(),
                consoleWriter);
            return await application
                .RunAsync(
                    startupOptions,
                    currentDirectory)
                .ConfigureAwait(false);
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            consoleWriter.PrintUsage();
            return 1;
        }
        catch (AppResolutionException exception)
        {
            Console.Error.WriteLine(exception.Message);
            consoleWriter.PrintUsage();
            return 1;
        }
    }

    public static Task<int> RunAsync(
        IAppIntegration appIntegration,
        string[] args)
    {
        return RunAsync(
            appIntegration,
            args,
            Environment.CurrentDirectory);
    }

    public static async Task<int> RunAsync(
        IAppIntegration appIntegration,
        string[] args,
        string currentDirectory)
    {
        ArgumentNullException.ThrowIfNull(appIntegration);

        HostConsoleWriter consoleWriter = new();
        HostStartupCommandLine startupCommandLine = new();

        try
        {
            var startupOptions = startupCommandLine.Parse(args, currentDirectory);
            if (startupOptions.ShowVersion)
            {
                consoleWriter.PrintVersion(GetDisplayVersion());
                return 0;
            }

            if (startupOptions.ShowHelp)
            {
                consoleWriter.PrintUsage();
                return 0;
            }

            if (startupOptions.HasTargetSelection)
            {
                throw new ArgumentException(
                    "This app-owned Sqloom host already provides its integration. Remove the explicit target path selection and use the generic Sqloom.Host executable when you need runtime app selection.");
            }

            HostApplication application = new(
                appIntegration,
                consoleWriter);
            return await application
                .RunAsync(
                    startupOptions,
                    currentDirectory)
                .ConfigureAwait(false);
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            consoleWriter.PrintUsage();
            return 1;
        }
        catch (AppResolutionException exception)
        {
            Console.Error.WriteLine(exception.Message);
            consoleWriter.PrintUsage();
            return 1;
        }
    }

    internal static string GetDisplayVersion()
    {
        var assembly = typeof(HostRuntime).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var buildMetadataIndex = informationalVersion.IndexOf('+', StringComparison.Ordinal);
            return buildMetadataIndex >= 0
                ? informationalVersion[..buildMetadataIndex]
                : informationalVersion;
        }

        return assembly.GetName().Version?.ToString()
            ?? "unknown";
    }
}
