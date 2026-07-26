using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Pipeline.Execution;

/// <summary>
/// Describes replay input generation for a run, including per-operation output and warnings.
/// </summary>
public sealed class ReplayDataGenerationReport
{
    /// <summary>
    /// Identifies the application whose replay inputs were evaluated.
    /// </summary>
    [JsonPropertyName("appName")]
    public required string AppName { get; init; }

    /// <summary>
    /// Records the replay data agent mode used for the run.
    /// </summary>
    [JsonPropertyName("mode")]
    public required string Mode { get; init; }

    /// <summary>
    /// Identifies the model selected for replay data generation, when an agent was configured.
    /// </summary>
    [JsonPropertyName("modelName")]
    public string? ModelName { get; init; }

    /// <summary>
    /// Records when the generation report was created in UTC.
    /// </summary>
    [JsonPropertyName("generatedAtUtc")]
    public DateTimeOffset GeneratedAtUtc { get; init; }

    /// <summary>
    /// Lists generation outcomes for the replay operations evaluated during the run.
    /// </summary>
    [JsonPropertyName("operations")]
    public IReadOnlyList<ReplayDataGenerationOperation> Operations { get; init; } =
        Array.Empty<ReplayDataGenerationOperation>();

    /// <summary>
    /// Lists warnings that apply to the overall generation run.
    /// </summary>
    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } =
        Array.Empty<string>();
}

/// <summary>
/// Describes the replay input generation outcome for one operation.
/// </summary>
public sealed class ReplayDataGenerationOperation
{
    /// <summary>
    /// Identifies the replay operation by its stable HTTP method and route key.
    /// </summary>
    [JsonPropertyName("operationKey")]
    public required string OperationKey { get; init; }

    /// <summary>
    /// Identifies the component or strategy that produced the generation outcome.
    /// </summary>
    [JsonPropertyName("strategy")]
    public required string Strategy { get; init; }

    /// <summary>
    /// Records whether input generation succeeded, was not needed, or failed.
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>
    /// Records the generator's confidence in the returned inputs.
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    /// <summary>
    /// Contains generated input values ready to apply to the replay request.
    /// </summary>
    [JsonPropertyName("preparedData")]
    public ReplayPreparedData PreparedData { get; init; } = new();

    /// <summary>
    /// Lists warnings specific to this operation's generated inputs.
    /// </summary>
    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } =
        Array.Empty<string>();

    /// <summary>
    /// Lists the evidence sources used to generate the replay inputs.
    /// </summary>
    [JsonPropertyName("sourcesUsed")]
    public IReadOnlyList<string> SourcesUsed { get; init; } =
        Array.Empty<string>();

    /// <summary>
    /// Provides generator rationale or diagnostic detail for the outcome.
    /// </summary>
    [JsonPropertyName("notes")]
    public string Notes { get; init; } = string.Empty;
}

/// <summary>
/// Carries replay input values that are safe to persist and apply to an HTTP request.
/// </summary>
public sealed class ReplayPreparedData
{
    /// <summary>
    /// Describes the caller persona used to generate the values, when available.
    /// </summary>
    [JsonPropertyName("persona")]
    public string? Persona { get; init; }

    /// <summary>
    /// Contains the generated request body as JSON, or <see langword="null"/> when no body is supplied.
    /// </summary>
    [JsonPropertyName("requestBodyJson")]
    public string? RequestBodyJson { get; init; }

    /// <summary>
    /// Maps route parameter names to generated values.
    /// </summary>
    [JsonPropertyName("pathValues")]
    public IReadOnlyDictionary<string, string> PathValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Maps query-string parameter names to generated values.
    /// </summary>
    [JsonPropertyName("queryValues")]
    public IReadOnlyDictionary<string, string> QueryValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Maps HTTP header names to generated values.
    /// </summary>
    [JsonPropertyName("headerValues")]
    public IReadOnlyDictionary<string, string> HeaderValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
