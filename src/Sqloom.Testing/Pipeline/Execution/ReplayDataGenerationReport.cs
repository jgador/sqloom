using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Pipeline.Execution;

/// <summary>
/// Captures replay data prepared for one replay run.
/// </summary>
public sealed class ReplayDataGenerationReport
{
    /// <summary>
    /// Gets the app name.
    /// </summary>
    [JsonPropertyName("appName")]
    public required string AppName { get; init; }

    /// <summary>
    /// Gets the mode.
    /// </summary>
    [JsonPropertyName("mode")]
    public required string Mode { get; init; }

    /// <summary>
    /// Gets the model name.
    /// </summary>
    [JsonPropertyName("modelName")]
    public string? ModelName { get; init; }

    /// <summary>
    /// Gets the generated at utc.
    /// </summary>
    [JsonPropertyName("generatedAtUtc")]
    public DateTimeOffset GeneratedAtUtc { get; init; }

    /// <summary>
    /// Gets the operations.
    /// </summary>
    [JsonPropertyName("operations")]
    public IReadOnlyList<ReplayDataGenerationOperation> Operations { get; init; } =
        Array.Empty<ReplayDataGenerationOperation>();

    /// <summary>
    /// Gets the warnings.
    /// </summary>
    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } =
        Array.Empty<string>();
}

/// <summary>
/// Captures replay data prepared for one operation.
/// </summary>
public sealed class ReplayDataGenerationOperation
{
    /// <summary>
    /// Gets the operation key.
    /// </summary>
    [JsonPropertyName("operationKey")]
    public required string OperationKey { get; init; }

    /// <summary>
    /// Gets the strategy.
    /// </summary>
    [JsonPropertyName("strategy")]
    public required string Strategy { get; init; }

    /// <summary>
    /// Gets the status.
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>
    /// Gets the confidence.
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    /// <summary>
    /// Gets the prepared data.
    /// </summary>
    [JsonPropertyName("preparedData")]
    public ReplayPreparedData PreparedData { get; init; } = new();

    /// <summary>
    /// Gets the warnings.
    /// </summary>
    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } =
        Array.Empty<string>();

    /// <summary>
    /// Gets the sources used.
    /// </summary>
    [JsonPropertyName("sourcesUsed")]
    public IReadOnlyList<string> SourcesUsed { get; init; } =
        Array.Empty<string>();

    /// <summary>
    /// Gets the notes.
    /// </summary>
    [JsonPropertyName("notes")]
    public string Notes { get; init; } = string.Empty;
}

/// <summary>
/// Carries generated replay values that are safe to persist.
/// </summary>
public sealed class ReplayPreparedData
{
    /// <summary>
    /// Gets the persona.
    /// </summary>
    [JsonPropertyName("persona")]
    public string? Persona { get; init; }

    /// <summary>
    /// Gets the request body json.
    /// </summary>
    [JsonPropertyName("requestBodyJson")]
    public string? RequestBodyJson { get; init; }

    /// <summary>
    /// Gets the path values.
    /// </summary>
    [JsonPropertyName("pathValues")]
    public IReadOnlyDictionary<string, string> PathValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the query values.
    /// </summary>
    [JsonPropertyName("queryValues")]
    public IReadOnlyDictionary<string, string> QueryValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the header values.
    /// </summary>
    [JsonPropertyName("headerValues")]
    public IReadOnlyDictionary<string, string> HeaderValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
