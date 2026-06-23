using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sqloom.AspNetCore.Endpoints;
using Sqloom.Core.Execution;
using Sqloom.Correlation.QueryStore;
using Sqloom.QueryStore.QueryStore;

namespace Sqloom.Host;

/// <summary>
/// Prints Sqloom host output to the console.
/// </summary>
internal sealed class HostConsoleWriter
{
    public void PrintBanner(
        string? appName,
        IReadOnlyList<string> projectNames)
    {
        Console.WriteLine("Sqloom host");
        Console.WriteLine($"App: {appName ?? "not selected"}");
        Console.WriteLine("Projects:");
        foreach (var projectName in projectNames)
        {
            Console.WriteLine($"- {projectName}");
        }

        Console.WriteLine("Replay catalog: OpenAPI discovery with app-owned overlays.");
    }

    public void PrintUsage()
    {
        Console.WriteLine("Sqloom host usage:");
        Console.WriteLine("  --help");
        Console.WriteLine("  --version");
        Console.WriteLine("  [--debug] observe [<path>] --read-only-connection-string <connection-string> [options]");
        Console.WriteLine("  [--debug] tune <path> [--read-only-connection-string <connection-string>] [options]");
        Console.WriteLine("  [--debug] replay <path> [options]");
        Console.WriteLine("  [--debug] correlate --replay-artifact-dir <path> --query-store-snapshot-file <path> --read-only-connection-string <connection-string> [options]");
        Console.WriteLine("  [--debug] advise --replay-artifact-dir <path> [options]");
        Console.WriteLine(
            "  [--debug] observe [<path>] [--dotnet-command <command>] [--no-build] --read-only-connection-string <connection-string> [--lookback-hours <hours>] [--max-plans <count>] [--max-waits <count>] [--command-timeout-seconds <seconds>] [--json-output-file <path>] [--app-only] [--show-classification]");
        Console.WriteLine("    --app-only implies classification display and filters the console view to App-classified queries when the selected harness supplies Query Store profile data.");
        Console.WriteLine(
            "  [--debug] tune <path> [--dotnet-command <command>] [--no-build] [--read-only-connection-string <connection-string>] [--lookback-hours <hours>] [--max-plans <count>] [--max-waits <count>] [--command-timeout-seconds <seconds>] [--app-only] [--show-classification] [--openapi-file <path>] [--sqlserver-dacpac-file <path>] [--sqlserver-seed-sql-file <path>] [--artifact-dir <path>] [--max-operations <count>] [--target \"<METHOD /path/template>\"] --model-provider openai --openai-api-key <key> [--sqlserver-schema-file <path>] [--openai-model <id>] [--openai-base-url <url>]");
        Console.WriteLine("    Tune starts the harness session, runs replay -> observe -> correlate -> advise in one command, and disposes the session. It writes query-store-snapshot.json and tune-summary.json at the workflow root, then replay, correlation, and advice artifacts under the workflow replay/ directory.");
        Console.WriteLine("    Tune uses --read-only-connection-string and --sqlserver-schema-file when supplied, otherwise it uses the harness session connection string and manifest schema path.");
        Console.WriteLine("    When omitted, --artifact-dir defaults to artifacts/sqloom/tune/tune-<timestamp>. With tune, --artifact-dir means the workflow root, not a replay-only directory.");
        Console.WriteLine(
            "  [--debug] replay <path> [--dotnet-command <command>] [--no-build] [--openapi-file <path>] [--sqlserver-dacpac-file <path>] [--sqlserver-seed-sql-file <path>] [--artifact-dir <path>] [--max-operations <count>] [--target \"<METHOD /path/template>\"]");
        Console.WriteLine("    Standalone replay requires an explicit target path after the replay verb. Supported target paths are harness project files, harness assemblies, solution files, solution filters, and directories.");
        Console.WriteLine("    Sqloom resolves that target, builds harness projects unless --no-build is supplied, and requires exactly one public non-abstract ISqloomApplication implementation.");
        Console.WriteLine("    Pass --dotnet-command <command> when Sqloom should use a non-default dotnet executable for nested project resolution and builds.");
        Console.WriteLine("    If a solution, solution filter, or directory resolves to zero or multiple ISqloomApplication implementations, Sqloom fails and asks for a narrower target.");
        Console.WriteLine("    SQL Server-backed replay harnesses can provide app-owned DACPAC and seed defaults; --sqlserver-dacpac-file and --sqlserver-seed-sql-file override them.");
        Console.WriteLine("    Replay targets must use the exact form 'METHOD /path/template', for example --target \"GET /api/expenses/dashboard\".");
        Console.WriteLine("    Replay defaults to authenticated GET operations plus any app overlays enabled by default. Opt-in operations such as POST /api/advisor/query require explicit --target selection.");
        Console.WriteLine(
            "  [--debug] correlate --replay-artifact-dir <path> --query-store-snapshot-file <path> --read-only-connection-string <connection-string> [--json-output-file <path>]");
        Console.WriteLine("    Correlation resolves statement_sql_handle against captured replay SQL, then writes query-store-correlation.json under the replay artifact directory by default.");
        Console.WriteLine(
            "  [--debug] advise --replay-artifact-dir <path> [--query-store-correlation-file <path>] [--json-output-file <path>] --model-provider openai --openai-api-key <key> --sqlserver-schema-file <path> [--openai-model <id>] [--openai-base-url <url>]");
        Console.WriteLine("    Advice derives operation-level tuning guidance from query-store-correlation.json plus the supplied SQL Server schema file, then writes tuning-advice.json, sql-tuning-proposal.json, and sql-tuning-proposal.sql under the replay artifact directory by default.");
        Console.WriteLine("    OpenAI advice requires --model-provider openai, --openai-api-key, and --sqlserver-schema-file.");
        Console.WriteLine("    Use --debug to print per-stage diagnostics to stderr. With advise, debug prints the redacted OpenAI request and response payloads.");
    }

