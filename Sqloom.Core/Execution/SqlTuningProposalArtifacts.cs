using System;
using System.Linq;
using System.Text;

namespace Sqloom.Core.Execution;

/// <summary>
/// Builds the sidecar JSON and SQL artifacts for Sqloom SQL proposals.
/// </summary>
public static class SqlTuningProposalArtifacts
{
    public static SqlTuningProposalReport CreateReport(
        AdviceReport adviceReport,
        string adviceOutputPath)
    {
        ArgumentNullException.ThrowIfNull(adviceReport);
        ArgumentException.ThrowIfNullOrWhiteSpace(adviceOutputPath);

        var operations = adviceReport.Operations
            .Select(static operation => new SqlTuningProposalOperationReport
            {
                OperationKey = operation.OperationKey,
                HttpMethod = operation.HttpMethod,
                Route = operation.Route,
                ReplayStatus = operation.ReplayStatus,
                Proposals = operation.Proposals,
            })
            .ToArray();

        return new SqlTuningProposalReport
        {
            GeneratedAtUtc = adviceReport.GeneratedAtUtc,
            AppName = adviceReport.AppName,
            ReplayArtifactDirectory = adviceReport.ReplayArtifactDirectory,
            QueryStoreCorrelationPath = adviceReport.QueryStoreCorrelationPath,
            SourceAdvicePath = adviceOutputPath,
            SqlScriptPath = adviceReport.SqlProposalScriptPath,
            ModelProvider = adviceReport.ModelProvider,
            ModelName = adviceReport.ModelName,
            StrategyName = adviceReport.StrategyName,
            Summary = new SqlTuningProposalSummary
            {
                OperationCount = operations.Length,
                ProposalCount = operations.Sum(static operation => operation.Proposals.Count),
            },
            Operations = operations,
            Warnings = adviceReport.Warnings,
        };
    }

    public static string RenderSqlScript(SqlTuningProposalReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        StringBuilder builder = new();
        builder.AppendLine("/*");
        builder.AppendLine("Sqloom SQL proposals");
        builder.AppendLine($"GeneratedAtUtc: {report.GeneratedAtUtc:O}");
        builder.AppendLine($"App: {report.AppName}");
        builder.AppendLine($"Model provider: {report.ModelProvider}");
        if (!string.IsNullOrWhiteSpace(report.ModelName))
        {
            builder.AppendLine($"Model: {report.ModelName}");
        }

        builder.AppendLine($"SourceAdvice: {report.SourceAdvicePath}");
        builder.AppendLine($"QueryStoreCorrelation: {report.QueryStoreCorrelationPath}");
        builder.AppendLine("These scripts are suggestions only. Review against the target schema and workload before applying.");
        builder.AppendLine("*/");
        builder.AppendLine();

        var proposals = report.Operations
            .SelectMany(static operation => operation.Proposals.Select(proposal => (Operation: operation, Proposal: proposal)))
            .ToArray();
        if (proposals.Length == 0)
        {
            builder.AppendLine("-- No SQL proposals were emitted for this advice run.");
            return builder.ToString();
        }

        for (var index = 0; index < proposals.Length; index++)
        {
            var entry = proposals[index];
            builder.AppendLine($"-- Operation: {entry.Operation.OperationKey}");
            builder.AppendLine($"-- Proposal: {entry.Proposal.Title}");
            builder.AppendLine($"-- Target: {entry.Proposal.TargetObject}");
            builder.AppendLine($"-- Kind: {entry.Proposal.ProposalKind}");
            builder.AppendLine($"-- Diagnosis: {entry.Proposal.Diagnosis}");
            builder.AppendLine($"-- Expected benefit: {entry.Proposal.ExpectedBenefit}");
            builder.AppendLine($"-- Verification: {entry.Proposal.VerificationMetric}");
            builder.AppendLine($"-- Confidence: {entry.Proposal.Confidence:F2}");
            builder.AppendLine(entry.Proposal.SqlScript.Trim());
            builder.AppendLine();
            builder.AppendLine("/*");
            builder.AppendLine("Rollback");
            builder.AppendLine(GetRollbackSectionText(entry.Proposal));
            builder.AppendLine("*/");

            if (index + 1 < proposals.Length)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    private static string GetRollbackSectionText(SqlTuningProposal proposal)
    {
        ArgumentNullException.ThrowIfNull(proposal);

        var rollbackSqlScript = proposal.RollbackSqlScript?.Trim();
        return string.IsNullOrWhiteSpace(rollbackSqlScript)
            ? "No rollback SQL was provided by the model."
            : rollbackSqlScript;
    }
}
