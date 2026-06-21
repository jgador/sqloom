using System;
using System.IO;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises Sqloom advise argument parsing.
/// </summary>
public sealed class AdviseArgumentParserTests
{
    [Fact]
    public void Parse_ThrowsWhenCorrelationArtifactIsMissing()
    {
        AdviseArgumentParser parser = new();
        var replayDirectory = CreateTempDirectory();

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "--replay-artifact-dir",
                    replayDirectory,
                ]));

        Assert.Contains("Query Store correlation artifact", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_WithOpenAIModelProvider_ResolvesExplicitOpenAIOptions()
    {
        AdviseArgumentParser parser = new();
        var replayDirectory = CreateTempDirectory();
        var correlationPath = Path.Combine(replayDirectory, "query-store-correlation.json");
        File.WriteAllText(correlationPath, "{}");
        var schemaPath = CreateSchemaFile(replayDirectory);
        var jsonOutputPath = Path.Combine(replayDirectory, "custom-advice.json");

        var arguments = parser.Parse(
            [
                "--replay-artifact-dir",
                replayDirectory,
                "--query-store-correlation-file",
                correlationPath,
                "--json-output-file",
                jsonOutputPath,
                "--model-provider",
                "openai",
                "--openai-api-key",
                "openai-key",
                "--sqlserver-schema-file",
                schemaPath,
                "--openai-base-url",
                "https://api.openai.com",
                "--openai-model",
                "gpt-5.4-mini",
            ]);

        Assert.Equal(ModelProviderKind.OpenAI, arguments.ModelProvider);
        Assert.NotNull(arguments.OpenAIOptions);
        Assert.Equal("openai-key", arguments.OpenAIOptions!.ApiKey);
        Assert.Equal("https://api.openai.com", arguments.OpenAIOptions.BaseUrl);
        Assert.Equal("gpt-5.4-mini", arguments.OpenAIOptions.Model);
        Assert.Equal(schemaPath, arguments.SqlServerSchemaPath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(jsonOutputPath, arguments.JsonOutputPath, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_WithOpenAIModelProvider_UsesDefaultBaseUrlAndModelWhenNotSpecified()
    {
        AdviseArgumentParser parser = new();
        var replayDirectory = CreateTempDirectory();
        var correlationPath = Path.Combine(replayDirectory, "query-store-correlation.json");
        File.WriteAllText(correlationPath, "{}");
        var schemaPath = CreateSchemaFile(replayDirectory);
        var jsonOutputPath = Path.Combine(replayDirectory, "custom-advice.json");

        var arguments = parser.Parse(
            [
                "--replay-artifact-dir",
                replayDirectory,
                "--query-store-correlation-file",
                correlationPath,
                "--json-output-file",
                jsonOutputPath,
                "--model-provider",
                "openai",
                "--openai-api-key",
                "openai-key",
                "--sqlserver-schema-file",
                schemaPath,
            ]);

        Assert.Equal(ModelProviderKind.OpenAI, arguments.ModelProvider);
        Assert.NotNull(arguments.OpenAIOptions);
        Assert.Equal("openai-key", arguments.OpenAIOptions!.ApiKey);
        Assert.Equal("https://api.openai.com", arguments.OpenAIOptions.BaseUrl);
        Assert.Equal("gpt-5.4-mini", arguments.OpenAIOptions.Model);
        Assert.Equal(schemaPath, arguments.SqlServerSchemaPath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(jsonOutputPath, arguments.JsonOutputPath, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_RequiresModelProvider()
    {
        AdviseArgumentParser parser = new();
        var replayDirectory = CreateTempDirectory();
        var correlationPath = Path.Combine(replayDirectory, "query-store-correlation.json");
        File.WriteAllText(correlationPath, "{}");
        var schemaPath = CreateSchemaFile(replayDirectory);

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "--replay-artifact-dir",
                    replayDirectory,
                    "--query-store-correlation-file",
                    correlationPath,
                    "--sqlserver-schema-file",
                    schemaPath,
                ]));

        Assert.Contains("--model-provider", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_WithOpenAIModelProvider_RequiresApiKey()
    {
        AdviseArgumentParser parser = new();
        var replayDirectory = CreateTempDirectory();
        var correlationPath = Path.Combine(replayDirectory, "query-store-correlation.json");
        File.WriteAllText(correlationPath, "{}");
        var schemaPath = CreateSchemaFile(replayDirectory);

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "--replay-artifact-dir",
                    replayDirectory,
                    "--query-store-correlation-file",
                    correlationPath,
                    "--model-provider",
                    "openai",
                    "--sqlserver-schema-file",
                    schemaPath,
                ]));

        Assert.Contains("--openai-api-key", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_WithOpenAIModelProvider_RequiresSchemaFile()
    {
        AdviseArgumentParser parser = new();
        var replayDirectory = CreateTempDirectory();
        var correlationPath = Path.Combine(replayDirectory, "query-store-correlation.json");
        File.WriteAllText(correlationPath, "{}");

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "--replay-artifact-dir",
                    replayDirectory,
                    "--query-store-correlation-file",
                    correlationPath,
                    "--model-provider",
                    "openai",
                    "--openai-api-key",
                    "openai-key",
                ]));

        Assert.Contains("--sqlserver-schema-file", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_RejectsLegacyAdviceProviderSwitch()
    {
        AdviseArgumentParser parser = new();
        var replayDirectory = CreateTempDirectory();
        var correlationPath = Path.Combine(replayDirectory, "query-store-correlation.json");
        File.WriteAllText(correlationPath, "{}");

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "--replay-artifact-dir",
                    replayDirectory,
                    "--query-store-correlation-file",
                    correlationPath,
                    "--advice-provider",
                    "openai",
                ]));

        Assert.Contains("Unsupported switch", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--advice-provider", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_RejectsLegacyCorrelationSwitch()
    {
        AdviseArgumentParser parser = new();
        var replayDirectory = CreateTempDirectory();
        var correlationPath = Path.Combine(replayDirectory, "query-store-correlation.json");
        File.WriteAllText(correlationPath, "{}");

        var exception = Assert.Throws<ArgumentException>(
            () => parser.Parse(
                [
                    "--replay-artifact-dir",
                    replayDirectory,
                    "--query-store-correlation",
                    correlationPath,
                ]));

        Assert.Contains("Unsupported switch", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--query-store-correlation", exception.Message, StringComparison.OrdinalIgnoreCase);
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

    private static string CreateSchemaFile(string directory)
    {
        var path = Path.Combine(directory, "schema.sql");
        File.WriteAllText(
            path,
            """
            CREATE TABLE [dbo].[ExpenseRecord] (
                [Id] INT NOT NULL,
                [UserId] UNIQUEIDENTIFIER NOT NULL
            );
            GO
            """);
        return path;
    }
}
