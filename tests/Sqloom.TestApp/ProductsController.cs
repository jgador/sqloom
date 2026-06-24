using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Sqloom.TestApp;

/// <summary>
/// Exposes the seeded AdventureWorks sample query used by the Sqloom tuning tests.
/// </summary>
[ApiController]
[Route("api/products")]
public sealed class ProductsController
    : ControllerBase
{
    private readonly IProductCatalogService _productCatalogService;

    public ProductsController(IProductCatalogService productCatalogService)
    {
        _productCatalogService = productCatalogService;
    }

    /// <summary>
    /// Returns products in one category filtered by a minimum list price.
    /// </summary>
    [HttpGet("by-category")]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetByCategoryAsync(
        [FromQuery] int categoryId,
        [FromQuery] decimal minPrice,
        CancellationToken cancellationToken)
    {
        var products = await _productCatalogService
            .GetByCategoryAsync(categoryId, minPrice, cancellationToken)
            .ConfigureAwait(false);
        return Ok(products);
    }
}

/// <summary>
/// Returns one product row from the seeded sample catalog query.
/// </summary>
public sealed class ProductResponse
{
    [JsonPropertyName("productId")]
    public int ProductId { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("productNumber")]
    public string ProductNumber { get; init; } = string.Empty;

    [JsonPropertyName("listPrice")]
    public decimal ListPrice { get; init; }
}
