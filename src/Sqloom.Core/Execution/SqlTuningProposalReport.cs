using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Persists the SQL proposal subset of a Sqloom advice run as a dedicated artifact.
/// </summary>
public sealed class SqlTuningProposalReport
{
    [JsonPropertyName("generatedAtUtc")]
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    [JsonPropertyName("appName")]
    public required string AppName { get; init; }

    [JsonPropertyName("replayArtifactDirectory")]
    public required string ReplayArtifactDirectory { get; init; }

    [JsonPropertyName("queryStoreCorrelationPath")]
    public required string QueryStoreCorrelationPath { get; init; }

    [JsonPropertyName("sourceAdvicePath")]
    public required string SourceAdvicePath { get; init; }

    [JsonPropertyName("sqlScriptPath")]
    public required string SqlScriptPath { get; init; }

    [JsonPropertyName("modelProvider")]
    public string ModelProvider { get; init; } = "openai";

    [JsonPropertyName("modelName")]
    public string? ModelName { get; init; }

    [JsonPropertyName("strategyName")]
    public required string StrategyName { get; init; }

    [JsonPropertyName("summary")]
    public required SqlTuningProposalSummary Summary { get; init; }

    [JsonPropertyName("operations")]
    public required IReadOnlyList<SqlTuningProposalOperationReport> Operations { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Summarizes the SQL proposals emitted by a Sqloom advice run.
/// </summary>
public sealed class SqlTuningProposalSummary
{
    [JsonPropertyName("operationCount")]
    public int OperationCount { get; init; }

    [JsonPropertyName("proposalCount")]
    public int ProposalCount { get; init; }
}

/// <summary>
/// Captures the SQL proposals emitted for one replayed operation.
/// </summary>
public sealed class SqlTuningProposalOperationReport
{
    [JsonPropertyName("operationKey")]
    public required string OperationKey { get; init; }

    [JsonPropertyName("httpMethod")]
    public required string HttpMethod { get; init; }

    [JsonPropertyName("route")]
    public required string Route { get; init; }

    [JsonPropertyName("replayStatus")]
    public required string ReplayStatus { get; init; }

    [JsonPropertyName("proposals")]
    public required IReadOnlyList<SqlTuningProposal> Proposals { get; init; }
}
