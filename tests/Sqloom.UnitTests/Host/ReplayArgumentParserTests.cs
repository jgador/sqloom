using System;
using System.IO;
using Sqloom.Host.Replay;
using Sqloom.Pipeline.Execution;
using Sqloom.Testing;
using Sqloom.Tests;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises Sqloom replay argument parsing.
/// </summary>
public sealed class ReplayArgumentParserTests
{
    [Fact]
    public void WithSqlServerDacpac_ResolvesReplayLaunchOptions()
    {
        var currentDirectory = CreateTempDir();
        var dacpacPath = Path.Combine(currentDirectory, "SqloomTestApp.dacpac");
        var seedSqlPath = Path.Combine(currentDirectory, "SqloomTestApp.seed.sql");
        var sourceProjectPath = RepositoryPaths.GetTestAppProjectPath();
        File.WriteAllText(dacpacPath, "sqloom");
        File.WriteAllText(seedSqlPath, "SELECT 1;");

        var arguments = ReplayArgumentParser.Parse(
            [
                "--sqlserver-dacpac-file",
                dacpacPath,
                "--sqlserver-seed-sql-file",
                seedSqlPath,
                "--app-project",
                sourceProjectPath,
                "--target",
                SampleCatalogReplayScenario.OperationKey,
                "--openai-api-key",
                "openai-key",
            ],
            ManifestFactory.CreateManifest(),
            new ReplayHostFake(),
            currentDirectory);

        Assert.Equal(
            Path.GetFullPath(sourceProjectPath),
            arguments.RunnerOptions.SourceProjectPath);
        Assert.Equal(SampleCatalogReplayScenario.OperationKey, arguments.RunnerOptions.TargetFilter);
        Assert.Equal(
            Path.GetFullPath(dacpacPath),
            arguments.RunnerOptions.ReplayLaunchOptions.DacpacPath);
        Assert.Equal(
            Path.GetFullPath(seedSqlPath),
            arguments.RunnerOptions.ReplayLaunchOptions.SeedSqlPath);
    }

