#pragma warning disable OPENAI001

using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;
using Sqloom.Core.Execution;

namespace Sqloom.Host.Replay;

/// <summary>
/// Uses Microsoft Agent Framework to fill replay inputs for harder binding cases.
/// </summary>
internal sealed class AgentFrameworkReplayDataPreparer : IReplayDataPreparer
{
    private const string Instructions =
        "You prepare ASP.NET Core endpoint replay data for Sqloom. " +
        "Return only type-valid path, query, header, and JSON body values. " +
        "Return pathValues, queryValues, and headerValues as arrays of name=value strings. " +
        "Use an empty array when no values are needed. " +
        "Do not invent secrets, auth tokens, or database connection strings. " +
        "Prefer simple values that satisfy model binding over business-realistic data.";

    private readonly OpenAIAdviceOptions _options;
    private readonly IReplayDataPreparer _fallback;
    private readonly bool _allowFallback;

    public AgentFrameworkReplayDataPreparer(
        OpenAIAdviceOptions options,
        IReplayDataPreparer fallback,
        bool allowFallback = true)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        _allowFallback = allowFallback;
    }

    public async Task<ReplayDataPreparationOperation> PrepareAsync(
        ReplayDataPreparationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var fallback = await _fallback
            .PrepareAsync(context, cancellationToken)
            .ConfigureAwait(false);
        if (string.Equals(fallback.Status, "not-needed", StringComparison.OrdinalIgnoreCase))
        {
            return fallback;
        }

        try
        {
            var agent = CreateAgent();
            var prompt = BuildPrompt(context, fallback.PreparedData);
            var response = await agent
                .RunAsync<AgentReplayPreparedData>(
                    prompt,
                    serializerOptions: JsonSerializerOptions.Web,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new ReplayDataPreparationOperation
            {
                OperationKey = context.Operation.StableOperationKey,
                Strategy = "microsoft-agent-framework-openai",
                Status = "generated",
                Confidence = 0.8,
                PreparedData = response.Result.ToReplayPreparedData(),
                SourcesUsed =
                [
                    "openapi-operation",
                    "deterministic-openapi-fallback",
                    "microsoft-agent-framework",
                ],
                Warnings = fallback.Warnings,
                Notes = "Generated replay data with Microsoft Agent Framework structured output.",
            };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (!_allowFallback)
            {
                throw new InvalidOperationException(
                    "Microsoft Agent Framework replay data generation failed in required mode.",
                    exception);
            }

            return new ReplayDataPreparationOperation
            {
                OperationKey = context.Operation.StableOperationKey,
                Strategy = "deterministic-openapi",
                Status = fallback.Status,
                Confidence = fallback.Confidence,
                PreparedData = fallback.PreparedData,
                SourcesUsed = fallback.SourcesUsed,
                Warnings =
                [
                    .. fallback.Warnings,
                    $"Microsoft Agent Framework replay data generation failed: {exception.Message}",
                ],
                Notes = "Fell back to deterministic replay data generation.",
            };
        }
    }

    private AIAgent CreateAgent()
    {
        OpenAIClientOptions clientOptions = new()
        {
            Endpoint = BuildOpenAIEndpoint(_options.BaseUrl),
        };
        ResponsesClient client = new(
            new ApiKeyCredential(_options.ApiKey),
            clientOptions);

        return client.AsAIAgent(
            new ChatClientAgentOptions
            {
                Name = "SqloomReplayDataAgent",
                ChatOptions = new ChatOptions
                {
                    ModelId = _options.Model,
                    Instructions = Instructions,
                },
            },
            model: _options.Model);
    }

    internal static Uri BuildOpenAIEndpoint(string baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        var endpoint = baseUrl.TrimEnd('/');
        if (!endpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = $"{endpoint}/v1";
        }

        return new Uri(endpoint, UriKind.Absolute);
    }

    private static string BuildPrompt(
        ReplayDataPreparationContext context,
        ReplayPreparedData fallback)
    {
        var payload = new
        {
            operation = context.Operation,
            resolvedOperation = new
            {
                context.ResolvedOperation.OperationKey,
                context.ResolvedOperation.HttpMethod,
                context.ResolvedOperation.Route,
                context.ResolvedOperation.Persona,
                hasRequestBody = !string.IsNullOrWhiteSpace(context.ResolvedOperation.RequestBodyJson),
                context.ResolvedOperation.PathValues,
                context.ResolvedOperation.QueryValues,
                context.ResolvedOperation.HeaderValues,
            },
            deterministicFallback = fallback,
        };

        return JsonSerializer.Serialize(payload, JsonSerializerOptions.Web);
    }

    internal sealed class AgentReplayPreparedData
    {
        [JsonPropertyName("persona")]
        public string? Persona { get; init; }

        [JsonPropertyName("requestBodyJson")]
        public string RequestBodyJson { get; init; } = string.Empty;

        [JsonPropertyName("pathValues")]
        public List<string> PathValues { get; init; } = [];

        [JsonPropertyName("queryValues")]
        public List<string> QueryValues { get; init; } = [];

        [JsonPropertyName("headerValues")]
        public List<string> HeaderValues { get; init; } = [];

        public ReplayPreparedData ToReplayPreparedData()
        {
            return new ReplayPreparedData
            {
                Persona = string.IsNullOrWhiteSpace(Persona) ? null : Persona,
                RequestBodyJson = string.IsNullOrWhiteSpace(RequestBodyJson) ? null : RequestBodyJson,
                PathValues = ToDictionary(PathValues),
                QueryValues = ToDictionary(QueryValues),
                HeaderValues = ToDictionary(HeaderValues),
            };
        }

        private static IReadOnlyDictionary<string, string> ToDictionary(IEnumerable<string> values)
        {
            Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);
            foreach (var value in values)
            {
                var separatorIndex = value.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var name = value[..separatorIndex].Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                result[name] = value[(separatorIndex + 1)..];
            }

            return result;
        }
    }
}
