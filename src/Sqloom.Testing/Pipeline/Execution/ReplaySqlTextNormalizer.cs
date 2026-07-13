using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Sqloom.Pipeline.Execution;

/// <summary>
/// Normalizes replay-captured SQL so it can be compared against Query Store text.
/// </summary>
public static partial class ReplaySqlTextNormalizer
{
    /// <summary>
    /// Normalizes captured SQL text for stable replay comparison.
    /// </summary>
    public static string Normalize(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        var collapsedParameters = ParameterPattern().Replace(sql.Trim(), "@p");
        var collapsedWhitespace = WhitespacePattern().Replace(collapsedParameters, " ");
        return collapsedWhitespace.Trim();
    }

    /// <summary>
    /// Computes a stable fingerprint from normalized captured SQL text.
    /// </summary>
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
