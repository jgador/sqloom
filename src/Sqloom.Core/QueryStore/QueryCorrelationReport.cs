using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Sqloom.Core.Execution;
using Sqloom.Core.QueryStore;

namespace Sqloom.Core.QueryStore;

/// <summary>
/// Captures the output of a replay-to-Query Store correlation run.
/// </summary>
public sealed class QueryCorrelationReport
{
    /// <summary>
    /// Gets the generated at utc.
    /// </summary>
    [JsonPropertyName("generatedAtUtc")]
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    /// <summary>
    /// Gets the app name.
    /// </summary>
    [JsonPropertyName("appName")]
    public string? AppName { get; init; }

    /// <summary>
    /// Gets the replay artifact dir.
    /// </summary>
    [JsonPropertyName("replayArtifactDir")]
    public required string ReplayArtifactDir { get; init; }

    /// <summary>
    /// Gets the query store snapshot path.
    /// </summary>
    [JsonPropertyName("queryStoreSnapshotPath")]
    public string? QueryStoreSnapshotPath { get; init; }

    /// <summary>
    /// Gets the query store captured at utc.
    /// </summary>
    [JsonPropertyName("queryStoreCapturedAtUtc")]
    public required DateTimeOffset QueryStoreCapturedAtUtc { get; init; }

    /// <summary>
    /// Gets the records.
    /// </summary>
    [JsonPropertyName("records")]
    public IReadOnlyList<QueryCorrelationRecord> Records { get; init; } =
        Array.Empty<QueryCorrelationRecord>();

    /// <summary>
    /// Gets the summary.
    /// </summary>
    [JsonPropertyName("summary")]
    public required QueryCorrelationSummary Summary { get; init; }

    /// <summary>
    /// Gets the pipeline.
    /// </summary>
    [JsonPropertyName("pipeline")]
    public required PipelineReport Pipeline { get; init; }

    /// <summary>
    /// Gets the warnings.
    /// </summary>
    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } =
        Array.Empty<string>();
}
