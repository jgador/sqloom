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
    /// <summary>
    /// Runs Sqloom with the process working directory and resolves the application harness from the command line.
    /// </summary>
    public static Task<int> RunAsync(string[] args)
    {
        return RunAsync(args, Environment.CurrentDirectory);
    }

    /// <summary>
    /// Runs Sqloom from an explicit working directory and resolves the application harness from the command line.
    /// </summary>
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

    /// <summary>
    /// Runs Sqloom against an application harness supplied by the caller.
    /// </summary>
    public static Task<int> RunAsync(
        ISqloomApplication application,
        string[] args)
    {
        return RunAsync(
            application,
            args,
            Environment.CurrentDirectory);
    }

    /// <summary>
    /// Runs Sqloom against a caller-supplied application harness from an explicit working directory.
    /// </summary>
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
            return informationalVersion;
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
        // Handle global actions before dispatch and reject target paths when this host is already app-bound.
        if (startupOptions.ShowVersion)
        {
            consoleWriter.PrintVersion(GetDisplayVersion());
            exitCode = 0;
            return true;
        }

        if (startupOptions.ShowHelp)
        {
            consoleWriter.PrintHelp(startupOptions.ApplicationArguments);
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