    public void PrintVersion(string version)
    {
        Console.WriteLine($"sqloom {version}");
    }

    public void PrintNoCommandHint()
    {
        Console.WriteLine("Use tune <path> to start the harness and run the full replay, observe, correlate, and advise workflow in one command.");
        Console.WriteLine("Use observe to capture a readonly SQL Server or Azure SQL Query Store snapshot.");
        Console.WriteLine("Use replay <path> to execute OpenAPI-driven in-process ASP.NET Core replays explicitly.");
        Console.WriteLine("Standalone replay accepts a harness project, harness assembly, solution, solution filter, or directory path immediately after the replay verb.");
        Console.WriteLine("Sqloom resolves the target to exactly one public non-abstract ISqloomApplication implementation.");
        Console.WriteLine("SQL Server-backed replay harnesses can provide app-owned DACPAC and seed defaults; CLI paths override them.");
        Console.WriteLine("Replay target selection uses --target \"METHOD /path/template\" when you need one exact operation.");
        Console.WriteLine("Use correlate to map replay SQL back to captured Query Store rows.");
        Console.WriteLine("Use advise to turn a correlation artifact into operation-level tuning guidance.");
        Console.WriteLine("Use --debug to print per-stage diagnostics to stderr.");
        Console.WriteLine("Use --help to print the available host arguments.");
        Console.WriteLine("Use --version to print the installed Sqloom tool version.");
    }

