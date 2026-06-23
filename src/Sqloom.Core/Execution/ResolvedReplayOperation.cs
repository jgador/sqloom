using System;
using System.Collections.Generic;

namespace Sqloom.Core.Execution;

/// <summary>
/// Captures one replay operation after Sqloom overlays and defaults are resolved.
/// </summary>
public sealed class ResolvedReplayOperation
{
    public required string OperationKey { get; init; }

    public string? OperationId { get; init; }

    public required string HttpMethod { get; init; }

    public required string Route { get; init; }

    public string? Persona { get; init; }

    public string? RequestBodyJson { get; init; }

    public IReadOnlyDictionary<string, string> PathValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> QueryValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> HeaderValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public string Notes { get; init; } = string.Empty;
}
