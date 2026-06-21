using System;
using System.IO;
using Sqloom.TestApp.IntegrationTests;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises Sqloom replay argument parsing.
/// </summary>
public sealed class ReplayArgumentParserTests
{
    [Fact]
    public void Parse_WithSqlServerDacpac_ResolvesReplayLaunchOptions()
    {
        ReplayArgumentParser parser = new();
        var currentDirectory = CreateTempDirectory();
        var dacpacPath = Path.Combine(currentDirectory, "Talio.dacpac");
        var seedSqlPath = Path.Combine(currentDirectory, "Talio.seed.sql");
        var openApiPath = Path.Combine(currentDirectory, "openapi.json");
        File.WriteAllText(dacpacPath, "sqloom");
        File.WriteAllText(seedSqlPath, "SELECT 1;");
        File.WriteAllText(openApiPath, "{}");

        var arguments = parser.Parse(
            [
                "--sqlserver-dacpac-file",
                dacpacPath,
                "--sqlserver-seed-sql-file",
                seedSqlPath,
                "--openapi-file",
                openApiPath,
                "--target",
                TestAppProductCatalogScenario.OperationKey,
            ],
            new MultipleTestAppIntegrationA(),
            currentDirectory);

        Assert.Equal(
            Path.GetFullPath(openApiPath),
            arguments.RunnerOptions.OpenApiDocumentPath);
        Assert.Equal(TestAppProductCatalogScenario.OperationKey, arguments.RunnerOptions.TargetFilter);
        Assert.Equal(
            Path.GetFullPath(dacpacPath),
            arguments.RunnerOptions.ReplayLaunchOptions.SqlServerDacpacPath);
        Assert.Equal(
            Path.GetFullPath(seedSqlPath),
            arguments.RunnerOptions.ReplayLaunchOptions.SqlServerSeedSqlPath);
    }

    [Fact]
    public void Parse_ThrowsWhenSqlServerDacpacIsMissing()
    {
        ReplayArgumentParser parser = new();
        var currentDirectory = CreateTempDirectory();
        var missingDacpacPath = Path.Combine(currentDirectory, "missing.dacpac");

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "--sqlserver-dacpac-file",
                    missingDacpacPath,
                ],
                new MultipleTestAppIntegrationA(),
                currentDirectory));

        Assert.Contains("SQL Server DACPAC", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_ThrowsWhenSqlSeedScriptIsMissing()
    {
        ReplayArgumentParser parser = new();
        var currentDirectory = CreateTempDirectory();
        var dacpacPath = Path.Combine(currentDirectory, "Talio.dacpac");
        var missingSeedSqlPath = Path.Combine(currentDirectory, "missing.seed.sql");
        File.WriteAllText(dacpacPath, "sqloom");

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "--sqlserver-dacpac-file",
                    dacpacPath,
                    "--sqlserver-seed-sql-file",
                    missingSeedSqlPath,
                ],
                new MultipleTestAppIntegrationA(),
                currentDirectory));

        Assert.Contains("SQL seed script", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_ThrowsWhenSqlSeedScriptIsSuppliedWithoutDacpac()
    {
        ReplayArgumentParser parser = new();
        var currentDirectory = CreateTempDirectory();
        var seedSqlPath = Path.Combine(currentDirectory, "Talio.seed.sql");
        File.WriteAllText(seedSqlPath, "SELECT 1;");

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "--sqlserver-seed-sql-file",
                    seedSqlPath,
                ],
                new MultipleTestAppIntegrationA(),
                currentDirectory));

        Assert.Contains("--sqlserver-dacpac-file", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("--workload", "GET /api/expenses/dashboard")]
    [InlineData("--operation", "GET /api/expenses/dashboard")]
    public void Parse_RejectsLegacyOperationSwitches(string legacySwitch, string value)
    {
        ReplayArgumentParser parser = new();
        var currentDirectory = CreateTempDirectory();

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    legacySwitch,
                    value,
                ],
                new MultipleTestAppIntegrationA(),
                currentDirectory));

        Assert.Contains("Unsupported switch", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(legacySwitch, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("--openapi-path", "openapi.json")]
    [InlineData("--sqlserver-dacpac", "Talio.dacpac")]
    public void Parse_RejectsLegacyPathSwitches(string legacySwitch, string fileName)
    {
        ReplayArgumentParser parser = new();
        var currentDirectory = CreateTempDirectory();

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    legacySwitch,
                    Path.Combine(currentDirectory, fileName),
                ],
                new MultipleTestAppIntegrationA(),
                currentDirectory));

        Assert.Contains("Unsupported switch", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(legacySwitch, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("get /api/products/by-category", "The HTTP method must be uppercase.", TestAppProductCatalogScenario.OperationKey)]
    [InlineData("GET api/products/by-category", "The route template must start with '/'.", TestAppProductCatalogScenario.OperationKey)]
    [InlineData("GET /api/products/by-category/", "Do not include a trailing '/' in the route template.", TestAppProductCatalogScenario.OperationKey)]
    [InlineData("GET //api/products/by-category", "Do not include repeated '/' characters in the route template.", TestAppProductCatalogScenario.OperationKey)]
    public void Parse_RejectsMalformedTargetValues(
        string targetFilter,
        string expectedReason,
        string expectedSuggestion)
    {
        ReplayArgumentParser parser = new();
        var currentDirectory = CreateTempDirectory();

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "--target",
                    targetFilter,
                ],
                new MultipleTestAppIntegrationA(),
                currentDirectory));

        Assert.Contains("METHOD /path/template", exception.Message, StringComparison.Ordinal);
        Assert.Contains(expectedReason, exception.Message, StringComparison.Ordinal);
        Assert.Contains($"Did you mean '{expectedSuggestion}'?", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("expenses.dashboard")]
    [InlineData("GetSecure")]
    public void Parse_RejectsNonOperationKeyTargetValues(string targetFilter)
    {
        ReplayArgumentParser parser = new();
        var currentDirectory = CreateTempDirectory();

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "--target",
                    targetFilter,
                ],
                new MultipleTestAppIntegrationA(),
                currentDirectory));

        Assert.Contains("METHOD /path/template", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Did you mean", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("--query-store")]
    [InlineData("--replay")]
    [InlineData("--correlate")]
    [InlineData("--advise")]
    public void Parse_RejectsLegacyStageAliasSwitches(string legacySwitch)
    {
        ReplayArgumentParser parser = new();
        var currentDirectory = CreateTempDirectory();

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "replay",
                    legacySwitch,
                ],
                new MultipleTestAppIntegrationA(),
                currentDirectory));

        Assert.Contains("Unsupported switch", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(legacySwitch, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "sqloom-host-command-line-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
