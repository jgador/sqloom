using System.Collections.Generic;
using Sqloom.Core.Execution;

namespace Sqloom.Host.Replay;

/// <summary>
/// Resolves resolved replay operation.
/// </summary>
internal static class ReplayOperationResolver
{
    public static ResolvedReplayOperation Resolve(
        OpenApiOperation discoveredOperation,
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
