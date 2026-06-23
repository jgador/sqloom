using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Captures the bootstrap evidence for a replay host.
/// </summary>
public sealed class ReplayBootstrapReport
{
    [JsonPropertyName("sqlServerDacpac")]
    public SqlServerDacpacArtifact? SqlServerDacpac { get; init; }

    [JsonPropertyName("sqlServerSeedSql")]
    public SqlServerSeedSqlArtifact? SqlServerSeedSql { get; init; }
}
