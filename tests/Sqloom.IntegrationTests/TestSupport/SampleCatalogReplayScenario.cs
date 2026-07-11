namespace Sqloom.Host.Tests;

internal static class SampleCatalogReplayScenario
{
    public const string Route = "/api/products/by-category";
    public const string OperationKey = "GET /api/products/by-category";
    public const int HotCategoryId = 1;
    public const string MinPriceText = "900";

    public static string CreateRequestPath()
    {
        return $"{Route}?categoryId={HotCategoryId}&minPrice={MinPriceText}";
    }
}
