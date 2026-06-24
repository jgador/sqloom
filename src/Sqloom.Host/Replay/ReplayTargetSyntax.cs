using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sqloom.Host.Replay;

/// <summary>
/// Validates replay targets that must match an exact discovered HTTP operation key.
/// </summary>
public static class ReplayTargetSyntax
{
    private static readonly string[] KnownHttpMethods =
    [
        "GET",
        "POST",
        "PUT",
        "PATCH",
        "DELETE",
        "HEAD",
        "OPTIONS",
        "TRACE",
        "CONNECT",
    ];

    /// <summary>
    /// Returns a validated replay target or null when no target was supplied.
    /// </summary>
    public static string? ValidateOperationKeyOrNull(string? targetFilter)
    {
        var trimmedTarget = targetFilter?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedTarget))
        {
            return null;
        }

        var suggestion = TryCreateSuggestion(trimmedTarget);
        if (TryValidate(trimmedTarget, out var reason))
        {
            return trimmedTarget;
        }

        var message =
            $"Invalid replay target '{trimmedTarget}'. --target must use the exact form 'METHOD /path/template'. {reason}";
        if (!string.IsNullOrWhiteSpace(suggestion)
            && !string.Equals(suggestion, trimmedTarget, StringComparison.Ordinal))
        {
            message += $" Did you mean '{suggestion}'?";
        }

        throw new ArgumentException(message);
    }

    private static bool TryValidate(
        string targetFilter,
        out string reason)
    {
        if (targetFilter.Any(static character => char.IsWhiteSpace(character) && character != ' '))
        {
            reason = "Use exactly one space between the HTTP method and the route template.";
            return false;
        }

        var firstSpaceIndex = targetFilter.IndexOf(' ');
        if (firstSpaceIndex <= 0 || firstSpaceIndex != targetFilter.LastIndexOf(' '))
        {
            reason = "Use exactly one space between the HTTP method and the route template.";
            return false;
        }

        var method = targetFilter[..firstSpaceIndex];
        var route = targetFilter[(firstSpaceIndex + 1)..];

        if (!KnownHttpMethods.Contains(method, StringComparer.Ordinal))
        {
            reason = string.Equals(method, method.ToUpperInvariant(), StringComparison.Ordinal)
                ? "Use an uppercase HTTP method such as GET, POST, PUT, PATCH, or DELETE."
                : "The HTTP method must be uppercase.";
            return false;
        }

        if (!route.StartsWith("/", StringComparison.Ordinal))
        {
            reason = "The route template must start with '/'.";
            return false;
        }

        if (route.Length > 1 && route.EndsWith("/", StringComparison.Ordinal))
        {
            reason = "Do not include a trailing '/' in the route template.";
            return false;
        }

        if (route.Contains("//", StringComparison.Ordinal))
        {
            reason = "Do not include repeated '/' characters in the route template.";
            return false;
        }

        if (route.Any(char.IsWhiteSpace))
        {
            reason = "The route template cannot contain spaces.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static string? TryCreateSuggestion(string targetFilter)
    {
        var normalizedWhitespace = Regex.Replace(targetFilter.Trim(), @"\s+", " ");
        var firstSpaceIndex = normalizedWhitespace.IndexOf(' ');
        if (firstSpaceIndex <= 0)
        {
            return null;
        }

        var method = normalizedWhitespace[..firstSpaceIndex];
        var route = normalizedWhitespace[(firstSpaceIndex + 1)..];
        if (string.IsNullOrWhiteSpace(method) || string.IsNullOrWhiteSpace(route))
        {
            return null;
        }

        var normalizedMethod = NormalizeMethod(method);
        var normalizedRoute = NormalizeRoute(route);
        if (string.IsNullOrWhiteSpace(normalizedMethod)
            || string.IsNullOrWhiteSpace(normalizedRoute))
        {
            return null;
        }

        return $"{normalizedMethod} {normalizedRoute}";
    }

    private static string NormalizeMethod(string method)
    {
        var upperMethod = method.ToUpperInvariant();
        return KnownHttpMethods.Contains(upperMethod, StringComparer.Ordinal)
            ? upperMethod
            : method;
    }

    private static string NormalizeRoute(string route)
    {
        var normalizedRoute = Regex.Replace(route.Trim(), "/{2,}", "/");
        if (!normalizedRoute.StartsWith("/", StringComparison.Ordinal))
        {
            normalizedRoute = "/" + normalizedRoute.TrimStart('/');
        }

        if (normalizedRoute.Length > 1)
        {
            normalizedRoute = normalizedRoute.TrimEnd('/');
        }

        return normalizedRoute;
    }
}
