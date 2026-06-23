using System;
using System.Collections.Generic;

namespace Sqloom.Core.Execution;

/// <summary>
/// Carries the inputs prepared for one replay operation.
/// </summary>
public sealed class PreparedReplayOperation
{
    public string? Persona { get; init; }

    public string? AccessToken { get; init; }

    public string? RequestBodyJson { get; init; }

    public IReadOnlyDictionary<string, string> PathValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> QueryValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> HeaderValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public string Notes { get; init; } = string.Empty;
}
