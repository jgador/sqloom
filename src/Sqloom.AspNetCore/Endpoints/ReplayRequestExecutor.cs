using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.AspNetCore.Capture;
using Sqloom.Core.Execution;

namespace Sqloom.AspNetCore.Endpoints;

/// <summary>
/// Executes replay requests and captures the resulting HTTP and SQL evidence.
/// </summary>
internal sealed class ReplayRequestExecutor
{
    public async Task<EndpointReplayResult> ExecuteAsync(
        HttpClient httpClient,
        ReplaySqlCaptureCollector? captureCollector,
        EndpointReplayRequest request,
        string? accessToken,
        string artifactPath,
        CancellationToken cancellationToken)
    {
        var message = new HttpRequestMessage(new HttpMethod(request.HttpMethod), request.RelativePathAndQuery);
        try
        {
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            foreach ((var key, var value) in request.HeaderValues)
            {
                message.Headers.TryAddWithoutValidation(key, value);
            }

            if (!string.IsNullOrWhiteSpace(request.RequestBodyJson))
            {
                message.Content = new StringContent(
                    request.RequestBodyJson,
                    Encoding.UTF8,
                    "application/json");
            }

            string? captureKey = null;
            if (captureCollector is not null)
            {
                captureKey = $"{request.OperationKey}:{Guid.NewGuid():N}";
                message.Headers.TryAddWithoutValidation(
                    ReplaySqlCaptureHeaderNames.CaptureKey,
                    captureKey);
            }

            var stopwatch = Stopwatch.StartNew();
            HttpResponseMessage? response = null;
            try
            {
                response = await httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();

                return new EndpointReplayResult
                {
                    OperationKey = request.OperationKey,
                    HttpMethod = request.HttpMethod,
                    Route = request.Route,
                    Persona = request.Persona,
                    Status = "replayed",
                    HttpStatusCode = (int)response.StatusCode,
                    DurationMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
                    ResponseBody = responseBody,
                    ResponseHeaders = CaptureHeaders(response),
                    Request = request,
                    CapturedSqlCommands = TakeCapturedCommands(captureCollector, captureKey),
                    ArtifactPath = artifactPath,
                };
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                stopwatch.Stop();
                return new EndpointReplayResult
                {
                    OperationKey = request.OperationKey,
                    HttpMethod = request.HttpMethod,
                    Route = request.Route,
                    Persona = request.Persona,
                    Status = "failed",
                    DurationMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
                    ErrorMessage = exception.Message,
                    Request = request,
                    CapturedSqlCommands = TakeCapturedCommands(captureCollector, captureKey),
                    ArtifactPath = artifactPath,
                };
            }
            finally
            {
                response?.Dispose();
            }
        }
        finally
        {
            message.Dispose();
        }
    }

    private static IReadOnlyList<CapturedSqlCommand> TakeCapturedCommands(
        ReplaySqlCaptureCollector? captureCollector,
        string? captureKey)
    {
        return captureKey is null || captureCollector is null
            ? Array.Empty<CapturedSqlCommand>()
            : captureCollector.TakeCompleted(captureKey);
    }

    private static IReadOnlyDictionary<string, string[]> CaptureHeaders(HttpResponseMessage response)
    {
        Dictionary<string, string[]> headers = new(StringComparer.OrdinalIgnoreCase);
        foreach ((var key, var value) in response.Headers)
        {
            headers[key] = value.ToArray();
        }

        foreach ((var key, var value) in response.Content.Headers)
        {
            headers[key] = value.ToArray();
        }

        return headers;
    }
}
