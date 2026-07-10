using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Describes the SQL seed script used to populate a SQL Server replay database after DACPAC deploy.
/// </summary>
public sealed class SqlServerSeedSqlArtifact
{
    /// <summary>
    /// Gets the source path.
    /// </summary>
    [JsonPropertyName("sourcePath")]
    public required string SourcePath { get; init; }

    /// <summary>
    /// Gets the file name.
    /// </summary>
    [JsonPropertyName("fileName")]
    public required string FileName { get; init; }

    /// <summary>
    /// Gets the sha256.
    /// </summary>
    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }
}
