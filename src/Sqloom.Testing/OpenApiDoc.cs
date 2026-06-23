using System;
using System.IO;

namespace Sqloom.Testing;

/// <summary>
/// Resolves OpenAPI documents owned by the application under test.
/// </summary>
public static class OpenApiDoc
{
    public const string DefaultFileName = "openapi.json";

    public static string FindRequired(
        string applicationDirectory,
        string fileName = DefaultFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var documentPath = Path.GetFullPath(
            Path.Combine(applicationDirectory, fileName));
        if (!File.Exists(documentPath))
        {
            throw new FileNotFoundException(
                $"Could not find the OpenAPI document '{fileName}' in '{applicationDirectory}'.",
                documentPath);
        }

        return documentPath;
    }
}
