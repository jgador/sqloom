using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Core.QueryStore;
using Sqloom.Core.Artifacts;
using Sqloom.Core.Execution;
using Sqloom.OpenAI.Advice;

namespace Sqloom.Host;

/// <summary>
/// Generates Sqloom tuning advice by calling the OpenAI Responses API.
/// </summary>
internal sealed class OpenAIAdviceGenerator : IAdviceReportGenerator
{
    private const string OpenAIAdviceStrategyName = "openai-responses-structured-outputs";
    private readonly HostDebugWriter _debugWriter;
    private readonly HttpClient _httpClient;
    private readonly OpenAIAdviceOptions _options;
    private readonly bool _ownsHttpClient;

    public OpenAIAdviceGenerator(OpenAIAdviceOptions options)
        : this(
            options,
            CreateHttpClient(options),
            ownsHttpClient: true,
            HostDebugWriter.Disabled)
    {
    }

    internal OpenAIAdviceGenerator(
        OpenAIAdviceOptions options,
        HostDebugWriter debugWriter)
        : this(
            options,
            CreateHttpClient(options),
            ownsHttpClient: true,
            debugWriter)
    {
    }

    internal OpenAIAdviceGenerator(
        OpenAIAdviceOptions options,
        HttpClient httpClient)
        : this(
            options,
            httpClient,
            ownsHttpClient: false,
            HostDebugWriter.Disabled)
    {
    }

    internal OpenAIAdviceGenerator(
        OpenAIAdviceOptions options,
        HttpClient httpClient,
        HostDebugWriter debugWriter)
        : this(
            options,
            httpClient,
            ownsHttpClient: false,
            debugWriter)
    {
    }

    private OpenAIAdviceGenerator(
        OpenAIAdviceOptions options,
        HttpClient httpClient,
        bool ownsHttpClient,
        HostDebugWriter debugWriter)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _ownsHttpClient = ownsHttpClient;
        _debugWriter = debugWriter ?? throw new ArgumentNullException(nameof(debugWriter));
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

            var request = OpenAIAdviceRequestBuilder.Build(
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

/// <summary>
/// Carries the OpenAI settings for Sqloom advice generation.
/// </summary>
internal sealed class OpenAIAdviceOptions
{
    public required string ApiKey { get; init; }

    public string BaseUrl { get; init; } = "https://api.openai.com";

    public string Model { get; init; } = "gpt-5.4-mini";
}

/// <summary>
/// Builds OpenAI request payloads from Sqloom correlation evidence.
/// </summary>
internal static class OpenAIAdviceRequestBuilder
{
    public static OpenAITuningAdviceRequest Build(
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
}

/// <summary>
/// Posts advice requests to the configured OpenAI-compatible endpoint.
/// </summary>
internal sealed class OpenAIAdviceClient
{
    private const string ResponsesPath = "v1/responses";

    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly HostDebugWriter _debugWriter;
    private readonly string _model;

    public OpenAIAdviceClient(
        HttpClient httpClient,
        string model,
        HostDebugWriter debugWriter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _model = model;
        _debugWriter = debugWriter ?? throw new ArgumentNullException(nameof(debugWriter));
    }

    public async Task<OpenAITuningAdviceResponse> CreateAdviceAsync(
        OpenAITuningAdviceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestPayload = BuildResponsesRequest(request);
        var requestJson = JsonSerializer.Serialize(
            requestPayload,
            _serializerOptions);
        var requestUri = _httpClient.BaseAddress is { } baseAddress
            ? new Uri(baseAddress, ResponsesPath)
            : new Uri(ResponsesPath, UriKind.Relative);
        _debugWriter.PrintOpenAIRequest(
            requestUri,
            _httpClient.DefaultRequestHeaders.Authorization,
            requestJson);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(
                requestJson,
                Encoding.UTF8,
                "application/json"),
        };

        using var response = await _httpClient
            .SendAsync(httpRequest, cancellationToken)
            .ConfigureAwait(false);
        var responseJson = await response.Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);
        _debugWriter.PrintOpenAIResponse(
            response.StatusCode,
            responseJson);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenAI tuning advice failed with status {(int)response.StatusCode}.");
        }

        var content = ReadResponseOutputText(responseJson);
        var parsedResponse = JsonSerializer.Deserialize<OpenAITuningAdviceResponse>(
                content,
                _serializerOptions)
            ?? throw new InvalidOperationException("OpenAI tuning advice returned invalid JSON.");
        var recommendations = NormalizeRecommendations(parsedResponse.Recommendations);
        var proposalResult = NormalizeProposals(parsedResponse.Proposals);
        if (recommendations.Count == 0)
        {
            throw new InvalidOperationException("OpenAI tuning advice returned no recommendations.");
        }

        return new OpenAITuningAdviceResponse
        {
            Recommendations = recommendations,
            Proposals = proposalResult.Proposals,
            Warnings = proposalResult.Warnings,
            ModelName = _model,
        };
    }

