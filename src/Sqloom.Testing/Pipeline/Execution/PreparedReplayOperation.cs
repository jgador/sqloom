using System;
using System.Collections.Generic;

namespace Sqloom.Pipeline.Execution;

/// <summary>
/// Carries the inputs prepared for one replay operation.
/// </summary>
public sealed class PreparedReplayOperation
{
    /// <summary>
    /// Gets the persona.
    /// </summary>
    public string? Persona { get; init; }

    /// <summary>
    /// Gets the access token.
    /// </summary>
    public string? AccessToken { get; init; }

    /// <summary>
    /// Gets the request body json.
    /// </summary>
    public string? RequestBodyJson { get; init; }

    /// <summary>
    /// Gets the path values.
    /// </summary>
    public IReadOnlyDictionary<string, string> PathValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the query values.
    /// </summary>
    public IReadOnlyDictionary<string, string> QueryValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the header values.
    /// </summary>
    public IReadOnlyDictionary<string, string> HeaderValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the notes.
    /// </summary>
    public string Notes { get; init; } = string.Empty;
}
