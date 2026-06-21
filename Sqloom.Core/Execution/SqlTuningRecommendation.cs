namespace Sqloom.Core.Execution;

/// <summary>
/// Describes SQL tuning recommendation.
/// </summary>
public sealed class SqlTuningRecommendation
{
    public required string Title { get; init; }

    public required string RootCause { get; init; }

    public required string SuggestedChange { get; init; }

    public required string VerificationMetric { get; init; }
}
