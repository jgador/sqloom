using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Pipeline.Artifacts;
using Sqloom.Pipeline.Execution;
using Sqloom.Pipeline.OpenAI.Advice;
using Sqloom.Pipeline.QueryStore;

namespace Sqloom.Host;

/// <summary>
/// Generates Sqloom tuning advice by calling the OpenAI Responses API.
/// </summary>
internal sealed class OpenAIAdviceGenerator : IDisposable
{
    private const string OpenAIAdviceStrategyName = "openai-responses-structured-outputs";
    private readonly HostDebugWriter _debugWriter;
    private readonly HttpClient _httpClient;
    private readonly OpenAIAdviceOptions _options;
    private readonly bool _ownsHttpClient;

    internal OpenAIAdviceGenerator(
        OpenAIAdviceOptions options,
        HttpClient? httpClient = null,
        HostDebugWriter? debugWriter = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _httpClient = httpClient ?? CreateHttpClient(_options);
        _ownsHttpClient = httpClient is null;
        _debugWriter = debugWriter ?? HostDebugWriter.Disabled;
        _httpClient.BaseAddress ??= BuildOpenAIBaseAddress(_options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Authorization ??=
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task<AdviceReport> CreateReportAsync(
        QueryCorrelationReport correlationReport,
        string queryStoreCorrelationPath,
        string adviceOutputPath,
        string sqlServerSchemaPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(correlationReport);
        ArgumentException.ThrowIfNullOrWhiteSpace(queryStoreCorrelationPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(adviceOutputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(sqlServerSchemaPath);

        var appName = correlationReport.AppName ?? "unknown";
        var sqlProposalJsonPath = ArtifactLayout.GetSqlProposalPath(
            correlationReport.ReplayArtifactDir);
        var sqlProposalScriptPath = ArtifactLayout.GetSqlProposalScriptPath(
            correlationReport.ReplayArtifactDir);
        var schemaSql = await File
            .ReadAllTextAsync(
                sqlServerSchemaPath,
                cancellationToken)
            .ConfigureAwait(false);
        var evidencePackBuilder = await OpenAIAdviceEvidencePackBuilder
            .CreateAsync(
                queryStoreCorrelationPath,
                sqlServerSchemaPath,
                schemaSql,
                correlationReport.QueryStoreSnapshotPath,
                cancellationToken)
            .ConfigureAwait(false);
        OpenAIAdviceClient client = new(
            _httpClient,
            _options.Model,
            _debugWriter);
        HashSet<string> warnings = new(StringComparer.Ordinal);
        foreach (var warning in correlationReport.Warnings)
        {
            warnings.Add(warning);
        }

        List<AdviceOperationReport> operations = new(correlationReport.Summary.Operations.Count);
        foreach (var operation in correlationReport.Summary.Operations)
        {

            var records = correlationReport.Records
                .Where(record => string.Equals(
                    record.OperationKey,
                    operation.OperationKey,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var evidencePack = await evidencePackBuilder
                .BuildAsync(
                    correlationReport,
                    operation,
                    records,
                    cancellationToken)
                .ConfigureAwait(false);
            foreach (var warning in evidencePack.Warnings)
            {
                warnings.Add(warning);
            }

            var request = CreateAdviceRequest(
                appName,
                operation,
                evidencePack.ArtifactManifestJson,
                evidencePack.SourceEvidenceJson,
                evidencePack.SchemaSql);
            var response = await client
                .CreateAdviceAsync(request, cancellationToken)
                .ConfigureAwait(false);
            foreach (var warning in response.Warnings)
            {
                warnings.Add($"Operation '{operation.OperationKey}': {warning}");
            }

            operations.Add(new AdviceOperationReport
            {
                OperationKey = operation.OperationKey,
                HttpMethod = operation.HttpMethod,
                Route = operation.Route,
                ReplayStatus = operation.ReplayStatus,
                CapturedCommandCount = operation.CapturedCommandCount,
                MatchedCommandCount = operation.MatchedCommandCount,
                Recommendations = response.Recommendations,
                Proposals = response.Proposals,
            });
        }

        return new AdviceReport
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            AppName = appName,
            ReplayArtifactDir = correlationReport.ReplayArtifactDir,
            QueryStoreCorrelationPath = queryStoreCorrelationPath,
            ModelProvider = "openai",
            ModelName = _options.Model,
            StrategyName = OpenAIAdviceStrategyName,
            SqlProposalJsonPath = sqlProposalJsonPath,
            SqlProposalScriptPath = sqlProposalScriptPath,
            Pipeline = CreatePipeline(
                correlationReport,
                queryStoreCorrelationPath,
                adviceOutputPath),
            Summary = new AdviceSummary
            {
                OperationCount = operations.Count,
                RecommendationCount = operations.Sum(static operation => operation.Recommendations.Count),
                ProposalCount = operations.Sum(static operation => operation.Proposals.Count),
            },
            Operations = operations,
            Warnings = warnings.ToArray(),
        };
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private static OpenAITuningAdviceRequest CreateAdviceRequest(
        string appName,
        OperationCorrelationSummary operation,
        string artifactManifestJson,
        string sourceEvidenceJson,
        string schemaSql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appName);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactManifestJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceEvidenceJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaSql);

        return new OpenAITuningAdviceRequest
        {
            AppName = appName,
            OperationKey = operation.OperationKey,
            HttpMethod = operation.HttpMethod,
            Route = operation.Route,
            ArtifactManifestJson = artifactManifestJson,
            SourceEvidenceJson = sourceEvidenceJson,
            SchemaSql = schemaSql,
        };
    }

    private static HttpClient CreateHttpClient(OpenAIAdviceOptions options)
    {
        HttpClient httpClient = new()
        {
            BaseAddress = BuildOpenAIBaseAddress(options.BaseUrl),
        };
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", options.ApiKey);
        return httpClient;
    }

    private static Uri BuildOpenAIBaseAddress(string baseUrl)
    {
        return new Uri($"{baseUrl.TrimEnd('/')}/", UriKind.Absolute);
    }

    private static PipelineReport CreatePipeline(
        QueryCorrelationReport correlationReport,
        string queryStoreCorrelationPath,
        string adviceOutputPath)
    {
        var replaySummaryPath = ArtifactLayout.GetReplaySummaryPath(
            correlationReport.ReplayArtifactDir);

        return new PipelineReport
        {
            Stages =
            [
                new PipelineStageReport
                {
                    Name = PipelineStageNames.Observe,
                    Status = PipelineStageStatuses.Completed,
                    Summary = "Captured a Query Store snapshot for this advice run.",
                    ArtifactPath = correlationReport.QueryStoreSnapshotPath,
                },
                new PipelineStageReport
                {
                    Name = PipelineStageNames.Replay,
                    Status = PipelineStageStatuses.Completed,
                    Summary = "Replay artifacts were available for this advice run.",
                    ArtifactPath = replaySummaryPath,
                },
                new PipelineStageReport
                {
                    Name = PipelineStageNames.Capture,
                    Status = PipelineStageStatuses.Completed,
                    Summary = "Captured replay SQL fed the advice run.",
                    ArtifactPath = correlationReport.ReplayArtifactDir,
                },
                new PipelineStageReport
                {
                    Name = PipelineStageNames.Correlate,
                    Status = PipelineStageStatuses.Completed,
                    Summary = "Query Store correlation completed before advice generation.",
                    ArtifactPath = queryStoreCorrelationPath,
                },
                new PipelineStageReport
                {
                    Name = PipelineStageNames.Advise,
                    Status = PipelineStageStatuses.Completed,
                    Summary = "Operation-level tuning guidance was emitted from replay evidence plus the resolved SQL Server schema.",
                    ArtifactPath = adviceOutputPath,
                },
            ],
        };
    }
}
