using System;
using System.IO;
using System.Threading.Tasks;
using Sqloom.Core.Artifacts;
using Sqloom.Tests;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises DACPAC-backed SQL Server schema extraction.
/// </summary>
public sealed class SqlServerDacpacSchemaExtractorTests
{
    [Fact]
    public async Task ExtractAsync_UnpacksModelSqlIntoReplayArtifactDirectory()
    {
        var artifactDirectory = CreateTempDir();
        var expectedSchemaPath = ArtifactLayout.GetSqlServerSchemaPath(artifactDirectory);
        SqlServerDacpacSchemaExtractor extractor = new();

        var schemaPath = await extractor
            .ExtractAsync(
                RepositoryPaths.GetSampleApplicationDacpacPath(),
                artifactDirectory)
            ;

        Assert.Equal(expectedSchemaPath, schemaPath, StringComparer.OrdinalIgnoreCase);
        Assert.True(File.Exists(schemaPath), $"Expected generated schema at '{schemaPath}'.");

        var schemaSql = await File.ReadAllTextAsync(schemaPath);
        Assert.Contains("SalesLT", schemaSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Product", schemaSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExtractAsync_ThrowsWhenDacpacIsMissing()
    {
        var artifactDirectory = CreateTempDir();
        SqlServerDacpacSchemaExtractor extractor = new();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => extractor.ExtractAsync(
                Path.Combine(artifactDirectory, "missing.dacpac"),
                artifactDirectory));

        Assert.Contains("SQL Server DACPAC", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateTempDir()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "sqloom-dacpac-schema-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