    public void PrintQueryStoreSnapshot(
        QueryStoreSnapshot snapshot,
        string jsonOutputPath,
        bool appOnly,
        bool showClassification)
    {
        var displayedPlans = appOnly
            ? snapshot.Plans.Where(ShouldIncludeInAppOnly).ToArray()
            : snapshot.Plans;
        var displayedWaits = appOnly
            ? snapshot.Waits.Where(ShouldIncludeInAppOnly).ToArray()
            : snapshot.Waits;

        Console.WriteLine("Query Store snapshot:");
        Console.WriteLine(
            $"- Window: last {snapshot.LookbackWindow.TotalHours:F1} hours captured at {snapshot.CapturedAtUtc:O}");
        Console.WriteLine(
            $"- State: {snapshot.DatabaseOptions.ActualState} (desired {snapshot.DatabaseOptions.DesiredState})");
        Console.WriteLine(
            $"- Storage: {snapshot.DatabaseOptions.CurrentStorageSizeMb:F1} MB / {snapshot.DatabaseOptions.MaxStorageSizeMb:F1} MB");
        Console.WriteLine($"- Profile: {snapshot.WorkloadProfileName ?? WorkloadProfile.Empty.Name}");
        Console.WriteLine(appOnly
            ? $"- Plans: {displayedPlans.Count} shown / {snapshot.Plans.Count} captured"
            : $"- Plans: {snapshot.Plans.Count}");
        Console.WriteLine(appOnly
            ? $"- Waits: {displayedWaits.Count} shown / {snapshot.Waits.Count} captured"
            : $"- Waits: {snapshot.Waits.Count}");
        Console.WriteLine($"- Snapshot path: {jsonOutputPath}");
        if (snapshot.DiscoveredObjectCatalog is { } discoveredObjectCatalog)
        {
            Console.WriteLine(
                $"- Discovered objects: {discoveredObjectCatalog.Objects.Count} from {discoveredObjectCatalog.SourceName} ({(discoveredObjectCatalog.IsComplete ? "complete" : "partial")})");
            if (discoveredObjectCatalog.Warnings.Count > 0)
            {
                Console.WriteLine($"- Discovery warnings: {string.Join(" | ", discoveredObjectCatalog.Warnings)}");
            }
        }

        if (snapshot.DatabaseOptions.ReadOnlyReason != 0)
        {
            Console.WriteLine($"- Readonly reason: {snapshot.DatabaseOptions.ReadOnlyReason}");
        }

        if (appOnly)
        {
            Console.WriteLine("- Filter: app-only console view");
        }

        if (displayedPlans.Count > 0)
        {
            Console.WriteLine("Top plans:");
            foreach (var plan in displayedPlans)
            {
                Console.WriteLine(
                    $"- query_id={plan.QueryId}, plan_id={plan.PlanId}, duration_ms={plan.MeanDuration.TotalMilliseconds:F2}, cpu_ms={plan.MeanCpuMilliseconds:F2}, reads={plan.MeanLogicalReads:F1}, execs={plan.ExecutionCount}");
                Console.WriteLine(
                    $"  query_hash={plan.QueryHash}, object={plan.ObjectName ?? "ad hoc"}, last_exec_utc={FormatTimestamp(plan.LastExecutionTimeUtc)}");
                Console.WriteLine($"  sql={SummarizeSqlText(plan.QueryText)}");
                if (showClassification)
                {
                    PrintClassification(plan.Classification);
                }
            }
        }
        else if (appOnly)
        {
            Console.WriteLine("Top plans:");
            Console.WriteLine("- No App-classified Query Store plans matched the current snapshot.");
        }

        if (displayedWaits.Count > 0)
        {
            Console.WriteLine("Top waits:");
            foreach (var wait in displayedWaits)
            {
                Console.WriteLine(
                    $"- query_id={wait.QueryId}, plan_id={wait.PlanId}, wait={wait.WaitCategory}, total_ms={wait.TotalWaitMilliseconds:F2}, avg_ms={wait.AvgWaitMs:F2}");
                if (showClassification)
                {
                    PrintClassification(wait.Classification);
                }
            }
        }
        else if (appOnly)
        {
            Console.WriteLine("Top waits:");
            Console.WriteLine("- No App-classified Query Store waits matched the current snapshot.");
        }
    }

