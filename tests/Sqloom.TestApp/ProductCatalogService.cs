using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Sqloom.TestApp;

public sealed class ProductCatalogService
{
    private readonly TestAppProductCatalogDbContext _dbContext;

    public ProductCatalogService(TestAppProductCatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ProductResponse>> GetByCategoryAsync(
        int categoryId,
        decimal minPrice,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Where(product => product.ProductCategoryId == categoryId && product.ListPrice >= minPrice)
            .OrderByDescending(product => product.ListPrice)
            .Select(product => new ProductResponse
            {
                ProductId = product.ProductId,
                Name = product.Name,
                ProductNumber = product.ProductNumber,
                ListPrice = product.ListPrice,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
