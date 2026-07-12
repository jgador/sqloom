using System.Collections.Generic;
using System.Text.Json.Serialization;
using Sqloom.Core.Execution;

namespace Sqloom.OpenAI.Advice;

/// <summary>
/// Carries the OpenAI response payload used for Sqloom advice generation.
/// </summary>
public sealed class OpenAITuningAdviceResponse
{
    /// <summary>
    /// Gets the recommendations.
    /// </summary>
    [JsonPropertyName("recommendations")]
    public required IReadOnlyList<SqlTuningRecommendation> Recommendations { get; init; }

    /// <summary>
    /// Gets the proposals.
    /// </summary>
    [JsonPropertyName("proposals")]
    public required IReadOnlyList<SqlTuningProposal> Proposals { get; init; }

    /// <summary>
    /// Gets the warnings.
    /// </summary>
    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>
    /// Gets the model name.
    /// </summary>
    [JsonPropertyName("modelName")]
    public string? ModelName { get; init; }
}
