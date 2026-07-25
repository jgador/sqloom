using System;
using System.Linq;
using System.Threading.Tasks;
using Sqloom.Host.Replay;
using Sqloom.Pipeline.Execution;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises the live Microsoft Agent Framework replay data path without running Sqloom tune.
/// </summary>
public sealed class AgentFrameworkReplayDataGeneratorLiveTests
{
    private const string LiveModel = "gpt-5.4-mini";

    [Fact(Explicit = true)]
    [Trait("Category", "Integration")]
    [Trait("Category", "OpenAI")]
    public async Task WithOpenAIApiKey_GeneratesProductByCategoryQueryValues()
    {
        // Run with: dotnet test .\tests\Sqloom.IntegrationTests\Sqloom.IntegrationTests.csproj -- --filter-class Sqloom.Host.Tests.AgentFrameworkReplayDataGeneratorLiveTests --explicit only
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Assert.Skip("Set OPENAI_API_KEY to run the live Microsoft Agent Framework replay data test.");
        }

        AgentFrameworkReplayDataGenerator generator = new(
            new OpenAIAdviceOptions
            {
                ApiKey = apiKey,
                BaseUrl = "https://api.openai.com",
                Model = LiveModel,
            });

        var context = await CreateContextAsync();
        var result = await generator.GenerateAsync(context);

        Assert.Equal("microsoft-agent-framework-openai", result.Strategy);
        Assert.Equal("generated", result.Status);
        Assert.Equal(SampleCatalogReplayScenario.OperationKey, result.OperationKey);
        Assert.True(result.PreparedData.QueryValues.ContainsKey("categoryId"));
        Assert.True(result.PreparedData.QueryValues.ContainsKey("minPrice"));
        Assert.Contains("microsoft-agent-framework", result.SourcesUsed);
    }

    private static async Task<ReplayDataGenerationContext> CreateContextAsync()
    {
        var operations = await new EndpointCatalogLoader()
            .LoadAsync(SqloomTestAppPaths.GetProjectPath());
        var operation = operations.Single(operation =>
            string.Equals(operation.StableOperationKey, SampleCatalogReplayScenario.OperationKey, StringComparison.Ordinal));

        Assert.Contains(
            operation.Parameters,
            parameter => string.Equals(parameter.Name, "categoryId", StringComparison.Ordinal)
                && string.Equals(parameter.Location, "query", StringComparison.Ordinal)
                && parameter.Required);
        Assert.Contains(
            operation.Parameters,
            parameter => string.Equals(parameter.Name, "minPrice", StringComparison.Ordinal)
                && string.Equals(parameter.Location, "query", StringComparison.Ordinal)
                && parameter.Required);

        return new ReplayDataGenerationContext
        {
            Operation = operation,
            ResolvedOperation = new ResolvedReplayOperation
            {
                OperationKey = operation.StableOperationKey,
                HttpMethod = operation.HttpMethod,
                Route = operation.Route,
            },
            Mode = ReplayDataAgentMode.Required,
            ModelName = LiveModel,
        };
    }
}
