using System;
using System.Collections.Generic;
using Sqloom.Core.Contracts;

namespace Sqloom.Host;

/// <summary>
/// Builds the command execution context passed into Sqloom host handlers.
/// </summary>
internal sealed class HostCommandExecutionContextFactory
{
    private readonly HostConsoleWriter _consoleWriter;

    public HostCommandExecutionContextFactory(HostConsoleWriter consoleWriter)
    {
        _consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
    }

    public CommandExecutionContext Create(
        HostStartupOptions startupOptions,
        string currentDirectory,
        IAppIntegration? appIntegration = null,
        IReadOnlyList<IAppIntegration>? appIntegrations = null)
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
            AppIntegration = appIntegration,
            AppIntegrations = appIntegrations ?? Array.Empty<IAppIntegration>(),
        };
    }
}
