using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Describes the DACPAC used to bootstrap a SQL Server replay database.
/// </summary>
public sealed class SqlServerDacpacArtifact
{
    [JsonPropertyName("sourcePath")]
    public required string SourcePath { get; init; }

    [JsonPropertyName("fileName")]
    public required string FileName { get; init; }

    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }
}
