using System;
using System.IO;
using Sqloom.Core.Execution;
using Sqloom.Testing;
using Sqloom.TestApp.Harness;
using Sqloom.Tests;
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
        var currentDirectory = CreateTempDir();
        var dacpacPath = Path.Combine(currentDirectory, "SqloomTestApp.dacpac");
        var seedSqlPath = Path.Combine(currentDirectory, "SqloomTestApp.seed.sql");
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
                CatalogScenario.OperationKey,
            ],
            ManifestFactory.CreateManifest(),
            new ReplayHostFake(),
            currentDirectory);

        Assert.Equal(
            Path.GetFullPath(openApiPath),
            arguments.RunnerOptions.OpenApiPath);
        Assert.Equal(CatalogScenario.OperationKey, arguments.RunnerOptions.TargetFilter);
        Assert.Equal(
            Path.GetFullPath(dacpacPath),
            arguments.RunnerOptions.ReplayLaunchOptions.DacpacPath);
        Assert.Equal(
            Path.GetFullPath(seedSqlPath),
            arguments.RunnerOptions.ReplayLaunchOptions.SeedSqlPath);
    }

    [Fact]
    public void Parse_UsesManifestOpenApiPathByDefault()
    {
        ReplayArgumentParser parser = new();
        var currentDirectory = CreateTempDir();

        var arguments = parser.Parse(
            [],
            ManifestFactory.CreateManifest(),
            new ReplayHostFake(),
            currentDirectory);

        Assert.Equal(
            RepositoryPaths.GetTestAppOpenApiPath(),
            arguments.RunnerOptions.OpenApiPath,
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_ThrowsWhenManifestOpenApiPathIsRelative()
    {
        ReplayArgumentParser parser = new();
        var currentDirectory = CreateTempDir();
        SqloomApplicationManifest manifest = new()
        {
            Name = "Relative OpenAPI Test App",
            OpenApiPath = "openapi.json",
            ReplayProfile = new ReplayProfile(),
        };

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [],
                manifest,
                new ReplayHostFake(),
                currentDirectory));

        Assert.Contains("OpenApiPath", exception.Message, StringComparison.Ordinal);
        Assert.Contains("absolute", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_ThrowsWhenSqlServerDacpacIsMissing()
    {
        ReplayArgumentParser parser = new();
        var currentDirectory = CreateTempDir();
        var missingDacpacPath = Path.Combine(currentDirectory, "missing.dacpac");

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "--sqlserver-dacpac-file",
                    missingDacpacPath,
                ],
                ManifestFactory.CreateManifest(),
            new ReplayHostFake(),
            currentDirectory));

        Assert.Contains("SQL Server DACPAC", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_ThrowsWhenSqlSeedScriptIsMissing()
    {
        ReplayArgumentParser parser = new();
        var currentDirectory = CreateTempDir();
        var dacpacPath = Path.Combine(currentDirectory, "SqloomTestApp.dacpac");
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
                ManifestFactory.CreateManifest(),
            new ReplayHostFake(),
            currentDirectory));

        Assert.Contains("SQL seed script", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_ThrowsWhenSqlSeedScriptIsSuppliedWithoutDacpac()
    {
        ReplayArgumentParser parser = new();
        var currentDirectory = CreateTempDir();
        var seedSqlPath = Path.Combine(currentDirectory, "SqloomTestApp.seed.sql");
        File.WriteAllText(seedSqlPath, "SELECT 1;");

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "--sqlserver-seed-sql-file",
                    seedSqlPath,
                ],
                ManifestFactory.CreateManifest(),
            new ReplayHostFake(),
            currentDirectory));

        Assert.Contains("--sqlserver-dacpac-file", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("--workload", "GET /api/expenses/dashboard")]
    [InlineData("--operation", "GET /api/expenses/dashboard")]
    public void Parse_RejectsLegacyOperationSwitches(string legacySwitch, string value)
    {
        ReplayArgumentParser parser = new();
        var currentDirectory = CreateTempDir();

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    legacySwitch,
                    value,
                ],
                ManifestFactory.CreateManifest(),
            new ReplayHostFake(),
            currentDirectory));

        Assert.Contains("Unsupported switch", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(legacySwitch, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("--openapi-path", "openapi.json")]
    [InlineData("--sqlserver-dacpac", "SqloomTestApp.dacpac")]
    public void Parse_RejectsLegacyPathSwitches(string legacySwitch, string fileName)
    {
        ReplayArgumentParser parser = new();
        var currentDirectory = CreateTempDir();

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    legacySwitch,
                    Path.Combine(currentDirectory, fileName),
                ],
                ManifestFactory.CreateManifest(),
            new ReplayHostFake(),
            currentDirectory));

        Assert.Contains("Unsupported switch", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(legacySwitch, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("get /api/products/by-category", "The HTTP method must be uppercase.", CatalogScenario.OperationKey)]
    [InlineData("GET api/products/by-category", "The route template must start with '/'.", CatalogScenario.OperationKey)]
    [InlineData("GET /api/products/by-category/", "Do not include a trailing '/' in the route template.", CatalogScenario.OperationKey)]
    [InlineData("GET //api/products/by-category", "Do not include repeated '/' characters in the route template.", CatalogScenario.OperationKey)]
    public void Parse_RejectsMalformedTargetValues(
        string targetFilter,
        string expectedReason,
        string expectedSuggestion)
    {
        ReplayArgumentParser parser = new();
        var currentDirectory = CreateTempDir();

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "--target",
                    targetFilter,
                ],
                ManifestFactory.CreateManifest(),
            new ReplayHostFake(),
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
        var currentDirectory = CreateTempDir();

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "--target",
                    targetFilter,
                ],
                ManifestFactory.CreateManifest(),
            new ReplayHostFake(),
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
        var currentDirectory = CreateTempDir();

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "replay",
                    legacySwitch,
                ],
                ManifestFactory.CreateManifest(),
            new ReplayHostFake(),
            currentDirectory));

        Assert.Contains("Unsupported switch", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(legacySwitch, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateTempDir()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "sqloom-host-command-line-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
