using System.Collections.Generic;
using Sqloom.Pipeline.Execution;

namespace Sqloom.Host.Replay;

/// <summary>
/// Builds resolved replay operations from discovered endpoint metadata and overlays.
/// </summary>
internal static class ResolvedReplayOperationBuilder
{
    public static ResolvedReplayOperation Build(
        ReplayOperation discoveredOperation,
        ReplayOverlay? overlay)
    {
        return new ResolvedReplayOperation
        {
            OperationKey = discoveredOperation.StableOperationKey,
            OperationId = discoveredOperation.OperationId,
            HttpMethod = discoveredOperation.HttpMethod,
            Route = discoveredOperation.Route,
            Persona = overlay?.Persona,
            RequestBodyJson = overlay?.RequestBodyJson,
            PathValues = overlay?.PathValues
                ?? EmptyStringDictionary.Instance,
            QueryValues = overlay?.QueryValues
                ?? EmptyStringDictionary.Instance,
            HeaderValues = overlay?.HeaderValues
                ?? EmptyStringDictionary.Instance,
            Notes = overlay?.Notes ?? string.Empty,
        };
    }

    /// <summary>
    /// Provides reusable empty dictionaries for resolved replay operations.
    /// </summary>
    private static class EmptyStringDictionary
    {
        public static IReadOnlyDictionary<string, string> Instance { get; } =
            new Dictionary<string, string>();
    }
}
