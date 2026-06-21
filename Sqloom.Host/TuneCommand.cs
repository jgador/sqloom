using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sqloom.Host;

/// <summary>
/// Runs the Sqloom tune workflow against one resolved app integration.
/// </summary>
internal sealed class TuneCommand
    : ICommandHandler
{
    private readonly TuneArgumentParser _argumentParser = new();
    private readonly TuneWorkflowRunner _workflowRunner = new();

    public HostCommandKind CommandKind => HostCommandKind.Tune;

    public async Task<int> ExecuteAsync(CommandExecutionContext context)
    {
        var appIntegration = context.AppIntegration
            ?? throw new InvalidOperationException(
                "Sqloom tune requires one resolved app integration.");

        context.ConsoleWriter.PrintBanner(
            appIntegration.AppName,
            HostApplication.GetProjectNames(appIntegration));

        var readOnlyConnectionString = _argumentParser.GetQueryStoreConnectionString(context.Arguments);
        if (string.IsNullOrWhiteSpace(readOnlyConnectionString))
        {
            Console.Error.WriteLine(
                "Sqloom tune requires --read-only-connection-string.");
            return 1;
        }

        var arguments = _argumentParser.Parse(
            context.Arguments,
            appIntegration,
            readOnlyConnectionString,
            context.CurrentDirectory);
        arguments.DebugWriter = context.DebugWriter;
        arguments.ObserveArguments.DebugWriter = context.DebugWriter;
        arguments.ReplayArguments.DebugWriter = context.DebugWriter;
        arguments.CorrelateArguments.DebugWriter = context.DebugWriter;
        arguments.AdviseArguments.DebugWriter = context.DebugWriter;
        var result = await ExecuteAsync(arguments).ConfigureAwait(false);
        context.ConsoleWriter.PrintTuneSummary(
            result.Report,
            result.SummaryOutputPath);
        return result.ExitCode;
    }

    internal Task<TuneWorkflowResult> ExecuteAsync(
        TuneArguments arguments,
        CancellationToken cancellationToken = default)
    {
        return _workflowRunner.RunAsync(arguments, cancellationToken);
    }
}
