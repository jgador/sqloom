using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using Sqloom.Pipeline.Artifacts;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises live DacFx extraction from a localhost SQL Server database.
/// </summary>
public sealed class SqlServerDacpacSchemaExtractorLiveTests
{
    private const string LocalhostConnectionString =
        "Server=localhost;Database=AdventureWorksLT2025;Integrated Security=True;TrustServerCertificate=True;ApplicationIntent=ReadOnly";

    [Fact(Explicit = true)]
    [Trait("Category", "Integration")]
    [Trait("Category", "LocalSqlServer")]
    public async Task WithStaticLocalhostConnectionString_ExportsDacpacAndUnpacksSchema()
    {
        // Run with: dotnet test .\tests\Sqloom.IntegrationTests\Sqloom.IntegrationTests.csproj -- --filter-class Sqloom.Host.Tests.SqlServerDacpacSchemaExtractorLiveTests --explicit only
        SqlConnectionStringBuilder connectionBuilder = new(LocalhostConnectionString);

        var artifactDirectory = CreateArtifactDirectory();
        var exportedDacpacPath = Path.Combine(
            artifactDirectory,
            "localhost-extracted.dacpac");
        var expectedSchemaPath = ArtifactLayout.GetSqlServerSchemaPath(artifactDirectory);
        DeleteFileIfExists(exportedDacpacPath);
        DeleteFileIfExists(expectedSchemaPath);

        DacServices dacServices = new(LocalhostConnectionString);
        dacServices.Extract(
            targetPath: exportedDacpacPath,
            databaseName: connectionBuilder.InitialCatalog,
            applicationName: "Sqloom Localhost Extraction Test",
            applicationVersion: new Version(1, 0, 0),
            applicationDescription: "Extracted by Sqloom integration tests.",
            tables: null,
            extractOptions: new DacExtractOptions
            {
                VerifyExtraction = true,
            },
            cancellationToken: CancellationToken.None);

        FileInfo exportedDacpac = new(exportedDacpacPath);
        Assert.True(
            exportedDacpac.Exists,
            $"Expected exported DACPAC at '{exportedDacpacPath}'.");
        Assert.True(
            exportedDacpac.Length > 0,
            $"Expected exported DACPAC '{exportedDacpacPath}' to be non-empty.");

        SqlServerDacpacSchemaExtractor extractor = new();
        var schemaPath = await extractor.ExtractAsync(
            exportedDacpacPath,
            artifactDirectory,
            CancellationToken.None);

        Assert.Equal(
            expectedSchemaPath,
            schemaPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.True(File.Exists(schemaPath), $"Expected generated schema at '{schemaPath}'.");

        var schemaSql = await File.ReadAllTextAsync(schemaPath, CancellationToken.None);
        Assert.Contains("SalesLT", schemaSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Product", schemaSql, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateArtifactDirectory()
    {
        var directoryPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "artifacts",
            "sqloom",
            "integration-tests",
            "dacpac-extraction"));
        Directory.CreateDirectory(directoryPath);
        return directoryPath;
    }

    private static void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
