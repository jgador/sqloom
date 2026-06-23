using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Sqloom.Correlation.QueryStore;
using Sqloom.QueryStore.QueryStore;

namespace Sqloom.Host;

/// <summary>
/// Prints Sqloom stage diagnostics to stderr when debug output is enabled.
/// </summary>
internal sealed class HostDebugWriter
{
    private static readonly string[] SecretConnectionStringKeys =
    [
        "password",
        "pwd",
        "token",
        "secret",
        "key",
    ];

    public static HostDebugWriter Disabled { get; } = new(false);

    public HostDebugWriter(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }

    public bool IsEnabled { get; }

    public void PrintObserveRun(
        ObserveArguments arguments,
        string jsonOutputPath,
        QueryStoreSnapshot snapshot)
    {
        if (!IsEnabled)
        {
            return;
        }

        PrintBlock(
            "observe",
            "resolved inputs",
            [
                $"connection_string={RedactConnectionString(arguments.ReadOnlyConnection)}",
                $"lookback_hours={arguments.ObservationOptions.LookbackWindow.TotalHours.ToString("F1", CultureInfo.InvariantCulture)}",
                $"max_plans={arguments.ObservationOptions.MaxPlans.ToString(CultureInfo.InvariantCulture)}",
                $"max_waits={arguments.ObservationOptions.MaxWaits.ToString(CultureInfo.InvariantCulture)}",
                $"command_timeout_seconds={arguments.ObservationOptions.CommandTimeoutSeconds.ToString(CultureInfo.InvariantCulture)}",
                $"workload_profile={arguments.BaseWorkloadProfile.Name}",
                $"app_only={arguments.AppOnly}",
                $"show_classification={arguments.ShowClassification}",
                $"json_output_path={jsonOutputPath}",
                $"captured_plans={snapshot.Plans.Count.ToString(CultureInfo.InvariantCulture)}",
                $"captured_waits={snapshot.Waits.Count.ToString(CultureInfo.InvariantCulture)}",
            ]);
    }

    public void PrintReplayRun(ReplayArguments arguments)
    {
        if (!IsEnabled)
        {
            return;
        }

        var options = arguments.RunnerOptions;
        PrintBlock(
            "replay",
            "resolved inputs",
            [
                $"app={options.AppName}",
                $"openapi_document={options.OpenApiPath}",
                $"artifact_directory={options.ReplayArtifactDir}",
                $"max_operations={options.MaxOperations.ToString(CultureInfo.InvariantCulture)}",
                $"target_filter={options.TargetFilter ?? "default"}",
                $"sqlserver_dacpac={options.ReplayLaunchOptions.DacpacPath ?? "none"}",
                $"sqlserver_seed_sql={options.ReplayLaunchOptions.SeedSqlPath ?? "none"}",
            ]);
    }

    public void PrintCorrelationRun(
        CorrelateArguments arguments,
        QueryCorrelationReport report)
    {
        if (!IsEnabled)
        {
            return;
        }

        PrintBlock(
            "correlate",
            "resolved inputs",
            [
                $"connection_string={RedactConnectionString(arguments.ConnectionString)}",
                $"replay_artifact_directory={arguments.ReplayArtifactDir}",
                $"query_store_snapshot={arguments.QueryStoreSnapshotPath}",
                $"json_output_path={arguments.JsonOutputPath}",
                $"operation_count={report.Summary.OperationCount.ToString(CultureInfo.InvariantCulture)}",
                $"captured_command_count={report.Summary.CapturedCommandCount.ToString(CultureInfo.InvariantCulture)}",
                $"matched_command_count={report.Summary.MatchedCommandCount.ToString(CultureInfo.InvariantCulture)}",
                $"unmatched_count={report.Summary.UnmatchedCount.ToString(CultureInfo.InvariantCulture)}",
            ]);
    }

