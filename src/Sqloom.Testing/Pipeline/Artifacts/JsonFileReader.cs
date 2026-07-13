using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sqloom.Pipeline.Artifacts;

/// <summary>
/// Reads typed Sqloom JSON artifacts from disk.
/// </summary>
public static class JsonFileReader
{
    /// <summary>
    /// Reads a JSON artifact with Sqloom's default serializer options.
    /// </summary>
    public static Task<T?> ReadAsync<T>(
        string path,
        CancellationToken cancellationToken = default)
    {
        return ReadAsync<T>(
            path,
            configureOptions: null,
            cancellationToken);
    }

    /// <summary>
    /// Reads a JSON artifact after applying caller-provided serializer configuration.
    /// </summary>
    public static async Task<T?> ReadAsync<T>(
        string path,
        Action<JsonSerializerOptions>? configureOptions,
        CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);

        // Keep reads aligned with persisted artifact contracts instead of web serializer defaults.
        JsonSerializerOptions serializerOptions = new();
        configureOptions?.Invoke(serializerOptions);
        return JsonSerializer.Deserialize<T>(json, serializerOptions);
    }
}
