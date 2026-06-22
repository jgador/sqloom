using System.Collections.Generic;
using System.Text.Json.Serialization;
using Sqloom.Core.Execution;

namespace Sqloom.OpenAI.Advice;

/// <summary>
/// Carries the OpenAI response payload used for Sqloom advice generation.
/// </summary>
public sealed class OpenAITuningAdviceResponse
{
    [JsonPropertyName("recommendations")]
    public required IReadOnlyList<SqlTuningRecommendation> Recommendations { get; init; }

    [JsonPropertyName("proposals")]
    public required IReadOnlyList<SqlTuningProposal> Proposals { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];

    [JsonPropertyName("modelName")]
    public string? ModelName { get; init; }
}
