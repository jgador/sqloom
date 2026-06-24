using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Describes the current state of a Sqloom run inside the observe/replay/capture/correlate/advise flow.
/// </summary>
public sealed class PipelineReport
{
    [JsonPropertyName("stages")]
    public required IReadOnlyList<PipelineStageReport> Stages { get; init; }
}

/// <summary>
/// Describes one stage in the Sqloom flow.
/// </summary>
public sealed class PipelineStageReport
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("summary")]
    public required string Summary { get; init; }

    [JsonPropertyName("artifactPath")]
    public string? ArtifactPath { get; init; }
}

/// <summary>
/// Canonical stage names for the Sqloom pipeline.
/// </summary>
public static class PipelineStageNames
{
    public const string Observe = "observe";
    public const string Replay = "replay";
    public const string Capture = "capture";
    public const string Correlate = "correlate";
    public const string Advise = "advise";
}

/// <summary>
/// Canonical status values for Sqloom pipeline stages.
/// </summary>
public static class PipelineStageStatuses
{
    public const string Available = "available";
    public const string Completed = "completed";
}