    public void PrintReplaySummary(
        EndpointReplayRunResult replayResult,
        RunReport runReport)
    {
        Console.WriteLine("Replay summary:");
        Console.WriteLine($"- App: {runReport.AppName}");
        Console.WriteLine($"- OpenAPI document: {replayResult.OpenApiPath}");
        Console.WriteLine($"- Artifact directory: {replayResult.ReplayArtifactDir}");
        Console.WriteLine($"- Discovered operations: {runReport.DiscoveredOperationCount}");
        Console.WriteLine($"- Planned operations: {runReport.PlannedOperationCount}");
        Console.WriteLine($"- Replayed operations: {replayResult.Results.Count(result => string.Equals(result.Status, "replayed", StringComparison.OrdinalIgnoreCase))}");
        Console.WriteLine($"- Failed operations: {replayResult.Results.Count(result => string.Equals(result.Status, "failed", StringComparison.OrdinalIgnoreCase))}");
        Console.WriteLine($"- Skipped operations: {replayResult.ReplayPlan.Operations.Count(item => string.Equals(item.Status, "skipped", StringComparison.OrdinalIgnoreCase))}");
        if (runReport.ReplayBootstrap.SqlServerDacpac is { } sqlServerDacpac)
        {
            Console.WriteLine($"- SQL Server DACPAC: {sqlServerDacpac.FileName}");
            Console.WriteLine($"- DACPAC path: {sqlServerDacpac.SourcePath}");
            Console.WriteLine($"- DACPAC sha256: {sqlServerDacpac.Sha256}");
        }

        if (runReport.ReplayBootstrap.SqlServerSeedSql is { } sqlServerSeedSql)
        {
            Console.WriteLine($"- SQL seed script: {sqlServerSeedSql.FileName}");
            Console.WriteLine($"- Seed script path: {sqlServerSeedSql.SourcePath}");
            Console.WriteLine($"- Seed script sha256: {sqlServerSeedSql.Sha256}");
        }

        Console.WriteLine($"- Discovery artifact: {replayResult.DiscoveredOpsPath}");
        Console.WriteLine($"- Replay plan artifact: {replayResult.ReplayPlanArtifactPath}");
        Console.WriteLine($"- Summary artifact: {replayResult.SummaryArtifactPath}");
        PrintPipeline(runReport.Pipeline);

        Console.WriteLine("Discovered operations:");
        foreach (var operation in replayResult.DiscoveredOperations)
        {
            Console.WriteLine(
                $"- [{operation.HttpMethod}] {operation.Route} | auth={(operation.RequiresAuthentication ? "yes" : "no")} | body={(operation.HasJsonRequestBody ? "json" : "none")}");
        }

        Console.WriteLine("Replay plan:");
        foreach (var planItem in replayResult.ReplayPlan.Operations)
        {
            Console.WriteLine(
                $"- operation={planItem.OperationKey} | status={planItem.Status}");
            if (!string.IsNullOrWhiteSpace(planItem.Reason))
            {
                Console.WriteLine($"  reason={planItem.Reason}");
            }
        }

        if (replayResult.Results.Count == 0)
        {
            return;
        }

        Console.WriteLine("Replay results:");
        foreach (var result in replayResult.Results)
        {
            Console.WriteLine(
                $"- {result.OperationKey}: status={result.Status}, http={(result.HttpStatusCode?.ToString(CultureInfo.InvariantCulture) ?? "n/a")}, duration_ms={result.DurationMilliseconds:F2}, sql={result.CapturedSqlCommands.Count}");
            Console.WriteLine($"  artifact={result.ArtifactPath}");
            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                Console.WriteLine($"  error={result.ErrorMessage}");
            }

            foreach (var command in result.CapturedSqlCommands)
            {
                Console.WriteLine(
                    $"  sql[{command.SourceKind}] duration_ms={command.Duration.TotalMilliseconds:F2}, fingerprint={command.Fingerprint}, params={command.Parameters.Count}");
                Console.WriteLine($"  text={SummarizeSqlText(command.CommandText)}");
            }
        }
    }

    public void PrintCorrelationSummary(
        QueryCorrelationReport report,
        string jsonOutputPath)
    {
        Console.WriteLine("Query Store correlation:");
        Console.WriteLine($"- App: {report.AppName ?? "unknown"}");
        Console.WriteLine($"- Replay artifact directory: {report.ReplayArtifactDir}");
        Console.WriteLine($"- Query Store snapshot: {report.QueryStoreSnapshotPath ?? "n/a"}");
        Console.WriteLine($"- Query Store captured at: {report.QueryStoreCapturedAtUtc:O}");
        Console.WriteLine($"- Output path: {jsonOutputPath}");
        Console.WriteLine($"- Operations: {report.Summary.OperationCount}");
        Console.WriteLine($"- Captured SQL commands: {report.Summary.CapturedCommandCount}");
        Console.WriteLine($"- Matched commands: {report.Summary.MatchedCommandCount}");
        Console.WriteLine($"- StatementHandleExact: {report.Summary.HandleExactCount}");
        Console.WriteLine($"- QueryTextExact: {report.Summary.QueryTextExactCount}");
        Console.WriteLine($"- FingerprintFallback: {report.Summary.FingerprintFallbackCount}");
        Console.WriteLine($"- Unmatched: {report.Summary.UnmatchedCount}");

        if (report.Warnings.Count > 0)
        {
            Console.WriteLine($"- Warnings: {string.Join(" | ", report.Warnings)}");
        }

        PrintPipeline(report.Pipeline);

        Console.WriteLine("Operation summary:");
        foreach (var operation in report.Summary.Operations)
        {
            Console.WriteLine(
                $"- {operation.OperationKey}: status={operation.ReplayStatus}, sql={operation.CapturedCommandCount}, matched={operation.MatchedCommandCount}, handle={operation.HandleExactCount}, text={operation.QueryTextExactCount}, fingerprint={operation.FingerprintFallbackCount}, unmatched={operation.UnmatchedCount}");
            if (operation.MatchedQueryIds.Count > 0)
            {
                Console.WriteLine($"  query_ids={string.Join(", ", operation.MatchedQueryIds)}");
            }
        }
    }

    public void PrintAdviceSummary(
        AdviceReport report,
        string jsonOutputPath)
    {
        Console.WriteLine("Tuning advice:");
        Console.WriteLine($"- App: {report.AppName}");
        Console.WriteLine($"- Replay artifact directory: {report.ReplayArtifactDir}");
        Console.WriteLine($"- Correlation artifact: {report.QueryStoreCorrelationPath}");
        Console.WriteLine($"- Output path: {jsonOutputPath}");
        Console.WriteLine($"- SQL proposal JSON: {report.SqlProposalJsonPath}");
        Console.WriteLine($"- SQL proposal script: {report.SqlProposalScriptPath}");
        Console.WriteLine($"- Model provider: {report.ModelProvider}");
        if (!string.IsNullOrWhiteSpace(report.ModelName))
        {
            Console.WriteLine($"- Model: {report.ModelName}");
        }

        Console.WriteLine($"- Strategy: {report.StrategyName}");
        Console.WriteLine($"- Operations: {report.Summary.OperationCount}");
        Console.WriteLine($"- Recommendations: {report.Summary.RecommendationCount}");
        Console.WriteLine($"- SQL proposals: {report.Summary.ProposalCount}");
        if (report.Warnings.Count > 0)
        {
            Console.WriteLine($"- Warnings: {string.Join(" | ", report.Warnings)}");
        }

        PrintPipeline(report.Pipeline);

        Console.WriteLine("Operation advice:");
        foreach (var operation in report.Operations)
        {
            Console.WriteLine(
                $"- {operation.OperationKey}: replay={operation.ReplayStatus}, sql={operation.CapturedCommandCount}, matched={operation.MatchedCommandCount}, recommendations={operation.Recommendations.Count}, proposals={operation.Proposals.Count}");
            foreach (var recommendation in operation.Recommendations)
            {
                Console.WriteLine($"  title={recommendation.Title}");
                Console.WriteLine($"  cause={recommendation.RootCause}");
                Console.WriteLine($"  change={recommendation.SuggestedChange}");
                Console.WriteLine($"  verify={recommendation.VerificationMetric}");
            }

            foreach (var proposal in operation.Proposals)
            {
                Console.WriteLine($"  proposal={proposal.Title}");
                Console.WriteLine($"  target={proposal.TargetObject}");
                Console.WriteLine($"  kind={proposal.ProposalKind}");
                Console.WriteLine($"  verify={proposal.VerificationMetric}");
            }
        }
    }

    public void PrintTuneSummary(
        TuneWorkflowReport report,
        string summaryOutputPath)
    {
        Console.WriteLine("Tune summary:");
        Console.WriteLine($"- App: {report.AppName}");
        Console.WriteLine($"- Workflow artifact directory: {report.WorkflowArtifactDir}");
        Console.WriteLine($"- Query Store snapshot: {report.QueryStoreSnapshotPath}");
        Console.WriteLine($"- Replay artifact directory: {report.ReplayArtifactDir}");
        Console.WriteLine($"- Correlation artifact: {report.QueryStoreCorrelationPath}");
        Console.WriteLine($"- Advice artifact: {report.TuningAdvicePath}");
        Console.WriteLine($"- SQL proposal JSON: {report.SqlProposalJsonPath}");
        Console.WriteLine($"- SQL proposal script: {report.SqlProposalScriptPath}");
        Console.WriteLine($"- Summary path: {summaryOutputPath}");
        Console.WriteLine($"- Model provider: {report.ModelProvider}");
        if (!string.IsNullOrWhiteSpace(report.ModelName))
        {
            Console.WriteLine($"- Model: {report.ModelName}");
        }

        Console.WriteLine($"- Snapshot plans: {report.Summary.QueryStorePlanCount}");
        Console.WriteLine($"- Snapshot waits: {report.Summary.QueryStoreWaitCount}");
        Console.WriteLine($"- Replay operations: {report.Summary.ReplayOperationCount}");
        Console.WriteLine($"- Replayed operations: {report.Summary.ReplayedOperationCount}");
        Console.WriteLine($"- Failed operations: {report.Summary.FailedOperationCount}");
        Console.WriteLine($"- Captured SQL commands: {report.Summary.CapturedCommandCount}");
        Console.WriteLine($"- Matched SQL commands: {report.Summary.MatchedCommandCount}");
        Console.WriteLine($"- Recommendations: {report.Summary.RecommendationCount}");
        Console.WriteLine($"- SQL proposals: {report.Summary.ProposalCount}");
        if (report.Warnings.Count > 0)
        {
            Console.WriteLine($"- Warnings: {string.Join(" | ", report.Warnings)}");
        }

        PrintPipeline(report.Pipeline);
    }

    private static string FormatTimestamp(DateTimeOffset? value)
    {
        return value?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) ?? "n/a";
    }

    private static void PrintClassification(QueryWorkloadClassification? classification)
    {
        var effectiveClassification = classification ?? new QueryWorkloadClassification
        {
            Kind = QueryWorkloadKind.Unknown,
            Confidence = 0d,
            IncludeInAppOnly = false,
            Reasons = ["No classification was attached."],
        };

        Console.WriteLine(
            $"  classification={effectiveClassification.Kind}, confidence={effectiveClassification.Confidence:F2}, include_in_app_only={effectiveClassification.IncludeInAppOnly}");
        Console.WriteLine($"  reasons={string.Join(" | ", effectiveClassification.Reasons)}");
    }

    private static void PrintPipeline(PipelineReport pipeline)
    {
        Console.WriteLine("Pipeline:");
        foreach (var stage in pipeline.Stages)
        {
            Console.WriteLine($"- {stage.Name}: {stage.Status}");
            Console.WriteLine($"  summary={stage.Summary}");
            if (!string.IsNullOrWhiteSpace(stage.ArtifactPath))
            {
                Console.WriteLine($"  artifact={stage.ArtifactPath}");
            }
        }
    }

    private static bool ShouldIncludeInAppOnly(QueryStorePlanRecord plan)
    {
        return plan.Classification?.IncludeInAppOnly == true;
    }

    private static bool ShouldIncludeInAppOnly(QueryStoreWaitStat wait)
    {
        return wait.Classification?.IncludeInAppOnly == true;
    }

    private static string SummarizeSqlText(string sqlText)
    {
        const int maxLength = 280;

        var normalized = string.Join(
            ' ',
            sqlText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        return string.Concat(normalized.AsSpan(0, maxLength - 3), "...");
    }
}
