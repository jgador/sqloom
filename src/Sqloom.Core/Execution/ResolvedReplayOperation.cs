using System;
using System.Collections.Generic;

namespace Sqloom.Core.Execution;

/// <summary>
/// Captures one replay operation after Sqloom overlays and defaults are resolved.
/// </summary>
public sealed class ResolvedReplayOperation
{
    /// <summary>
    /// Gets the operation key.
    /// </summary>
    public required string OperationKey { get; init; }

    /// <summary>
    /// Gets the operation id.
    /// </summary>
    public string? OperationId { get; init; }

    /// <summary>
    /// Gets the http method.
    /// </summary>
    public required string HttpMethod { get; init; }

    /// <summary>
    /// Gets the route.
    /// </summary>
    public required string Route { get; init; }

    /// <summary>
    /// Gets the persona.
    /// </summary>
    public string? Persona { get; init; }

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
