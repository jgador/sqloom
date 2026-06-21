using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.AspNetCore.Endpoints;
using Sqloom.Core.Artifacts;
using Sqloom.Correlation.QueryStore;
using Sqloom.QueryStore.QueryStore;

namespace Sqloom.Host;

/// <summary>
/// Builds the evidence-and-schema bundle sent to OpenAI for one Sqloom operation.
/// </summary>
internal sealed class OpenAIAdviceEvidencePackBuilder
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    private readonly string _queryStoreCorrelationPath;
    private readonly QueryStoreSnapshot? _queryStoreSnapshot;
    private readonly string? _queryStoreSnapshotPath;
    private readonly string _sqlServerSchemaPath;
    private readonly string _sqlServerSchemaText;
    private readonly IReadOnlyList<string> _sharedWarnings;

    private OpenAIAdviceEvidencePackBuilder(
        string queryStoreCorrelationPath,
        string sqlServerSchemaPath,
        string sqlServerSchemaText,
        QueryStoreSnapshot? queryStoreSnapshot,
        string? queryStoreSnapshotPath,
        IReadOnlyList<string> sharedWarnings)
    {
        _queryStoreCorrelationPath = queryStoreCorrelationPath;
        _sqlServerSchemaPath = sqlServerSchemaPath;
        _sqlServerSchemaText = sqlServerSchemaText;
        _queryStoreSnapshot = queryStoreSnapshot;
        _queryStoreSnapshotPath = queryStoreSnapshotPath;
        _sharedWarnings = sharedWarnings;
    }

    public static async Task<OpenAIAdviceEvidencePackBuilder> CreateAsync(
        string queryStoreCorrelationPath,
        string sqlServerSchemaPath,
        string sqlServerSchemaText,
        string? queryStoreSnapshotPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queryStoreCorrelationPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(sqlServerSchemaPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(sqlServerSchemaText);

        HashSet<string> warnings = new(StringComparer.Ordinal);
        var queryStoreSnapshot = await LoadQueryStoreSnapshotAsync(
                queryStoreSnapshotPath,
                warnings,
                cancellationToken)
            .ConfigureAwait(false);

        return new OpenAIAdviceEvidencePackBuilder(
            queryStoreCorrelationPath,
            sqlServerSchemaPath,
            sqlServerSchemaText,
            queryStoreSnapshot,
            queryStoreSnapshotPath,
            warnings.ToArray());
    }

    public async Task<OpenAIAdviceEvidencePack> BuildAsync(
        QueryStoreCorrelationReport correlationReport,
        QueryStoreCorrelationOperationSummary operation,
        IReadOnlyList<QueryStoreCorrelationRecord> records,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(correlationReport);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(records);

        HashSet<string> warnings = new(StringComparer.Ordinal);
        foreach (var warning in correlationReport.Warnings)
        {
            warnings.Add(warning);
        }

        foreach (var warning in _sharedWarnings)
        {
            warnings.Add(warning);
        }

        var replayOperation = await LoadReplayOperationAsync(
                operation,
                warnings,
                cancellationToken)
            .ConfigureAwait(false);

        var artifactManifest = new
        {
            manifestVersion = "sqloom-artifact-manifest-v2",
            bundleKind = "replay_advice",
            operationKey = operation.OperationKey,
            artifacts = BuildArtifacts(operation, replayOperation is not null),
        };
        var sourceEvidence = new
        {
            operationContext = new
            {
                appName = correlationReport.AppName ?? "unknown",
                operationKey = operation.OperationKey,
                httpMethod = operation.HttpMethod,
                route = operation.Route,
                persona = operation.Persona,
                replayStatus = operation.ReplayStatus,
                capturedCommandCount = operation.CapturedCommandCount,
                matchedCommandCount = operation.MatchedCommandCount,
            },
            queryStoreCorrelation = new
            {
                summary = new
                {
                    capturedCommandCount = operation.CapturedCommandCount,
                    matchedCommandCount = operation.MatchedCommandCount,
                    statementHandleExactCount = operation.StatementHandleExactCount,
                    queryTextExactCount = operation.QueryTextExactCount,
                    fingerprintFallbackCount = operation.FingerprintFallbackCount,
                    unmatchedCount = operation.UnmatchedCount,
                    matchedQueryIds = operation.MatchedQueryIds,
                    matchedPlanIds = operation.MatchedPlanIds,
                },
                records = records.Select(CreateCorrelationRecordProjection).ToArray(),
            },
            replayOperation = replayOperation is null
                ? null
                : CreateReplayOperationProjection(replayOperation),
            queryStoreSnapshot = _queryStoreSnapshot is null
                ? null
                : CreateQueryStoreSnapshotProjection(
                    _queryStoreSnapshot,
                    records),
            warnings = warnings.ToArray(),
        };

        return new OpenAIAdviceEvidencePack
        {
            ArtifactManifestJson = JsonSerializer.Serialize(
                artifactManifest,
                SerializerOptions),
            SourceEvidenceJson = JsonSerializer.Serialize(
                sourceEvidence,
                SerializerOptions),
            SqlServerSchemaText = _sqlServerSchemaText,
            Warnings = warnings.ToArray(),
        };
    }

    private object[] BuildArtifacts(
        QueryStoreCorrelationOperationSummary operation,
        bool hasReplayOperation)
    {
        List<object> artifacts =
        [
            new
            {
                artifactId = "query_store_correlation",
                stage = "correlate",
                logicalPath = Path.GetFileName(_queryStoreCorrelationPath),
                format = "json",
                projection = "operation_projection",
            },
            new
            {
                artifactId = "sqlserver_schema",
                stage = "schema",
                logicalPath = Path.GetFileName(_sqlServerSchemaPath),
                format = "sql",
                projection = "full_text",
            },
        ];

        if (_queryStoreSnapshot is not null)
        {
            artifacts.Add(
                new
                {
                    artifactId = "query_store_snapshot",
                    stage = "observe",
                    logicalPath = Path.GetFileName(_queryStoreSnapshotPath),
                    format = "json",
                    projection = "matched_plan_subset",
                });
        }

        if (hasReplayOperation)
        {
            artifacts.Add(
                new
                {
                    artifactId = "replay_operation",
                    stage = "replay",
                    logicalPath = BuildReplayOperationLogicalPath(operation.OperationArtifactPath),
                    format = "json",
                    projection = "safe_projection",
                });
        }

        return artifacts.ToArray();
    }

    private static object CreateCorrelationRecordProjection(QueryStoreCorrelationRecord record)
    {
        return new
        {
            commandOrdinal = record.CommandOrdinal,
            matchKind = record.MatchKind.ToString(),
            confidence = record.Confidence,
            comparableSqlText = record.ComparableSqlText,
            statementSqlHandle = record.StatementSqlHandle,
            statementSqlHandleCandidateCount = record.StatementSqlHandleCandidates.Count,
            capturedCommand = CreateCapturedCommandProjection(record.CapturedCommand),
            matchedPlans = record.MatchedPlans.Select(CreateMatchedPlanProjection).ToArray(),
            notes = record.Notes,
        };
    }

    private static object CreateReplayOperationProjection(EndpointReplayResult replayOperation)
    {
        return new
        {
            status = replayOperation.Status,
            httpStatusCode = replayOperation.HttpStatusCode,
            durationMilliseconds = replayOperation.DurationMilliseconds,
            errorMessage = replayOperation.ErrorMessage,
            request = new
            {
                relativePathAndQuery = replayOperation.Request.RelativePathAndQuery,
                pathValues = replayOperation.Request.PathValues,
                queryValues = replayOperation.Request.QueryValues,
                requestBodyJson = replayOperation.Request.RequestBodyJson,
            },
            capturedSqlCommands = replayOperation.CapturedSqlCommands
                .Select((command, index) => new
                {
                    ordinal = index + 1,
                    sourceKind = command.SourceKind.ToString(),
                    source = command.Source,
                    commandText = command.CommandText,
                    normalizedCommandText = command.NormalizedCommandText,
                    fingerprint = command.Fingerprint,
                    durationMilliseconds = command.Duration.TotalMilliseconds,
                    recordsAffected = command.RecordsAffected,
                    parameters = command.Parameters.Select(static parameter => new
                    {
                        name = parameter.Name,
                        dbType = parameter.DbType,
                        size = parameter.Size,
                        precision = parameter.Precision,
                        scale = parameter.Scale,
                        value = parameter.Value,
                    }).ToArray(),
                })
                .ToArray(),
        };
    }

    private static object CreateQueryStoreSnapshotProjection(
        QueryStoreSnapshot queryStoreSnapshot,
        IReadOnlyList<QueryStoreCorrelationRecord> records)
    {
        var matchedPlanIds = records
            .SelectMany(static record => record.MatchedPlans)
            .Select(static plan => (plan.QueryId, plan.PlanId))
            .Distinct()
            .ToHashSet();
        var relevantWaits = queryStoreSnapshot.Waits
            .Where(wait => matchedPlanIds.Contains((wait.QueryId, wait.PlanId)))
            .Select(static wait => new
            {
                queryId = wait.QueryId,
                planId = wait.PlanId,
                waitCategory = wait.WaitCategory,
                averageQueryWaitMilliseconds = wait.AverageQueryWaitMilliseconds,
                totalWaitMilliseconds = wait.TotalWaitMilliseconds,
                classification = wait.Classification is null
                    ? null
                    : new
                    {
                        kind = wait.Classification.Kind.ToString(),
                        confidence = wait.Classification.Confidence,
                        includeInAppOnly = wait.Classification.IncludeInAppOnly,
                        reasons = wait.Classification.Reasons,
                    },
            })
            .ToArray();

        return new
        {
            capturedAtUtc = queryStoreSnapshot.CapturedAtUtc,
            lookbackWindowHours = queryStoreSnapshot.LookbackWindow.TotalHours,
            databaseOptions = new
            {
                desiredState = queryStoreSnapshot.DatabaseOptions.DesiredState,
                actualState = queryStoreSnapshot.DatabaseOptions.ActualState,
                readOnlyReason = queryStoreSnapshot.DatabaseOptions.ReadOnlyReason,
                currentStorageSizeMb = queryStoreSnapshot.DatabaseOptions.CurrentStorageSizeMb,
                maxStorageSizeMb = queryStoreSnapshot.DatabaseOptions.MaxStorageSizeMb,
            },
            workloadProfileName = queryStoreSnapshot.WorkloadProfileName,
            discoveredObjectCatalog = queryStoreSnapshot.DiscoveredObjectCatalog is null
                ? null
                : new
                {
                    sourceName = queryStoreSnapshot.DiscoveredObjectCatalog.SourceName,
                    isComplete = queryStoreSnapshot.DiscoveredObjectCatalog.IsComplete,
                    objectCount = queryStoreSnapshot.DiscoveredObjectCatalog.Objects.Count,
                    warnings = queryStoreSnapshot.DiscoveredObjectCatalog.Warnings,
                },
            relevantWaits,
        };
    }

    private static object CreateCapturedCommandProjection(CapturedSqlCommand command)
    {
        return new
        {
            sourceKind = command.SourceKind.ToString(),
            source = command.Source,
            commandText = command.CommandText,
            normalizedCommandText = command.NormalizedCommandText,
            fingerprint = command.Fingerprint,
            durationMilliseconds = command.Duration.TotalMilliseconds,
            recordsAffected = command.RecordsAffected,
            parameters = command.Parameters.Select(static parameter => new
            {
                name = parameter.Name,
                dbType = parameter.DbType,
                size = parameter.Size,
                precision = parameter.Precision,
                scale = parameter.Scale,
                value = parameter.Value,
            }).ToArray(),
        };
    }

    private static object CreateMatchedPlanProjection(QueryStorePlanRecord plan)
    {
        return new
        {
            queryId = plan.QueryId,
            planId = plan.PlanId,
            queryTextId = plan.QueryTextId,
            statementSqlHandle = plan.StatementSqlHandle,
            objectId = plan.ObjectId,
            objectName = plan.ObjectName,
            queryHash = plan.QueryHash,
            queryText = plan.QueryText,
            queryParameterizationType = plan.QueryParameterizationType,
            queryParameterizationTypeDescription = plan.QueryParameterizationTypeDescription,
            executionCount = plan.ExecutionCount,
            meanDurationMilliseconds = plan.MeanDuration.TotalMilliseconds,
            maxDurationMilliseconds = plan.MaxDuration.TotalMilliseconds,
            meanCpuMilliseconds = plan.MeanCpuMilliseconds,
            meanLogicalReads = plan.MeanLogicalReads,
            lastExecutionTimeUtc = plan.LastExecutionTimeUtc,
            classification = plan.Classification is null
                ? null
                : new
                {
                    kind = plan.Classification.Kind.ToString(),
                    confidence = plan.Classification.Confidence,
                    includeInAppOnly = plan.Classification.IncludeInAppOnly,
                    reasons = plan.Classification.Reasons,
                },
        };
    }

    private static async Task<QueryStoreSnapshot?> LoadQueryStoreSnapshotAsync(
        string? queryStoreSnapshotPath,
        ISet<string> warnings,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(queryStoreSnapshotPath))
        {
            return null;
        }

        if (!File.Exists(queryStoreSnapshotPath))
        {
            warnings.Add(
                $"Query Store snapshot artifact '{Path.GetFileName(queryStoreSnapshotPath)}' was unavailable while building OpenAI evidence.");
            return null;
        }

        var snapshot = await JsonFileReader
            .ReadAsync<QueryStoreSnapshot>(
                queryStoreSnapshotPath,
                static serializerOptions => serializerOptions.Converters.Add(new JsonStringEnumConverter()),
                cancellationToken)
            .ConfigureAwait(false);
        if (snapshot is null)
        {
            warnings.Add(
                $"Query Store snapshot artifact '{Path.GetFileName(queryStoreSnapshotPath)}' could not be deserialized while building OpenAI evidence.");
        }

        return snapshot;
    }

    private static async Task<EndpointReplayResult?> LoadReplayOperationAsync(
        QueryStoreCorrelationOperationSummary operation,
        ISet<string> warnings,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(operation.OperationArtifactPath))
        {
            warnings.Add(
                $"Replay operation artifact '{Path.GetFileName(operation.OperationArtifactPath)}' was unavailable while building OpenAI evidence for '{operation.OperationKey}'.");
            return null;
        }

        var replayOperation = await JsonFileReader
            .ReadAsync<EndpointReplayResult>(
                operation.OperationArtifactPath,
                cancellationToken)
            .ConfigureAwait(false);
        if (replayOperation is null)
        {
            warnings.Add(
                $"Replay operation artifact '{Path.GetFileName(operation.OperationArtifactPath)}' could not be deserialized while building OpenAI evidence for '{operation.OperationKey}'.");
        }

        return replayOperation;
    }

    private static string BuildReplayOperationLogicalPath(string operationArtifactPath)
    {
        return Path.Combine(
                "operations",
                Path.GetFileName(operationArtifactPath))
            .Replace('\\', '/');
    }

}

/// <summary>
/// Carries the serialized evidence bundle for one OpenAI advice request.
/// </summary>
internal sealed class OpenAIAdviceEvidencePack
{
    public required string ArtifactManifestJson { get; init; }

    public required string SourceEvidenceJson { get; init; }

    public required string SqlServerSchemaText { get; init; }

    public required IReadOnlyList<string> Warnings { get; init; }
}
