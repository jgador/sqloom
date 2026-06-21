using System.Threading.Tasks;

namespace Sqloom.Host;

/// <summary>
/// Defines the execution contract for a Sqloom stage handler.
/// </summary>
internal interface ICommandHandler
{
    HostCommandKind CommandKind { get; }

    Task<int> ExecuteAsync(CommandExecutionContext context);
}
