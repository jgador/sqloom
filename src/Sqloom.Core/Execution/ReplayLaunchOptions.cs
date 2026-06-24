using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Carries app-owned launch inputs for replay host bootstrap.
/// </summary>
public sealed class ReplayLaunchOptions
{
    [JsonPropertyName("dacpacPath")]
    public string? DacpacPath { get; init; }

    [JsonPropertyName("seedSqlPath")]
    public string? SeedSqlPath { get; init; }
}
