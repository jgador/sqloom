using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqloom.AspNetCore.Endpoints;
using Sqloom.AzureSql.Capture;
using Sqloom.Core.Contracts;
using Sqloom.Core.Execution;
using Sqloom.QueryStore.QueryStore;

namespace Sqloom.Host;

/// <summary>
/// Coordinates the observe, tune, replay, correlate, and advise stages.
/// </summary>
internal sealed class HostApplication
{
    private readonly IAppIntegration? _boundAppIntegration;
    private readonly AppResolver _appResolver;
    private readonly CommandRegistry _commandRegistry;
    private readonly HostConsoleWriter _consoleWriter;

    public HostApplication(
        AppResolver appResolver,
        HostConsoleWriter consoleWriter)
        : this(
            appResolver,
            consoleWriter,
            null,
            CreateDefaultRegistry())
    {
    }

    internal HostApplication(
        AppResolver appResolver,
        HostConsoleWriter consoleWriter,
        CommandRegistry commandRegistry)
        : this(
            appResolver,
            consoleWriter,
            null,
            commandRegistry)
    {
    }

    public HostApplication(
        IAppIntegration appIntegration,
        HostConsoleWriter consoleWriter)
        : this(
            new AppResolver(),
            consoleWriter,
            appIntegration,
            CreateDefaultRegistry())
    {
    }

    internal HostApplication(
        IAppIntegration appIntegration,
        HostConsoleWriter consoleWriter,
        CommandRegistry commandRegistry)
        : this(
            new AppResolver(),
            consoleWriter,
            appIntegration,
            commandRegistry)
    {
    }

    private HostApplication(
        AppResolver appResolver,
        HostConsoleWriter consoleWriter,
        IAppIntegration? boundAppIntegration,
        CommandRegistry commandRegistry)
    {
        _appResolver = appResolver;
        _consoleWriter = consoleWriter;
        _boundAppIntegration = boundAppIntegration;
        _commandRegistry = commandRegistry;
    }

    public async Task<int> RunAsync(
        HostStartupOptions startupOptions,
        string currentDirectory)
    {
        var args = startupOptions.ApplicationArguments;
        var commandKind = CommandRegistry.GetCommandKind(args);

        switch (commandKind)
        {
            case HostCommandKind.Help:
                _consoleWriter.PrintUsage();
                return 0;
            case HostCommandKind.Observe:
                return await RunHandlerAsync(
                        commandKind,
                        startupOptions,
                        args,
                        currentDirectory,
                        ResolveObserveIntegration(startupOptions))
                    .ConfigureAwait(false);
            case HostCommandKind.Tune:
                return await RunHandlerAsync(
                        commandKind,
                        startupOptions,
                        args,
                        currentDirectory,
                        ResolveTuneIntegration(startupOptions))
                    .ConfigureAwait(false);
            case HostCommandKind.Replay:
                return await RunHandlerAsync(
                        commandKind,
                        startupOptions,
                        args,
                        currentDirectory,
                        null,
                        ResolveReplayIntegrations(startupOptions))
                    .ConfigureAwait(false);
            case HostCommandKind.Correlate:
            case HostCommandKind.Advise:
                return await RunHandlerAsync(
                        commandKind,
                        startupOptions,
                        args,
                        currentDirectory,
                        _boundAppIntegration)
                    .ConfigureAwait(false);
            default:
                if (args.Length > 0)
                {
                    throw new ArgumentException(
                        "Sqloom now requires an explicit stage verb. Use tune, observe, replay, correlate, or advise.");
                }

                PrintBanner(ResolveObserveIntegration(startupOptions));
                _consoleWriter.PrintNoCommandHint();

                return 0;
        }
    }

    public async Task<int> RunAsync(
        string[] args,
        string currentDirectory)
    {
        HostStartupOptions startupOptions = new()
        {
            ApplicationArguments = args,
        };

        return await RunAsync(startupOptions, currentDirectory).ConfigureAwait(false);
    }

    internal static IReadOnlyList<string> GetProjectNames(IAppIntegration? appIntegration)
    {
        List<string> projectNames =
        [
            typeof(RunOptions).Assembly.GetName().Name ?? "Sqloom.Core",
            typeof(QueryStoreSnapshot).Assembly.GetName().Name ?? "Sqloom.QueryStore",
            typeof(AzureSqlObservationOptions).Assembly.GetName().Name ?? "Sqloom.AzureSql",
            typeof(EndpointReplayRequest).Assembly.GetName().Name ?? "Sqloom.AspNetCore",
            typeof(HostApplication).Assembly.GetName().Name ?? "Sqloom.Host",
        ];

        if (appIntegration is not null)
        {
            projectNames.Add(appIntegration.GetType().Assembly.GetName().Name ?? appIntegration.AppName);
        }

        return projectNames;
    }

    private async Task<int> RunHandlerAsync(
        HostCommandKind commandKind,
        HostStartupOptions startupOptions,
        string[] args,
        string currentDirectory,
        IAppIntegration? appIntegration = null,
        IReadOnlyList<IAppIntegration>? appIntegrations = null)
    {
        // The startup parser stays separate from command parsing so each stage
        // can keep its own argument rules and execution boundary.
        CommandExecutionContext context = new()
        {
            StartupOptions = startupOptions,
            Arguments = args,
            CurrentDirectory = currentDirectory,
            ConsoleWriter = _consoleWriter,
            DebugWriter = startupOptions.DebugEnabled
                ? new HostDebugWriter(isEnabled: true)
                : HostDebugWriter.Disabled,
            AppIntegration = appIntegration,
            AppIntegrations = appIntegrations ?? Array.Empty<IAppIntegration>(),
        };

        return await _commandRegistry
            .GetRequiredHandler(commandKind)
            .ExecuteAsync(context)
            .ConfigureAwait(false);
    }

    private void PrintBanner(IAppIntegration? appIntegration)
    {
        _consoleWriter.PrintBanner(
            appIntegration?.AppName,
            GetProjectNames(appIntegration));
    }

    private IAppIntegration? ResolveObserveIntegration(HostStartupOptions startupOptions)
    {
        if (_boundAppIntegration is not null)
        {
            return _boundAppIntegration;
        }

        if (!startupOptions.HasTargetSelection)
        {
            return null;
        }

        return _appResolver.Resolve(startupOptions);
    }

    private IReadOnlyList<IAppIntegration> ResolveReplayIntegrations(HostStartupOptions startupOptions)
    {
        if (_boundAppIntegration is not null)
        {
            return [_boundAppIntegration];
        }

        return _appResolver.ResolveReplayIntegrations(startupOptions);
    }

    private IAppIntegration ResolveTuneIntegration(HostStartupOptions startupOptions)
    {
        if (_boundAppIntegration is not null)
        {
            return _boundAppIntegration;
        }

        return _appResolver.Resolve(startupOptions);
    }

    private static CommandRegistry CreateDefaultRegistry()
    {
        return new CommandRegistry(
            new ObserveCommand(),
            new TuneCommand(),
            new ReplayCommand(),
            new CorrelateCommand(),
            new AdviceCommand());
    }
}
