using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Describes the replay defaults, personas, and overlays for a Sqloom app.
/// </summary>
public sealed class ReplayProfile
{
    /// <summary>
    /// Gets the include auth get ops.
    /// </summary>
    [JsonPropertyName("includeAuthGetOps")]
    public bool IncludeAuthGetOps { get; init; } = true;

    /// <summary>
    /// Gets the personas.
    /// </summary>
    [JsonPropertyName("personas")]
    public IReadOnlyList<ReplayPersonaDefinition> Personas { get; init; } =
        Array.Empty<ReplayPersonaDefinition>();

    /// <summary>
    /// Gets the operation overlays.
    /// </summary>
    [JsonPropertyName("operationOverlays")]
    public IReadOnlyList<ReplayOverlay> OperationOverlays { get; init; } =
        Array.Empty<ReplayOverlay>();
}
