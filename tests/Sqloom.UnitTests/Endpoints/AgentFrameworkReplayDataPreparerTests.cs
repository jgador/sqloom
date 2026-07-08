using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Core.Execution;
using Sqloom.Host.Replay;
using Xunit;

namespace Sqloom.Host.Tests.Replay;

/// <summary>
/// Exercises Microsoft Agent Framework replay data preparation behavior without network calls.
/// </summary>
public sealed class AgentFrameworkReplayDataPreparerTests
{
    [Fact]
    public async Task PrepareAsync_WhenFallbackNeedsNoData_ReturnsFallbackWithoutCreatingAgent()
    {
        var fallbackOperation = CreateFallbackOperation(status: "not-needed");
        StubReplayDataPreparer fallback = new(fallbackOperation);
        AgentFrameworkReplayDataPreparer preparer = new(
            CreateOptions(baseUrl: "not a uri"),
            fallback);

        var result = await preparer.PrepareAsync(CreateContext(ReplayDataAgentMode.Auto));

        Assert.Same(fallbackOperation, result);
        Assert.Equal(1, fallback.Calls);
    }

    [Fact]
    public async Task PrepareAsync_InAutoMode_WhenAgentSetupFails_ReturnsFallbackWithWarning()
    {
        var fallbackOperation = CreateFallbackOperation(status: "generated");
        StubReplayDataPreparer fallback = new(fallbackOperation);
        AgentFrameworkReplayDataPreparer preparer = new(
            CreateOptions(baseUrl: "not a uri"),
            fallback,
            allowFallback: true);

        var result = await preparer.PrepareAsync(CreateContext(ReplayDataAgentMode.Auto));

        Assert.Equal("deterministic-openapi", result.Strategy);
        Assert.Equal("generated", result.Status);
        Assert.Equal("1", result.PreparedData.PathValues["id"]);
        Assert.Contains(result.Warnings, warning => warning.Contains("Microsoft Agent Framework"));
    }

    [Fact]
    public async Task PrepareAsync_InRequiredMode_WhenAgentSetupFails_Throws()
    {
        StubReplayDataPreparer fallback = new(CreateFallbackOperation(status: "generated"));
        AgentFrameworkReplayDataPreparer preparer = new(
            CreateOptions(baseUrl: "not a uri"),
            fallback,
            allowFallback: false);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => preparer.PrepareAsync(CreateContext(ReplayDataAgentMode.Required)));

        Assert.Contains("required mode", exception.Message);
    }

    private static OpenAIAdviceOptions CreateOptions(string baseUrl)
    {
        return new OpenAIAdviceOptions
        {
            ApiKey = "test-key",
            BaseUrl = baseUrl,
            Model = "gpt-test",
        };
    }

    private static ReplayDataPreparationContext CreateContext(ReplayDataAgentMode mode)
    {
        return new ReplayDataPreparationContext
        {
            Operation = new OpenApiOperation
            {
                StableOperationKey = "GET /api/orders/{id}",
                HttpMethod = "GET",
                Route = "/api/orders/{id}",
            },
            ResolvedOperation = new ResolvedReplayOperation
            {
                OperationKey = "GET /api/orders/{id}",
                HttpMethod = "GET",
                Route = "/api/orders/{id}",
            },
            Mode = mode,
            ModelName = "gpt-test",
        };
    }

    private static ReplayDataPreparationOperation CreateFallbackOperation(string status)
    {
        return new ReplayDataPreparationOperation
        {
            OperationKey = "GET /api/orders/{id}",
            Strategy = "deterministic-openapi",
            Status = status,
            Confidence = 0.75,
            PreparedData = new ReplayPreparedData
            {
                PathValues = new Dictionary<string, string>
                {
                    ["id"] = "1",
                },
            },
            SourcesUsed =
            [
                "openapi-parameter:path:id",
            ],
        };
    }

    private sealed class StubReplayDataPreparer : IReplayDataPreparer
    {
        private readonly ReplayDataPreparationOperation _operation;

        public StubReplayDataPreparer(ReplayDataPreparationOperation operation)
        {
            _operation = operation;
        }

        public int Calls { get; private set; }

        public Task<ReplayDataPreparationOperation> PrepareAsync(
            ReplayDataPreparationContext context,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(_operation);
        }
    }
}
