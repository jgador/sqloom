using System;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.AspNetCore.Endpoints;
using Sqloom.AspNetCore.OpenApi;
using Sqloom.Core.Execution;
using Xunit;

namespace Sqloom.AspNetCore.Tests.Endpoints;

/// <summary>
/// Exercises endpoint replay plan builder.
/// </summary>
public sealed class EndpointReplayPlanBuilderTests
{
    [Fact]
    public void BuildInitialPlan_PlansAuthenticatedGetOperationsWithoutAppOverlays()
    {
        EndpointReplayPlanBuilder builder = new();
        var plan = builder.BuildInitialPlan(
            CreateOptions(
                new ReplayProfile
                {
                    DefaultOpenApiDocumentPath = "openapi.json",
                }),
            [
                CreateOperation("GET", "/api/secure", requiresAuthentication: true),
            ]);

        var item = Assert.Single(plan.Operations);
        Assert.Equal("planned", item.Status);
        Assert.Equal("GET /api/secure", item.OperationKey);
        Assert.Null(item.Reason);
    }

    [Fact]
    public void BuildInitialPlan_SkipsUnsafeOperationsWithoutReplayOverlays()
    {
        EndpointReplayPlanBuilder builder = new();
        var plan = builder.BuildInitialPlan(
            CreateOptions(
                new ReplayProfile
                {
                    DefaultOpenApiDocumentPath = "openapi.json",
                }),
            [
                CreateOperation("GET", "/api/public", requiresAuthentication: false),
                CreateOperation("POST", "/api/items", requiresAuthentication: true),
            ]);

        Assert.Collection(
            plan.Operations,
            item =>
            {
                Assert.Equal("skipped", item.Status);
                Assert.Equal("Anonymous operations are not replayed by default in V1.", item.Reason);
            },
            item =>
            {
                Assert.Equal("skipped", item.Status);
                Assert.Equal("Non-GET operations require an explicit replay overlay.", item.Reason);
            });
    }

    [Fact]
    public void BuildInitialPlan_SkipsOptInReplayWhenItWasNotSelectedExplicitly()
    {
        EndpointReplayPlanBuilder builder = new();
        var plan = builder.BuildInitialPlan(
            CreateOptions(
                new ReplayProfile
                {
                    DefaultOpenApiDocumentPath = "openapi.json",
                    OperationOverlays =
                    [
                        new ReplayOperationOverlayDefinition
                        {
                            OperationKey = "POST /api/advisor/query",
                            ReplayByDefault = false,
                            AllowNonGetReplay = true,
                        },
                    ],
                }),
            [
                CreateOperation(
                    "POST",
                    "/api/advisor/query",
                    requiresAuthentication: true,
                    requestBodyRequired: true),
            ]);

        var item = Assert.Single(plan.Operations);
        Assert.Equal("skipped", item.Status);
        Assert.Equal(
            "Skipped because operation 'POST /api/advisor/query' is opt-in. Select it with --target \"POST /api/advisor/query\".",
            item.Reason);
    }

    [Fact]
    public void BuildInitialPlan_AllowsOptInReplayWhenTargetMatchesOperationKey()
    {
        EndpointReplayPlanBuilder builder = new();
        var plan = builder.BuildInitialPlan(
            CreateOptions(
                new ReplayProfile
                {
                    DefaultOpenApiDocumentPath = "openapi.json",
                    OperationOverlays =
                    [
                        new ReplayOperationOverlayDefinition
                        {
                            OperationKey = "POST /api/advisor/query",
                            ReplayByDefault = false,
                            AllowNonGetReplay = true,
                        },
                    ],
                },
                targetFilter: "POST /api/advisor/query"),
            [
                CreateOperation(
                    "POST",
                    "/api/advisor/query",
                    requiresAuthentication: true,
                    requestBodyRequired: true),
            ]);

        var item = Assert.Single(plan.Operations);
        Assert.Equal("planned", item.Status);
        Assert.Null(item.Reason);
    }

    [Fact]
    public void BuildInitialPlan_ThrowsWhenTargetMatchesOperationIdInsteadOfOperationKey()
    {
        EndpointReplayPlanBuilder builder = new();
        var exception = Assert.Throws<ArgumentException>(
            () => builder.BuildInitialPlan(
                CreateOptions(
                    new ReplayProfile
                    {
                        DefaultOpenApiDocumentPath = "openapi.json",
                    },
                    targetFilter: "GetSecure"),
                [
                    CreateOperation(
                        "GET",
                        "/api/secure",
                        requiresAuthentication: true,
                        operationId: "GetSecure"),
                ]));

        Assert.Contains("METHOD /path/template", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildInitialPlan_ThrowsWhenTargetDoesNotMatchAnyOperation()
    {
        EndpointReplayPlanBuilder builder = new();
        var exception = Assert.Throws<ArgumentException>(
            () => builder.BuildInitialPlan(
                CreateOptions(
                    new ReplayProfile
                    {
                        DefaultOpenApiDocumentPath = "openapi.json",
                    },
                    targetFilter: "GET /api/expense/dashboard"),
                [
                    CreateOperation(
                        "GET",
                        "/api/expenses/dashboard",
                        requiresAuthentication: true,
                        operationId: "GetSecure"),
                ]));

        Assert.Contains("No replay operation matched", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Did you mean 'GET /api/expenses/dashboard'?", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildInitialPlan_ThrowsWhenTargetUsesInvalidOperationKeySyntax()
    {
        EndpointReplayPlanBuilder builder = new();

        var exception = Assert.Throws<ArgumentException>(
            () => builder.BuildInitialPlan(
                CreateOptions(
                    new ReplayProfile
                    {
                        DefaultOpenApiDocumentPath = "openapi.json",
                    },
                    targetFilter: "get api/secure"),
                [
                    CreateOperation("GET", "/api/secure", requiresAuthentication: true),
                ]));

        Assert.Contains("METHOD /path/template", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Did you mean 'GET /api/secure'?", exception.Message, StringComparison.Ordinal);
    }

    private static EndpointReplayRunnerOptions CreateOptions(
        ReplayProfile replayProfile,
        string? targetFilter = null)
    {
        return new EndpointReplayRunnerOptions
        {
            AppName = "TestApp",
            OpenApiDocumentPath = "openapi.json",
            ReplayArtifactDirectory = "artifacts",
            ReplayProfile = replayProfile,
            ReplayHostFactory = new UnusedReplayHostFactory(),
            TargetFilter = targetFilter,
        };
    }

    private static DiscoveredOpenApiOperation CreateOperation(
        string httpMethod,
        string route,
        bool requiresAuthentication,
        bool requestBodyRequired = false,
        string? operationId = null)
    {
        return new DiscoveredOpenApiOperation
        {
            StableOperationKey = $"{httpMethod} {route}",
            OperationId = operationId,
            HttpMethod = httpMethod,
            Route = route,
            RequiresAuthentication = requiresAuthentication,
            HasJsonRequestBody = requestBodyRequired,
            RequestBodyRequired = requestBodyRequired,
        };
    }

    /// <summary>
    /// Provides an unused replay host factory placeholder for plan-builder tests.
    /// </summary>
    private sealed class UnusedReplayHostFactory : IReplayHostFactory
    {
        public Task<IReplayHost> CreateAsync(
            ReplayLaunchOptions? launchOptions = null,
            CancellationToken cancellationToken = default)
        {
            throw new Xunit.Sdk.XunitException("The replay host should not be created in plan builder tests.");
        }
    }
}
