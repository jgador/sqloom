using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Carries app-owned launch inputs for replay host bootstrap.
/// </summary>
public sealed class ReplayLaunchOptions
{
    /// <summary>
    /// Gets the dacpac path.
    /// </summary>
    [JsonPropertyName("dacpacPath")]
    public string? DacpacPath { get; init; }

    /// <summary>
    /// Gets the seed sql path.
    /// </summary>
    [JsonPropertyName("seedSqlPath")]
    public string? SeedSqlPath { get; init; }
}
