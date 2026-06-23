using System;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Testing;

namespace Sqloom.Host;

/// <summary>
/// Runs the Sqloom tune workflow against one resolved app harness.
/// </summary>
internal sealed class TuneCommand
    : ICommandHandler
{
    private readonly TuneArgumentParser _argumentParser = new();
    private readonly TuneWorkflowRunner _workflowRunner = new();

    public HostCommandKind CommandKind => HostCommandKind.Tune;

    public async Task<int> ExecuteAsync(CommandExecutionContext context)
    {
        var application = context.Application
            ?? throw new InvalidOperationException(
                "Sqloom tune requires one resolved app harness.");
        var launchOptions = _argumentParser.CreateReplayLaunchOptions(
            context.Arguments,
            context.CurrentDirectory);
        var applicationContext = new SqloomApplicationContext
        {
            CurrentDirectory = context.CurrentDirectory,
            ReplayLaunchOptions = launchOptions,
        };
        var manifest = application.Describe(applicationContext);

        context.ConsoleWriter.PrintBanner(
            manifest.Name,
            HostApplication.GetProjectNames(application));
        _argumentParser.ValidateBeforeSession(
            context.Arguments,
            manifest,
            context.CurrentDirectory);

        var openApiDocumentPath = _argumentParser.GetOpenApiDocumentPath(
            context.Arguments,
            manifest,
            context.CurrentDirectory);

        await using var session = await application
            .StartAsync(applicationContext)
            .ConfigureAwait(false);
        var readOnlyConnectionString = _argumentParser.GetQueryStoreConnectionString(context.Arguments)
            ?? session.ReadOnlyConnectionString;
        if (string.IsNullOrWhiteSpace(readOnlyConnectionString))
        {
            Console.Error.WriteLine(
                "Sqloom tune requires --read-only-connection-string or a read-only connection string from the harness session.");
            return 1;
        }

        var arguments = _argumentParser.Parse(
            context.Arguments,
            manifest,
            session.ReplayHost,
            readOnlyConnectionString,
            context.CurrentDirectory,
            openApiDocumentPath);
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
