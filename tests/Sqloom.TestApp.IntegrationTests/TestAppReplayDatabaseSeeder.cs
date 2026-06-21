using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Sqloom.TestApp.IntegrationTests;

internal sealed class TestAppReplayDatabaseSeeder
{
    private const string SeedSql = """
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
                (1, NULL, N'Sqloom Hot Category', NEWID(), SYSUTCDATETIME()),
                (2, NULL, N'Sqloom Cold Category', NEWID(), SYSUTCDATETIME());
            SET IDENTITY_INSERT [SalesLT].[ProductCategory] OFF;
        END;

        IF NOT EXISTS (SELECT 1 FROM [SalesLT].[Product] WHERE [ProductNumber] = N'HOT-000001')
        BEGIN
            ;WITH [HotNumbers] AS
            (
                SELECT TOP (4000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS [Number]
                FROM sys.all_objects AS [a]
                CROSS JOIN sys.all_objects AS [b]
            )
            INSERT INTO [SalesLT].[Product] (
                [Name],
                [ProductNumber],
                [StandardCost],
                [ListPrice],
                [ProductCategoryID],
                [SellStartDate],
                [rowguid],
                [ModifiedDate])
            SELECT
                N'Sqloom Hot Product ' + CONVERT(nvarchar(10), [Number]),
                N'HOT-' + RIGHT(N'000000' + CONVERT(nvarchar(6), [Number]), 6),
                CONVERT(money, 10 + ([Number] % 9)),
                CONVERT(money, 100 + ([Number] % 900)),
                1,
                DATEADD(day, -([Number] % 30), SYSUTCDATETIME()),
                NEWID(),
                SYSUTCDATETIME()
            FROM [HotNumbers];
        END;

        IF NOT EXISTS (SELECT 1 FROM [SalesLT].[Product] WHERE [ProductNumber] = N'COLD-000001')
        BEGIN
            ;WITH [ColdNumbers] AS
            (
                SELECT TOP (1000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS [Number]
                FROM sys.all_objects AS [a]
                CROSS JOIN sys.all_objects AS [b]
            )
            INSERT INTO [SalesLT].[Product] (
                [Name],
                [ProductNumber],
                [StandardCost],
                [ListPrice],
                [ProductCategoryID],
                [SellStartDate],
                [rowguid],
                [ModifiedDate])
            SELECT
                N'Sqloom Cold Product ' + CONVERT(nvarchar(10), [Number]),
                N'COLD-' + RIGHT(N'000000' + CONVERT(nvarchar(6), [Number]), 6),
                CONVERT(money, 5 + ([Number] % 5)),
                CONVERT(money, 25 + ([Number] % 300)),
                2,
                DATEADD(day, -([Number] % 60), SYSUTCDATETIME()),
                NEWID(),
                SYSUTCDATETIME()
            FROM [ColdNumbers];
        END;
        """;

    public async Task SeedAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        SqlConnection connection = new(connectionString);
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            var command = connection.CreateCommand();
            await using (command.ConfigureAwait(false))
            {
                command.CommandText = SeedSql;
                command.CommandTimeout = TestAppReplayConstants.CommandTimeoutSeconds;
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
