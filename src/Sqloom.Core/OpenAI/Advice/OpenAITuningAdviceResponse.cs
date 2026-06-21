using System.Collections.Generic;
using Sqloom.Core.Execution;

namespace Sqloom.OpenAI.Advice;

/// <summary>
/// Carries the OpenAI response payload used for Sqloom advice generation.
/// </summary>
public sealed class OpenAITuningAdviceResponse
{
    public required IReadOnlyList<SqlTuningRecommendation> Recommendations { get; init; }

    public required IReadOnlyList<SqlTuningProposal> Proposals { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public string? ModelName { get; init; }
}
