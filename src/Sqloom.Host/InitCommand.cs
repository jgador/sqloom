using System;
using System.Threading.Tasks;

namespace Sqloom.Host;

/// <summary>
/// Runs the target-independent Sqloom init command.
/// </summary>
internal sealed class InitCommand
    : ICommandHandler
{
    private readonly InitCommandExecutor _executor;

    public InitCommand()
        : this(new InitCommandExecutor())
    {
    }

    internal InitCommand(InitCommandExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    public HostCommandKind CommandKind => HostCommandKind.Init;

    public Task<int> ExecuteAsync(CommandExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var result = _executor.Execute(
            context.Arguments,
            context.CurrentDirectory);
        context.ConsoleWriter.PrintInitResult(result);
        return Task.FromResult(0);
    }
}
