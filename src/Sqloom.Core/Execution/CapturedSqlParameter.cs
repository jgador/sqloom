using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Captures one SQL parameter observed during replay.
/// </summary>
public sealed class CapturedSqlParameter
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("dbType")]
    public string? DbType { get; init; }

    [JsonPropertyName("size")]
    public int? Size { get; init; }

    [JsonPropertyName("precision")]
    public byte? Precision { get; init; }

    [JsonPropertyName("scale")]
    public byte? Scale { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }
}
