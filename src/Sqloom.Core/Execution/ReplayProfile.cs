using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Describes the replay defaults, personas, and overlays for a Sqloom app.
/// </summary>
public sealed class ReplayProfile
{
    [JsonPropertyName("includeAuthGetOps")]
    public bool IncludeAuthGetOps { get; init; } = true;

    [JsonPropertyName("personas")]
    public IReadOnlyList<ReplayPersonaDefinition> Personas { get; init; } =
        Array.Empty<ReplayPersonaDefinition>();

    [JsonPropertyName("operationOverlays")]
    public IReadOnlyList<ReplayOverlay> OperationOverlays { get; init; } =
        Array.Empty<ReplayOverlay>();
}
