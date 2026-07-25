#pragma warning disable OPENAI001

using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;
using Sqloom.Pipeline.Execution;

namespace Sqloom.Host.Replay;

/// <summary>
/// Uses Microsoft Agent Framework to fill replay inputs for harder binding cases.
/// </summary>
internal sealed class AgentFrameworkReplayDataGenerator : IReplayDataGenerator
{
    private const string Instructions =
        "You generate ASP.NET Core endpoint replay data for Sqloom. " +
        "Use the supplied endpoint operation and missing input list to choose replay values. " +
        "Return only type-valid values for missing path, query, header, and JSON body inputs. " +
        "Return pathValues, queryValues, and headerValues as arrays of name=value strings. " +
        "Use an empty array when no values are needed. " +
        "Do not return values for inputs that are already resolved. " +
        "Do not invent secrets, auth tokens, or database connection strings. " +
        "Prefer realistic values that satisfy the endpoint contract and model binding.";

    private readonly IAgentReplayDataClient _agentClient;

    internal AgentFrameworkReplayDataGenerator(
        OpenAIAdviceOptions options,
        IAgentReplayDataClient? agentClient = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        _agentClient = agentClient ?? new OpenAIAgentReplayDataClient(options);
    }

    public async Task<ReplayDataGenerationOperation> GenerateAsync(
        ReplayDataGenerationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var missingInputs = MissingReplayInputs.Create(context);
        if (!missingInputs.NeedsGeneration)
        {
            return new ReplayDataGenerationOperation
            {
                OperationKey = context.Operation.StableOperationKey,
                Strategy = "microsoft-agent-framework-openai",
                Status = "not-needed",
                Confidence = 1,
                SourcesUsed =
                [
                    "endpoint-operation",
                ],
                Notes = "Resolved replay operation already had the required values.",
            };
        }

        AgentReplayGeneratedData response;
        try
        {
            var prompt = BuildPrompt(context, missingInputs);
            response = await _agentClient
                .RunAsync(prompt, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                "Microsoft Agent Framework replay data generation failed.",
                exception);
        }

        var validation = ValidateGeneratedData(response, missingInputs);
        return new ReplayDataGenerationOperation
        {
            OperationKey = context.Operation.StableOperationKey,
            Strategy = "microsoft-agent-framework-openai",
            Status = "generated",
            Confidence = 0.8,
            PreparedData = validation.PreparedData,
            SourcesUsed =
            [
                "endpoint-operation",
                "microsoft-agent-framework",
            ],
            Warnings = validation.Warnings,
            Notes = "Generated replay data with Microsoft Agent Framework structured output.",
        };
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
        ReplayDataGenerationContext context,
        MissingReplayInputs missingInputs)
    {
        var payload = new
        {
            operation = context.Operation,
            missingInputs = new
            {
                pathParameters = missingInputs.PathParameters,
                queryParameters = missingInputs.QueryParameters,
                headerParameters = missingInputs.HeaderParameters,
                requestBodyRequired = missingInputs.RequestBodyRequired,
            },
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
        };

        return JsonSerializer.Serialize(payload, JsonSerializerOptions.Web);
    }

    private static (ReplayPreparedData PreparedData, IReadOnlyList<string> Warnings) ValidateGeneratedData(
        AgentReplayGeneratedData agentData,
        MissingReplayInputs missingInputs)
    {
        var rawPreparedData = agentData.ToReplayPreparedData();
        List<string> warnings = [];
        var pathValues = ValidateParameterValues(
            "path",
            missingInputs.PathParameters,
            rawPreparedData.PathValues,
            warnings);
        var queryValues = ValidateParameterValues(
            "query",
            missingInputs.QueryParameters,
            rawPreparedData.QueryValues,
            warnings);
        var headerValues = ValidateParameterValues(
            "header",
            missingInputs.HeaderParameters,
            rawPreparedData.HeaderValues,
            warnings);
        var requestBodyJson = ValidateRequestBody(
            missingInputs.RequestBodyRequired,
            rawPreparedData.RequestBodyJson,
            warnings);

        return (
            new ReplayPreparedData
            {
                Persona = rawPreparedData.Persona,
                RequestBodyJson = requestBodyJson,
                PathValues = pathValues,
                QueryValues = queryValues,
                HeaderValues = headerValues,
            },
            warnings);
    }

    private static IReadOnlyDictionary<string, string> ValidateParameterValues(
        string location,
        IReadOnlyList<ReplayParameter> missingParameters,
        IReadOnlyDictionary<string, string> candidateValues,
        ICollection<string> warnings)
    {
        Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);
        var expectedByName = missingParameters.ToDictionary(
            parameter => parameter.Name,
            StringComparer.OrdinalIgnoreCase);

        foreach ((var name, _) in candidateValues)
        {
            if (!expectedByName.ContainsKey(name))
            {
                warnings.Add($"Ignored unexpected {location} value '{name}' returned by Microsoft Agent Framework.");
            }
        }

        foreach (var parameter in missingParameters)
        {
            if (!candidateValues.TryGetValue(parameter.Name, out var value)
                || string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"Microsoft Agent Framework did not return required {location} value '{parameter.Name}'.");
            }

