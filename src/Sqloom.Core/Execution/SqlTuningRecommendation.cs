using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Describes SQL tuning recommendation.
/// </summary>
public sealed class SqlTuningRecommendation
{
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("rootCause")]
    public required string RootCause { get; init; }

    [JsonPropertyName("suggestedChange")]
    public required string SuggestedChange { get; init; }

    [JsonPropertyName("verificationMetric")]
    public required string VerificationMetric { get; init; }
}
