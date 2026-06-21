using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sqloom.Core.Artifacts;

/// <summary>
/// Reads typed Sqloom JSON artifacts from disk.
/// </summary>
public static class JsonFileReader
{
    public static Task<T?> ReadAsync<T>(
        string path,
        CancellationToken cancellationToken = default)
    {
        return ReadAsync<T>(
            path,
            configureOptions: null,
            cancellationToken);
    }

    public static async Task<T?> ReadAsync<T>(
        string path,
        Action<JsonSerializerOptions>? configureOptions,
        CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        JsonSerializerOptions serializerOptions = new();
        configureOptions?.Invoke(serializerOptions);
        return JsonSerializer.Deserialize<T>(json, serializerOptions);
    }
}