            ValidatePrimitiveValue(parameter, value);
            result[parameter.Name] = value;
        }

        return result;
    }

    private static string? ValidateRequestBody(
        bool requestBodyRequired,
        string? candidateJson,
        ICollection<string> warnings)
    {
        if (!requestBodyRequired)
        {
            if (!string.IsNullOrWhiteSpace(candidateJson))
            {
                warnings.Add("Ignored unexpected request body returned by Microsoft Agent Framework.");
            }

            return null;
        }

        if (string.IsNullOrWhiteSpace(candidateJson))
        {
            throw new InvalidOperationException(
                "Microsoft Agent Framework did not return the required JSON request body.");
        }

        try
        {
            using var _ = JsonDocument.Parse(candidateJson);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "Microsoft Agent Framework returned invalid JSON for the request body.",
                exception);
        }

        return candidateJson;
    }

    private static void ValidatePrimitiveValue(
        ReplayParameter parameter,
        string value)
    {
        var schemaType = parameter.SchemaType?.Trim();
        var format = parameter.Format?.Trim();

        if (string.Equals(schemaType, "integer", StringComparison.OrdinalIgnoreCase)
            && !long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            throw new InvalidOperationException(
                $"Microsoft Agent Framework returned a non-integer value for '{parameter.Name}'.");
        }

        if (string.Equals(schemaType, "number", StringComparison.OrdinalIgnoreCase)
            && !decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            throw new InvalidOperationException(
                $"Microsoft Agent Framework returned a non-number value for '{parameter.Name}'.");
        }

        if (string.Equals(schemaType, "boolean", StringComparison.OrdinalIgnoreCase)
            && !bool.TryParse(value, out _))
        {
            throw new InvalidOperationException(
                $"Microsoft Agent Framework returned a non-boolean value for '{parameter.Name}'.");
        }

        if (string.Equals(format, "date-time", StringComparison.OrdinalIgnoreCase)
            && !DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out _))
        {
            throw new InvalidOperationException(
                $"Microsoft Agent Framework returned a non-date-time value for '{parameter.Name}'.");
        }

        if (string.Equals(format, "date", StringComparison.OrdinalIgnoreCase)
            && !DateOnly.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
        {
            throw new InvalidOperationException(
                $"Microsoft Agent Framework returned a non-date value for '{parameter.Name}'.");
        }

        if (string.Equals(format, "uuid", StringComparison.OrdinalIgnoreCase)
            && !Guid.TryParse(value, out _))
        {
            throw new InvalidOperationException(
                $"Microsoft Agent Framework returned a non-UUID value for '{parameter.Name}'.");
        }
    }

    internal interface IAgentReplayDataClient
    {
        Task<AgentReplayGeneratedData> RunAsync(
            string prompt,
            CancellationToken cancellationToken = default);
    }

    internal sealed class AgentReplayGeneratedData
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

        internal ReplayPreparedData ToReplayPreparedData()
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

    private sealed class OpenAIAgentReplayDataClient : IAgentReplayDataClient
    {
        private readonly OpenAIAdviceOptions _options;

        internal OpenAIAgentReplayDataClient(OpenAIAdviceOptions options)
        {
            _options = options;
        }

        public async Task<AgentReplayGeneratedData> RunAsync(
            string prompt,
            CancellationToken cancellationToken = default)
        {
            var agent = CreateAgent();
            var response = await agent
                .RunAsync<AgentReplayGeneratedData>(
                    prompt,
                    serializerOptions: JsonSerializerOptions.Web,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return response.Result;
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
    }

    private sealed class MissingReplayInputs
    {
        public IReadOnlyList<ReplayParameter> PathParameters { get; init; } =
            Array.Empty<ReplayParameter>();

        public IReadOnlyList<ReplayParameter> QueryParameters { get; init; } =
            Array.Empty<ReplayParameter>();

        public IReadOnlyList<ReplayParameter> HeaderParameters { get; init; } =
            Array.Empty<ReplayParameter>();

        public bool RequestBodyRequired { get; init; }

        public bool NeedsGeneration =>
            PathParameters.Count > 0
            || QueryParameters.Count > 0
            || HeaderParameters.Count > 0
            || RequestBodyRequired;

        internal static MissingReplayInputs Create(ReplayDataGenerationContext context)
        {
            List<ReplayParameter> pathParameters = [];
            List<ReplayParameter> queryParameters = [];
            List<ReplayParameter> headerParameters = [];

            foreach (var parameter in context.Operation.Parameters.Where(static parameter => parameter.Required))
            {
                var existingValues = parameter.Location switch
                {
                    "path" => context.ResolvedOperation.PathValues,
                    "query" => context.ResolvedOperation.QueryValues,
                    "header" => context.ResolvedOperation.HeaderValues,
                    _ => throw new InvalidOperationException(
                        $"Unsupported required endpoint parameter location '{parameter.Location}' for '{parameter.Name}'."),
                };

                if (existingValues.ContainsKey(parameter.Name))
                {
                    continue;
                }

                switch (parameter.Location)
                {
                    case "path":
                        pathParameters.Add(parameter);
                        break;
                    case "query":
                        queryParameters.Add(parameter);
                        break;
                    case "header":
                        headerParameters.Add(parameter);
                        break;
                }
            }

            var requestBodyRequired = context.Operation.RequestBodyRequired
                && string.IsNullOrWhiteSpace(context.ResolvedOperation.RequestBodyJson)
                && string.IsNullOrWhiteSpace(context.Operation.JsonBodyExample);

            return new MissingReplayInputs
            {
                PathParameters = pathParameters,
                QueryParameters = queryParameters,
                HeaderParameters = headerParameters,
                RequestBodyRequired = requestBodyRequired,
            };
        }
    }

}
