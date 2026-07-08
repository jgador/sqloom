using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Captures replay data prepared for one replay run.
/// </summary>
public sealed class ReplayDataPreparationReport
{
    [JsonPropertyName("appName")]
    public required string AppName { get; init; }

    [JsonPropertyName("mode")]
    public required string Mode { get; init; }

    [JsonPropertyName("modelName")]
    public string? ModelName { get; init; }

    [JsonPropertyName("generatedAtUtc")]
    public DateTimeOffset GeneratedAtUtc { get; init; }

    [JsonPropertyName("operations")]
    public IReadOnlyList<ReplayDataPreparationOperation> Operations { get; init; } =
        Array.Empty<ReplayDataPreparationOperation>();

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } =
        Array.Empty<string>();
}

/// <summary>
/// Captures replay data prepared for one operation.
/// </summary>
public sealed class ReplayDataPreparationOperation
{
    [JsonPropertyName("operationKey")]
    public required string OperationKey { get; init; }

    [JsonPropertyName("strategy")]
    public required string Strategy { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    [JsonPropertyName("preparedData")]
    public ReplayPreparedData PreparedData { get; init; } = new();

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } =
        Array.Empty<string>();

    [JsonPropertyName("sourcesUsed")]
    public IReadOnlyList<string> SourcesUsed { get; init; } =
        Array.Empty<string>();

    [JsonPropertyName("notes")]
    public string Notes { get; init; } = string.Empty;
}

/// <summary>
/// Carries generated replay values that are safe to persist.
/// </summary>
public sealed class ReplayPreparedData
{
    [JsonPropertyName("persona")]
    public string? Persona { get; init; }

    [JsonPropertyName("requestBodyJson")]
    public string? RequestBodyJson { get; init; }

    [JsonPropertyName("pathValues")]
    public IReadOnlyDictionary<string, string> PathValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("queryValues")]
    public IReadOnlyDictionary<string, string> QueryValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("headerValues")]
    public IReadOnlyDictionary<string, string> HeaderValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
