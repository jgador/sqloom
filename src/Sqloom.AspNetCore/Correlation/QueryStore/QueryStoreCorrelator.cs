using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.AspNetCore.Capture;
using Sqloom.AspNetCore.Endpoints;
using Sqloom.Core.Artifacts;
using Sqloom.Core.Execution;
using Sqloom.QueryStore.QueryStore;

namespace Sqloom.Correlation.QueryStore;

/// <summary>
/// Maps captured replay SQL back to Query Store rows.
/// </summary>
public sealed class QueryStoreCorrelator
{
    private const double FingerprintFallbackConfidence = 0.40d;
    private const double QueryTextExactConfidence = 0.92d;
    private const double StatementHandleExactConfidence = 1.00d;
    private readonly ISqlHandleResolver _statementHandleResolver;

    public QueryStoreCorrelator(ISqlHandleResolver statementHandleResolver)
    {
        _statementHandleResolver = statementHandleResolver
            ?? throw new ArgumentNullException(nameof(statementHandleResolver));
    }

    public async Task<QueryCorrelationReport> CorrelateAsync(
        QueryStoreSnapshot snapshot,
        IReadOnlyList<EndpointReplayResult> replayResults,
        string replayArtifactDirectory,
        string? appName = null,
        string? queryStoreSnapshotPath = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(replayResults);
        ArgumentException.ThrowIfNullOrWhiteSpace(replayArtifactDirectory);

        var plansByStatementHandle = BuildPlanLookup(
            snapshot.Plans,
            static plan => QueryStoreStatementHandle.Normalize(plan.StatementSqlHandle));
        var plansByExactText = BuildPlanLookup(
            snapshot.Plans,
            static plan => QueryStoreSqlText.TrimOuterNoise(plan.QueryText));
        var plansByFingerprint = BuildPlanLookup(
            snapshot.Plans,
            static plan => ComputeCorrelationFingerprint(plan.QueryText));

        Dictionary<string, SqlHandleResolution> resolutionCache =
            new(StringComparer.Ordinal);
        List<QueryCorrelationRecord> records = new();
        HashSet<string> warnings = new(StringComparer.Ordinal);

        foreach (var replayResult in replayResults)
        {
            for (var commandIndex = 0; commandIndex < replayResult.CapturedSqlCommands.Count; commandIndex++)
            {
                var capturedCommand = replayResult.CapturedSqlCommands[commandIndex];
                var resolution = await ResolveAsync(
                        capturedCommand,
                        resolutionCache,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(resolution.ErrorMessage))
                {
                    warnings.Add(resolution.ErrorMessage);
                }

                records.Add(CreateRecord(
                    replayResult,
                    capturedCommand,
                    commandIndex + 1,
                    resolution,
                    plansByStatementHandle,
                    plansByExactText,
                    plansByFingerprint));
            }
        }

        var summary = BuildSummary(replayResults, records);
        return new QueryCorrelationReport
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            AppName = appName,
            ReplayArtifactDir = replayArtifactDirectory,
            QueryStoreSnapshotPath = queryStoreSnapshotPath,
            QueryStoreCapturedAtUtc = snapshot.CapturedAtUtc,
            Records = records,
            Summary = summary,
            Pipeline = CreatePipeline(replayArtifactDirectory, queryStoreSnapshotPath),
            Warnings = warnings.OrderBy(static warning => warning, StringComparer.Ordinal).ToArray(),
        };
    }

    private static PipelineReport CreatePipeline(
        string replayArtifactDirectory,
        string? queryStoreSnapshotPath)
    {
        var replaySummaryPath = ArtifactLayout.GetReplaySummaryPath(replayArtifactDirectory);
        var correlationArtifactPath = ArtifactLayout.GetCorrelationPath(replayArtifactDirectory);
        var adviceArtifactPath = ArtifactLayout.GetReplayTuningAdvicePath(replayArtifactDirectory);

        return new PipelineReport
        {
            Stages =
            [
                new PipelineStageReport
                {
                    Name = PipelineStageNames.Observe,
                    Status = PipelineStageStatuses.Completed,
                    Summary = "Captured a Query Store snapshot for correlation.",
                    ArtifactPath = queryStoreSnapshotPath,
                },
                new PipelineStageReport
                {
                    Name = PipelineStageNames.Replay,
                    Status = PipelineStageStatuses.Completed,
                    Summary = "Replay artifacts were available for correlation.",
                    ArtifactPath = replaySummaryPath,
                },
                new PipelineStageReport
                {
                    Name = PipelineStageNames.Capture,
                    Status = PipelineStageStatuses.Completed,
                    Summary = "Captured replay SQL was available for correlation.",
                    ArtifactPath = replayArtifactDirectory,
                },
                new PipelineStageReport
                {
                    Name = PipelineStageNames.Correlate,
                    Status = PipelineStageStatuses.Completed,
                    Summary = "Captured SQL was mapped back to Query Store rows.",
                    ArtifactPath = correlationArtifactPath,
                },
                new PipelineStageReport
                {
                    Name = PipelineStageNames.Advise,
                    Status = PipelineStageStatuses.Available,
                    Summary = "Run --advise to turn the correlation artifact into operation-level tuning guidance.",
                    ArtifactPath = adviceArtifactPath,
                },
            ],
        };
    }

    private static Dictionary<string, List<QueryStorePlanRecord>> BuildPlanLookup(
        IReadOnlyList<QueryStorePlanRecord> plans,
        Func<QueryStorePlanRecord, string> keySelector)
    {
        Dictionary<string, List<QueryStorePlanRecord>> lookup = new(StringComparer.Ordinal);
        foreach (var plan in plans)
        {
            var key = keySelector(plan);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (!lookup.TryGetValue(key, out var bucket))
            {
                bucket = new List<QueryStorePlanRecord>();
                lookup[key] = bucket;
            }

            bucket.Add(plan);
        }

        return lookup;
    }

    private async Task<SqlHandleResolution> ResolveAsync(
        CapturedSqlCommand capturedCommand,
        IDictionary<string, SqlHandleResolution> cache,
        CancellationToken cancellationToken)
    {
        var cacheKey = string.Concat(
            capturedCommand.CommandText,
            "\n--sqloom-parameters--\n",
            string.Join(
                "|",
                capturedCommand.Parameters.Select(static parameter =>
                    $"{parameter.Name}:{parameter.DbType}:{parameter.Size}:{parameter.Precision}:{parameter.Scale}")));
        if (cache.TryGetValue(cacheKey, out var existingResolution))
        {
            return existingResolution;
        }

        var resolution = await _statementHandleResolver
            .ResolveAsync(
                capturedCommand.CommandText,
                capturedCommand.Parameters.Select(static parameter => new SqlHandleParameter
                {
                    Name = parameter.Name,
                    DbType = parameter.DbType,
                    Size = parameter.Size,
                    Precision = parameter.Precision,
                    Scale = parameter.Scale,
                }).ToArray(),
                cancellationToken)
            .ConfigureAwait(false);
        cache[cacheKey] = resolution;
        return resolution;
    }

    private static QueryCorrelationRecord CreateRecord(
        EndpointReplayResult replayResult,
        CapturedSqlCommand capturedCommand,
        int commandOrdinal,
        SqlHandleResolution resolution,
        IReadOnlyDictionary<string, List<QueryStorePlanRecord>> plansByStatementHandle,
        IReadOnlyDictionary<string, List<QueryStorePlanRecord>> plansByExactText,
        IReadOnlyDictionary<string, List<QueryStorePlanRecord>> plansByFingerprint)
    {
        var comparableStatements = QueryStoreSqlText.GetComparableStatements(capturedCommand.CommandText);
        var matchedPlans = FindStatementHandleMatches(
            resolution,
            plansByStatementHandle);
        CorrelationMatchKind matchKind;
        double confidence;
        string[] notes;

        if (matchedPlans.Count > 0)
        {
            matchKind = CorrelationMatchKind.StatementHandleExact;
            confidence = StatementHandleExactConfidence;
            notes = BuildNotes(resolution, matchedPlans, "Matched Query Store statement_sql_handle exactly.");
        }
        else if (TryFindPlanMatches(comparableStatements, plansByExactText, out var exactTextMatches))
        {
            matchedPlans = exactTextMatches;
            matchKind = CorrelationMatchKind.QueryTextExact;
            confidence = QueryTextExactConfidence;
            notes = BuildNotes(
                resolution,
                matchedPlans,
                "Matched Query Store SQL text exactly after trimming only outer whitespace/comments.");
        }
        else if (TryFindPlanMatches(
            comparableStatements.Select(static statement => ComputeCorrelationFingerprint(statement)),
            plansByFingerprint,
            out var fingerprintMatches))
        {
            matchedPlans = fingerprintMatches;
            matchKind = CorrelationMatchKind.FingerprintFallback;
            confidence = FingerprintFallbackConfidence;
            notes = BuildNotes(
                resolution,
                matchedPlans,
                "Matched only by local normalized SQL fingerprint. Treat this as diagnostic, not authoritative.");
        }
        else
        {
            matchedPlans = Array.Empty<QueryStorePlanRecord>();
            matchKind = CorrelationMatchKind.Unmatched;
            confidence = 0d;
            notes = BuildNotes(resolution, matchedPlans, "No safe Query Store correlation matched this captured SQL.");
        }

        return new QueryCorrelationRecord
        {
            OperationKey = replayResult.OperationKey,
            HttpMethod = replayResult.HttpMethod,
            Route = replayResult.Route,
            Persona = replayResult.Persona,
            OperationArtifactPath = replayResult.ArtifactPath,
            CommandOrdinal = commandOrdinal,
            CapturedCommand = capturedCommand,
            ComparableSqlText = resolution.ComparableSqlText,
            StatementSqlHandle = resolution.StatementSqlHandle,
            SqlHandleCandidates = resolution.Candidates,
            MatchKind = matchKind,
            Confidence = confidence,
            MatchedPlans = matchedPlans,
            Notes = notes,
        };
    }

    private static IReadOnlyList<QueryStorePlanRecord> FindStatementHandleMatches(
        SqlHandleResolution resolution,
        IReadOnlyDictionary<string, List<QueryStorePlanRecord>> plansByStatementHandle)
    {
        HashSet<(long QueryId, long PlanId)> seen = new();
        List<QueryStorePlanRecord> matches = new();
        foreach (var candidate in resolution.Candidates)
        {
            var normalizedHandle = QueryStoreStatementHandle.Normalize(candidate.StatementSqlHandle);
            if (normalizedHandle.Length == 0
                || !plansByStatementHandle.TryGetValue(normalizedHandle, out var plans))
            {
                continue;
            }

            foreach (var plan in plans)
            {
                if (seen.Add((plan.QueryId, plan.PlanId)))
                {
                    matches.Add(plan);
                }
            }
        }

        return matches;
    }

    private static bool TryFindPlanMatches(
        IEnumerable<string> keys,
        IReadOnlyDictionary<string, List<QueryStorePlanRecord>> plansByKey,
        out List<QueryStorePlanRecord> matches)
    {
        foreach (var key in keys)
        {
            if (!string.IsNullOrWhiteSpace(key)
                && plansByKey.TryGetValue(key, out var candidateMatches)
                && candidateMatches is not null)
            {
                matches = candidateMatches;
                return true;
            }
        }

        matches = [];
        return false;
    }

    private static string ComputeCorrelationFingerprint(string sqlText)
    {
        var trimmed = QueryStoreSqlText.TrimOuterNoise(sqlText);
        var withoutParameterPrefix = QueryStoreSqlText.TrimLeadingParameterDefinitionPrefix(trimmed);
        var statementText = QueryStoreSqlText.TrimLeadingSetStatements(withoutParameterPrefix);
        return ReplaySqlTextNormalizer.ComputeFingerprint(statementText);
    }

    private static string[] BuildNotes(
        SqlHandleResolution resolution,
        IReadOnlyList<QueryStorePlanRecord> matchedPlans,
        string primaryNote)
    {
        List<string> notes = new()
        {
            primaryNote,
        };

        if (!string.IsNullOrWhiteSpace(resolution.ErrorMessage))
        {
            notes.Add($"statement_sql_handle resolution warning: {resolution.ErrorMessage}");
        }

        if (matchedPlans.Count > 1)
        {
            notes.Add("Multiple Query Store rows matched this captured statement; the correlation intentionally preserves one-to-many ownership.");
        }

        return notes.ToArray();
    }

    private static QueryCorrelationSummary BuildSummary(
        IReadOnlyList<EndpointReplayResult> replayResults,
        IReadOnlyList<QueryCorrelationRecord> records)
    {
        Dictionary<string, List<QueryCorrelationRecord>> recordsByOperationKey =
            new(StringComparer.OrdinalIgnoreCase);
        foreach (var record in records)
        {
            if (!recordsByOperationKey.TryGetValue(record.OperationKey, out var operationRecords))
            {
                operationRecords = new List<QueryCorrelationRecord>();
                recordsByOperationKey[record.OperationKey] = operationRecords;
            }

            operationRecords.Add(record);
        }

        var operations = replayResults
            .Select(replayResult =>
            {
                var operationRecords = recordsByOperationKey.TryGetValue(
                    replayResult.OperationKey,
                    out var capturedRecords)
                    ? capturedRecords
                    : [];

                return new OperationCorrelationSummary
                {
                    OperationKey = replayResult.OperationKey,
                    HttpMethod = replayResult.HttpMethod,
                    Route = replayResult.Route,
                    Persona = replayResult.Persona,
                    ReplayStatus = replayResult.Status,
                    OperationArtifactPath = replayResult.ArtifactPath,
                    CapturedCommandCount = operationRecords.Count,
                    MatchedCommandCount = operationRecords.Count(static record => record.MatchKind is not CorrelationMatchKind.Unmatched),
                    HandleExactCount = operationRecords.Count(static record => record.MatchKind == CorrelationMatchKind.StatementHandleExact),
                    QueryTextExactCount = operationRecords.Count(static record => record.MatchKind == CorrelationMatchKind.QueryTextExact),
                    FingerprintFallbackCount = operationRecords.Count(static record => record.MatchKind == CorrelationMatchKind.FingerprintFallback),
                    UnmatchedCount = operationRecords.Count(static record => record.MatchKind == CorrelationMatchKind.Unmatched),
                    MatchedQueryIds = operationRecords
                        .SelectMany(static record => record.MatchedPlans)
                        .Select(static plan => plan.QueryId)
                        .Distinct()
                        .OrderBy(static value => value)
                        .ToArray(),
                    MatchedPlanIds = operationRecords
                        .SelectMany(static record => record.MatchedPlans)
                        .Select(static plan => plan.PlanId)
                        .Distinct()
                        .OrderBy(static value => value)
                        .ToArray(),
                };
            })
            .ToArray();

        return new QueryCorrelationSummary
        {
            OperationCount = replayResults.Count,
            CapturedCommandCount = records.Count,
            MatchedCommandCount = records.Count(static record => record.MatchKind is not CorrelationMatchKind.Unmatched),
            HandleExactCount = records.Count(static record => record.MatchKind == CorrelationMatchKind.StatementHandleExact),
            QueryTextExactCount = records.Count(static record => record.MatchKind == CorrelationMatchKind.QueryTextExact),
            FingerprintFallbackCount = records.Count(static record => record.MatchKind == CorrelationMatchKind.FingerprintFallback),
            UnmatchedCount = records.Count(static record => record.MatchKind == CorrelationMatchKind.Unmatched),
            Operations = operations,
        };
    }
}