    public void PrintAdviceRun(AdviseArguments arguments)
    {
        if (!IsEnabled)
        {
            return;
        }

        PrintBlock(
            "advise",
            "resolved inputs",
            [
                $"replay_artifact_directory={arguments.ReplayArtifactDir}",
                $"query_store_correlation={arguments.QueryStoreCorrelationPath}",
                $"sqlserver_schema_file={arguments.SchemaPath}",
                $"json_output_path={arguments.JsonOutputPath}",
                $"model_provider={arguments.ModelProvider}",
                $"openai_base_url={arguments.OpenAIOptions?.BaseUrl ?? "n/a"}",
                $"openai_model={arguments.OpenAIOptions?.Model ?? "n/a"}",
            ]);
    }

    public void PrintTuneRun(TuneArguments arguments)
    {
        if (!IsEnabled)
        {
            return;
        }

        PrintBlock(
            "tune",
            "resolved workflow",
            [
                $"workflow_artifact_directory={arguments.WorkflowArtifactDir}",
                $"query_store_snapshot={arguments.CorrelateArguments.QueryStoreSnapshotPath}",
                $"replay_artifact_directory={arguments.ReplayArguments.RunnerOptions.ReplayArtifactDir}",
                $"query_store_correlation={arguments.CorrelateArguments.JsonOutputPath}",
                $"tuning_advice={arguments.AdviseArguments.JsonOutputPath}",
                $"sqlserver_schema_file={arguments.AdviseArguments.SchemaPath}",
                $"model_provider={arguments.AdviseArguments.ModelProvider}",
                $"openai_model={arguments.AdviseArguments.OpenAIOptions?.Model ?? "n/a"}",
            ]);
    }

    public void PrintTuneStageStarting(string stageName)
    {
        if (!IsEnabled)
        {
            return;
        }

        PrintBlock(
            "tune",
            "stage starting",
            [$"stage={stageName}"]);
    }

    public void PrintTuneStageCompleted(
        string stageName,
        string artifactPath)
    {
        if (!IsEnabled)
        {
            return;
        }

        PrintBlock(
            "tune",
            "stage completed",
            [
                $"stage={stageName}",
                $"artifact={artifactPath}",
            ]);
    }

    public void PrintOpenAIRequest(
        Uri requestUri,
        AuthenticationHeaderValue? authorizationHeader,
        string requestJson)
    {
        if (!IsEnabled)
        {
            return;
        }

        PrintBlock(
            "advise",
            "OpenAI request",
            [
                "method=POST",
                $"url={FormatUri(requestUri)}",
                $"authorization={FormatAuthorizationHeader(authorizationHeader)}",
                "body=",
            ],
            requestJson);
    }

    public void PrintOpenAIResponse(
        HttpStatusCode statusCode,
        string responseJson)
    {
        if (!IsEnabled)
        {
            return;
        }

        PrintBlock(
            "advise",
            "OpenAI response",
            [
                $"status={(int)statusCode} {statusCode}",
                "body=",
            ],
            responseJson);
    }

    private static void PrintBlock(
        string stage,
        string title,
        IReadOnlyList<string> lines,
        string? body = null)
    {
        Console.Error.WriteLine($"[sqloom debug] [{stage}] {title}");
        foreach (var line in lines)
        {
            Console.Error.WriteLine($"  {line}");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return;
        }

        using StringReader reader = new(FormatJson(body));
        while (reader.ReadLine() is { } line)
        {
            Console.Error.WriteLine($"    {line}");
        }
    }

    private static string FormatAuthorizationHeader(AuthenticationHeaderValue? authorizationHeader)
    {
        if (authorizationHeader is null)
        {
            return "none";
        }

        return string.IsNullOrWhiteSpace(authorizationHeader.Scheme)
            ? "***REDACTED***"
            : $"{authorizationHeader.Scheme} ***REDACTED***";
    }

    private static string FormatUri(Uri value)
    {
        return value.IsAbsoluteUri
            ? value.AbsoluteUri
            : value.OriginalString;
    }

