using System;
using System.Collections.Generic;
using System.Linq;
using Sqloom.Core.Execution;

namespace Sqloom.Host.Replay;

/// <summary>
/// Builds the initial replay plan from discovered operations and app overlays.
/// </summary>
internal sealed class ReplayPlanBuilder
{
    public EndpointReplayPlan BuildInitialPlan(
        ReplayRunnerOptions options,
        IReadOnlyList<OpenApiOperation> discoveredOperations)
    {
        var targetFilter = ReplayTargetSyntax.ValidateOperationKeyOrNull(options.TargetFilter);
        var overlays = options.ReplayProfile.OperationOverlays.ToDictionary(
            operation => operation.OperationKey,
            StringComparer.OrdinalIgnoreCase);
        var candidates = discoveredOperations
            .Select(discoveredOperation =>
            {
                overlays.TryGetValue(
                    discoveredOperation.StableOperationKey,
                    out var overlay);
                var resolvedOperation = ReplayOperationResolver.Resolve(
                    discoveredOperation,
                    overlay);
                var replaySafe = IsReplaySafe(discoveredOperation, overlay, options.ReplayProfile);

                return new ReplayPlanCandidate(
                    discoveredOperation,
                    overlay,
                    resolvedOperation,
                    replaySafe);
            })
            .ToArray();

        ValidateTargetFilter(targetFilter, candidates);

        var planItems = new List<EndpointReplayPlanItem>(candidates.Length);
        var scheduledCount = 0;

        foreach (var candidate in candidates)
        {
            var discoveredOperation = candidate.DiscoveredOperation;
            var overlay = candidate.Overlay;
            var resolvedOperation = candidate.ResolvedOperation;
            var replaySafe = candidate.ReplaySafe;
            string status;
            string? reason = null;

            if (IsFilteredOut(targetFilter, discoveredOperation))
            {
                status = "skipped";
                reason = BuildFilterReason(targetFilter, discoveredOperation.StableOperationKey);
            }
            else if (overlay?.SkipReason is { Length: > 0 } skipReason)
            {
                status = "skipped";
                reason = skipReason;
            }
            else if (!replaySafe)
            {
                status = "skipped";
                reason = BuildUnsafeReason(discoveredOperation, overlay);
            }
            else if (overlay?.ReplayByDefault == false
                && !HasExplicitReplaySelection(targetFilter))
            {
                status = "skipped";
                reason = BuildOptInReason(discoveredOperation.StableOperationKey);
            }
            else if (scheduledCount >= options.MaxOperations)
            {
                status = "skipped";
                reason = $"Skipped because --max-operations={options.MaxOperations} was reached.";
            }
            else
            {
                scheduledCount++;
                status = "planned";
            }

            planItems.Add(new EndpointReplayPlanItem
            {
                OperationKey = discoveredOperation.StableOperationKey,
                OperationId = discoveredOperation.OperationId,
                HttpMethod = discoveredOperation.HttpMethod,
                Route = discoveredOperation.Route,
                Persona = resolvedOperation.Persona,
                RequiresAuthentication = discoveredOperation.RequiresAuthentication,
                HasJsonRequestBody = discoveredOperation.HasJsonRequestBody,
                ReplaySafe = replaySafe,
                Status = status,
                Reason = reason,
                Notes = resolvedOperation.Notes,
            });
        }

        return new EndpointReplayPlan
        {
            AppName = options.AppName,
            OpenApiPath = options.OpenApiPath,
            PlannedAtUtc = DateTimeOffset.UtcNow,
            Operations = planItems,
        };
    }

    private static bool IsFilteredOut(
        string? targetFilter,
        OpenApiOperation operation)
    {
        return !string.IsNullOrWhiteSpace(targetFilter)
            && !MatchesTarget(targetFilter, operation);
    }

    private static bool HasExplicitReplaySelection(string? targetFilter)
    {
        return !string.IsNullOrWhiteSpace(targetFilter);
    }

    private static string BuildOptInReason(string operationKey)
    {
        return $"Skipped because operation '{operationKey}' is opt-in. Select it with --target \"{operationKey}\".";
    }

    private static string BuildFilterReason(
        string? targetFilter,
        string operationKey)
    {
        if (!string.IsNullOrWhiteSpace(targetFilter))
        {
            return $"Skipped by --target=\"{targetFilter}\".";
        }

        return $"Skipped because operation '{operationKey}' did not match the active filter.";
    }