    private object BuildResponsesRequest(OpenAITuningAdviceRequest request)
    {
        return new
        {
            model = _model,
            instructions = BuildSystemPrompt(),
            input = BuildInput(request),
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "sqloom_tuning_advice",
                    strict = true,
                    schema = BuildResponseSchema(),
                },
            },
        };
    }

    private static string BuildSystemPrompt()
    {
        return string.Join(
            "\n",
            "You generate operation-level SQL tuning advice for Sqloom from supplied evidence files and SQL Server schema text.",
            "Return one JSON object only that matches the provided schema.",
            "Treat artifact_manifest_json, source_evidence_json, and sql_server_schema_sql as the only ground truth.",
            "Use only the supplied evidence. Do not invent tables, indexes, joins, filters, plan operators, or metrics that are not supported by the evidence.",
            "Do not preload or assume a baseline fix. Derive every recommendation from the supplied evidence and schema only.",
            "Only reference objects and columns that exist in the supplied SQL Server schema text.",
            "Return 1 to 4 recommendations.",
            "Return 0 to 3 SQL proposals when the evidence supports a concrete database-side change.",
            "If replay status is not 'replayed', or if matched command count is 0, prioritize recovering replay or correlation evidence before suggesting query tuning changes.",
            "Keep each recommendation concise, specific, and actionable.",
            "SQL proposals must be SQL Server compatible and grounded in the supplied evidence and schema.",
            "SQL proposals are review artifacts only. Sqloom will not auto-apply them.",
            "proposalKind is a short free-form classifier owned by the model. Preserve the specific proposal shape in that string instead of forcing a fixed Sqloom enum.",
            "Fingerprint fallback correlation is weaker than exact ownership, but it can still support a concrete proposal when the captured SQL text, matched plan metrics, and schema align on the same change. Do not withhold a supported proposal solely because the correlation used fingerprint fallback.",
            "Include rollbackSqlScript when you can provide a meaningful rollback. If no rollback can be stated confidently from the evidence, set rollbackSqlScript to null.",
            "Every SQL proposal must include the supporting command ordinals and matched plan ids when available.",
            "If the evidence is insufficient for a concrete database-side change, explain the evidence gap in recommendations and emit no SQL proposal.",
            "Do not use markdown.");
    }

    private static string BuildInput(OpenAITuningAdviceRequest request)
    {
        return string.Join(
            "\n\n",
            $"App: {request.AppName}\nOperation: {request.OperationKey}\nHTTP: {request.HttpMethod} {request.Route}",
            $"artifact_manifest_json:\n{request.ArtifactManifestJson}",
            $"source_evidence_json:\n{request.SourceEvidenceJson}",
            $"sql_server_schema_sql:\n{request.SchemaSql}");
    }

    private static object BuildResponseSchema()
    {
        return new
        {
            type = "object",
            additionalProperties = false,
            properties = new Dictionary<string, object>
            {
                ["recommendations"] = new
                {
                    type = "array",
                    minItems = 1,
                    maxItems = 4,
                    items = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new Dictionary<string, object>
                        {
                            ["title"] = new
                            {
                                type = "string",
                                maxLength = 120,
                            },
                            ["rootCause"] = new
                            {
                                type = "string",
                                maxLength = 400,
                            },
                            ["suggestedChange"] = new
                            {
                                type = "string",
                                maxLength = 400,
                            },
                            ["verificationMetric"] = new
                            {
                                type = "string",
                                maxLength = 240,
                            },
                        },
                        required = new[]
                        {
                            "title",
                            "rootCause",
                            "suggestedChange",
                            "verificationMetric",
                        },
                    },
                },
                ["proposals"] = new
                {
                    type = "array",
                    minItems = 0,
                    maxItems = 3,
                    items = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new Dictionary<string, object>
                        {
                            ["title"] = new
                            {
                                type = "string",
                                maxLength = 160,
                            },
                            ["diagnosis"] = new
                            {
                                type = "string",
                                maxLength = 400,
                            },
                            ["proposalKind"] = new
                            {
                                type = "string",
                                maxLength = 80,
                            },
                            ["targetObject"] = new
                            {
                                type = "string",
                                maxLength = 256,
                            },
                            ["sqlScript"] = new
                            {
                                type = "string",
                                maxLength = 4000,
                            },
                            ["rollbackSqlScript"] = new
                            {
                                type = new[] { "string", "null" },
                                maxLength = 3000,
                            },
                            ["expectedBenefit"] = new
                            {
                                type = "string",
                                maxLength = 400,
                            },
                            ["verificationMetric"] = new
                            {
                                type = "string",
                                maxLength = 240,
                            },
                            ["confidence"] = new
                            {
                                type = "number",
                                minimum = 0,
                                maximum = 1,
                            },
                            ["sourceCommandOrdinals"] = new
                            {
                                type = "array",
                                maxItems = 16,
                                items = new
                                {
                                    type = "integer",
                                    minimum = 1,
                                },
                            },
                            ["matchedPlanIds"] = new
                            {
                                type = "array",
                                maxItems = 16,
                                items = new
                                {
                                    type = "integer",
                                    minimum = 1,
                                },
                            },
                        },
                        required = new[]
                        {
                            "title",
                            "diagnosis",
                            "proposalKind",
                            "targetObject",
                            "sqlScript",
                            "rollbackSqlScript",
                            "expectedBenefit",
                            "verificationMetric",
                            "confidence",
                            "sourceCommandOrdinals",
                            "matchedPlanIds",
                        },
                    },
                },
            },
            required = new[] { "recommendations", "proposals" },
        };
    }

    private static string ReadResponseOutputText(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);
        if (TryReadStringProperty(document.RootElement, "output_text", out var outputText))
        {
            return outputText;
        }

        if (TryReadNestedOutputText(document.RootElement, out outputText))
        {
            return outputText;
        }

        throw new InvalidOperationException("OpenAI tuning advice returned empty output.");
    }

    private static bool TryReadNestedOutputText(JsonElement root, out string outputText)
    {
        outputText = string.Empty;
        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var outputItem in output.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (TryReadOutputTextContent(contentItem, out outputText))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryReadOutputTextContent(JsonElement contentItem, out string outputText)
    {
        outputText = string.Empty;
        return contentItem.TryGetProperty("type", out var type)
            && string.Equals(type.GetString(), "output_text", StringComparison.Ordinal)
            && TryReadStringProperty(contentItem, "text", out outputText);
    }

    private static bool TryReadStringProperty(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var text = property.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        value = text;
        return true;
    }

    private static IReadOnlyList<SqlTuningRecommendation> NormalizeRecommendations(
        IReadOnlyList<SqlTuningRecommendation>? recommendations)
    {
        if (recommendations is null || recommendations.Count == 0)
        {
            return [];
        }

        List<SqlTuningRecommendation> normalizedRecommendations = new(recommendations.Count);
        foreach (var recommendation in recommendations)
        {
            var title = recommendation.Title.Trim();
            var rootCause = recommendation.RootCause.Trim();
            var suggestedChange = recommendation.SuggestedChange.Trim();
            var verificationMetric = recommendation.VerificationMetric.Trim();
            if (title.Length == 0
                || rootCause.Length == 0
                || suggestedChange.Length == 0
                || verificationMetric.Length == 0)
            {
                throw new InvalidOperationException(
                    "OpenAI tuning advice returned an incomplete recommendation.");
            }

            normalizedRecommendations.Add(new SqlTuningRecommendation
            {
                Title = title,
                RootCause = rootCause,
                SuggestedChange = suggestedChange,
                VerificationMetric = verificationMetric,
            });
        }

        return normalizedRecommendations;
    }

    private static NormalizedProposalResult NormalizeProposals(
        IReadOnlyList<SqlTuningProposal>? proposals)
    {
        if (proposals is null || proposals.Count == 0)
        {
            return new NormalizedProposalResult
            {
                Proposals = [],
                Warnings = [],
            };
        }

        List<SqlTuningProposal> normalizedProposals = new(proposals.Count);
        List<string> warnings = [];
        foreach (var proposal in proposals)
        {
            var title = proposal.Title.Trim();
            var diagnosis = proposal.Diagnosis.Trim();
            var proposalKind = proposal.ProposalKind.Trim();
            var targetObject = proposal.TargetObject.Trim();
            var sqlScript = proposal.SqlScript.Trim();
            var rollbackSqlScript = proposal.RollbackSqlScript?.Trim() ?? string.Empty;
            var expectedBenefit = proposal.ExpectedBenefit.Trim();
            var verificationMetric = proposal.VerificationMetric.Trim();
            if (title.Length == 0
                || diagnosis.Length == 0
                || proposalKind.Length == 0
                || targetObject.Length == 0
                || sqlScript.Length == 0
                || expectedBenefit.Length == 0
                || verificationMetric.Length == 0)
            {
                throw new InvalidOperationException(
                    "OpenAI tuning advice returned an incomplete SQL proposal.");
            }

            if (rollbackSqlScript.Length == 0)
            {
                warnings.Add(
                    $"SQL proposal '{title}' did not include rollback SQL. Sqloom persisted the proposal with an empty rollback script.");
            }

            normalizedProposals.Add(new SqlTuningProposal
            {
                Title = title,
                Diagnosis = diagnosis,
                ProposalKind = proposalKind,
                TargetObject = targetObject,
                SqlScript = sqlScript,
                RollbackSqlScript = rollbackSqlScript,
                ExpectedBenefit = expectedBenefit,
                VerificationMetric = verificationMetric,
                Confidence = Math.Clamp(proposal.Confidence, 0d, 1d),
                SourceCommandOrdinals = proposal.SourceCommandOrdinals
                    .Distinct()
                    .OrderBy(static ordinal => ordinal)
                    .ToArray(),
                MatchedPlanIds = proposal.MatchedPlanIds
                    .Distinct()
                    .OrderBy(static planId => planId)
                    .ToArray(),
            });
        }

        return new NormalizedProposalResult
        {
            Proposals = normalizedProposals,
            Warnings = warnings,
        };
    }
}

internal sealed class NormalizedProposalResult
{
    public required IReadOnlyList<SqlTuningProposal> Proposals { get; init; }

    public required IReadOnlyList<string> Warnings { get; init; }
}
