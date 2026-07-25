using System;
using System.Threading.Tasks;

namespace Sqloom.Host;

/// <summary>
/// Runs the target-independent Sqloom init command.
/// </summary>
internal sealed class InitCommand
    : ICommandHandler
{
    public HostCommandKind CommandKind => HostCommandKind.Init;

    public Task<int> ExecuteAsync(CommandExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var result = InitCommandExecutor.Execute(
            context.Arguments,
            context.CurrentDirectory);
        context.ConsoleWriter.PrintInitResult(result);
        return Task.FromResult(0);
    }
}
