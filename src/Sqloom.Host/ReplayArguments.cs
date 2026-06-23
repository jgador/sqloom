using Sqloom.AspNetCore.Endpoints;

namespace Sqloom.Host;

/// <summary>
/// Carries arguments for the Sqloom replay command.
/// </summary>
internal sealed class ReplayArguments
{
    public required ReplayRunnerOptions RunnerOptions { get; init; }

    public HostDebugWriter DebugWriter { get; set; } = HostDebugWriter.Disabled;
}
