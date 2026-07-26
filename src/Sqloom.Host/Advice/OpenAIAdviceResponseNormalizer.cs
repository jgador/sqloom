using System;
using System.Collections.Generic;
using System.Linq;
using Sqloom.Pipeline.Execution;

namespace Sqloom.Host;

/// <summary>
/// Normalizes OpenAI advice responses into stable Sqloom artifact contracts.
/// </summary>
internal static class OpenAIAdviceResponseNormalizer
{
    public static IReadOnlyList<SqlTuningRecommendation> NormalizeRecommendations(
        IReadOnlyList<SqlTuningRecommendation>? recommendations)
    {
        if (recommendations is null || recommendations.Count == 0)
        {
            return [];
        }

        List<SqlTuningRecommendation> normalizedRecommendations = new(recommendations.Count);
        foreach (var recommendation in recommendations)
        {
            var title = recommendation.Title.Trim();
            var rootCause = recommendation.RootCause.Trim();
            var suggestedChange = recommendation.SuggestedChange.Trim();
            var verificationMetric = recommendation.VerificationMetric.Trim();
            if (title.Length == 0
                || rootCause.Length == 0
                || suggestedChange.Length == 0
                || verificationMetric.Length == 0)
            {
                throw new InvalidOperationException(
                    "OpenAI tuning advice returned an incomplete recommendation.");
            }

            normalizedRecommendations.Add(new SqlTuningRecommendation
            {
                Title = title,
                RootCause = rootCause,
                SuggestedChange = suggestedChange,
                VerificationMetric = verificationMetric,
            });
        }

        return normalizedRecommendations;
    }

    public static (IReadOnlyList<SqlTuningProposal> Proposals, IReadOnlyList<string> Warnings) NormalizeProposals(
        IReadOnlyList<SqlTuningProposal>? proposals)
    {
        if (proposals is null || proposals.Count == 0)
        {
            return (Array.Empty<SqlTuningProposal>(), Array.Empty<string>());
        }

        // Normalize model output into stable review artifacts without discarding rollback-warning cases.
        List<SqlTuningProposal> normalizedProposals = new(proposals.Count);
        List<string> warnings = [];
        foreach (var proposal in proposals)
        {
            var title = proposal.Title.Trim();
            var diagnosis = proposal.Diagnosis.Trim();
            var proposalKind = proposal.ProposalKind.Trim();
            var targetObject = proposal.TargetObject.Trim();
            var sqlScript = proposal.SqlScript.Trim();
            var rollbackSqlScript = proposal.RollbackSqlScript?.Trim() ?? string.Empty;
            var expectedBenefit = proposal.ExpectedBenefit.Trim();
            var verificationMetric = proposal.VerificationMetric.Trim();
            if (title.Length == 0
                || diagnosis.Length == 0
                || proposalKind.Length == 0
                || targetObject.Length == 0
                || sqlScript.Length == 0
                || expectedBenefit.Length == 0
                || verificationMetric.Length == 0)
            {
                throw new InvalidOperationException(
                    "OpenAI tuning advice returned an incomplete SQL proposal.");
            }

            if (rollbackSqlScript.Length == 0)
            {
                warnings.Add(
                    $"SQL proposal '{title}' did not include rollback SQL. Sqloom persisted the proposal with an empty rollback script.");
            }

            normalizedProposals.Add(new SqlTuningProposal
            {
                Title = title,
                Diagnosis = diagnosis,
                ProposalKind = proposalKind,
                TargetObject = targetObject,
                SqlScript = sqlScript,
                RollbackSqlScript = rollbackSqlScript,
                ExpectedBenefit = expectedBenefit,
                VerificationMetric = verificationMetric,
                Confidence = Math.Clamp(proposal.Confidence, 0d, 1d),
                SourceCommandOrdinals = proposal.SourceCommandOrdinals
                    .Distinct()
                    .OrderBy(static ordinal => ordinal)
                    .ToArray(),
                MatchedPlanIds = proposal.MatchedPlanIds
                    .Distinct()
                    .OrderBy(static planId => planId)
                    .ToArray(),
            });
        }

        return (normalizedProposals, warnings);
    }
}
