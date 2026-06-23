using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Core.Artifacts;
using Sqloom.Core.Execution;
using Sqloom.Correlation.QueryStore;

namespace Sqloom.Host;

/// <summary>
/// Runs the Sqloom advise stage against a correlation artifact.
/// </summary>
internal sealed class AdviceCommand
    : ICommandHandler
{
    private readonly AdviseArgumentParser _argumentParser = new();
    private readonly Func<OpenAIAdviceOptions, IAdviceReportGenerator>? _generatorFactory;

    public AdviceCommand()
    {
    }

    internal AdviceCommand(Func<OpenAIAdviceOptions, IAdviceReportGenerator> generatorFactory)
    {
        _generatorFactory = generatorFactory ?? throw new ArgumentNullException(nameof(generatorFactory));
    }

    public HostCommandKind CommandKind => HostCommandKind.Advise;

    public async Task<int> ExecuteAsync(CommandExecutionContext context)
    {
        context.ConsoleWriter.PrintBanner(
            null,
            HostApplication.GetProjectNames(context.Application));

        var arguments = _argumentParser.Parse(context.Arguments);
        arguments.DebugWriter = context.DebugWriter;
        var result = await ExecuteAsync(arguments).ConfigureAwait(false);
        context.ConsoleWriter.PrintAdviceSummary(
            result.Report,
            result.JsonOutputPath);
        return 0;
    }

    public async Task<AdviceCommandResult> ExecuteAsync(
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

    internal async Task<AdviceCommandResult> ExecuteAsync(
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

        using IAdviceReportGenerator adviceGenerator = CreateAdviceReportGenerator(arguments);
        var report = await adviceGenerator
            .CreateReportAsync(
                correlationReport,
                arguments.QueryStoreCorrelationPath,
                arguments.JsonOutputPath,
                arguments.SchemaPath,
                cancellationToken)
            .ConfigureAwait(false);

        await WriteArtifactsAsync(
                arguments,
                report,
                cancellationToken)
            .ConfigureAwait(false);

        return new AdviceCommandResult
        {
            Report = report,
            JsonOutputPath = arguments.JsonOutputPath,
        };
    }

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "Ownership of the created advice generator is transferred to the caller.")]
    private IAdviceReportGenerator CreateAdviceReportGenerator(AdviseArguments arguments)
    {
        return _generatorFactory is null
            ? new OpenAIAdviceGenerator(arguments.OpenAIOptions!, arguments.DebugWriter)
            : _generatorFactory(arguments.OpenAIOptions!);
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

internal interface IAdviceReportGenerator : IDisposable
{
    Task<AdviceReport> CreateReportAsync(
        QueryCorrelationReport correlationReport,
        string queryStoreCorrelationPath,
        string adviceOutputPath,
        string sqlServerSchemaPath,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Carries the result of the Sqloom advice command.
/// </summary>
internal sealed class AdviceCommandResult
{
    public required AdviceReport Report { get; init; }

    public required string JsonOutputPath { get; init; }
}
