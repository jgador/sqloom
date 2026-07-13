using System.Text.Json.Serialization;

namespace Sqloom.Pipeline.Execution;

/// <summary>
/// Describes the DACPAC used to bootstrap a SQL Server replay database.
/// </summary>
public sealed class SqlServerDacpacArtifact
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
