using System.Text.Json.Serialization;

namespace Sqloom.Pipeline.Execution;

/// <summary>
/// Captures the bootstrap evidence for a replay host.
/// </summary>
public sealed class ReplayBootstrapReport
{
    /// <summary>
    /// Gets the sql server dacpac.
    /// </summary>
    [JsonPropertyName("sqlServerDacpac")]
    public SqlServerDacpacArtifact? SqlServerDacpac { get; init; }

    /// <summary>
    /// Gets the sql server seed sql.
    /// </summary>
    [JsonPropertyName("sqlServerSeedSql")]
    public SqlServerSeedSqlArtifact? SqlServerSeedSql { get; init; }
}