    private static bool MatchesTarget(
        string? targetFilter,
        OpenApiOperation operation)
    {
        if (string.IsNullOrWhiteSpace(targetFilter))
        {
            return true;
        }

        return string.Equals(operation.StableOperationKey, targetFilter, StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateTargetFilter(
        string? targetFilter,
        IReadOnlyList<ReplayPlanCandidate> candidates)
    {
        if (string.IsNullOrWhiteSpace(targetFilter))
        {
            return;
        }

        var matchingOperationKeys = candidates
            .Where(candidate => MatchesTarget(
                targetFilter,
                candidate.DiscoveredOperation))
            .Select(candidate => candidate.DiscoveredOperation.StableOperationKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (matchingOperationKeys.Length == 1)
        {
            return;
        }

        if (matchingOperationKeys.Length == 0)
        {
            var message =
                $"No replay operation matched '{targetFilter}'. --target must match one discovered OpenAPI operation key exactly in the form 'METHOD /path/template'.";
            if (TryFindSuggestedOperationKey(targetFilter, candidates) is { } suggestedOperationKey)
            {
                message += $" Did you mean '{suggestedOperationKey}'?";
            }

            throw new ArgumentException(message);
        }

        throw new ArgumentException(
            $"The replay target '{targetFilter}' matched multiple operations: {string.Join(", ", matchingOperationKeys)}. Use an exact operation key in the form 'METHOD /path/template'.");
    }

    private static string? TryFindSuggestedOperationKey(
        string targetFilter,
        IReadOnlyList<ReplayPlanCandidate> candidates)
    {
        var availableOperationKeys = candidates
            .Select(candidate => candidate.DiscoveredOperation.StableOperationKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (availableOperationKeys.Length == 0)
        {
            return null;
        }

        var targetMethod = GetMethod(targetFilter);
        var closestCandidate = availableOperationKeys
            .Select(operationKey => new
            {
                OperationKey = operationKey,
                Score = CalculateSuggestionScore(targetFilter, operationKey, targetMethod),
            })
            .OrderBy(candidate => candidate.Score)
            .ThenBy(candidate => candidate.OperationKey, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (closestCandidate is null || closestCandidate.Score > 4)
        {
            return null;
        }

        return closestCandidate.OperationKey;
    }

    private static string GetMethod(string operationKey)
    {
        var separatorIndex = operationKey.IndexOf(' ');
        return separatorIndex > 0
            ? operationKey[..separatorIndex]
            : operationKey;
    }

    private static int CalculateSuggestionScore(
        string targetFilter,
        string operationKey,
        string targetMethod)
    {
        var baseDistance = CalculateLevenshteinDistance(
            targetFilter.ToLowerInvariant(),
            operationKey.ToLowerInvariant());
        if (string.Equals(targetMethod, GetMethod(operationKey), StringComparison.OrdinalIgnoreCase))
        {
            return baseDistance;
        }

        return baseDistance + 2;
    }

    private static int CalculateLevenshteinDistance(string source, string target)
    {
        var previousRow = new int[target.Length + 1];
        var currentRow = new int[target.Length + 1];

        for (var targetIndex = 0; targetIndex <= target.Length; targetIndex++)
        {
            previousRow[targetIndex] = targetIndex;
        }

        for (var sourceIndex = 1; sourceIndex <= source.Length; sourceIndex++)
        {
            currentRow[0] = sourceIndex;
            for (var targetIndex = 1; targetIndex <= target.Length; targetIndex++)
            {
                var substitutionCost = source[sourceIndex - 1] == target[targetIndex - 1] ? 0 : 1;
                currentRow[targetIndex] = Math.Min(
                    Math.Min(
                        currentRow[targetIndex - 1] + 1,
                        previousRow[targetIndex] + 1),
                    previousRow[targetIndex - 1] + substitutionCost);
            }

            (previousRow, currentRow) = (currentRow, previousRow);
        }

        return previousRow[target.Length];
    }

    private static bool IsReplaySafe(
        OpenApiOperation operation,
        ReplayOverlay? overlay,
        ReplayProfile replayProfile)
    {
        if (overlay?.AllowNonGetReplay == true)
        {
            return true;
        }

        return replayProfile.IncludeAuthGetOps
            && operation.RequiresAuthentication
            && string.Equals(operation.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildUnsafeReason(
        OpenApiOperation operation,
        ReplayOverlay? overlay)
    {
        if (!operation.RequiresAuthentication)
        {
            return "Anonymous operations are not replayed by default in V1.";
        }

        if (!string.Equals(operation.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase)
            && overlay?.AllowNonGetReplay != true)
        {
            return "Non-GET operations require an explicit replay overlay.";
        }

        return "Operation did not satisfy the V1 replay-safe filter.";
    }

    private sealed record ReplayPlanCandidate(
        OpenApiOperation DiscoveredOperation,
        ReplayOverlay? Overlay,
        ResolvedReplayOperation ResolvedOperation,
        bool ReplaySafe);
}
