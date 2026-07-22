using System;

namespace Sqloom.Host.Replay;

/// <summary>
/// Builds stable replay operation keys from HTTP method and route metadata.
/// </summary>
internal static class ReplayOperationKeys
{
    public static string Build(string httpMethod, string route)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(httpMethod);
        ArgumentException.ThrowIfNullOrWhiteSpace(route);

        return $"{httpMethod.Trim().ToUpperInvariant()} {route.Trim()}";
    }
}
