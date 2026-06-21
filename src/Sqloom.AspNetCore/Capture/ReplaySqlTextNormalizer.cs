using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Sqloom.AspNetCore.Capture;

/// <summary>
/// Normalizes replay-captured SQL so it can be compared against Query Store text.
/// </summary>
public static partial class ReplaySqlTextNormalizer
{
    public static string Normalize(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        var collapsedParameters = ParameterPattern().Replace(sql.Trim(), "@p");
        var collapsedWhitespace = WhitespacePattern().Replace(collapsedParameters, " ");
        return collapsedWhitespace.Trim();
    }

    public static string ComputeFingerprint(string sql)
    {
        var normalized = Normalize(sql).ToLowerInvariant();
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    [GeneratedRegex(@"@\w+", RegexOptions.CultureInvariant)]
    private static partial Regex ParameterPattern();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespacePattern();
}
