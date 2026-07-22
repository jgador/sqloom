using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Pipeline.Execution;
using Sqloom.Host.Replay;
using Xunit;

namespace Sqloom.Host.Tests.Replay;

/// <summary>
/// Exercises Microsoft Agent Framework replay data preparation behavior without network calls.
/// </summary>
public sealed class AgentFrameworkReplayDataPreparerTests
{
    [Theory]
    [InlineData("https://api.openai.com", "https://api.openai.com/v1")]
    [InlineData("https://api.openai.com/", "https://api.openai.com/v1")]
    [InlineData("https://api.openai.com/v1", "https://api.openai.com/v1")]
    public void BuildOpenAIEndpoint_NormalizesSqloomBaseUrlForOpenAISdk(
        string baseUrl,
        string expectedEndpoint)
    {
        var endpoint = AgentFrameworkReplayDataPreparer.BuildOpenAIEndpoint(baseUrl);

        Assert.Equal(expectedEndpoint, endpoint.AbsoluteUri);
    }

    [Fact]
    public void AgentReplayPreparedData_ConvertsValueListsToReplayPreparedData()
    {
        AgentFrameworkReplayDataPreparer.AgentReplayPreparedData agentData = new()
        {
            Persona = "customer",
            RequestBodyJson = """{"name":"probe"}""",
            PathValues =
            [
                "id=42",
            ],
            QueryValues =
            [
                "sqloomMafProbe=sqloom",
                "ignored",
                "=ignored",
            ],
            HeaderValues =
            [
                "x-test=true",
            ],
        };

        var preparedData = agentData.ToReplayPreparedData();

        Assert.Equal("customer", preparedData.Persona);
        Assert.Equal("""{"name":"probe"}""", preparedData.RequestBodyJson);
        Assert.Equal("42", preparedData.PathValues["id"]);
        Assert.Equal("sqloom", preparedData.QueryValues["sqloomMafProbe"]);
        Assert.False(preparedData.QueryValues.ContainsKey(""));
        Assert.Equal("true", preparedData.HeaderValues["x-test"]);
    }

    [Fact]
    public async Task PrepareAsync_WhenResolvedOperationNeedsNoData_ReturnsNotNeededWithoutCallingAgent()
    {
        FakeAgentReplayDataClient agentClient = new(
            new AgentFrameworkReplayDataPreparer.AgentReplayPreparedData());
        AgentFrameworkReplayDataPreparer preparer = new(
            CreateOptions(),
            agentClient);

        var result = await preparer.PrepareAsync(
            CreateProductContext(
                new Dictionary<string, string>
                {
                    ["categoryId"] = "1",
                    ["minPrice"] = "900",
                }));

        Assert.Equal("microsoft-agent-framework-openai", result.Strategy);
        Assert.Equal("not-needed", result.Status);
        Assert.Equal(0, agentClient.Calls);
    }

