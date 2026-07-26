using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Host.Replay;
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
    private readonly ISqlServerDacpacExporter _dacpacExporter;

    public TuneCommand(
        ISqlServerDacpacExporter? dacpacExporter = null)
    {
        _dacpacExporter = dacpacExporter ?? new SqlServerDacpacExporter();
    }

    public HostCommandKind CommandKind => HostCommandKind.Tune;

    public async Task<int> ExecuteAsync(CommandExecutionContext context)
    {
        var application = context.Application
            ?? throw new InvalidOperationException(
                "Sqloom tune requires one resolved app harness.");
        var requestedLaunchOptions = TuneArgumentParser.CreateReplayLaunchOptions(
            context.Arguments,
            context.CurrentDirectory);
        var requestedApplicationContext = new SqloomApplicationContext
        {
            CurrentDirectory = context.CurrentDirectory,
            ReplayLaunchOptions = requestedLaunchOptions,
        };
        var manifest = application.Describe(requestedApplicationContext);

        HostConsoleWriter.PrintBanner(
            manifest.Name,
            HostApplication.GetProjectNames(application));
        TuneArgumentParser.ValidateBeforeSession(
            context.Arguments,
            manifest,
            context.CurrentDirectory);

        var sourceProjectPath = EndpointSourceProjectResolver.Resolve(
            context.Arguments,
            context.StartupOptions,
            context.CurrentDirectory);
        var workflowArtifactDir = TuneArgumentParser.GetWorkflowArtifactDir(
            context.Arguments,
            context.CurrentDirectory);
        var replayArtifactDirectory = ArtifactLayout.GetTuneReplayArtifactDir(workflowArtifactDir);
        var commandLineReadOnlyConnectionString = TuneArgumentParser
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

        var arguments = TuneArgumentParser.Parse(
            context.Arguments,
            manifest,
            session.ReplayHost,
            readOnlyConnectionString,
            context.CurrentDirectory,
            sourceProjectPath,
            workflowArtifactDir,
            launchOptions,
            launchOptions.DacpacPath);
        arguments.DebugEnabled = context.DebugEnabled;
        arguments.ObserveArguments.DebugEnabled = context.DebugEnabled;
        arguments.ReplayArguments.DebugEnabled = context.DebugEnabled;
        arguments.CorrelateArguments.DebugEnabled = context.DebugEnabled;
        arguments.AdviseArguments.DebugEnabled = context.DebugEnabled;
        var result = await TuneWorkflowRunner
            .RunAsync(arguments)
            .ConfigureAwait(false);
        HostConsoleWriter.PrintTuneSummary(
            result.Report,
            result.SummaryOutputPath);
        return result.ExitCode;
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
