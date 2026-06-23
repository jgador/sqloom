using System;
using System.Collections.Generic;
using System.Linq;
using Sqloom.Core.Artifacts;
using Sqloom.Core.Execution;
using Sqloom.Core.QueryStore;

namespace Sqloom.Host.QueryStore;

/// <summary>
/// Turns a completed Query Store correlation report into baseline heuristic operation-level guidance.
/// </summary>
public sealed class QueryStoreCorrelationAdvisor
{
    private const double HighCpuMs = 100d;
    private const double HighDurationMs = 200d;
    private const double HighReads = 1000d;
    private const string StrategyName = "query-store-correlation-heuristics";

    /// <summary>
    /// Builds a baseline heuristic advice report from a completed Query Store correlation artifact.
    /// </summary>
    public AdviceReport CreateReport(
        QueryCorrelationReport correlationReport,
        string queryStoreCorrelationPath,
        string adviceOutputPath)
    {
        ArgumentNullException.ThrowIfNull(correlationReport);
        ArgumentException.ThrowIfNullOrWhiteSpace(queryStoreCorrelationPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(adviceOutputPath);

        var operations = correlationReport.Summary.Operations
            .Select(operation => CreateOperationReport(
                operation,
                correlationReport.Records.Where(record =>
                    string.Equals(record.OperationKey, operation.OperationKey, StringComparison.OrdinalIgnoreCase))))
            .ToArray();
        var sqlProposalJsonPath = ArtifactLayout.GetSqlProposalPath(correlationReport.ReplayArtifactDir);
        var sqlProposalScriptPath = ArtifactLayout.GetSqlProposalScriptPath(correlationReport.ReplayArtifactDir);

        return new AdviceReport
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            AppName = correlationReport.AppName ?? "unknown",
            ReplayArtifactDir = correlationReport.ReplayArtifactDir,
            QueryStoreCorrelationPath = queryStoreCorrelationPath,
            StrategyName = StrategyName,
            SqlProposalJsonPath = sqlProposalJsonPath,
            SqlProposalScriptPath = sqlProposalScriptPath,
            Pipeline = CreatePipeline(correlationReport, queryStoreCorrelationPath, adviceOutputPath),
            Summary = new AdviceSummary
            {
                OperationCount = operations.Length,
                RecommendationCount = operations.Sum(static operation => operation.Recommendations.Count),
                ProposalCount = operations.Sum(static operation => operation.Proposals.Count),
            },
            Operations = operations,
            Warnings = correlationReport.Warnings,
        };
    }

    private static PipelineReport CreatePipeline(
        QueryCorrelationReport correlationReport,
        string queryStoreCorrelationPath,
        string adviceOutputPath)
    {
        var replaySummaryPath = ArtifactLayout.GetReplaySummaryPath(correlationReport.ReplayArtifactDir);

        return new PipelineReport
        {
            Stages =
            [
                new PipelineStageReport
                {
                    Name = PipelineStageNames.Observe,
                    Status = PipelineStageStatuses.Completed,
                    Summary = "Captured a Query Store snapshot for this advice run.",
                    ArtifactPath = correlationReport.QueryStoreSnapshotPath,
                },
                new PipelineStageReport
                {
                    Name = PipelineStageNames.Replay,
                    Status = PipelineStageStatuses.Completed,
                    Summary = "Replay artifacts were available for this advice run.",
                    ArtifactPath = replaySummaryPath,
                },
                new PipelineStageReport
                {
                    Name = PipelineStageNames.Capture,
                    Status = PipelineStageStatuses.Completed,
                    Summary = "Captured replay SQL fed the advice run.",
                    ArtifactPath = correlationReport.ReplayArtifactDir,
                },
                new PipelineStageReport
                {
                    Name = PipelineStageNames.Correlate,
                    Status = PipelineStageStatuses.Completed,
                    Summary = "Query Store correlation completed before advice generation.",
                    ArtifactPath = queryStoreCorrelationPath,
                },
                new PipelineStageReport
                {
                    Name = PipelineStageNames.Advise,
                    Status = PipelineStageStatuses.Completed,
                    Summary = "Operation-level tuning guidance was emitted from the correlation artifact.",
                    ArtifactPath = adviceOutputPath,
                },
            ],
        };
    }

    private AdviceOperationReport CreateOperationReport(
        OperationCorrelationSummary operation,
        IEnumerable<QueryCorrelationRecord> operationRecords)
    {
        var records = operationRecords.ToList();
        var recommendations = BuildRecommendations(operation, records);

        return new AdviceOperationReport
        {
            OperationKey = operation.OperationKey,
            HttpMethod = operation.HttpMethod,
            Route = operation.Route,
            ReplayStatus = operation.ReplayStatus,
            CapturedCommandCount = operation.CapturedCommandCount,
            MatchedCommandCount = operation.MatchedCommandCount,
            Recommendations = recommendations,
            Proposals = [],
        };
    }

