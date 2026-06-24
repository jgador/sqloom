using System;
using System.Reflection;
using System.Threading.Tasks;
using Sqloom.Testing;

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
        return await RunCoreAsync(
                null,
                args,
                currentDirectory)
            .ConfigureAwait(false);
    }

    public static Task<int> RunAsync(
        ISqloomApplication application,
        string[] args)
    {
        return RunAsync(
            application,
            args,
            Environment.CurrentDirectory);
    }

    public static async Task<int> RunAsync(
        ISqloomApplication application,
        string[] args,
        string currentDirectory)
    {
        ArgumentNullException.ThrowIfNull(application);

        return await RunCoreAsync(
                application,
                args,
                currentDirectory)
            .ConfigureAwait(false);
    }

    private static async Task<int> RunCoreAsync(
        ISqloomApplication? application,
        string[] args,
        string currentDirectory)
    {
        HostConsoleWriter consoleWriter = new();
        HostStartupCommandLine startupCommandLine = new();

        try
        {
            var startupOptions = startupCommandLine.Parse(args, currentDirectory);
            if (TryHandleStartupAction(
                startupOptions,
                application is not null,
                consoleWriter,
                out var exitCode))
            {
                return exitCode;
            }

            var hostApplication = CreateApplication(
                application,
                consoleWriter);
            return await hostApplication
                .RunAsync(
                    startupOptions,
                    currentDirectory)
                .ConfigureAwait(false);
        }
        catch (ArgumentException exception)
        {
            return HandleStartupFailure(exception.Message, consoleWriter);
        }
        catch (AppResolutionException exception)
        {
            return HandleStartupFailure(exception.Message, consoleWriter);
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

    private static HostApplication CreateApplication(
        ISqloomApplication? application,
        HostConsoleWriter consoleWriter)
    {
        return application is null
            ? new HostApplication(
                new AppResolver(),
                consoleWriter)
            : new HostApplication(
                application,
                consoleWriter);
    }

    private static int HandleStartupFailure(
        string message,
        HostConsoleWriter consoleWriter)
    {
        Console.Error.WriteLine(message);
        consoleWriter.PrintUsage();
        return 1;
    }

    private static bool TryHandleStartupAction(
        HostStartupOptions startupOptions,
        bool hasBoundApplication,
        HostConsoleWriter consoleWriter,
        out int exitCode)
    {
        if (startupOptions.ShowVersion)
        {
            consoleWriter.PrintVersion(GetDisplayVersion());
            exitCode = 0;
            return true;
        }

        if (startupOptions.ShowHelp)
        {
            consoleWriter.PrintUsage();
            exitCode = 0;
            return true;
        }

        if (hasBoundApplication && startupOptions.HasTargetSelection)
        {
            throw new ArgumentException(
                "This app-owned Sqloom host already provides its harness. Remove the explicit target path selection and use the generic Sqloom.Host executable when you need runtime app selection.");
        }

        exitCode = 0;
        return false;
    }
}
