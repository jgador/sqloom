using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Describes replay persona.
/// </summary>
public sealed class ReplayPersonaDefinition
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("requiresAuthentication")]
    public bool RequiresAuthentication { get; init; } = true;

    [JsonPropertyName("notes")]
    public string Notes { get; init; } = string.Empty;
}