    private static List<SqlTuningRecommendation> BuildRecommendations(
        OperationCorrelationSummary operation,
        IReadOnlyList<QueryCorrelationRecord> records)
    {
        var recommendations = new List<SqlTuningRecommendation>();

        if (!string.Equals(operation.ReplayStatus, "replayed", StringComparison.OrdinalIgnoreCase))
        {
            recommendations.Add(new SqlTuningRecommendation
            {
                Title = $"Stabilize replay before tuning {operation.OperationKey}",
                RootCause = $"Sqloom recorded replay status '{operation.ReplayStatus}', so downstream SQL evidence is incomplete.",
                SuggestedChange = "Fix the harness setup or replay scenario data until the operation completes with status 'replayed'.",
                VerificationMetric = "Replay status becomes 'replayed' and the operation captures the expected SQL commands.",
            });
            return recommendations;
        }

        if (operation.CapturedCommandCount == 0)
        {
            recommendations.Add(new SqlTuningRecommendation
            {
                Title = $"Recover SQL capture for {operation.OperationKey}",
                RootCause = "The operation replayed but no SQL commands were captured from the request path.",
                SuggestedChange = "Check the replay scenario, SQL interception, and whether the endpoint still exercises the expected database path.",
                VerificationMetric = "CapturedCommandCount is greater than 0 for this operation.",
            });
            return recommendations;
        }

        if (operation.MatchedCommandCount == 0)
        {
            recommendations.Add(new SqlTuningRecommendation
            {
                Title = $"Recover exact Query Store matches for {operation.OperationKey}",
                RootCause = "Captured SQL exists, but none of it resolved to a safe Query Store match for this operation.",
                SuggestedChange = "Capture a fresh Query Store snapshot near the replay window and rerun --correlate so exact matches replace unmatched SQL.",
                VerificationMetric = "MatchedCommandCount is greater than 0 and exact matches outnumber unmatched commands for this operation.",
            });
            return recommendations;
        }

        if (operation.FingerprintFallbackCount > 0)
        {
            recommendations.Add(new SqlTuningRecommendation
            {
                Title = $"Replace fingerprint fallback matches for {operation.OperationKey}",
                RootCause = $"{operation.FingerprintFallbackCount} captured command(s) matched only by local SQL fingerprint instead of exact Query Store ownership.",
                SuggestedChange = "Prefer statement_sql_handle or exact text correlation by collecting a fresh Query Store snapshot and confirming the readonly principal can resolve statement handles.",
                VerificationMetric = "HandleExactCount + QueryTextExactCount increases while FingerprintFallbackCount decreases for this operation.",
            });
        }

        var matchedPlans = GetDistinctMatchedPlans(records);
        var hottestPlan = matchedPlans
            .OrderByDescending(ScorePlan)
            .FirstOrDefault();
        if (hottestPlan is not null && IsElevated(hottestPlan))
        {
            var target = hottestPlan.ObjectName ?? operation.Route;
            recommendations.Add(new SqlTuningRecommendation
            {
                Title = $"Reduce query cost for {operation.OperationKey}",
                RootCause = $"Matched Query Store plan {hottestPlan.PlanId} on {target} averages {hottestPlan.MeanDuration.TotalMilliseconds:F1} ms duration, {hottestPlan.MeanCpuMilliseconds:F1} ms CPU, and {hottestPlan.MeanLogicalReads:F1} logical reads.",
                SuggestedChange = "Inspect the exact matched plan, then review predicate selectivity, covering indexes, and unnecessary column or row shape in the replayed SQL.",
                VerificationMetric = $"Lower mean duration, CPU, or logical reads for Query Store plan {hottestPlan.PlanId} from the current replay baseline.",
            });
        }

        var distinctPlanCount = matchedPlans
            .Select(static plan => plan.PlanId)
            .Distinct()
            .Count();
        if (distinctPlanCount > 1)
        {
            recommendations.Add(new SqlTuningRecommendation
            {
                Title = $"Investigate plan instability for {operation.OperationKey}",
                RootCause = $"The operation correlated to {distinctPlanCount} Query Store plans across the same replay path.",
                SuggestedChange = "Compare parameterization, sniffing behavior, and index shape across the matched plans before changing SQL text.",
                VerificationMetric = "The number of matched Query Store plan ids decreases or the slower matched plan's mean duration drops.",
            });
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add(new SqlTuningRecommendation
            {
                Title = $"Keep a replay baseline for {operation.OperationKey}",
                RootCause = "Replay, capture, and correlation completed without an obvious hotspot threshold breach.",
                SuggestedChange = "Use this replay and correlation pair as the baseline before schema, query, or index changes.",
                VerificationMetric = "Future replay runs do not regress matched mean duration, CPU, or logical reads for this operation.",
            });
        }

        return recommendations;
    }

    private static IReadOnlyList<QueryStorePlanRecord> GetDistinctMatchedPlans(
        IReadOnlyList<QueryCorrelationRecord> records)
    {
        return records
            .SelectMany(static record => record.MatchedPlans)
            .GroupBy(static plan => (plan.QueryId, plan.PlanId))
            .Select(static group => group.First())
            .ToArray();
    }

    private static bool IsElevated(QueryStorePlanRecord plan)
    {
        return plan.MeanDuration.TotalMilliseconds >= HighDurationMs
            || plan.MeanCpuMilliseconds >= HighCpuMs
            || plan.MeanLogicalReads >= HighReads;
    }

    private static double ScorePlan(QueryStorePlanRecord plan)
    {
        return plan.MeanDuration.TotalMilliseconds
            + plan.MeanCpuMilliseconds
            + (plan.MeanLogicalReads / 10d);
    }
}
