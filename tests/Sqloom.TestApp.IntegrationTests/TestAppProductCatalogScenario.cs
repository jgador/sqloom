namespace Sqloom.TestApp.IntegrationTests;

/// <summary>
/// Defines the seeded product-catalog replay scenario used by the sample Sqloom harness.
/// </summary>
public static class TestAppProductCatalogScenario
{
    public const string Route = "/api/products/by-category";
    public const string OperationKey = "GET /api/products/by-category";
    public const int HotCategoryId = 1;
    public const int ColdCategoryId = 2;
    public const decimal ReplayMinPrice = 900m;
    public const string ReplayMinPriceText = "900";
    public const int HotProductCount = 4000;
    public const int ColdProductCount = 1000;

    public static string CreateRequestPath()
    {
        return $"{Route}?categoryId={HotCategoryId}&minPrice={ReplayMinPriceText}";
    }
}
