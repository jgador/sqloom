using System;
using System.IO;
using Sqloom.Core.Artifacts;
using Sqloom.Host.Replay;
using Sqloom.TestApp.Harness;
using Sqloom.Testing;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises Sqloom tune argument parsing.
/// </summary>
public sealed class TuneArgumentParserTests
{
    [Fact]
    public void Parse_BuildsWorkflowArtifactLayoutAndNestedStageArguments()
    {
        TuneArgumentParser parser = new();
        var currentDirectory = CreateTempDir();
        var workflowRoot = Path.Combine(currentDirectory, "custom-tune-run");
        var dacpacPath = Path.Combine(currentDirectory, "AdventureWorksLT2025.dacpac");
        var seedSqlPath = Path.Combine(currentDirectory, "AdventureWorksLT2025.seed.sql");
        var openApiPath = Path.Combine(currentDirectory, "openapi.json");
        var schemaPath = Path.Combine(currentDirectory, "manual.schema.sql");
        File.WriteAllText(dacpacPath, "sqloom");
        File.WriteAllText(seedSqlPath, "SELECT 1;");
        File.WriteAllText(openApiPath, "{}");
        File.WriteAllText(
            schemaPath,
            """
            CREATE TABLE [SalesLT].[Product] (
                [ProductID] INT NOT NULL,
                [ProductCategoryID] INT NULL,
                [ListPrice] MONEY NOT NULL
            );
            GO
            """);

        var arguments = parser.Parse(
            [
                "tune",
                "--read-only-connection-string",
                "Server=localhost;Database=Sqloom;Trusted_Connection=True;",
                "--artifact-dir",
                workflowRoot,
                "--lookback-hours",
                "6",
                "--app-only",
                "--sqlserver-dacpac-file",
                dacpacPath,
                "--sqlserver-seed-sql-file",
                seedSqlPath,
                "--openapi-file",
                openApiPath,
                "--target",
                CatalogScenario.OperationKey,
                "--replay-data-agent",
                "auto",
                "--replay-data-agent-model",
                "gpt-replay",
                "--model-provider",
                "openai",
                "--openai-api-key",
                "openai-key",
                "--sqlserver-schema-file",
                schemaPath,
            ],
            ManifestFactory.CreateManifest(),
            new ReplayHostFake(),
            "Server=localhost;Database=Sqloom;Trusted_Connection=True;",
            currentDirectory);

        var expectedWorkflowRoot = Path.GetFullPath(workflowRoot, currentDirectory);
        var expectedReplayDirectory = ArtifactLayout.GetTuneReplayArtifactDir(expectedWorkflowRoot);
        var expectedSnapshotPath = ArtifactLayout.GetTuneQueryStoreSnapshotPath(expectedWorkflowRoot);
        var expectedCorrelationPath = ArtifactLayout.GetCorrelationPath(expectedReplayDirectory);
        var expectedAdvicePath = ArtifactLayout.GetReplayTuningAdvicePath(expectedReplayDirectory);

        Assert.Equal(expectedWorkflowRoot, arguments.WorkflowArtifactDir, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(expectedSnapshotPath, arguments.ObserveArguments.JsonOutputPathOverride, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(expectedReplayDirectory, arguments.ReplayArguments.RunnerOptions.ReplayArtifactDir, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(TimeSpan.FromHours(6), arguments.ObserveArguments.ObservationOptions.LookbackWindow);
        Assert.Equal(100, arguments.ObserveArguments.ObservationOptions.MaxPlans);
        Assert.Equal(10, arguments.ObserveArguments.ObservationOptions.MaxWaits);
        Assert.Equal(30, arguments.ObserveArguments.ObservationOptions.CommandTimeoutSeconds);
        Assert.True(arguments.ObserveArguments.AppOnly);
        Assert.True(arguments.ObserveArguments.ShowClassification);
        Assert.Equal(
            Path.GetFullPath(openApiPath),
            arguments.ReplayArguments.RunnerOptions.OpenApiPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            Path.GetFullPath(dacpacPath),
            arguments.ReplayArguments.RunnerOptions.ReplayLaunchOptions.DacpacPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            Path.GetFullPath(seedSqlPath),
            arguments.ReplayArguments.RunnerOptions.ReplayLaunchOptions.SeedSqlPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(CatalogScenario.OperationKey, arguments.ReplayArguments.RunnerOptions.TargetFilter);
        Assert.Equal(ReplayDataAgentMode.Auto, arguments.ReplayArguments.RunnerOptions.ReplayDataAgentOptions.Mode);
        Assert.Equal("gpt-replay", arguments.ReplayArguments.RunnerOptions.ReplayDataAgentOptions.ModelName);
        Assert.IsType<AgentFrameworkReplayDataPreparer>(arguments.ReplayArguments.RunnerOptions.ReplayDataPreparer);
        Assert.Equal(expectedSnapshotPath, arguments.CorrelateArguments.QueryStoreSnapshotPath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(expectedReplayDirectory, arguments.CorrelateArguments.ReplayArtifactDir, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(expectedCorrelationPath, arguments.CorrelateArguments.JsonOutputPath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(expectedReplayDirectory, arguments.AdviseArguments.ReplayArtifactDir, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(expectedCorrelationPath, arguments.AdviseArguments.QueryStoreCorrelationPath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(Path.GetFullPath(schemaPath), arguments.AdviseArguments.SchemaPath, StringComparer.OrdinalIgnoreCase);
        Assert.Null(arguments.AdviseArguments.DacpacPath);
        Assert.Equal(expectedAdvicePath, arguments.AdviseArguments.JsonOutputPath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(ModelProviderKind.OpenAI, arguments.AdviseArguments.ModelProvider);
        Assert.NotNull(arguments.AdviseArguments.OpenAIOptions);
        Assert.Equal("openai-key", arguments.AdviseArguments.OpenAIOptions!.ApiKey);
        Assert.Equal("https://api.openai.com", arguments.AdviseArguments.OpenAIOptions.BaseUrl);
        Assert.Equal("gpt-5.4-mini", arguments.AdviseArguments.OpenAIOptions.Model);
    }

    [Fact]
    public void Parse_RejectsTuneJsonOutputFileOverride()
    {
        TuneArgumentParser parser = new();
        var currentDirectory = CreateTempDir();

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "tune",
                    "--read-only-connection-string",
                    "Server=localhost;Database=Sqloom;Trusted_Connection=True;",
                    "--json-output-file",
                    Path.Combine(currentDirectory, "tune-summary.json"),
                ],
                ManifestFactory.CreateManifest(),
            new ReplayHostFake(),
            "Server=localhost;Database=Sqloom;Trusted_Connection=True;",
                currentDirectory));

        Assert.Contains("Unsupported switch", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--json-output-file", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RequiresModelProvider()
    {
        TuneArgumentParser parser = new();
        var currentDirectory = CreateTempDir();

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "tune",
                    "--read-only-connection-string",
                    "Server=localhost;Database=Sqloom;Trusted_Connection=True;",
                ],
                ManifestFactory.CreateManifest(),
            new ReplayHostFake(),
            "Server=localhost;Database=Sqloom;Trusted_Connection=True;",
                currentDirectory));

        Assert.Contains("--model-provider", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithOpenAIModelProvider_RequiresApiKey()
    {
        TuneArgumentParser parser = new();
        var currentDirectory = CreateTempDir();

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "tune",
                    "--read-only-connection-string",
                    "Server=localhost;Database=Sqloom;Trusted_Connection=True;",
                    "--model-provider",
                    "openai",
                ],
                ManifestFactory.CreateManifest(),
            new ReplayHostFake(),
            "Server=localhost;Database=Sqloom;Trusted_Connection=True;",
                currentDirectory));

        Assert.Contains("--openai-api-key", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateBeforeSession_RejectsInvalidReplayDataAgentMode()
    {
        TuneArgumentParser parser = new();
        var currentDirectory = CreateTempDir();

        var exception = Assert.Throws<ArgumentException>(
            () => parser.ValidateBeforeSession(
                [
                    "tune",
                    "--replay-data-agent",
                    "sometimes",
                    "--model-provider",
                    "openai",
                    "--openai-api-key",
                    "openai-key",
                ],
                ManifestFactory.CreateManifest(),
                currentDirectory));

        Assert.Contains("--replay-data-agent", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("auto")]
    [InlineData("required")]
    public void ValidateBeforeSession_RequiresOpenAIKeyForEnabledReplayDataAgent(
        string replayDataAgentMode)
    {
        TuneArgumentParser parser = new();
        var currentDirectory = CreateTempDir();

        var exception = Assert.Throws<ArgumentException>(
            () => parser.ValidateBeforeSession(
                [
                    "tune",
                    "--replay-data-agent",
                    replayDataAgentMode,
                    "--model-provider",
                    "openai",
                ],
                ManifestFactory.CreateManifest(),
                currentDirectory));

        Assert.Contains("replay data agent", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--openai-api-key", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_UsesManifestDacpacForAdviceSchemaSource()
    {
        TuneArgumentParser parser = new();
        var currentDirectory = CreateTempDir();
        var dacpacPath = Path.Combine(currentDirectory, "AdventureWorksLT2025.dacpac");
        File.WriteAllText(dacpacPath, "sqloom");
        var manifest = new SqloomApplicationManifest
        {
            Name = "Sqloom Test Harness",
            OpenApiPath = Sqloom.Tests.RepositoryPaths.GetTestAppOpenApiPath(),
            ReplayProfile = ManifestFactory.CreateReplayProfile(),
            SqlServerDacpacPath = dacpacPath,
        };

        var arguments = parser.Parse(
            [
                "tune",
                "--read-only-connection-string",
                "Server=localhost;Database=Sqloom;Trusted_Connection=True;",
                "--model-provider",
                "openai",
                "--openai-api-key",
                "openai-key",
            ],
            manifest,
            new ReplayHostFake(),
            "Server=localhost;Database=Sqloom;Trusted_Connection=True;",
            currentDirectory);

        Assert.Null(arguments.AdviseArguments.SchemaPath);
        Assert.Equal(Path.GetFullPath(dacpacPath), arguments.AdviseArguments.DacpacPath, StringComparer.OrdinalIgnoreCase);
        Assert.Null(arguments.AdviseArguments.ReadOnlyConnectionString);
    }

    [Fact]
    public void Parse_UsesReadOnlyConnectionStringForAdviceSchemaSource()
    {
        TuneArgumentParser parser = new();
        var currentDirectory = CreateTempDir();
        const string readOnlyConnectionString =
            "Server=localhost;Database=Sqloom;Trusted_Connection=True;";

        var arguments = parser.Parse(
            [
                "tune",
                "--read-only-connection-string",
                readOnlyConnectionString,
                "--model-provider",
                "openai",
                "--openai-api-key",
                "openai-key",
            ],
            ManifestFactory.CreateManifest(),
            new ReplayHostFake(),
            readOnlyConnectionString,
            currentDirectory);

        Assert.Null(arguments.AdviseArguments.SchemaPath);
        Assert.Null(arguments.AdviseArguments.DacpacPath);
        Assert.Equal(readOnlyConnectionString, arguments.AdviseArguments.ReadOnlyConnectionString);
    }

    [Fact]
    public void ValidateBeforeSession_AllowsSessionReadOnlyConnectionSchemaSource()
    {
        TuneArgumentParser parser = new();
        var currentDirectory = CreateTempDir();

        parser.ValidateBeforeSession(
            [
                "tune",
                "--model-provider",
                "openai",
                "--openai-api-key",
                "openai-key",
            ],
            ManifestFactory.CreateManifest(),
            currentDirectory);
    }

    [Fact]
    public void RejectsLegacyAdviceProviderSwitch()
    {
        TuneArgumentParser parser = new();
        var currentDirectory = CreateTempDir();

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "tune",
                    "--read-only-connection-string",
                    "Server=localhost;Database=Sqloom;Trusted_Connection=True;",
                    "--advice-provider",
                    "openai",
                ],
                ManifestFactory.CreateManifest(),
            new ReplayHostFake(),
            "Server=localhost;Database=Sqloom;Trusted_Connection=True;",
                currentDirectory));

        Assert.Contains("Unsupported switch", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--advice-provider", exception.Message, StringComparison.OrdinalIgnoreCase);
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
