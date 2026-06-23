using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Sqloom.Core.Execution;
using Sqloom.QueryStore.QueryStore;

namespace Sqloom.Correlation.QueryStore;

/// <summary>
/// Captures the output of a replay-to-Query Store correlation run.
/// </summary>
public sealed class QueryCorrelationReport
{
    [JsonPropertyName("generatedAtUtc")]
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    [JsonPropertyName("appName")]
    public string? AppName { get; init; }

    [JsonPropertyName("replayArtifactDir")]
    public required string ReplayArtifactDir { get; init; }

    [JsonPropertyName("queryStoreSnapshotPath")]
    public string? QueryStoreSnapshotPath { get; init; }

    [JsonPropertyName("queryStoreCapturedAtUtc")]
    public required DateTimeOffset QueryStoreCapturedAtUtc { get; init; }

    [JsonPropertyName("records")]
    public IReadOnlyList<QueryCorrelationRecord> Records { get; init; } =
        Array.Empty<QueryCorrelationRecord>();

    [JsonPropertyName("summary")]
    public required QueryCorrelationSummary Summary { get; init; }

    [JsonPropertyName("pipeline")]
    public required PipelineReport Pipeline { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } =
        Array.Empty<string>();
}
