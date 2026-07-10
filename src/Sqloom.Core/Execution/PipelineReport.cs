using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Describes the current state of a Sqloom run inside the observe/replay/capture/correlate/advise flow.
/// </summary>
public sealed class PipelineReport
{
    /// <summary>
    /// Gets the stages.
    /// </summary>
    [JsonPropertyName("stages")]
    public required IReadOnlyList<PipelineStageReport> Stages { get; init; }
}

/// <summary>
/// Describes one stage in the Sqloom flow.
/// </summary>
public sealed class PipelineStageReport
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the status.
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>
    /// Gets the summary.
    /// </summary>
    [JsonPropertyName("summary")]
    public required string Summary { get; init; }

    /// <summary>
    /// Gets the artifact path.
    /// </summary>
    [JsonPropertyName("artifactPath")]
    public string? ArtifactPath { get; init; }
}

/// <summary>
/// Canonical stage names for the Sqloom pipeline.
/// </summary>
public static class PipelineStageNames
{
    /// <summary>
    /// Defines the observe value.
    /// </summary>
    public const string Observe = "observe";
    /// <summary>
    /// Defines the replay value.
    /// </summary>
    public const string Replay = "replay";
    /// <summary>
    /// Defines the capture value.
    /// </summary>
    public const string Capture = "capture";
    /// <summary>
    /// Defines the correlate value.
    /// </summary>
    public const string Correlate = "correlate";
    /// <summary>
    /// Defines the advise value.
    /// </summary>
    public const string Advise = "advise";
}

/// <summary>
/// Canonical status values for Sqloom pipeline stages.
/// </summary>
public static class PipelineStageStatuses
{
    /// <summary>
    /// Defines the available value.
    /// </summary>
    public const string Available = "available";
    /// <summary>
    /// Defines the completed value.
    /// </summary>
    public const string Completed = "completed";
}