    [Fact]
    public async Task PrepareAsync_WhenValuesAreMissing_UsesAgentValues()
    {
        FakeAgentReplayDataClient agentClient = new(
            new AgentFrameworkReplayDataPreparer.AgentReplayPreparedData
            {
                QueryValues =
                [
                    "categoryId=1",
                    "minPrice=900",
                    "extra=ignored",
                ],
            });
        AgentFrameworkReplayDataPreparer preparer = new(
            CreateOptions(),
            agentClient);

        var result = await preparer.PrepareAsync(CreateProductContext());

        Assert.Equal("microsoft-agent-framework-openai", result.Strategy);
        Assert.Equal("generated", result.Status);
        Assert.Equal(SampleCatalogReplayScenario.OperationKey, result.OperationKey);
        Assert.Equal("1", result.PreparedData.QueryValues["categoryId"]);
        Assert.Equal("900", result.PreparedData.QueryValues["minPrice"]);
        Assert.False(result.PreparedData.QueryValues.ContainsKey("extra"));
        Assert.Contains("microsoft-agent-framework", result.SourcesUsed);
        Assert.Contains(
            result.Warnings,
            warning => warning.Contains("Ignored unexpected query value 'extra'", StringComparison.Ordinal));
        Assert.Equal(1, agentClient.Calls);
        Assert.NotNull(agentClient.Prompt);
        Assert.Contains("missingInputs", agentClient.Prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("deterministicFallback", agentClient.Prompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PrepareAsync_WhenAgentCallFails_Throws()
    {
        FakeAgentReplayDataClient agentClient = new(
            new InvalidOperationException("agent failed"));
        AgentFrameworkReplayDataPreparer preparer = new(
            CreateOptions(),
            agentClient);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => preparer.PrepareAsync(CreateProductContext()));

        Assert.Contains("Microsoft Agent Framework", exception.Message, StringComparison.Ordinal);
        Assert.Contains("agent failed", exception.InnerException?.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PrepareAsync_WhenAgentMissesRequiredValue_Throws()
    {
        FakeAgentReplayDataClient agentClient = new(
            new AgentFrameworkReplayDataPreparer.AgentReplayPreparedData
            {
                QueryValues =
                [
                    "categoryId=1",
                ],
            });
        AgentFrameworkReplayDataPreparer preparer = new(
            CreateOptions(),
            agentClient);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => preparer.PrepareAsync(CreateProductContext()));

        Assert.Contains("minPrice", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PrepareAsync_WhenAgentReturnsInvalidPrimitiveValue_Throws()
    {
        FakeAgentReplayDataClient agentClient = new(
            new AgentFrameworkReplayDataPreparer.AgentReplayPreparedData
            {
                QueryValues =
                [
                    "categoryId=abc",
                    "minPrice=900",
                ],
            });
        AgentFrameworkReplayDataPreparer preparer = new(
            CreateOptions(),
            agentClient);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => preparer.PrepareAsync(CreateProductContext()));

        Assert.Contains("categoryId", exception.Message, StringComparison.Ordinal);
        Assert.Contains("non-integer", exception.Message, StringComparison.Ordinal);
    }

    private static OpenAIAdviceOptions CreateOptions()
    {
        return new OpenAIAdviceOptions
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.openai.com",
            Model = "gpt-test",
        };
    }

    private static ReplayDataPreparationContext CreateProductContext(
        IReadOnlyDictionary<string, string>? queryValues = null)
    {
        return new ReplayDataPreparationContext
        {
            Operation = new ReplayOperation
            {
                StableOperationKey = SampleCatalogReplayScenario.OperationKey,
                HttpMethod = "GET",
                Route = SampleCatalogReplayScenario.Route,
                Parameters =
                [
                    new ReplayParameter
                    {
                        Name = "categoryId",
                        Location = "query",
                        Required = true,
                        SchemaType = "integer",
                    },
                    new ReplayParameter
                    {
                        Name = "minPrice",
                        Location = "query",
                        Required = true,
                        SchemaType = "number",
                    },
                ],
            },
            ResolvedOperation = new ResolvedReplayOperation
            {
                OperationKey = SampleCatalogReplayScenario.OperationKey,
                HttpMethod = "GET",
                Route = SampleCatalogReplayScenario.Route,
                QueryValues = queryValues
                    ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            },
            Mode = ReplayDataAgentMode.Required,
            ModelName = "gpt-test",
        };
    }

    private sealed class FakeAgentReplayDataClient
        : AgentFrameworkReplayDataPreparer.IAgentReplayDataClient
    {
        private readonly AgentFrameworkReplayDataPreparer.AgentReplayPreparedData? _response;
        private readonly Exception? _exception;

        public FakeAgentReplayDataClient(
            AgentFrameworkReplayDataPreparer.AgentReplayPreparedData response)
        {
            _response = response;
        }

        public FakeAgentReplayDataClient(Exception exception)
        {
            _exception = exception;
        }

        public int Calls { get; private set; }

        public string? Prompt { get; private set; }

        public Task<AgentFrameworkReplayDataPreparer.AgentReplayPreparedData> RunAsync(
            string prompt,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            Prompt = prompt;
            if (_exception is not null)
            {
                throw _exception;
            }

            return Task.FromResult(_response!);
        }
    }
}
