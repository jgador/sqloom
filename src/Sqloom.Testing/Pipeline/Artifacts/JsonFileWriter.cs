using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sqloom.Pipeline.Artifacts;

/// <summary>
/// Writes typed Sqloom JSON artifacts from disk.
/// </summary>
public static class JsonFileWriter
{
    /// <summary>
    /// Writes a JSON artifact with Sqloom's default serializer options.
    /// </summary>
    public static Task WriteAsync<T>(
        string path,
        T value,
        CancellationToken cancellationToken = default)
    {
        return WriteAsync(
            path,
            value,
            configureOptions: null,
            cancellationToken);
    }

    /// <summary>
    /// Writes a JSON artifact after applying caller-provided serializer configuration.
    /// </summary>
    public static async Task WriteAsync<T>(
        string path,
        T value,
        Action<JsonSerializerOptions>? configureOptions,
        CancellationToken cancellationToken = default)
    {
        var directoryPath = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // Artifact contracts own wire names with explicit attributes; avoid ambient naming policies here.
        JsonSerializerOptions serializerOptions = new()
        {
            WriteIndented = true,
        };
        configureOptions?.Invoke(serializerOptions);

        var json = JsonSerializer.Serialize(value, serializerOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
    }
}
