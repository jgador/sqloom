using Sqloom.Testing;

namespace Sqloom.Host;

/// <summary>
/// Carries the resolved runtime context for a Sqloom command handler.
/// </summary>
internal sealed class CommandExecutionContext
{
    public required HostStartupOptions StartupOptions { get; init; }

    public string[] Arguments => StartupOptions.ApplicationArguments;

    public required string CurrentDirectory { get; init; }

    public bool DebugEnabled { get; init; }

    public ISqloomApplication? Application { get; init; }
}
