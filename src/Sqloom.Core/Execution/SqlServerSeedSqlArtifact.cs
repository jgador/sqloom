using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Describes the SQL seed script used to populate a SQL Server replay database after DACPAC deploy.
/// </summary>
public sealed class SqlServerSeedSqlArtifact
{
    [JsonPropertyName("sourcePath")]
    public required string SourcePath { get; init; }

    [JsonPropertyName("fileName")]
    public required string FileName { get; init; }

    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }
}
