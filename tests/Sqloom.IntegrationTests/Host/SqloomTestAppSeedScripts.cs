namespace Sqloom.Host.Tests;

internal static class SqloomTestAppSeedScripts
{
    public static string CreateCustomSeedScript()
    {
        return """
            IF NOT EXISTS (SELECT 1 FROM [SalesLT].[ProductCategory] WHERE [ProductCategoryID] = 1)
            BEGIN
                SET IDENTITY_INSERT [SalesLT].[ProductCategory] ON;
                INSERT INTO [SalesLT].[ProductCategory] (
                    [ProductCategoryID],
                    [ParentProductCategoryID],
                    [Name],
                    [rowguid],
                    [ModifiedDate])
                VALUES
                    (1, NULL, N'Custom Seed Category', NEWID(), SYSUTCDATETIME());
                SET IDENTITY_INSERT [SalesLT].[ProductCategory] OFF;
            END;
            GO

            IF NOT EXISTS (SELECT 1 FROM [SalesLT].[Product] WHERE [ProductNumber] = N'SEED-900001')
            BEGIN
                SET IDENTITY_INSERT [SalesLT].[Product] ON;
                INSERT INTO [SalesLT].[Product] (
                    [ProductID],
                    [Name],
                    [ProductNumber],
                    [StandardCost],
                    [ListPrice],
                    [ProductCategoryID],
                    [SellStartDate],
                    [rowguid],
                    [ModifiedDate])
                VALUES
                    (900001, N'Custom Seed Product 1', N'SEED-900001', CONVERT(money, 10), CONVERT(money, 950), 1, SYSUTCDATETIME(), NEWID(), SYSUTCDATETIME()),
                    (900002, N'Custom Seed Product 2', N'SEED-900002', CONVERT(money, 12), CONVERT(money, 975), 1, SYSUTCDATETIME(), NEWID(), SYSUTCDATETIME());
                SET IDENTITY_INSERT [SalesLT].[Product] OFF;
            END;
            GO
            """;
    }
}
