#pragma warning disable OPENAI001

using System;
using System.ClientModel;
using System.Text.Json;
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
                .RunAsync<ReplayPreparedData>(
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
                PreparedData = response.Result,
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
            Endpoint = new Uri(_options.BaseUrl),
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
}
