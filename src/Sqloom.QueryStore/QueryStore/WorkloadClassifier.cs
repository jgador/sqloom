using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sqloom.QueryStore.QueryStore;

/// <summary>
/// Classifies Query Store rows against an app-owned workload profile.
/// </summary>
public sealed class WorkloadClassifier
{
    private static readonly string[] _defaultToolingPatterns =
    [
        "sys.all_objects",
        "sys.all_views",
        "sys.all_columns",
        "sys.sql_modules",
        "sys.extended_properties",
        "objectpropertyex",
        "columnproperty",
        "information_schema",
    ];

    private static readonly string[] _defaultPlatformPatterns =
    [
        "sys.dm_db_resource_stats",
        "sys.database_service_objectives",
        "backup_metadata_store",
    ];

    public QueryStoreSnapshot ApplyClassification(
        QueryStoreSnapshot snapshot,
        WorkloadProfile? profile = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var effectiveProfile = profile ?? WorkloadProfile.Empty;
        Dictionary<(long QueryId, long PlanId), QueryWorkloadClassification> planClassifications = new();
        var classifiedPlans = new List<QueryStorePlanRecord>(snapshot.Plans.Count);

        foreach (var plan in snapshot.Plans)
        {
            var classification = ClassifyPlan(plan, effectiveProfile);
            planClassifications[(plan.QueryId, plan.PlanId)] = classification;
            classifiedPlans.Add(new QueryStorePlanRecord
            {
                QueryId = plan.QueryId,
                PlanId = plan.PlanId,
                QueryTextId = plan.QueryTextId,
                StatementSqlHandle = plan.StatementSqlHandle,
                ObjectId = plan.ObjectId,
                QueryHash = plan.QueryHash,
                QueryText = plan.QueryText,
                ObjectName = plan.ObjectName,
                QueryParameterizationType = plan.QueryParameterizationType,
                ParamTypeDescription = plan.ParamTypeDescription,
                ExecutionCount = plan.ExecutionCount,
                MeanDuration = plan.MeanDuration,
                MaxDuration = plan.MaxDuration,
                MeanCpuMilliseconds = plan.MeanCpuMilliseconds,
                MeanLogicalReads = plan.MeanLogicalReads,
                LastExecutionTimeUtc = plan.LastExecutionTimeUtc,
                Classification = classification,
            });
        }

        var classifiedWaits = new List<QueryStoreWaitStat>(snapshot.Waits.Count);
        foreach (var wait in snapshot.Waits)
        {
            var classification = planClassifications.TryGetValue((wait.QueryId, wait.PlanId), out var inheritedClassification)
                ? InheritWaitClassification(wait, inheritedClassification)
                : CreateClassification(
                    QueryWorkloadKind.Unknown,
                    0.20d,
                    includeInAppOnly: false,
                    $"No matching plan record for query_id={wait.QueryId}, plan_id={wait.PlanId}; wait classification could not be inherited.");

            classifiedWaits.Add(new QueryStoreWaitStat
            {
                QueryId = wait.QueryId,
                PlanId = wait.PlanId,
                WaitCategory = wait.WaitCategory,
                AvgWaitMs = wait.AvgWaitMs,
                TotalWaitMilliseconds = wait.TotalWaitMilliseconds,
                Classification = classification,
            });
        }

        return new QueryStoreSnapshot
        {
            CapturedAtUtc = snapshot.CapturedAtUtc,
            LookbackWindow = snapshot.LookbackWindow,
            DatabaseOptions = snapshot.DatabaseOptions,
            WorkloadProfileName = snapshot.WorkloadProfileName ?? effectiveProfile.Name,
            DiscoveredObjectCatalog = snapshot.DiscoveredObjectCatalog ?? effectiveProfile.DiscoveredObjectCatalog,
            Plans = classifiedPlans,
            Waits = classifiedWaits,
        };
    }

    public QueryWorkloadClassification ClassifyPlan(
        QueryStorePlanRecord plan,
        WorkloadProfile? profile = null)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var effectiveProfile = profile ?? WorkloadProfile.Empty;
        var textView = new QueryTextView(plan.QueryText);
        var objectNameView = new QueryTextView(plan.ObjectName);
        var reasons = new List<string>();

