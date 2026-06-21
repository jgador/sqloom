using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Sqloom.QueryStore.QueryStore;

/// <summary>
/// Normalizes Query Store SQL text for safe matching.
/// </summary>
public static partial class QueryStoreSqlText
{
    private static readonly string[] _statementStarts =
    [
        "SELECT",
        "INSERT",
        "UPDATE",
        "DELETE",
        "MERGE",
        "EXEC",
        "EXECUTE",
        "WITH",
    ];

    public static IReadOnlyList<string> GetComparableStatements(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        var trimmed = TrimOuterNoise(sql);
        List<string> statements = new();
        foreach (var segment in SplitStatements(trimmed))
        {
            var statementText = TrimOuterNoise(segment);
            if (statementText.Length == 0 || !StartsWithStatementKeyword(statementText))
            {
                continue;
            }

            if (!ContainsOrdinal(statements, statementText))
            {
                statements.Add(statementText);
            }
        }

        if (statements.Count > 0)
        {
            return statements;
        }

        var leadingSetTrimmed = TrimLeadingSetStatements(trimmed);
        if (StartsWithStatementKeyword(leadingSetTrimmed))
        {
            return [leadingSetTrimmed];
        }

        return trimmed.Length == 0
            ? Array.Empty<string>()
            : [trimmed];
    }

    public static string TrimOuterNoise(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        var current = sql;
        while (true)
        {
            var trimmed = current.Trim();
            var withoutLeadingNoise = LeadingNoisePattern().Replace(trimmed, string.Empty);
            var withoutTrailingNoise = TrailingNoisePattern().Replace(withoutLeadingNoise, string.Empty);

            if (string.Equals(withoutTrailingNoise, trimmed, StringComparison.Ordinal))
            {
                return trimmed;
            }

            current = withoutTrailingNoise;
        }
    }

    public static string TrimLeadingSetStatements(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        var trimmed = sql.TrimStart();
        var stripped = LeadingSetStatementsPattern().Replace(trimmed, string.Empty, 1).TrimStart();
        return stripped.Length == 0 ? trimmed : stripped;
    }

    public static string TrimLeadingParameterDefinitionPrefix(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        var trimmed = sql.TrimStart();
        if (!trimmed.StartsWith('('))
        {
            return trimmed;
        }

        var depth = 0;
        for (var index = 0; index < trimmed.Length; index++)
        {
            var character = trimmed[index];
            if (character == '(')
            {
                depth++;
            }
            else if (character == ')')
            {
                depth--;
                if (depth == 0)
                {
                    var remainder = trimmed[(index + 1)..].TrimStart();
                    return StartsWithStatementKeyword(remainder)
                        ? remainder
                        : trimmed;
                }
            }
        }

        return trimmed;
    }

    private static bool StartsWithStatementKeyword(string value)
    {
        foreach (var keyword in _statementStarts)
        {
            if (value.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<string> SplitStatements(string sql)
    {
        List<string> statements = new();
        var statementStart = 0;
        var inLineComment = false;
        var inBlockComment = false;
        var inSingleQuotedString = false;
        var inBracketIdentifier = false;

        for (var index = 0; index < sql.Length; index++)
        {
            var character = sql[index];
            var next = index + 1 < sql.Length ? sql[index + 1] : '\0';

            if (inLineComment)
            {
                if (character is '\r' or '\n')
                {
                    inLineComment = false;
                }

                continue;
            }

            if (inBlockComment)
            {
                if (character == '*' && next == '/')
                {
                    inBlockComment = false;
                    index++;
                }

                continue;
            }

            if (inSingleQuotedString)
            {
                if (character == '\'' && next == '\'')
                {
                    index++;
                    continue;
                }

                if (character == '\'')
                {
                    inSingleQuotedString = false;
                }

                continue;
            }

            if (inBracketIdentifier)
            {
                if (character == ']' && next == ']')
                {
                    index++;
                    continue;
                }

                if (character == ']')
                {
                    inBracketIdentifier = false;
                }

                continue;
            }

            if (character == '-' && next == '-')
            {
                inLineComment = true;
                index++;
                continue;
            }

            if (character == '/' && next == '*')
            {
                inBlockComment = true;
                index++;
                continue;
            }

            if (character == '\'')
            {
                inSingleQuotedString = true;
                continue;
            }

            if (character == '[')
            {
                inBracketIdentifier = true;
                continue;
            }

            if (character == ';')
            {
                statements.Add(sql[statementStart..index]);
                statementStart = index + 1;
            }
        }

        if (statementStart < sql.Length)
        {
            statements.Add(sql[statementStart..]);
        }

        return statements;
    }

    private static bool ContainsOrdinal(IReadOnlyList<string> values, string candidate)
    {
        for (var index = 0; index < values.Count; index++)
        {
            if (string.Equals(values[index], candidate, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    [GeneratedRegex(
        @"^(?:(?:--[^\r\n]*(?:\r?\n|$))|(?:/\*.*?\*/\s*))+",
        RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex LeadingNoisePattern();

    [GeneratedRegex(
        @"(?:(?:/\*.*?\*/)|(?:\s--[^\r\n]*)|(?:^--[^\r\n]*))\s*$",
        RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex TrailingNoisePattern();

    [GeneratedRegex(
        @"^(?:(?:SET\s+[A-Za-z_]+\s+(?:ON|OFF)\s*;\s*))+",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex LeadingSetStatementsPattern();
}
