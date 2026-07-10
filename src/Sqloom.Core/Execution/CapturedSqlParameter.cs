using System.Text.Json.Serialization;

namespace Sqloom.Core.Execution;

/// <summary>
/// Captures one SQL parameter observed during replay.
/// </summary>
public sealed class CapturedSqlParameter
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the db type.
    /// </summary>
    [JsonPropertyName("dbType")]
    public string? DbType { get; init; }

    /// <summary>
    /// Gets the size.
    /// </summary>
    [JsonPropertyName("size")]
    public int? Size { get; init; }

    /// <summary>
    /// Gets the precision.
    /// </summary>
    [JsonPropertyName("precision")]
    public byte? Precision { get; init; }

    /// <summary>
    /// Gets the scale.
    /// </summary>
    [JsonPropertyName("scale")]
    public byte? Scale { get; init; }

    /// <summary>
    /// Gets the value.
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; init; }
}
