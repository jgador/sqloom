using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Pipeline.Artifacts;
using Sqloom.Pipeline.Execution;
using Sqloom.Pipeline.QueryStore;

namespace Sqloom.Host;

/// <summary>
/// Runs the Sqloom advise stage against a correlation artifact.
/// </summary>
internal sealed class AdviceCommand
    : ICommandHandler
{
    private readonly Func<OpenAIAdviceOptions, AdviceReportGenerator>? _generatorFactory;
    private readonly ISqlServerDacpacSchemaExtractor _schemaExtractor;
    private readonly ISqlServerDacpacExporter _dacpacExporter;

    public AdviceCommand(
        Func<OpenAIAdviceOptions, AdviceReportGenerator>? generatorFactory = null,
        ISqlServerDacpacSchemaExtractor? schemaExtractor = null,
        ISqlServerDacpacExporter? dacpacExporter = null)
    {
        _generatorFactory = generatorFactory;
        _schemaExtractor = schemaExtractor ?? new SqlServerDacpacSchemaExtractor();
        _dacpacExporter = dacpacExporter ?? new SqlServerDacpacExporter();
    }

    public HostCommandKind CommandKind => HostCommandKind.Advise;

    public async Task<int> ExecuteAsync(CommandExecutionContext context)
    {
        context.ConsoleWriter.PrintBanner(
            null,
            HostApplication.GetProjectNames(context.Application));

        var arguments = AdviseArgumentParser.Parse(
            context.Arguments,
            context.CurrentDirectory);
        arguments.DebugWriter = context.DebugWriter;
        var result = await ExecuteAsync(arguments).ConfigureAwait(false);
        context.ConsoleWriter.PrintAdviceSummary(
            result.Report,
            result.JsonOutputPath);
        return 0;
    }

    internal async Task<(AdviceReport Report, string JsonOutputPath)> ExecuteAsync(
        AdviseArguments arguments,
        CancellationToken cancellationToken = default)
    {
        var correlationReport = await JsonFileReader
            .ReadAsync<QueryCorrelationReport>(
                arguments.QueryStoreCorrelationPath,
                static serializerOptions => serializerOptions.Converters.Add(new JsonStringEnumConverter()),
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Could not deserialize Query Store correlation at '{arguments.QueryStoreCorrelationPath}'.");

        return await ExecuteAsync(
                arguments,
                correlationReport,
                cancellationToken)
            .ConfigureAwait(false);
    }

    internal async Task<(AdviceReport Report, string JsonOutputPath)> ExecuteAsync(
        AdviseArguments arguments,
        QueryCorrelationReport correlationReport,
        CancellationToken cancellationToken = default)
    {
        arguments.DebugWriter.PrintAdviceRun(arguments);
        if (arguments.OpenAIOptions is null)
        {
            throw new InvalidOperationException(
                "Sqloom advice with --model-provider openai requires resolved OpenAI options.");
        }

        var schemaPath = await ResolveSchemaPathAsync(
                arguments,
                cancellationToken)
            .ConfigureAwait(false);
        var report = await CreateAdviceReportAsync(
                arguments,
                correlationReport,
                arguments.QueryStoreCorrelationPath,
                arguments.JsonOutputPath,
                schemaPath,
                cancellationToken)
            .ConfigureAwait(false);

        await WriteArtifactsAsync(
                arguments,
                report,
                cancellationToken)
            .ConfigureAwait(false);

        return (report, arguments.JsonOutputPath);
    }

    private async Task<string> ResolveSchemaPathAsync(
        AdviseArguments arguments,
        CancellationToken cancellationToken)
    {
        // Materialize advice schema as one SQL file, whether it starts as SQL, DACPAC, or a live database.
        if (!string.IsNullOrWhiteSpace(arguments.SchemaPath))
        {
            return arguments.SchemaPath;
        }

        if (!string.IsNullOrWhiteSpace(arguments.DacpacPath))
        {
            return await _schemaExtractor
                .ExtractAsync(
                    arguments.DacpacPath,
                    arguments.ReplayArtifactDir,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(arguments.ReadOnlyConnectionString))
        {
            throw new ArgumentException(
                "Sqloom advice needs --sqlserver-schema-file, --sqlserver-dacpac-file, or --read-only-connection-string.");
        }

        var exportedDacpacPath = await _dacpacExporter
            .ExportAsync(
                arguments.ReadOnlyConnectionString,
                arguments.ReplayArtifactDir,
                cancellationToken)
            .ConfigureAwait(false);
        return await _schemaExtractor
            .ExtractAsync(
                exportedDacpacPath,
                arguments.ReplayArtifactDir,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<AdviceReport> CreateAdviceReportAsync(
        AdviseArguments arguments,
        QueryCorrelationReport correlationReport,
        string queryStoreCorrelationPath,
        string adviceOutputPath,
        string sqlServerSchemaPath,
        CancellationToken cancellationToken)
    {
        if (_generatorFactory is not null)
        {
            return await _generatorFactory(arguments.OpenAIOptions!)
                .Invoke(
                    correlationReport,
                    queryStoreCorrelationPath,
                    adviceOutputPath,
                    sqlServerSchemaPath,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        using OpenAIAdviceGenerator adviceGenerator =
            new(arguments.OpenAIOptions!, debugWriter: arguments.DebugWriter);
        return await adviceGenerator
            .CreateReportAsync(
                correlationReport,
                queryStoreCorrelationPath,
                adviceOutputPath,
                sqlServerSchemaPath,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task WriteArtifactsAsync(
        AdviseArguments arguments,
        AdviceReport report,
        CancellationToken cancellationToken)
    {
        await JsonFileWriter.WriteAsync(
                arguments.JsonOutputPath,
                report,
                cancellationToken)
            .ConfigureAwait(false);
        var proposalReport = SqlTuningProposalArtifacts.CreateReport(
            report,
            arguments.JsonOutputPath);
        await JsonFileWriter.WriteAsync(
                report.SqlProposalJsonPath,
                proposalReport,
                cancellationToken)
            .ConfigureAwait(false);
        var proposalScriptDirectory = Path.GetDirectoryName(report.SqlProposalScriptPath);
        if (!string.IsNullOrWhiteSpace(proposalScriptDirectory))
        {
            Directory.CreateDirectory(proposalScriptDirectory);
        }

        var proposalScript = SqlTuningProposalArtifacts.RenderSqlScript(proposalReport);
        await File.WriteAllTextAsync(
                report.SqlProposalScriptPath,
                proposalScript,
                cancellationToken)
            .ConfigureAwait(false);
    }
}

internal delegate Task<AdviceReport> AdviceReportGenerator(
    QueryCorrelationReport correlationReport,
    string queryStoreCorrelationPath,
    string adviceOutputPath,
    string sqlServerSchemaPath,
    CancellationToken cancellationToken = default);
