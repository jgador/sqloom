using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Sqloom.Host;

/// <summary>
/// Parses a SQL Server schema script into a minimal object-and-column catalog.
/// </summary>
internal sealed class SqlServerSchemaCatalog
{
    private readonly Dictionary<string, SqlServerSchemaTable> _tablesByFullName;
    private readonly Dictionary<string, List<SqlServerSchemaTable>> _tablesBySimpleName;

    private SqlServerSchemaCatalog(
        Dictionary<string, SqlServerSchemaTable> tablesByFullName,
        Dictionary<string, List<SqlServerSchemaTable>> tablesBySimpleName)
    {
        _tablesByFullName = tablesByFullName;
        _tablesBySimpleName = tablesBySimpleName;
    }

    public static SqlServerSchemaCatalog Parse(string schemaSqlText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaSqlText);

        Dictionary<string, SqlServerSchemaTable> tablesByFullName =
            new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, List<SqlServerSchemaTable>> tablesBySimpleName =
            new(StringComparer.OrdinalIgnoreCase);

        SqlServerSchemaTable? currentTable = null;
        foreach (var rawLine in schemaSqlText.Split(
                     ["\r\n", "\n"],
                     StringSplitOptions.None))
        {
            var line = rawLine.Trim();
            if (currentTable is null)
            {
                var createTableMatch = Regex.Match(
                    line,
                    @"^CREATE\s+TABLE\s+\[(?<schema>[^\]]+)\]\.\[(?<table>[^\]]+)\]\s*\($",
                    RegexOptions.IgnoreCase);
                if (!createTableMatch.Success)
                {
                    continue;
                }

                currentTable = new SqlServerSchemaTable(
                    createTableMatch.Groups["schema"].Value,
                    createTableMatch.Groups["table"].Value);
                continue;
            }

            if (string.Equals(line, ");", StringComparison.Ordinal))
            {
                tablesByFullName[currentTable.FullName] = currentTable;
                var simpleName = NormalizeIdentifier(currentTable.TableName);
                if (!tablesBySimpleName.TryGetValue(simpleName, out var matches))
                {
                    matches = [];
                    tablesBySimpleName[simpleName] = matches;
                }

                matches.Add(currentTable);
                currentTable = null;
                continue;
            }

            if (!line.StartsWith("[", StringComparison.Ordinal))
            {
                continue;
            }

            var closingBracketIndex = line.IndexOf(']');
            if (closingBracketIndex <= 1)
            {
                continue;
            }

            currentTable.AddColumn(line[1..closingBracketIndex]);
        }

        return new SqlServerSchemaCatalog(
            tablesByFullName,
            tablesBySimpleName);
    }

    public bool TryGetTable(
        string objectName,
        out SqlServerSchemaTable table)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectName);

        var identifiers = ExtractIdentifiers(objectName);
        if (identifiers.Count >= 2)
        {
            var fullName = BuildFullName(
                identifiers[^2],
                identifiers[^1]);
            if (_tablesByFullName.TryGetValue(fullName, out table!))
            {
                return true;
            }
        }

        var simpleName = NormalizeIdentifier(identifiers[^1]);
        if (_tablesBySimpleName.TryGetValue(simpleName, out var matches)
            && matches.Count == 1)
        {
            table = matches[0];
            return true;
        }

        table = null!;
        return false;
    }

    private static IReadOnlyList<string> ExtractIdentifiers(string value)
    {
        List<string> identifiers = [];
        var bracketedIdentifiers = Regex.Matches(value, @"\[(?<id>[^\]]+)\]");
        if (bracketedIdentifiers.Count > 0)
        {
            foreach (Match match in bracketedIdentifiers)
            {
                identifiers.Add(match.Groups["id"].Value);
            }

            return identifiers;
        }

        foreach (var part in value.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            identifiers.Add(part.Trim());
        }

        return identifiers.Count > 0
            ? identifiers
            : [value.Trim()];
    }

    private static string BuildFullName(
        string schemaName,
        string tableName)
    {
        return $"{NormalizeIdentifier(schemaName)}.{NormalizeIdentifier(tableName)}";
    }

    private static string NormalizeIdentifier(string value)
    {
        return value
            .Trim()
            .Trim('[', ']');
    }
}
/// <summary>
/// Carries the parsed column metadata for one SQL Server table.
/// </summary>
internal sealed class SqlServerSchemaTable
{
    private readonly HashSet<string> _columns = new(StringComparer.OrdinalIgnoreCase);

    public SqlServerSchemaTable(
        string schemaName,
        string tableName)
    {
        SchemaName = schemaName;
        TableName = tableName;
    }

    public string SchemaName { get; }

    public string TableName { get; }

    public string FullName => $"{NormalizeIdentifier(SchemaName)}.{NormalizeIdentifier(TableName)}";

    public bool ContainsColumn(string columnName)
    {
        return _columns.Contains(NormalizeIdentifier(columnName));
    }

    public void AddColumn(string columnName)
    {
        _columns.Add(NormalizeIdentifier(columnName));
    }

    private static string NormalizeIdentifier(string value)
    {
        return value
            .Trim()
            .Trim('[', ']');
    }
}
