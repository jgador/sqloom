using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Describes SQL tuning recommendation.
/// </summary>
public sealed class SqlTuningRecommendation
{
    /// <summary>
    /// Gets the title.
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>
    /// Gets the root cause.
    /// </summary>
    [JsonPropertyName("rootCause")]
    public required string RootCause { get; init; }

    /// <summary>
    /// Gets the suggested change.
    /// </summary>
    [JsonPropertyName("suggestedChange")]
    public required string SuggestedChange { get; init; }

    /// <summary>
    /// Gets the verification metric.
    /// </summary>
    [JsonPropertyName("verificationMetric")]
    public required string VerificationMetric { get; init; }
}
