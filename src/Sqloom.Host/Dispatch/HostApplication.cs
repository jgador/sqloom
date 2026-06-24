using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Core.Execution;
using Sqloom.Testing;

namespace Sqloom.Host;

/// <summary>
/// Coordinates the observe, tune, replay, correlate, and advise stages.
/// </summary>
internal sealed class HostApplication
{
    private readonly CommandRegistry _commandRegistry;
    private readonly HostConsoleWriter _consoleWriter;
    private readonly HostCommandExecutionContextFactory _contextFactory;
    private readonly HostCommandIntegrationResolver _integrationResolver;

    public HostApplication(
        AppResolver appResolver,
        HostConsoleWriter consoleWriter)
        : this(
            new HostCommandIntegrationResolver(appResolver),
            new HostCommandExecutionContextFactory(consoleWriter),
            consoleWriter,
            CreateDefaultRegistry())
    {
    }

    internal HostApplication(
        AppResolver appResolver,
        HostConsoleWriter consoleWriter,
        CommandRegistry commandRegistry)
        : this(
            new HostCommandIntegrationResolver(appResolver),
            new HostCommandExecutionContextFactory(consoleWriter),
            consoleWriter,
            commandRegistry)
    {
    }

    public HostApplication(
        ISqloomApplication application,
        HostConsoleWriter consoleWriter)
        : this(
            new HostCommandIntegrationResolver(application),
            new HostCommandExecutionContextFactory(consoleWriter),
            consoleWriter,
            CreateDefaultRegistry())
    {
    }

    internal HostApplication(
        ISqloomApplication application,
        HostConsoleWriter consoleWriter,
        CommandRegistry commandRegistry)
        : this(
            new HostCommandIntegrationResolver(application),
            new HostCommandExecutionContextFactory(consoleWriter),
            consoleWriter,
            commandRegistry)
    {
    }

    internal HostApplication(
        HostCommandIntegrationResolver integrationResolver,
        HostCommandExecutionContextFactory contextFactory,
        HostConsoleWriter consoleWriter,
        CommandRegistry commandRegistry)
    {
        _integrationResolver = integrationResolver ?? throw new ArgumentNullException(nameof(integrationResolver));
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _consoleWriter = consoleWriter;
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
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
                _consoleWriter.PrintUsage();
                return 0;
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

    public async Task<int> RunAsync(
        string[] args,
        string currentDirectory,
        CancellationToken cancellationToken = default)
    {
        HostStartupOptions startupOptions = new()
        {
            ApplicationArguments = args,
        };

        return await RunAsync(startupOptions, currentDirectory, cancellationToken).ConfigureAwait(false);
    }

    internal static IReadOnlyList<string> GetProjectNames(ISqloomApplication? application)
    {
        List<string> projectNames =
        [
            typeof(RunOptions).Assembly.GetName().Name ?? "Sqloom.Core",
            typeof(ISqloomApplication).Assembly.GetName().Name ?? "Sqloom.Testing",
            typeof(HostApplication).Assembly.GetName().Name ?? "Sqloom.Host",
        ];

        if (application is not null)
        {
            projectNames.Add(application.GetType().Assembly.GetName().Name ?? "Sqloom.Application");
        }

        return projectNames;
    }

    private async Task<int> RunHandlerAsync(
        HostCommandKind commandKind,
        HostStartupOptions startupOptions,
        string currentDirectory,
        CancellationToken cancellationToken)
    {
        var bindings = await _integrationResolver
            .ResolveAsync(commandKind, startupOptions, cancellationToken)
            .ConfigureAwait(false);
        var context = _contextFactory.Create(
            startupOptions,
            currentDirectory,
            bindings.Application);

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

        var application = await _integrationResolver
            .ResolveBannerApplicationAsync(startupOptions, cancellationToken)
            .ConfigureAwait(false);
        PrintBanner(application, currentDirectory);
        _consoleWriter.PrintNoCommandHint();
        return 0;
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
