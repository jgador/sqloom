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
    public void ThrowsWhenCorrelationArtifactIsMissing()
    {
        var replayDirectory = CreateTempDir();

        var exception = Assert.Throws<ArgumentException>(
            () => AdviseArgumentParser.Parse(
                [
                    "--replay-artifact-dir",
                    replayDirectory,
                ]));

        Assert.Contains("Query Store correlation artifact", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithOpenAIModelProvider_ResolvesExplicitOptions()
    {
        var replayDirectory = CreateTempDir();
        var correlationPath = Path.Combine(replayDirectory, "query-store-correlation.json");
        File.WriteAllText(correlationPath, "{}");
        var schemaPath = CreateSchemaFile(replayDirectory);
        var jsonOutputPath = Path.Combine(replayDirectory, "custom-advice.json");

        var arguments = AdviseArgumentParser.Parse(
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

        Assert.NotNull(arguments.OpenAIOptions);
        Assert.Equal("openai-key", arguments.OpenAIOptions!.ApiKey);
        Assert.Equal("https://api.openai.com", arguments.OpenAIOptions.BaseUrl);
        Assert.Equal("gpt-5.4-mini", arguments.OpenAIOptions.Model);
        Assert.Equal(schemaPath, arguments.SchemaPath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(jsonOutputPath, arguments.JsonOutputPath, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithOpenAIModelProvider_AcceptsDacpacSchema()
    {
        var replayDirectory = CreateTempDir();
        var correlationPath = Path.Combine(replayDirectory, "query-store-correlation.json");
        File.WriteAllText(correlationPath, "{}");
        var dacpacPath = CreateDacpacFile(replayDirectory);

        var arguments = AdviseArgumentParser.Parse(
            [
                "--replay-artifact-dir",
                replayDirectory,
                "--query-store-correlation-file",
                correlationPath,
                "--model-provider",
                "openai",
                "--openai-api-key",
                "openai-key",
                "--sqlserver-dacpac-file",
                dacpacPath,
            ]);

        Assert.Null(arguments.SchemaPath);
        Assert.Equal(dacpacPath, arguments.DacpacPath, StringComparer.OrdinalIgnoreCase);
        Assert.Null(arguments.ReadOnlyConnectionString);
    }

    [Fact]
    public void WithOpenAIModelProvider_AcceptsReadOnlyConnectionSchemaSource()
    {
        var replayDirectory = CreateTempDir();
        var correlationPath = Path.Combine(replayDirectory, "query-store-correlation.json");
        File.WriteAllText(correlationPath, "{}");
        const string readOnlyConnectionString =
            "Server=localhost;Database=Sqloom;Trusted_Connection=True;";

        var arguments = AdviseArgumentParser.Parse(
            [
                "--replay-artifact-dir",
                replayDirectory,
                "--query-store-correlation-file",
                correlationPath,
                "--model-provider",
                "openai",
                "--openai-api-key",
                "openai-key",
                "--read-only-connection-string",
                readOnlyConnectionString,
            ]);

        Assert.Null(arguments.SchemaPath);
        Assert.Null(arguments.DacpacPath);
        Assert.Equal(readOnlyConnectionString, arguments.ReadOnlyConnectionString);
    }

    [Fact]
    public void WithOpenAIModelProvider_PrefersSchemaFile()
    {
        var replayDirectory = CreateTempDir();
        var correlationPath = Path.Combine(replayDirectory, "query-store-correlation.json");
        File.WriteAllText(correlationPath, "{}");
        var schemaPath = CreateSchemaFile(replayDirectory);
        var dacpacPath = CreateDacpacFile(replayDirectory);

        var arguments = AdviseArgumentParser.Parse(
            [
                "--replay-artifact-dir",
                replayDirectory,
                "--query-store-correlation-file",
                correlationPath,
                "--model-provider",
                "openai",
                "--openai-api-key",
                "openai-key",
                "--sqlserver-schema-file",
                schemaPath,
                "--sqlserver-dacpac-file",
                dacpacPath,
            ]);

        Assert.Equal(schemaPath, arguments.SchemaPath, StringComparer.OrdinalIgnoreCase);
        Assert.Null(arguments.DacpacPath);
    }

    [Fact]
    public void WithOpenAIModelProvider_UsesDefaultBaseUrlAndModel()
    {
        var replayDirectory = CreateTempDir();
        var correlationPath = Path.Combine(replayDirectory, "query-store-correlation.json");
        File.WriteAllText(correlationPath, "{}");
        var schemaPath = CreateSchemaFile(replayDirectory);
        var jsonOutputPath = Path.Combine(replayDirectory, "custom-advice.json");

        var arguments = AdviseArgumentParser.Parse(
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

        Assert.NotNull(arguments.OpenAIOptions);
        Assert.Equal("openai-key", arguments.OpenAIOptions!.ApiKey);
        Assert.Equal("https://api.openai.com", arguments.OpenAIOptions.BaseUrl);
        Assert.Equal("gpt-5.4-mini", arguments.OpenAIOptions.Model);
        Assert.Equal(schemaPath, arguments.SchemaPath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(jsonOutputPath, arguments.JsonOutputPath, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void RequiresModelProvider()
    {
        var replayDirectory = CreateTempDir();
        var correlationPath = Path.Combine(replayDirectory, "query-store-correlation.json");
        File.WriteAllText(correlationPath, "{}");
        var schemaPath = CreateSchemaFile(replayDirectory);

        var exception = Assert.Throws<ArgumentException>(
            () => AdviseArgumentParser.Parse(
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
    public void WithOpenAIModelProvider_RequiresApiKey()
    {
        var replayDirectory = CreateTempDir();
        var correlationPath = Path.Combine(replayDirectory, "query-store-correlation.json");
        File.WriteAllText(correlationPath, "{}");
        var schemaPath = CreateSchemaFile(replayDirectory);

        var exception = Assert.Throws<ArgumentException>(
            () => AdviseArgumentParser.Parse(
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
    public void WithOpenAIModelProvider_RequiresSchemaSource()
    {
        var replayDirectory = CreateTempDir();
        var correlationPath = Path.Combine(replayDirectory, "query-store-correlation.json");
        File.WriteAllText(correlationPath, "{}");

        var exception = Assert.Throws<ArgumentException>(
            () => AdviseArgumentParser.Parse(
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
        Assert.Contains("DACPAC", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--read-only-connection-string", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RejectsLegacyAdviceProviderSwitch()
    {
        var replayDirectory = CreateTempDir();
        var correlationPath = Path.Combine(replayDirectory, "query-store-correlation.json");
        File.WriteAllText(correlationPath, "{}");

        var exception = Assert.Throws<ArgumentException>(
            () => AdviseArgumentParser.Parse(
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
    public void RejectsLegacyCorrelationSwitch()
    {
        var replayDirectory = CreateTempDir();
        var correlationPath = Path.Combine(replayDirectory, "query-store-correlation.json");
        File.WriteAllText(correlationPath, "{}");

        var exception = Assert.Throws<ArgumentException>(
            () => AdviseArgumentParser.Parse(
                [
                    "--replay-artifact-dir",
                    replayDirectory,
                    "--query-store-correlation",
                    correlationPath,
                ]));

        Assert.Contains("Unsupported switch", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--query-store-correlation", exception.Message, StringComparison.OrdinalIgnoreCase);
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

    private static string CreateDacpacFile(string directory)
    {
        var path = Path.Combine(directory, "schema-source.dacpac");
        File.WriteAllText(path, "sqloom");
        return path;
    }
}