    private static string FormatJson(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "<empty>";
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(value);
            return string.Join(
                Environment.NewLine,
                RenderElementLines(
                    document.RootElement,
                    indentLevel: 0));
        }
        catch (JsonException)
        {
            return value;
        }
    }

    private static IReadOnlyList<string> RenderElementLines(
        JsonElement element,
        int indentLevel)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => RenderObjectLines(element, indentLevel),
            JsonValueKind.Array => RenderArrayLines(element, indentLevel),
            JsonValueKind.String => RenderAnonymousStringLines(
                element.GetString() ?? string.Empty,
                indentLevel),
            JsonValueKind.Number => [Indent(indentLevel) + element.GetRawText()],
            JsonValueKind.True => [Indent(indentLevel) + "true"],
            JsonValueKind.False => [Indent(indentLevel) + "false"],
            JsonValueKind.Null => [Indent(indentLevel) + "null"],
            _ => [Indent(indentLevel) + element.GetRawText()],
        };
    }

    private static IReadOnlyList<string> RenderObjectLines(
        JsonElement element,
        int indentLevel)
    {
        var properties = element.EnumerateObject().ToArray();
        if (properties.Length == 0)
        {
            return [Indent(indentLevel) + "{}"];
        }

        List<string> lines =
        [
            Indent(indentLevel) + "{",
        ];
        foreach (var property in properties)
        {
            lines.AddRange(RenderNamedValueLines(
                property.Name,
                property.Value,
                indentLevel + 1));
        }

        lines.Add(Indent(indentLevel) + "}");
        return lines;
    }

    private static IReadOnlyList<string> RenderArrayLines(
        JsonElement element,
        int indentLevel)
    {
        JsonElement[] items = [.. element.EnumerateArray()];
        if (items.Length == 0)
        {
            return [Indent(indentLevel) + "[]"];
        }

        List<string> lines =
        [
            Indent(indentLevel) + "[",
        ];
        foreach (var item in items)
        {
            lines.AddRange(RenderElementLines(item, indentLevel + 1));
        }

        lines.Add(Indent(indentLevel) + "]");
        return lines;
    }

    private static IReadOnlyList<string> RenderNamedValueLines(
        string propertyName,
        JsonElement value,
        int indentLevel)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Object => RenderNamedCompositeLines(
                propertyName,
                RenderObjectLines(value, indentLevel + 1),
                indentLevel),
            JsonValueKind.Array => RenderNamedCompositeLines(
                propertyName,
                RenderArrayLines(value, indentLevel + 1),
                indentLevel),
            JsonValueKind.String => RenderNamedStringLines(
                propertyName,
                value.GetString() ?? string.Empty,
                indentLevel),
            JsonValueKind.Number => [PropertyPrefix(propertyName, indentLevel) + " " + value.GetRawText()],
            JsonValueKind.True => [PropertyPrefix(propertyName, indentLevel) + " true"],
            JsonValueKind.False => [PropertyPrefix(propertyName, indentLevel) + " false"],
            JsonValueKind.Null => [PropertyPrefix(propertyName, indentLevel) + " null"],
            _ => [PropertyPrefix(propertyName, indentLevel) + " " + value.GetRawText()],
        };
    }

    private static IReadOnlyList<string> RenderNamedCompositeLines(
        string propertyName,
        IReadOnlyList<string> valueLines,
        int indentLevel)
    {
        List<string> lines =
        [
            PropertyPrefix(propertyName, indentLevel),
        ];
        lines.AddRange(valueLines);
        return lines;
    }

    private static IReadOnlyList<string> RenderNamedStringLines(
        string propertyName,
        string value,
        int indentLevel)
    {
        if (TryParseStructuredJsonString(value, out var document))
        {
            using (document)
            {
                return RenderNamedCompositeLines(
                    propertyName,
                    RenderElementLines(
                        document.RootElement,
                        indentLevel + 1),
                    indentLevel);
            }
        }

        if (ShouldRenderBlockString(value))
        {
            return RenderBlockStringLines(
                PropertyPrefix(propertyName, indentLevel),
                value,
                indentLevel + 1);
        }

        return [PropertyPrefix(propertyName, indentLevel) + $" \"{EscapeInlineString(value)}\""];
    }

    private static IReadOnlyList<string> RenderAnonymousStringLines(
        string value,
        int indentLevel)
    {
        if (TryParseStructuredJsonString(value, out var document))
        {
            using (document)
            {
                return RenderElementLines(
                    document.RootElement,
                    indentLevel);
            }
        }

        if (ShouldRenderBlockString(value))
        {
            return RenderBlockStringLines(
                Indent(indentLevel),
                value,
                indentLevel + 1);
        }

        return [Indent(indentLevel) + $"\"{EscapeInlineString(value)}\""];
    }

    private static IReadOnlyList<string> RenderBlockStringLines(
        string header,
        string value,
        int contentIndentLevel)
    {
        List<string> lines =
        [
            header + " |",
        ];

        string[] valueLines = NormalizeLines(value);
        if (valueLines.Length == 0)
        {
            lines.Add(Indent(contentIndentLevel));
            return lines;
        }

        foreach (var valueLine in valueLines)
        {
            lines.Add(Indent(contentIndentLevel) + valueLine);
        }

        return lines;
    }

    private static bool TryParseStructuredJsonString(
        string value,
        [NotNullWhen(true)]
        out JsonDocument? document)
    {
        document = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (!trimmed.StartsWith('{') && !trimmed.StartsWith('['))
        {
            return false;
        }

        try
        {
            document = JsonDocument.Parse(trimmed);
            return document.RootElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array;
        }
        catch (JsonException)
        {
            document?.Dispose();
            document = null;
            return false;
        }
    }

    private static bool ShouldRenderBlockString(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        if (value.Length > 120)
        {
            return true;
        }

        foreach (var character in value)
        {
            if (character is '\r' or '\n' or '\t' or '"' or '\\')
            {
                return true;
            }

            if (char.IsControl(character))
            {
                return true;
            }
        }

        return false;
    }

    private static string EscapeInlineString(string value)
    {
        StringBuilder builder = new(value.Length);
        foreach (var character in value)
        {
            _ = character switch
            {
                '\\' => builder.Append(@"\\"),
                '"' => builder.Append("\\\""),
                _ => builder.Append(character),
            };
        }

        return builder.ToString();
    }

    private static string[] NormalizeLines(string value)
    {
        if (value.Length == 0)
        {
            return [];
        }

        return value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
    }

    private static string PropertyPrefix(string propertyName, int indentLevel)
    {
        return $"{Indent(indentLevel)}\"{propertyName}\":";
    }

    private static string Indent(int indentLevel)
    {
        return new string(' ', indentLevel * 2);
    }

    private static string RedactConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "none";
        }

        try
        {
            DbConnectionStringBuilder builder = new()
            {
                ConnectionString = connectionString,
            };

            var keys = builder.Keys
                .Cast<string>()
                .ToArray();
            foreach (var key in keys)
            {
                if (ShouldRedactKey(key))
                {
                    builder[key] = "***REDACTED***";
                }
            }

            return builder.ConnectionString;
        }
        catch (ArgumentException)
        {
            return string.Join(
                ";",
                connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(static segment =>
                    {
                        var separatorIndex = segment.IndexOf('=', StringComparison.Ordinal);
                        if (separatorIndex < 0)
                        {
                            return segment;
                        }

                        var key = segment[..separatorIndex].Trim();
                        return ShouldRedactKey(key)
                            ? $"{key}=***REDACTED***"
                            : segment;
                    }));
        }
    }

    private static bool ShouldRedactKey(string key)
    {
        return SecretConnectionStringKeys.Any(secretKey =>
            key.Contains(secretKey, StringComparison.OrdinalIgnoreCase));
    }
}
