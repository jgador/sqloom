using System.Collections.Generic;
using System.Threading.Tasks;
using Sqloom.Core.Execution;
using Sqloom.Host.Replay;
using Xunit;

namespace Sqloom.Host.Tests.Replay;

/// <summary>
/// Exercises deterministic replay data preparation.
/// </summary>
public sealed class DeterministicReplayDataPreparerTests
{
    [Fact]
    public async Task GeneratesMissingRequiredValuesFromOpenApiMetadata()
    {
        DeterministicReplayDataPreparer preparer = new();

        var result = await preparer.PrepareAsync(
            new ReplayDataPreparationContext
            {
                Operation = new OpenApiOperation
                {
                    StableOperationKey = "GET /api/orders/{orderId}",
                    HttpMethod = "GET",
                    Route = "/api/orders/{orderId}",
                    Parameters =
                    [
                        new OpenApiParameter
                        {
                            Name = "orderId",
                            Location = "path",
                            Required = true,
                            SchemaType = "integer",
                        },
                        new OpenApiParameter
                        {
                            Name = "since",
                            Location = "query",
                            Required = true,
                            SchemaType = "string",
                            Format = "date-time",
                        },
                        new OpenApiParameter
                        {
                            Name = "includeDetails",
                            Location = "query",
                            Required = true,
                            SchemaType = "boolean",
                        },
                        new OpenApiParameter
                        {
                            Name = "correlationId",
                            Location = "header",
                            Required = true,
                            SchemaType = "string",
                            Format = "uuid",
                        },
                    ],
                },
                ResolvedOperation = new ResolvedReplayOperation
                {
                    OperationKey = "GET /api/orders/{orderId}",
                    HttpMethod = "GET",
                    Route = "/api/orders/{orderId}",
                },
                Mode = ReplayDataAgentMode.Auto,
            });

        Assert.Equal("generated", result.Status);
        Assert.Equal("deterministic-openapi", result.Strategy);
        Assert.Equal("1", result.PreparedData.PathValues["orderId"]);
        Assert.Equal("2026-01-01T00:00:00Z", result.PreparedData.QueryValues["since"]);
        Assert.Equal("true", result.PreparedData.QueryValues["includeDetails"]);
        Assert.Equal(
            "00000000-0000-0000-0000-000000000001",
            result.PreparedData.HeaderValues["correlationId"]);
    }

    [Fact]
    public async Task DoesNotOverwriteExistingResolvedValues()
    {
        DeterministicReplayDataPreparer preparer = new();

        var result = await preparer.PrepareAsync(
            new ReplayDataPreparationContext
            {
                Operation = new OpenApiOperation
                {
                    StableOperationKey = "GET /api/orders/{orderId}",
                    HttpMethod = "GET",
                    Route = "/api/orders/{orderId}",
                    Parameters =
                    [
                        new OpenApiParameter
                        {
                            Name = "orderId",
                            Location = "path",
                            Required = true,
                            SchemaType = "integer",
                        },
                    ],
                },
                ResolvedOperation = new ResolvedReplayOperation
                {
                    OperationKey = "GET /api/orders/{orderId}",
                    HttpMethod = "GET",
                    Route = "/api/orders/{orderId}",
                    PathValues = new Dictionary<string, string>
                    {
                        ["orderId"] = "42",
                    },
                },
                Mode = ReplayDataAgentMode.Auto,
            });

        Assert.Equal("not-needed", result.Status);
        Assert.Empty(result.PreparedData.PathValues);
    }

    [Fact]
    public async Task GeneratesEmptyJsonObjectForRequiredBodyWithoutExample()
    {
        DeterministicReplayDataPreparer preparer = new();

        var result = await preparer.PrepareAsync(
            new ReplayDataPreparationContext
            {
                Operation = new OpenApiOperation
                {
                    StableOperationKey = "POST /api/orders",
                    HttpMethod = "POST",
                    Route = "/api/orders",
                    HasJsonRequestBody = true,
                    RequestBodyRequired = true,
                },
                ResolvedOperation = new ResolvedReplayOperation
                {
                    OperationKey = "POST /api/orders",
                    HttpMethod = "POST",
                    Route = "/api/orders",
                },
                Mode = ReplayDataAgentMode.Auto,
            });

        Assert.Equal("generated", result.Status);
        Assert.Equal("{}", result.PreparedData.RequestBodyJson);
        Assert.Contains(result.Warnings, warning => warning.Contains("required request body"));
    }

    [Fact]
    public async Task WarnsForUnsupportedRequiredParameterLocation()
    {
        DeterministicReplayDataPreparer preparer = new();

        var result = await preparer.PrepareAsync(
            new ReplayDataPreparationContext
            {
                Operation = new OpenApiOperation
                {
                    StableOperationKey = "GET /api/orders",
                    HttpMethod = "GET",
                    Route = "/api/orders",
                    Parameters =
                    [
                        new OpenApiParameter
                        {
                            Name = "session",
                            Location = "cookie",
                            Required = true,
                            SchemaType = "string",
                        },
                    ],
                },
                ResolvedOperation = new ResolvedReplayOperation
                {
                    OperationKey = "GET /api/orders",
                    HttpMethod = "GET",
                    Route = "/api/orders",
                },
                Mode = ReplayDataAgentMode.Auto,
            });

        Assert.Equal("not-needed", result.Status);
        Assert.Contains(result.Warnings, warning => warning.Contains("Unsupported required OpenAPI parameter"));
        Assert.Empty(result.SourcesUsed);
        Assert.Empty(result.PreparedData.PathValues);
        Assert.Empty(result.PreparedData.QueryValues);
        Assert.Empty(result.PreparedData.HeaderValues);
    }
}
