using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises SQL Server DACPAC export validation.
/// </summary>
public sealed class SqlServerDacpacExporterTests
{
    [Fact]
    public async Task ExportAsync_RequiresDatabaseName()
    {
        SqlServerDacpacExporter exporter = new();
        var artifactDirectory = CreateTempDir();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => exporter.ExportAsync(
                "Server=localhost;Integrated Security=True;TrustServerCertificate=True",
                artifactDirectory));

        Assert.Contains("database", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateTempDir()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "sqloom-dacpac-exporter-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
