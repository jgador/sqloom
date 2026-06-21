using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sqloom.Core.Artifacts;

/// <summary>
/// Writes typed Sqloom JSON artifacts from disk.
/// </summary>
public static class JsonFileWriter
{
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

        JsonSerializerOptions serializerOptions = new()
        {
            WriteIndented = true,
        };
        configureOptions?.Invoke(serializerOptions);

        var json = JsonSerializer.Serialize(value, serializerOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
    }
}
