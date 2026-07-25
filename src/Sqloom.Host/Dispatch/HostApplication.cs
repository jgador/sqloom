using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Pipeline.Execution;
using Sqloom.Testing;

namespace Sqloom.Host;

/// <summary>
/// Coordinates the init, observe, tune, replay, correlate, and advise commands.
/// </summary>
internal sealed class HostApplication
{
    private readonly ISqloomApplication? _boundApplication;
    private readonly CommandRegistry _commandRegistry;
    private readonly HostConsoleWriter _consoleWriter;

    public HostApplication(
        HostConsoleWriter consoleWriter,
        CommandRegistry? commandRegistry = null)
    {
        _consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
        _commandRegistry = commandRegistry ?? CreateDefaultRegistry();
    }

    public HostApplication(
        ISqloomApplication application,
        HostConsoleWriter consoleWriter,
        CommandRegistry? commandRegistry = null)
    {
        _boundApplication = application ?? throw new ArgumentNullException(nameof(application));
        _consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
        _commandRegistry = commandRegistry ?? CreateDefaultRegistry();
    }

    public async Task<int> RunAsync(
        HostStartupOptions startupOptions,
        string currentDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(startupOptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentDirectory);

        var commandKind = CommandRegistry.GetCommandKind(startupOptions.ApplicationArguments);

        switch (commandKind)
        {
            case HostCommandKind.Help:
                _consoleWriter.PrintHelp(startupOptions.ApplicationArguments);
                return 0;
            case HostCommandKind.Init:
            case HostCommandKind.Endpoints:
                return await RunTargetIndependentHandlerAsync(
                        commandKind,
                        startupOptions,
                        currentDirectory)
                    .ConfigureAwait(false);
            case HostCommandKind.Correlate:
            case HostCommandKind.Observe:
            case HostCommandKind.Replay:
            case HostCommandKind.Tune:
            case HostCommandKind.Advise:
                return await RunHandlerAsync(
                        commandKind,
                        startupOptions,
                        currentDirectory,
                        cancellationToken)
                    .ConfigureAwait(false);
            default:
                return await HandleNoCommandAsync(
                        startupOptions,
                        currentDirectory,
                        cancellationToken)
                    .ConfigureAwait(false);
        }
    }

    public Task<int> RunAsync(
        string[] args,
        string currentDirectory,
        CancellationToken cancellationToken = default)
    {
        HostStartupOptions startupOptions = new()
        {
            ApplicationArguments = args,
        };

        return RunAsync(startupOptions, currentDirectory, cancellationToken);
    }

    internal static IReadOnlyList<string> GetProjectNames(ISqloomApplication? application)
    {
        List<string> projectNames = [];
        HashSet<string> seenProjectNames = new(StringComparer.OrdinalIgnoreCase);

        AddProjectName(typeof(RunOptions).Assembly.GetName().Name ?? "Sqloom.Pipeline");
        AddProjectName(typeof(ISqloomApplication).Assembly.GetName().Name ?? "Sqloom.Testing");
        AddProjectName(typeof(HostApplication).Assembly.GetName().Name ?? "Sqloom.Host");

        if (application is not null)
        {
            AddProjectName(application.GetType().Assembly.GetName().Name ?? "Sqloom.Application");
        }

        return projectNames;

        void AddProjectName(string projectName)
        {
            if (seenProjectNames.Add(projectName))
            {
                projectNames.Add(projectName);
            }
        }
    }

    private async Task<int> RunHandlerAsync(
        HostCommandKind commandKind,
        HostStartupOptions startupOptions,
        string currentDirectory,
        CancellationToken cancellationToken)
    {
        var application = await ResolveApplicationAsync(
                commandKind,
                startupOptions,
                cancellationToken)
            .ConfigureAwait(false);
        var context = CreateContext(
            startupOptions,
            currentDirectory,
            application);

        return await _commandRegistry
            .GetRequiredHandler(commandKind)
            .ExecuteAsync(context)
            .ConfigureAwait(false);
    }

