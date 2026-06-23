namespace Sqloom.Testing.AspNetCore;

/// <summary>
/// Defines the HTTP headers used to control replay SQL capture.
/// </summary>
public static class ReplaySqlCaptureHeaderNames
{
    public const string CaptureKey = "X-Sqloom-Capture-Key";
}