    [Fact]
    public void UsesAppProjectPathByDefault()
    {
        var currentDirectory = CreateTempDir();
        var sourceProjectPath = RepositoryPaths.GetTestAppProjectPath();

        var arguments = ReplayArgumentParser.Parse(
            [
                "--app-project",
                sourceProjectPath,
                "--openai-api-key",
                "openai-key",
            ],
            ManifestFactory.CreateManifest(),
            new ReplayHostFake(),
            currentDirectory);

        Assert.Equal(
            sourceProjectPath,
            arguments.RunnerOptions.SourceProjectPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(ReplayDataAgentMode.Required, arguments.RunnerOptions.ReplayDataAgentOptions.Mode);
        Assert.IsType<AgentFrameworkReplayDataGenerator>(arguments.RunnerOptions.ReplayDataGenerator);
    }

    [Fact]
    public void WithReplayDataAgentOff_DisablesAgentGenerator()
    {
        var currentDirectory = CreateTempDir();

        var arguments = ReplayArgumentParser.Parse(
            [
                "--replay-data-agent",
                "off",
            ],
            ManifestFactory.CreateManifest(),
            new ReplayHostFake(),
            currentDirectory,
            sourceProjectPathOverride: RepositoryPaths.GetTestAppProjectPath());

        Assert.Equal(ReplayDataAgentMode.Off, arguments.RunnerOptions.ReplayDataAgentOptions.Mode);
        Assert.Null(arguments.RunnerOptions.ReplayDataGenerator);
    }

    [Fact]
    public void WithReplayDataAgentAuto_CreatesAgentGenerator()
    {
        var currentDirectory = CreateTempDir();

        var arguments = ReplayArgumentParser.Parse(
            [
                "--replay-data-agent",
                "auto",
                "--replay-data-agent-model",
                "gpt-test",
                "--openai-api-key",
                "openai-key",
            ],
            ManifestFactory.CreateManifest(),
            new ReplayHostFake(),
            currentDirectory,
            sourceProjectPathOverride: RepositoryPaths.GetTestAppProjectPath());

        Assert.Equal(ReplayDataAgentMode.Auto, arguments.RunnerOptions.ReplayDataAgentOptions.Mode);
        Assert.Equal("gpt-test", arguments.RunnerOptions.ReplayDataAgentOptions.ModelName);
        Assert.IsType<AgentFrameworkReplayDataGenerator>(arguments.RunnerOptions.ReplayDataGenerator);
    }

    [Fact]
    public void WithReplayDataAgentAuto_RequiresOpenAIKey()
    {
        var currentDirectory = CreateTempDir();

        var exception = Assert.Throws<ArgumentException>(
            () => ReplayArgumentParser.Parse(
                [
                    "--replay-data-agent",
                    "auto",
                ],
                ManifestFactory.CreateManifest(),
                new ReplayHostFake(),
                currentDirectory,
                sourceProjectPathOverride: RepositoryPaths.GetTestAppProjectPath()));

        Assert.Contains("--openai-api-key", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithRequiredReplayDataAgent_CreatesAgentGenerator()
    {
        var currentDirectory = CreateTempDir();

        var arguments = ReplayArgumentParser.Parse(
            [
                "--replay-data-agent",
                "required",
                "--openai-api-key",
                "openai-key",
                "--openai-base-url",
                "https://api.openai.com",
            ],
            ManifestFactory.CreateManifest(),
            new ReplayHostFake(),
            currentDirectory,
            sourceProjectPathOverride: RepositoryPaths.GetTestAppProjectPath());

        Assert.Equal(ReplayDataAgentMode.Required, arguments.RunnerOptions.ReplayDataAgentOptions.Mode);
        Assert.Equal("gpt-5.4-mini", arguments.RunnerOptions.ReplayDataAgentOptions.ModelName);
        Assert.IsType<AgentFrameworkReplayDataGenerator>(arguments.RunnerOptions.ReplayDataGenerator);
    }

    [Fact]
    public void WithReplayDataAgentRequired_RequiresOpenAIKey()
    {
        var currentDirectory = CreateTempDir();

        var exception = Assert.Throws<ArgumentException>(
            () => ReplayArgumentParser.Parse(
                [
                    "--replay-data-agent",
                    "required",
                ],
                ManifestFactory.CreateManifest(),
                new ReplayHostFake(),
                currentDirectory,
                sourceProjectPathOverride: RepositoryPaths.GetTestAppProjectPath()));

        Assert.Contains("--openai-api-key", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RejectsInvalidReplayDataAgentMode()
    {
        var currentDirectory = CreateTempDir();

        var exception = Assert.Throws<ArgumentException>(
            () => ReplayArgumentParser.Parse(
                [
                    "--replay-data-agent",
                    "always",
                ],
                ManifestFactory.CreateManifest(),
                new ReplayHostFake(),
                currentDirectory,
                sourceProjectPathOverride: RepositoryPaths.GetTestAppProjectPath()));

        Assert.Contains("--replay-data-agent", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThrowsWhenSourceProjectPathIsMissing()
    {
        var currentDirectory = CreateTempDir();

        var exception = Assert.Throws<ArgumentException>(
            () => ReplayArgumentParser.Parse(
                [],
                ManifestFactory.CreateManifest(),
                new ReplayHostFake(),
                currentDirectory));

        Assert.Contains("source project", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--app-project", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThrowsWhenSqlServerDacpacIsMissing()
    {
        var currentDirectory = CreateTempDir();
        var missingDacpacPath = Path.Combine(currentDirectory, "missing.dacpac");

        var exception = Assert.Throws<ArgumentException>(
            () => ReplayArgumentParser.Parse(
                [
                    "--sqlserver-dacpac-file",
                    missingDacpacPath,
                ],
                ManifestFactory.CreateManifest(),
                new ReplayHostFake(),
                currentDirectory,
                sourceProjectPathOverride: RepositoryPaths.GetTestAppProjectPath()));

        Assert.Contains("SQL Server DACPAC", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThrowsWhenSqlSeedScriptIsMissing()
    {
        var currentDirectory = CreateTempDir();
        var dacpacPath = Path.Combine(currentDirectory, "SqloomTestApp.dacpac");
        var missingSeedSqlPath = Path.Combine(currentDirectory, "missing.seed.sql");
        File.WriteAllText(dacpacPath, "sqloom");

        var exception = Assert.Throws<ArgumentException>(
            () => ReplayArgumentParser.Parse(
                [
                    "--sqlserver-dacpac-file",
                    dacpacPath,
                    "--sqlserver-seed-sql-file",
                    missingSeedSqlPath,
                ],
                ManifestFactory.CreateManifest(),
                new ReplayHostFake(),
                currentDirectory,
                sourceProjectPathOverride: RepositoryPaths.GetTestAppProjectPath()));

        Assert.Contains("SQL seed script", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThrowsWhenSqlSeedScriptIsSuppliedWithoutDacpac()
    {
        var currentDirectory = CreateTempDir();
        var seedSqlPath = Path.Combine(currentDirectory, "SqloomTestApp.seed.sql");
        File.WriteAllText(seedSqlPath, "SELECT 1;");

        var exception = Assert.Throws<ArgumentException>(
            () => ReplayArgumentParser.Parse(
                [
                    "--sqlserver-seed-sql-file",
                    seedSqlPath,
                ],
                ManifestFactory.CreateManifest(),
                new ReplayHostFake(),
                currentDirectory,
                sourceProjectPathOverride: RepositoryPaths.GetTestAppProjectPath()));

        Assert.Contains("--sqlserver-dacpac-file", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("--workload", "GET /api/expenses/dashboard")]
    [InlineData("--operation", "GET /api/expenses/dashboard")]
    public void RejectsLegacyOperationSwitches(string legacySwitch, string value)
    {
        var currentDirectory = CreateTempDir();

        var exception = Assert.Throws<ArgumentException>(
            () => ReplayArgumentParser.Parse(
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
    [InlineData("--openapi-file", "openapi.json")]
    [InlineData("--openapi-path", "openapi.json")]
    [InlineData("--sqlserver-dacpac", "SqloomTestApp.dacpac")]
    public void RejectsLegacyPathSwitches(string legacySwitch, string fileName)
    {
        var currentDirectory = CreateTempDir();

        var exception = Assert.Throws<ArgumentException>(
            () => ReplayArgumentParser.Parse(
                [
                    legacySwitch,
                    Path.Combine(currentDirectory, fileName),
                ],
                ManifestFactory.CreateManifest(),
                new ReplayHostFake(),
                currentDirectory,
                sourceProjectPathOverride: RepositoryPaths.GetTestAppProjectPath()));

        Assert.Contains("Unsupported switch", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(legacySwitch, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("get /api/products/by-category", "The HTTP method must be uppercase.", SampleCatalogReplayScenario.OperationKey)]
    [InlineData("GET api/products/by-category", "The route template must start with '/'.", SampleCatalogReplayScenario.OperationKey)]
    [InlineData("GET /api/products/by-category/", "Do not include a trailing '/' in the route template.", SampleCatalogReplayScenario.OperationKey)]
    [InlineData("GET //api/products/by-category", "Do not include repeated '/' characters in the route template.", SampleCatalogReplayScenario.OperationKey)]
    public void RejectsMalformedTargetValues(
        string targetFilter,
        string expectedReason,
        string expectedSuggestion)
    {
        var currentDirectory = CreateTempDir();

        var exception = Assert.Throws<ArgumentException>(
            () => ReplayArgumentParser.Parse(
                [
                    "--target",
                    targetFilter,
                ],
                ManifestFactory.CreateManifest(),
                new ReplayHostFake(),
                currentDirectory,
                sourceProjectPathOverride: RepositoryPaths.GetTestAppProjectPath()));

        Assert.Contains("METHOD /path/template", exception.Message, StringComparison.Ordinal);
        Assert.Contains(expectedReason, exception.Message, StringComparison.Ordinal);
        Assert.Contains($"Did you mean '{expectedSuggestion}'?", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("expenses.dashboard")]
    [InlineData("GetSecure")]
    public void RejectsNonOperationKeyTargetValues(string targetFilter)
    {
        var currentDirectory = CreateTempDir();

        var exception = Assert.Throws<ArgumentException>(
            () => ReplayArgumentParser.Parse(
                [
                    "--target",
                    targetFilter,
                ],
                ManifestFactory.CreateManifest(),
                new ReplayHostFake(),
                currentDirectory,
                sourceProjectPathOverride: RepositoryPaths.GetTestAppProjectPath()));

        Assert.Contains("METHOD /path/template", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Did you mean", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("--query-store")]
    [InlineData("--replay")]
    [InlineData("--correlate")]
    [InlineData("--advise")]
    public void RejectsLegacyStageAliasSwitches(string legacySwitch)
    {
        var currentDirectory = CreateTempDir();

        var exception = Assert.Throws<ArgumentException>(
            () => ReplayArgumentParser.Parse(
                [
                    "replay",
                    legacySwitch,
                ],
                ManifestFactory.CreateManifest(),
                new ReplayHostFake(),
                currentDirectory,
                sourceProjectPathOverride: RepositoryPaths.GetTestAppProjectPath()));

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
