using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Pipeline.Artifacts;
using Sqloom.Pipeline.Execution;
using Sqloom.Testing;

namespace Sqloom.Host;

/// <summary>
/// Runs the Sqloom tune workflow against one resolved app harness.
/// </summary>
internal sealed class TuneCommand
    : ICommandHandler
{
    private readonly TuneArgumentParser _argumentParser;
    private readonly TuneWorkflowRunner _workflowRunner;
    private readonly ISqlServerDacpacExporter _dacpacExporter;

    public TuneCommand()
        : this(
            new TuneArgumentParser(),
            new TuneWorkflowRunner(),
            new SqlServerDacpacExporter())
    {
    }

    internal TuneCommand(
        TuneArgumentParser argumentParser,
        TuneWorkflowRunner workflowRunner,
        ISqlServerDacpacExporter dacpacExporter)
    {
        _argumentParser = argumentParser;
        _workflowRunner = workflowRunner;
        _dacpacExporter = dacpacExporter;
    }

    public HostCommandKind CommandKind => HostCommandKind.Tune;

    public async Task<int> ExecuteAsync(CommandExecutionContext context)
    {
        var application = context.Application
            ?? throw new InvalidOperationException(
                "Sqloom tune requires one resolved app harness.");
        var requestedLaunchOptions = _argumentParser.CreateReplayLaunchOptions(
            context.Arguments,
            context.CurrentDirectory);
        var requestedApplicationContext = new SqloomApplicationContext
        {
            CurrentDirectory = context.CurrentDirectory,
            ReplayLaunchOptions = requestedLaunchOptions,
        };
        var manifest = application.Describe(requestedApplicationContext);

        context.ConsoleWriter.PrintBanner(
            manifest.Name,
            HostApplication.GetProjectNames(application));
        _argumentParser.ValidateBeforeSession(
            context.Arguments,
            manifest,
            context.CurrentDirectory);

        var openApiPath = _argumentParser.GetOpenApiPath(
            context.Arguments,
            manifest,
            context.CurrentDirectory);
        var workflowArtifactDir = _argumentParser.GetWorkflowArtifactDir(
            context.Arguments,
            context.CurrentDirectory);
        var replayArtifactDirectory = ArtifactLayout.GetTuneReplayArtifactDir(workflowArtifactDir);
        var commandLineReadOnlyConnectionString = _argumentParser
            .GetQueryStoreConnectionString(context.Arguments);
        var launchOptions = await ResolveReplayLaunchOptionsAsync(
                requestedLaunchOptions,
                manifest,
                context.CurrentDirectory,
                replayArtifactDirectory,
                commandLineReadOnlyConnectionString)
            .ConfigureAwait(false);
        var applicationContext = new SqloomApplicationContext
        {
            CurrentDirectory = context.CurrentDirectory,
            ApplicationConnectionString = commandLineReadOnlyConnectionString,
            ReplayLaunchOptions = launchOptions,
        };

        await using var session = await application
            .StartAsync(applicationContext)
            .ConfigureAwait(false);
        var readOnlyConnectionString = commandLineReadOnlyConnectionString
            ?? session.ReadOnlyConnection;
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
            openApiPath,
            workflowArtifactDir,
            launchOptions,
            launchOptions.DacpacPath);
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

    internal async Task<ReplayLaunchOptions> ResolveReplayLaunchOptionsAsync(
        ReplayLaunchOptions requestedOptions,
        SqloomApplicationManifest manifest,
        string currentDirectory,
        string replayArtifactDirectory,
        string? commandLineReadOnlyConnectionString,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestedOptions);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        // Replay bootstrap inputs resolve in precedence order: CLI DACPAC, manifest DACPAC, exported DACPAC.
        var dacpacPath = requestedOptions.DacpacPath;
        if (string.IsNullOrWhiteSpace(dacpacPath)
            && !string.IsNullOrWhiteSpace(manifest.SqlServerDacpacPath))
        {
            dacpacPath = Path.GetFullPath(
                manifest.SqlServerDacpacPath,
                currentDirectory);
        }

        if (string.IsNullOrWhiteSpace(dacpacPath)
            && !string.IsNullOrWhiteSpace(commandLineReadOnlyConnectionString))
        {
            dacpacPath = await _dacpacExporter
                .ExportAsync(
                    commandLineReadOnlyConnectionString,
                    replayArtifactDirectory,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(dacpacPath)
            && !string.IsNullOrWhiteSpace(requestedOptions.SeedSqlPath))
        {
            throw new ArgumentException(
                "The post-DACPAC SQL seed script requires --sqlserver-dacpac-file <path>, a harness manifest DACPAC, or --read-only-connection-string <connection-string>.");
        }

        return new ReplayLaunchOptions
        {
            DacpacPath = dacpacPath,
            SeedSqlPath = requestedOptions.SeedSqlPath,
        };
    }
}
