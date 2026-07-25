using System;
using System.Text.Json;

namespace Sqloom.Host;

/// <summary>
/// Reads model output text from OpenAI Responses API payloads.
/// </summary>
internal static class OpenAIAdviceResponseReader
{
    public static string ReadOutputText(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);

        // Responses payloads can expose output text either at the root or under output[].content[].
        if (TryReadStringProperty(document.RootElement, "output_text", out var outputText))
        {
            return outputText;
        }

        if (TryReadNestedOutputText(document.RootElement, out outputText))
        {
            return outputText;
        }

        throw new InvalidOperationException("OpenAI tuning advice returned empty output.");
    }

    private static bool TryReadNestedOutputText(JsonElement root, out string outputText)
    {
        outputText = string.Empty;
        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var outputItem in output.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (TryReadOutputTextContent(contentItem, out outputText))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryReadOutputTextContent(JsonElement contentItem, out string outputText)
    {
        outputText = string.Empty;
        return contentItem.TryGetProperty("type", out var type)
            && string.Equals(type.GetString(), "output_text", StringComparison.Ordinal)
            && TryReadStringProperty(contentItem, "text", out outputText);
    }

    private static bool TryReadStringProperty(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var text = property.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        value = text;
        return true;
    }
}
