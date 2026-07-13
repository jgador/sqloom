using System.Text.Json.Serialization;

namespace Sqloom.Pipeline.Execution;

/// <summary>
/// Describes replay persona.
/// </summary>
public sealed class ReplayPersonaDefinition
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the requires authentication.
    /// </summary>
    [JsonPropertyName("requiresAuthentication")]
    public bool RequiresAuthentication { get; init; } = true;

    /// <summary>
    /// Gets the notes.
    /// </summary>
    [JsonPropertyName("notes")]
    public string Notes { get; init; } = string.Empty;
}
