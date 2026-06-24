using System;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Xunit;

namespace Sqloom.Host.Tests;

/// <summary>
/// Exercises terminal-friendly debug rendering.
/// </summary>
public sealed class HostDebugWriterTests
{
    [Fact]
    public void PrintOpenAIRequest_DecodesReadableStringBlocks()
    {
        var requestJson = JsonSerializer.Serialize(new
        {
            model = "gpt-5.5",
            instructions = "Line 1 \"quote\" > threshold\nLine 2\tTabbed and O'Hare",
            input = "App: Sqloom Test App\nartifact_manifest_json:\n{\r\n  \"manifestVersion\": \"v2\",\r\n  \"sample\": \"<tag>\"\r\n}",
            text = new
            {
                format = new
                {
                    type = "json_schema",
                },
            },
        });
        HostDebugWriter writer = new(isEnabled: true);

        var standardError = CaptureStandardError(() =>
        {
            writer.PrintOpenAIRequest(
                new Uri("https://api.openai.com/v1/responses"),
                new AuthenticationHeaderValue("Bearer", "secret-value"),
                requestJson);
        });

        Assert.Contains("[sqloom debug] [advise] OpenAI request", standardError, StringComparison.Ordinal);
        Assert.Contains("authorization=Bearer ***REDACTED***", standardError, StringComparison.Ordinal);
        Assert.Contains("\"instructions\": |", standardError, StringComparison.Ordinal);
        Assert.Contains("Line 1 \"quote\" > threshold", standardError, StringComparison.Ordinal);
        Assert.Contains("Line 2\tTabbed and O'Hare", standardError, StringComparison.Ordinal);
        Assert.Contains("\"input\": |", standardError, StringComparison.Ordinal);
        Assert.Contains("artifact_manifest_json:", standardError, StringComparison.Ordinal);
        Assert.Contains("\"manifestVersion\": \"v2\"", standardError, StringComparison.Ordinal);
        Assert.Contains("\"sample\": \"<tag>\"", standardError, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u0022", standardError, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u0027", standardError, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u003c", standardError, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\\u003e", standardError, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\\r\\n", standardError, StringComparison.Ordinal);
    }

    [Fact]
    public void PrintOpenAIResponse_PrettyPrintsNestedOutputTextJson()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            output_text = JsonSerializer.Serialize(new
            {
                recommendations = new[]
                {
                    new
                    {
                        title = "Reduce \"reads\"",
                    },
                },
                proposals = Array.Empty<object>(),
            }),
        });
        HostDebugWriter writer = new(isEnabled: true);

        var standardError = CaptureStandardError(() =>
        {
            writer.PrintOpenAIResponse(
                HttpStatusCode.OK,
                responseJson);
        });

        Assert.Contains("[sqloom debug] [advise] OpenAI response", standardError, StringComparison.Ordinal);
        Assert.Contains("\"output_text\":", standardError, StringComparison.Ordinal);
        Assert.Contains("\"recommendations\":", standardError, StringComparison.Ordinal);
        Assert.Contains("Reduce \"reads\"", standardError, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u0022", standardError, StringComparison.Ordinal);
    }

    [Fact]
    public void PrintOpenAIResponse_FallsBackToDecodedBlockForInvalidNestedJsonText()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            error = new
            {
                message = "{not-json}\nnext \"line\"",
            },
        });
        HostDebugWriter writer = new(isEnabled: true);

        var standardError = CaptureStandardError(() =>
        {
            writer.PrintOpenAIResponse(
                HttpStatusCode.BadRequest,
                responseJson);
        });

        Assert.Contains("\"message\": |", standardError, StringComparison.Ordinal);
        Assert.Contains("{not-json}", standardError, StringComparison.Ordinal);
        Assert.Contains("next \"line\"", standardError, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u0022", standardError, StringComparison.Ordinal);
        Assert.DoesNotContain("\\nnext", standardError, StringComparison.Ordinal);
    }

    private static string CaptureStandardError(Action action)
    {
        ConsoleGate.Semaphore.Wait();
        var originalError = Console.Error;
        using StringWriter standardError = new();

        try
        {
            Console.SetError(standardError);
            action();
            return standardError.ToString();
        }
        finally
        {
            Console.SetError(originalError);
            ConsoleGate.Semaphore.Release();
        }
    }
}
