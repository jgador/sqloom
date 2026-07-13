using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Pipeline.Execution;

/// <summary>
/// Persists the SQL proposal subset of a Sqloom advice run as a dedicated artifact.
/// </summary>
public sealed class SqlTuningProposalReport
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
    public required string AppName { get; init; }

    /// <summary>
    /// Gets the replay artifact dir.
    /// </summary>
    [JsonPropertyName("replayArtifactDir")]
    public required string ReplayArtifactDir { get; init; }

    /// <summary>
    /// Gets the query store correlation path.
    /// </summary>
    [JsonPropertyName("queryStoreCorrelationPath")]
    public required string QueryStoreCorrelationPath { get; init; }

    /// <summary>
    /// Gets the source advice path.
    /// </summary>
    [JsonPropertyName("sourceAdvicePath")]
    public required string SourceAdvicePath { get; init; }

    /// <summary>
    /// Gets the sql script path.
    /// </summary>
    [JsonPropertyName("sqlScriptPath")]
    public required string SqlScriptPath { get; init; }

    /// <summary>
    /// Gets the model provider.
    /// </summary>
    [JsonPropertyName("modelProvider")]
    public string ModelProvider { get; init; } = "openai";

    /// <summary>
    /// Gets the model name.
    /// </summary>
    [JsonPropertyName("modelName")]
    public string? ModelName { get; init; }

    /// <summary>
    /// Gets the strategy name.
    /// </summary>
    [JsonPropertyName("strategyName")]
    public required string StrategyName { get; init; }

    /// <summary>
    /// Gets the summary.
    /// </summary>
    [JsonPropertyName("summary")]
    public required SqlTuningProposalSummary Summary { get; init; }

    /// <summary>
    /// Gets the operations.
    /// </summary>
    [JsonPropertyName("operations")]
    public required IReadOnlyList<SqlTuningProposalOperationReport> Operations { get; init; }

    /// <summary>
    /// Gets the warnings.
    /// </summary>
    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Summarizes the SQL proposals emitted by a Sqloom advice run.
/// </summary>
public sealed class SqlTuningProposalSummary
{
    /// <summary>
    /// Gets the operation count.
    /// </summary>
    [JsonPropertyName("operationCount")]
    public int OperationCount { get; init; }

    /// <summary>
    /// Gets the proposal count.
    /// </summary>
    [JsonPropertyName("proposalCount")]
    public int ProposalCount { get; init; }
}

/// <summary>
/// Captures the SQL proposals emitted for one replayed operation.
/// </summary>
public sealed class SqlTuningProposalOperationReport
{
    /// <summary>
    /// Gets the operation key.
    /// </summary>
    [JsonPropertyName("operationKey")]
    public required string OperationKey { get; init; }

    /// <summary>
    /// Gets the http method.
    /// </summary>
    [JsonPropertyName("httpMethod")]
    public required string HttpMethod { get; init; }

    /// <summary>
    /// Gets the route.
    /// </summary>
    [JsonPropertyName("route")]
    public required string Route { get; init; }

    /// <summary>
    /// Gets the replay status.
    /// </summary>
    [JsonPropertyName("replayStatus")]
    public required string ReplayStatus { get; init; }

    /// <summary>
    /// Gets the proposals.
    /// </summary>
    [JsonPropertyName("proposals")]
    public required IReadOnlyList<SqlTuningProposal> Proposals { get; init; }
}
