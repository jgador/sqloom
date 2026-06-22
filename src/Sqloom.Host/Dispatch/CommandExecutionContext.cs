using Sqloom.Testing;

namespace Sqloom.Host;

/// <summary>
/// Carries the resolved runtime context for a Sqloom command handler.
/// </summary>
internal sealed class CommandExecutionContext
{
    public required HostStartupOptions StartupOptions { get; init; }

    public required string[] Arguments { get; init; }

    public required string CurrentDirectory { get; init; }

    public required HostConsoleWriter ConsoleWriter { get; init; }

    public required HostDebugWriter DebugWriter { get; init; }

    public ISqloomApplication? Application { get; init; }
}
