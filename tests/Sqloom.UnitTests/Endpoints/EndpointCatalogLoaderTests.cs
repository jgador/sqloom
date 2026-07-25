using System;
using System.Linq;
using System.Threading.Tasks;
using Sqloom.Host.Replay;
using Sqloom.Tests;
using Xunit;

namespace Sqloom.Host.Tests.Replay;

/// <summary>
/// Exercises Roslyn-powered endpoint catalog discovery.
/// </summary>
public sealed class EndpointCatalogLoaderTests
{
    [Fact]
    public async Task LoadAsync_ReadsControllerMethodAndParameterMetadata()
    {
        EndpointCatalogLoader loader = new();

        var operations = await loader.LoadAsync(RepositoryPaths.GetTestAppProjectPath());

        var operation = Assert.Single(
            operations,
            operation => string.Equals(
                operation.StableOperationKey,
                SampleCatalogReplayScenario.OperationKey,
                StringComparison.Ordinal));
        Assert.Equal("GET", operation.HttpMethod);
        Assert.Equal(SampleCatalogReplayScenario.Route, operation.Route);
        Assert.Equal("GetByCategoryAsync", operation.MethodName);
        Assert.EndsWith("Sqloom.TestApp.ProductsController", operation.ControllerType, StringComparison.Ordinal);
        Assert.NotNull(operation.MethodSymbolId);
        Assert.True(operation.RequiresAuthentication);
        Assert.False(operation.HasJsonRequestBody);

        var categoryId = Assert.Single(
            operation.Parameters,
            parameter => string.Equals(parameter.Name, "categoryId", StringComparison.Ordinal));
        Assert.Equal("query", categoryId.Location);
        Assert.True(categoryId.Required);
        Assert.Equal("integer", categoryId.SchemaType);
        Assert.Equal("int", categoryId.ClrType);

        var minPrice = Assert.Single(
            operation.Parameters,
            parameter => string.Equals(parameter.Name, "minPrice", StringComparison.Ordinal));
        Assert.Equal("query", minPrice.Location);
        Assert.True(minPrice.Required);
        Assert.Equal("number", minPrice.SchemaType);
        Assert.Equal("decimal", minPrice.ClrType);
    }
}
