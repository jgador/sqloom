namespace Sqloom.Testing.AspNetCore;

/// <summary>
/// Defines the HTTP headers used to control replay SQL capture.
/// </summary>
public static class ReplaySqlCaptureHeaderNames
{
    /// <summary>
    /// Identifies the request header that associates captured SQL with a replay operation.
    /// </summary>
    public const string CaptureKey = "X-Sqloom-Capture-Key";
}
