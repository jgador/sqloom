using System;
using System.Collections.Generic;
using System.IO;
using Sqloom.Core.Artifacts;

namespace Sqloom.Host;

/// <summary>
/// Parses and validates the Sqloom advise command arguments.
/// </summary>
internal sealed class AdviseArgumentParser
{
    private static readonly HashSet<string> SupportedSwitches = new(StringComparer.OrdinalIgnoreCase)
    {
        "--replay-artifact-dir",
        "--query-store-correlation-file",
        "--sqlserver-schema-file",
        "--json-output-file",
        "--model-provider",
        "--openai-model",
        "--openai-base-url",
        "--openai-api-key",
    };

    private static readonly HashSet<string> ValueSwitches = new(StringComparer.OrdinalIgnoreCase)
    {
        "--replay-artifact-dir",
        "--query-store-correlation-file",
        "--sqlserver-schema-file",
        "--json-output-file",
        "--model-provider",
        "--openai-model",
        "--openai-base-url",
        "--openai-api-key",
    };

    public AdviseArguments Parse(string[] args)
    {
        CommandArgumentSupport.ValidateArguments(
            args,
            HostCommandKind.Advise,
            SupportedSwitches,
            ValueSwitches);

        var replayArtifactDirectory = Path.GetFullPath(
            CommandArgumentSupport.GetRequiredArgumentValue(args, "--replay-artifact-dir"));
        if (!Directory.Exists(replayArtifactDirectory))
        {
            throw new ArgumentException(
                $"The replay artifact directory '{replayArtifactDirectory}' does not exist.");
        }

        var queryStoreCorrelationPath = CommandArgumentSupport.GetArgumentValue(args, "--query-store-correlation-file") is { } correlationPathOverride
            ? Path.GetFullPath(correlationPathOverride)
            : ArtifactLayout.GetCorrelationPath(replayArtifactDirectory);
        if (!File.Exists(queryStoreCorrelationPath))
        {
            throw new ArgumentException(
                $"The Query Store correlation artifact '{queryStoreCorrelationPath}' does not exist.");
        }

        var jsonOutputPath = CommandArgumentSupport.GetArgumentValue(args, "--json-output-file") is { } jsonOutputPathOverride
            ? Path.GetFullPath(jsonOutputPathOverride)
            : ArtifactLayout.GetReplayTuningAdvicePath(replayArtifactDirectory);
        return CreateArguments(
            args,
            replayArtifactDirectory,
            queryStoreCorrelationPath,
            jsonOutputPath);
    }

    internal AdviseArguments CreateArguments(
        string[] args,
        string replayArtifactDirectory,
        string queryStoreCorrelationPath,
        string jsonOutputPath,
        string? defaultSchemaPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(queryStoreCorrelationPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonOutputPath);

        var modelProvider = ParseModelProvider(CommandArgumentSupport.GetRequiredArgumentValue(args, "--model-provider"));
        var openAIOptions = ResolveOpenAIAdviceOptions(args);
        var sqlServerSchemaPath = ResolveSchemaPath(
            args,
            defaultSchemaPath);

        return new AdviseArguments
        {
            ReplayArtifactDir = replayArtifactDirectory,
            QueryStoreCorrelationPath = queryStoreCorrelationPath,
            SchemaPath = sqlServerSchemaPath,
            JsonOutputPath = jsonOutputPath,
            ModelProvider = modelProvider,
            OpenAIOptions = openAIOptions,
        };
    }

    internal static ModelProviderKind ParseModelProvider(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Missing required argument --model-provider.");
        }

        return normalized.ToLowerInvariant() switch
        {
            "openai" => ModelProviderKind.OpenAI,
            _ => throw new ArgumentException(
                "The value for --model-provider must be 'openai'."),
        };
    }

    internal static OpenAIAdviceOptions ResolveOpenAIAdviceOptions(string[] args)
    {
        var apiKey = CommandArgumentSupport.GetArgumentValue(args, "--openai-api-key");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException(
                "Sqloom advice with --model-provider openai requires --openai-api-key.");
        }

        var baseUrl = CommandArgumentSupport.GetArgumentValue(args, "--openai-base-url")
            ?? "https://api.openai.com";
        var model = CommandArgumentSupport.GetArgumentValue(args, "--openai-model")
            ?? "gpt-5.4-mini";

        return new OpenAIAdviceOptions
        {
            ApiKey = apiKey,
            BaseUrl = baseUrl,
            Model = model,
        };
    }

    private static string ResolveSchemaPath(
        string[] args,
        string? defaultSchemaPath)
    {
        var rawSchemaPath = CommandArgumentSupport.GetArgumentValue(args, "--sqlserver-schema-file")
            ?? defaultSchemaPath;
        if (string.IsNullOrWhiteSpace(rawSchemaPath))
        {
            throw new ArgumentException("Missing required argument --sqlserver-schema-file.");
        }

        var sqlServerSchemaPath = Path.GetFullPath(
            rawSchemaPath);
        if (!File.Exists(sqlServerSchemaPath))
        {
            throw new ArgumentException(
                $"The SQL Server schema file '{sqlServerSchemaPath}' does not exist.");
        }

        return sqlServerSchemaPath;
    }
}
