using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Sqloom.AspNetCore.Capture;

/// <summary>
/// Enables replay SQL capture for requests that carry the Sqloom capture header.
/// </summary>
public sealed class ReplaySqlCaptureMiddleware
{
    private readonly RequestDelegate _next;

    public ReplaySqlCaptureMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext httpContext,
        ReplaySqlCaptureCollector captureCollector)
    {
        if (!httpContext.Request.Headers.TryGetValue(
                ReplaySqlCaptureHeaderNames.CaptureKey,
                out var captureValues)
            || StringValues.IsNullOrEmpty(captureValues))
        {
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        var captureKey = captureValues[0]!;
        using var captureScope =
            captureCollector.BeginScope(captureKey);
        try
        {
            await _next(httpContext).ConfigureAwait(false);
        }
        finally
        {
            captureCollector.StoreCompleted(captureKey, captureScope.Complete());
        }
    }
}