        CollectPhraseReasons(reasons, _defaultPlatformPatterns, textView, objectNameView, "Matched platform pattern");
        if (reasons.Count > 0)
        {
            return CreateClassification(QueryWorkloadKind.Platform, 0.98d, includeInAppOnly: false, reasons);
        }

        reasons.Clear();
        CollectPhraseReasons(reasons, _defaultToolingPatterns, textView, objectNameView, "Matched tooling pattern");
        if (reasons.Count > 0)
        {
            return CreateClassification(QueryWorkloadKind.Tooling, 0.97d, includeInAppOnly: false, reasons);
        }

        reasons.Clear();
        CollectDbObjectReasons(reasons, effectiveProfile.DiscoveredObjectCatalog, textView, objectNameView);
        if (reasons.Count > 0)
        {
            return CreateClassification(QueryWorkloadKind.App, 0.94d, includeInAppOnly: true, reasons);
        }

        return CreateClassification(
            QueryWorkloadKind.Unknown,
            0.25d,
            includeInAppOnly: false,
            BuildUnknownReason(effectiveProfile.DiscoveredObjectCatalog));
    }

    private static void CollectDbObjectReasons(
        ICollection<string> reasons,
        DbObjectCatalog? catalog,
        QueryTextView queryText,
        QueryTextView objectName)
    {
        if (catalog is null)
        {
            return;
        }

        foreach (var discoveredObject in catalog.Objects)
        {
            if (!MatchesDiscoveredObject(queryText, objectName, discoveredObject))
            {
                continue;
            }

            AddUniqueReason(
                reasons,
                $"Matched discovered {discoveredObject.Kind.ToString().ToLowerInvariant()} object: {discoveredObject.FullyQualifiedName}");
        }
    }

    private static bool MatchesDiscoveredObject(
        QueryTextView queryText,
        QueryTextView objectName,
        DiscoveredDatabaseObject discoveredObject)
    {
        if (queryText.ContainsQualifiedIdentifier(discoveredObject.SchemaName, discoveredObject.ObjectName)
            || objectName.ContainsQualifiedIdentifier(discoveredObject.SchemaName, discoveredObject.ObjectName))
        {
            return true;
        }

        var normalizedObjectName = NormalizeIdentifier(discoveredObject.ObjectName);
        return discoveredObject.Kind switch
        {
            DbObjectKind.Table or DbObjectKind.View =>
                queryText.ContainsTableLikeReference(normalizedObjectName)
                || objectName.ContainsToken(normalizedObjectName),
            DbObjectKind.Module =>
                queryText.ContainsExecutionReference(normalizedObjectName)
                || objectName.ContainsToken(normalizedObjectName),
            _ => false,
        };
    }

    private static void CollectPhraseReasons(
        ICollection<string> reasons,
        IEnumerable<string> patterns,
        QueryTextView queryText,
        QueryTextView objectName,
        string reasonPrefix)
    {
        foreach (var pattern in patterns)
        {
            var normalizedPattern = NormalizePhrase(pattern);
            if (normalizedPattern.Length == 0)
            {
                continue;
            }

            if (queryText.ContainsPhrase(normalizedPattern) || objectName.ContainsPhrase(normalizedPattern))
            {
                AddUniqueReason(reasons, $"{reasonPrefix}: {pattern}");
            }
        }
    }

    private static QueryWorkloadClassification InheritWaitClassification(
        QueryStoreWaitStat wait,
        QueryWorkloadClassification planClassification)
    {
        var inheritedReasons = new string[planClassification.Reasons.Count + 1];
        inheritedReasons[0] = $"Inherited from matching plan record query_id={wait.QueryId}, plan_id={wait.PlanId}.";
        for (var index = 0; index < planClassification.Reasons.Count; index++)
        {
            inheritedReasons[index + 1] = planClassification.Reasons[index];
        }

        return new QueryWorkloadClassification
        {
            Kind = planClassification.Kind,
            Confidence = planClassification.Confidence,
            IncludeInAppOnly = planClassification.IncludeInAppOnly,
            Reasons = inheritedReasons,
        };
    }

    private static string BuildUnknownReason(DbObjectCatalog? catalog)
    {
        if (catalog is null)
        {
            return "No app, tooling, or platform workload rules matched. App classification requires a discovered-object catalog, but none was attached.";
        }

        if (!catalog.IsComplete)
        {
            return "No app, tooling, or platform workload rules matched. The discovered-object catalog is partial.";
        }

        return "No app, tooling, or platform workload rules matched.";
    }

    private static QueryWorkloadClassification CreateClassification(
        QueryWorkloadKind kind,
        double baseConfidence,
        bool includeInAppOnly,
        params string[] reasons)
    {
        return CreateClassification(kind, baseConfidence, includeInAppOnly, (IReadOnlyCollection<string>)reasons);
    }

    private static QueryWorkloadClassification CreateClassification(
        QueryWorkloadKind kind,
        double baseConfidence,
        bool includeInAppOnly,
        IReadOnlyCollection<string> reasons)
    {
        var confidence = Math.Clamp(baseConfidence + Math.Max(reasons.Count - 1, 0) * 0.02d, 0d, 1d);
        return new QueryWorkloadClassification
        {
            Kind = kind,
            Confidence = confidence,
            IncludeInAppOnly = includeInAppOnly,
            Reasons = reasons.ToArray(),
        };
    }

    private static void AddUniqueReason(ICollection<string> reasons, string reason)
    {
        if (reasons.Contains(reason, StringComparer.Ordinal))
        {
            return;
        }

        reasons.Add(reason);
    }

    private static string NormalizePhrase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(
            ' ',
            value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
    }

    private static string NormalizeIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return NormalizeTokenText(value).Trim();
    }

    private static string NormalizeTokenText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return " ";
        }

        var builder = new StringBuilder(value.Length + 2);
        builder.Append(' ');
        var previousWasSeparator = true;

        foreach (var character in value)
        {
            var normalized = char.ToLowerInvariant(character);
            if (char.IsLetterOrDigit(normalized) || normalized == '_')
            {
                builder.Append(normalized);
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator)
            {
                builder.Append(' ');
                previousWasSeparator = true;
            }
        }

        if (!previousWasSeparator)
        {
            builder.Append(' ');
        }

        return builder.ToString();
    }

    private readonly record struct QueryTextView(string? Value)
    {
        public string PhraseText { get; } = NormalizePhrase(Value);

        public string TokenText { get; } = NormalizeTokenText(Value);

        public bool ContainsPhrase(string normalizedPhrase)
        {
            return normalizedPhrase.Length > 0
                && PhraseText.Contains(normalizedPhrase, StringComparison.Ordinal);
        }

        public bool ContainsToken(string normalizedToken)
        {
            return normalizedToken.Length > 0
                && TokenText.Contains($" {normalizedToken} ", StringComparison.Ordinal);
        }

        public bool ContainsQualifiedIdentifier(string schema, string identifier)
        {
            if (string.IsNullOrWhiteSpace(schema) || string.IsNullOrWhiteSpace(identifier))
            {
                return false;
            }

            var bracketedPattern = NormalizePhrase($"[{schema}].[{identifier}]");
            var dottedPattern = NormalizePhrase($"{schema}.{identifier}");
            return ContainsPhrase(bracketedPattern)
                || ContainsPhrase(dottedPattern);
        }

        public bool ContainsTableLikeReference(string normalizedIdentifier)
        {
            return ContainsVerbReference("from", normalizedIdentifier)
                || ContainsVerbReference("join", normalizedIdentifier)
                || ContainsVerbReference("into", normalizedIdentifier)
                || ContainsVerbReference("update", normalizedIdentifier)
                || ContainsVerbReference("merge", normalizedIdentifier)
                || ContainsVerbReference("delete from", normalizedIdentifier)
                || ContainsVerbReference("truncate table", normalizedIdentifier);
        }

        public bool ContainsExecutionReference(string normalizedIdentifier)
        {
            return ContainsVerbReference("exec", normalizedIdentifier)
                || ContainsVerbReference("execute", normalizedIdentifier);
        }

        private bool ContainsVerbReference(string verb, string normalizedIdentifier)
        {
            if (normalizedIdentifier.Length == 0)
            {
                return false;
            }

            return ContainsPhrase(NormalizePhrase($"{verb} [{normalizedIdentifier}]"))
                || ContainsPhrase(NormalizePhrase($"{verb} {normalizedIdentifier}"));
        }
    }
}