    private async Task<int> RunTargetIndependentHandlerAsync(
        HostCommandKind commandKind,
        HostStartupOptions startupOptions,
        string currentDirectory)
    {
        var context = CreateContext(
            startupOptions,
            currentDirectory);

        return await _commandRegistry
            .GetRequiredHandler(commandKind)
            .ExecuteAsync(context)
            .ConfigureAwait(false);
    }

    private async Task<int> HandleNoCommandAsync(
        HostStartupOptions startupOptions,
        string currentDirectory,
        CancellationToken cancellationToken)
    {
        if (startupOptions.ApplicationArguments.Length > 0)
        {
            throw new ArgumentException(
                "Sqloom now requires an explicit stage verb. Use tune, observe, replay, correlate, or advise.");
        }

        var application = await ResolveObserveApplicationAsync(startupOptions, cancellationToken)
            .ConfigureAwait(false);
        PrintBanner(application, currentDirectory);
        _consoleWriter.PrintNoCommandHint();
        return 0;
    }

    private async Task<ISqloomApplication?> ResolveApplicationAsync(
        HostCommandKind commandKind,
        HostStartupOptions startupOptions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(startupOptions);

        // Map command kinds to harness requirements: optional for observe, required for replay/tune,
        // and pre-bound-only for artifact-only follow-up commands.
        switch (commandKind)
        {
            case HostCommandKind.Observe:
                return await ResolveObserveApplicationAsync(
                        startupOptions,
                        cancellationToken)
                    .ConfigureAwait(false);
            case HostCommandKind.Tune:
            case HostCommandKind.Replay:
                return await ResolveRequiredApplicationAsync(
                        startupOptions,
                        cancellationToken)
                    .ConfigureAwait(false);
            case HostCommandKind.Correlate:
            case HostCommandKind.Advise:
                return _boundApplication;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(commandKind),
                    commandKind,
                    "Sqloom could not resolve an app harness for the selected command kind.");
        }
    }

    private async Task<ISqloomApplication?> ResolveObserveApplicationAsync(
        HostStartupOptions startupOptions,
        CancellationToken cancellationToken)
    {
        if (_boundApplication is not null)
        {
            return _boundApplication;
        }

        // Observe can collect Query Store evidence without a harness; a supplied target only adds
        // application manifest data for workload profile classification.
        if (!startupOptions.HasTargetSelection)
        {
            return null;
        }

        return await AppResolver
            .ResolveAsync(startupOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<ISqloomApplication> ResolveRequiredApplicationAsync(
        HostStartupOptions startupOptions,
        CancellationToken cancellationToken)
    {
        if (_boundApplication is not null)
        {
            return _boundApplication;
        }

        return await AppResolver
            .ResolveAsync(startupOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    private void PrintBanner(
        ISqloomApplication? application,
        string currentDirectory)
    {
        var manifest = application?.Describe(new SqloomApplicationContext
        {
            CurrentDirectory = currentDirectory,
        });
        _consoleWriter.PrintBanner(
            manifest?.Name,
            GetProjectNames(application));
    }

    private CommandExecutionContext CreateContext(
        HostStartupOptions startupOptions,
        string currentDirectory,
        ISqloomApplication? application = null)
    {
        ArgumentNullException.ThrowIfNull(startupOptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentDirectory);

        return new CommandExecutionContext
        {
            StartupOptions = startupOptions,
            Arguments = startupOptions.ApplicationArguments,
            CurrentDirectory = currentDirectory,
            ConsoleWriter = _consoleWriter,
            DebugWriter = startupOptions.DebugEnabled
                ? new HostDebugWriter(isEnabled: true)
                : HostDebugWriter.Disabled,
            Application = application,
        };
    }

    private static CommandRegistry CreateDefaultRegistry()
    {
        return new CommandRegistry(
            new InitCommand(),
            new ObserveCommand(),
            new EndpointsCommand(),
            new TuneCommand(),
            new ReplayCommand(),
            new CorrelateCommand(),
            new AdviceCommand());
    }
}
