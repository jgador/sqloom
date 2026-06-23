using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Sqloom.Host.QueryStore;
using Sqloom.Core.QueryStore;

namespace Sqloom.Host.QueryStore;

/// <summary>
/// Resolves SQL statement_sql_handle values for captured commands.
/// </summary>
public sealed partial class SqlStatementHandleResolver : ISqlHandleResolver
{
    private const int DefaultCommandTimeoutSeconds = 30;
    private const string ResolveStatementHandleSql = """
        SELECT
            CONVERT(int, resolved.query_parameterization_type) AS query_parameterization_type,
            CONVERT(varchar(130), resolved.statement_sql_handle, 1) AS statement_sql_handle
        FROM sys.fn_stmt_sql_handle_from_sql_stmt(
            @QuerySqlText,
            @RequestedParamType) AS resolved;
        """;
    private static readonly RequestedParamType[] _requestedParamTypes =
    [
        new(null, "Default"),
        new(0, "None"),
        new(1, "User"),
        new(2, "Simple"),
        new(3, "Forced"),
    ];

    private readonly string _connectionString;
    private readonly ReadOnlySqlConnectionFactory _connectionFactory;
    private readonly int _commandTimeoutSeconds;

    public SqlStatementHandleResolver(
        string connectionString,
        ReadOnlySqlConnectionFactory? connectionFactory = null,
        int commandTimeoutSeconds = DefaultCommandTimeoutSeconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        _connectionString = connectionString;
        _connectionFactory = connectionFactory ?? new ReadOnlySqlConnectionFactory();
        _commandTimeoutSeconds = commandTimeoutSeconds > 0
            ? commandTimeoutSeconds
            : throw new ArgumentOutOfRangeException(
                nameof(commandTimeoutSeconds),
                commandTimeoutSeconds,
                "The command timeout must be positive.");
    }

    public async Task<SqlHandleResolution> ResolveAsync(
        string sqlText,
        IReadOnlyList<SqlHandleParameter> parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sqlText);
        ArgumentNullException.ThrowIfNull(parameters);

        var comparableSqlText = QueryStoreSqlText.TrimOuterNoise(sqlText);
        var queryTextCandidates =
            BuildQueryTextCandidates(comparableSqlText, parameters);

        try
        {
            var connection = await _connectionFactory
                .CreateOpenConnectionAsync(_connectionString, cancellationToken)
                .ConfigureAwait(false);
            await using (connection.ConfigureAwait(false))
            {
                using var command = connection.CreateCommand();
                command.CommandText = ResolveStatementHandleSql;
                command.CommandTimeout = _commandTimeoutSeconds;
                var querySqlTextParameter = command.Parameters.Add(
                    new SqlParameter("@QuerySqlText", System.Data.SqlDbType.NVarChar, -1));
                var requestedParamTypeParameter = command.Parameters.Add(
                    new SqlParameter("@RequestedParamType", System.Data.SqlDbType.TinyInt)
                    {
                        IsNullable = true,
                    });

                List<SqlHandleCandidateRecord> records = new();
                foreach (var queryTextCandidate in queryTextCandidates)
                {
                    querySqlTextParameter.Value = queryTextCandidate.QuerySqlText;
                    foreach (var requestedParamType in _requestedParamTypes)
                    {
                        requestedParamTypeParameter.Value =
                            requestedParamType.Value is null
                                ? DBNull.Value
                                : requestedParamType.Value.Value;

                        DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                records.Add(ReadCandidateRecord(
                                    reader,
                                    queryTextCandidate.QueryTextShape,
                                    requestedParamType.Description));
                            }
                            else
                            {
                                records.Add(new SqlHandleCandidateRecord(
                                    queryTextCandidate.QueryTextShape,
                                    requestedParamType.Description,
                                    null,
                                    null));
                            }
                        }
                    }
                }

                return BuildResolution(sqlText, comparableSqlText, records);
            }
        }
        catch (SqlException sqlException)
        {
            return BuildResolution(sqlText, comparableSqlText, Array.Empty<SqlHandleCandidateRecord>(), sqlException.Message);
        }
        catch (InvalidOperationException invalidOperationException)
        {
            return BuildResolution(
                sqlText,
                comparableSqlText,
                Array.Empty<SqlHandleCandidateRecord>(),
                invalidOperationException.Message);
        }
    }

    internal static SqlHandleCandidateRecord ReadCandidateRecord(
        DbDataReader reader,
        string queryTextShape,
        string requestedParamType)
    {
        return new SqlHandleCandidateRecord(
            queryTextShape,
            requestedParamType,
            GetNullableInt32(reader, "query_parameterization_type"),
            GetNullableString(reader, "statement_sql_handle"));
    }

    internal static IReadOnlyList<SqlStatementQueryTextCandidate> BuildQueryTextCandidates(
        string comparableSqlText,
        IReadOnlyList<SqlHandleParameter> parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(comparableSqlText);
        ArgumentNullException.ThrowIfNull(parameters);

        List<SqlStatementQueryTextCandidate> candidates = new();
        var statements = QueryStoreSqlText.GetComparableStatements(comparableSqlText);
        for (var index = 0; index < statements.Count; index++)
        {
            var statementText = statements[index];
            candidates.Add(new SqlStatementQueryTextCandidate($"Statement{index + 1}.Raw", statementText));

            var statementParameters =
                FilterParametersForStatement(statementText, parameters);
            var parameterPrefixedQuerySqlText =
                TryBuildParameterPrefixedQueryText(statementText, statementParameters);
            if (!string.IsNullOrWhiteSpace(parameterPrefixedQuerySqlText))
            {
                candidates.Add(new SqlStatementQueryTextCandidate(
                    $"Statement{index + 1}.ParameterDefinitionPrefix",
                    parameterPrefixedQuerySqlText));
            }
        }

        if (candidates.Count > 0)
        {
            return candidates;
        }

        List<SqlStatementQueryTextCandidate> fallbackCandidates =
        [
            new("Raw", comparableSqlText),
        ];
        var parameterPrefixedQueryText = TryBuildParameterPrefixedQueryText(comparableSqlText, parameters);
        if (!string.IsNullOrWhiteSpace(parameterPrefixedQueryText))
        {
            fallbackCandidates.Add(new SqlStatementQueryTextCandidate(
                "ParameterDefinitionPrefix",
                parameterPrefixedQueryText));
        }

        return fallbackCandidates;
    }

    internal static SqlHandleResolution BuildResolution(
        string sqlText,
        string comparableSqlText,
        IReadOnlyList<SqlHandleCandidateRecord> records,
        string? errorMessage = null)
    {
        var statementSqlHandle = records
            .Select(static record => record.StatementSqlHandle)
            .FirstOrDefault(static handle => !string.IsNullOrWhiteSpace(handle));

        return new SqlHandleResolution
        {
            SqlText = sqlText,
            ComparableSqlText = comparableSqlText,
            StatementSqlHandle = statementSqlHandle,
            Candidates = records.Select(static record => new SqlHandleCandidate
            {
                QueryTextShape = record.QueryTextShape,
                RequestedParamType = record.RequestedParamType,
                QueryParameterizationType = record.QueryParameterizationType,
                StatementSqlHandle = record.StatementSqlHandle,
            }).ToArray(),
            ErrorMessage = errorMessage,
        };
    }

    private static string? GetNullableString(DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static int? GetNullableInt32(DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static string? TryBuildParameterPrefixedQueryText(
        string comparableSqlText,
        IReadOnlyList<SqlHandleParameter> parameters)
    {
        if (parameters.Count == 0)
        {
            return null;
        }

        StringBuilder builder = new();
        builder.Append('(');
        for (var index = 0; index < parameters.Count; index++)
        {
            var parameter = parameters[index];
            if (!TryFormatParameterDefinition(parameter, out var parameterDefinition))
            {
                return null;
            }

            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append(parameterDefinition);
        }

        builder.Append(')');
        builder.Append(comparableSqlText);
        return builder.ToString();
    }

    private static IReadOnlyList<SqlHandleParameter> FilterParametersForStatement(
        string statementText,
        IReadOnlyList<SqlHandleParameter> parameters)
    {
        List<SqlHandleParameter> matchingParameters = new();
        foreach (var parameter in parameters)
        {
            if (StatementUsesParameter(statementText, parameter.Name))
            {
                matchingParameters.Add(parameter);
            }
        }

        return matchingParameters;
    }

    private static bool StatementUsesParameter(string statementText, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }

        var searchStart = 0;
        while (searchStart < statementText.Length)
        {
            var index = statementText.IndexOf(parameterName, searchStart, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return false;
            }

            var afterIndex = index + parameterName.Length;
            var startsAtBoundary = index == 0 || !IsIdentifierCharacter(statementText[index - 1]);
            var endsAtBoundary = afterIndex >= statementText.Length || !IsIdentifierCharacter(statementText[afterIndex]);
            if (startsAtBoundary && endsAtBoundary)
            {
                return true;
            }

            searchStart = afterIndex;
        }

        return false;
    }

    private static bool IsIdentifierCharacter(char character)
    {
        return char.IsLetterOrDigit(character) || character is '_' or '@' or '#';
    }

    private static bool TryFormatParameterDefinition(
        SqlHandleParameter parameter,
        out string? parameterDefinition)
    {
        parameterDefinition = null;
        if (string.IsNullOrWhiteSpace(parameter.Name))
        {
            return false;
        }

        var sqlType = TryMapSqlType(parameter);
        if (sqlType is null)
        {
            return false;
        }

        parameterDefinition = $"{parameter.Name} {sqlType}";
        return true;
    }

    private static string? TryMapSqlType(SqlHandleParameter parameter)
    {
        var dbType = parameter.DbType?.Trim() ?? string.Empty;
        if (dbType.Length == 0)
        {
            return null;
        }

        return dbType.ToUpperInvariant() switch
        {
            "BIGINT" or "INT64" => "bigint",
            "INT" or "INT32" => "int",
            "SMALLINT" or "INT16" => "smallint",
            "TINYINT" or "BYTE" => "tinyint",
            "BIT" or "BOOLEAN" => "bit",
            "UNIQUEIDENTIFIER" or "GUID" => "uniqueidentifier",
            "DATE" => "date",
            "DATETIMEOFFSET" => $"datetimeoffset({GetScaleOrDefault(parameter.Scale, 7)})",
            "DATETIME" or "DATETIME2" => $"datetime2({GetScaleOrDefault(parameter.Scale, 7)})",
            "TIME" => $"time({GetScaleOrDefault(parameter.Scale, 7)})",
            "DECIMAL" or "CURRENCY" or "VARNUMERIC" => $"decimal({GetPrecisionOrDefault(parameter.Precision, 18)},{GetScaleOrDefault(parameter.Scale, 2)})",
            "DOUBLE" or "FLOAT" => "float",
            "SINGLE" or "REAL" => "real",
            "XML" => "xml",
            "BINARY" or "VARBINARY" => FormatLengthType("varbinary", parameter.Size),
            "STRING" or "NVARCHAR" => FormatLengthType("nvarchar", parameter.Size),
            "STRINGFIXEDLENGTH" or "NCHAR" => FormatLengthType("nchar", parameter.Size),
            "ANSISTRING" or "VARCHAR" => FormatLengthType("varchar", parameter.Size),
            "ANSISTRINGFIXEDLENGTH" or "CHAR" => FormatLengthType("char", parameter.Size),
            _ => null,
        };
    }

    private static string FormatLengthType(string typeName, int? size)
    {
        if (size is null)
        {
            return $"{typeName}(max)";
        }

        return size.Value <= 0
            ? $"{typeName}(max)"
            : $"{typeName}({size.Value})";
    }

    private static int GetPrecisionOrDefault(byte? value, int fallback)
    {
        return value is > 0 ? value.Value : fallback;
    }

    private static int GetScaleOrDefault(byte? value, int fallback)
    {
        return value is > 0 ? value.Value : fallback;
    }

    internal sealed record SqlHandleCandidateRecord(
        string QueryTextShape,
        string RequestedParamType,
        int? QueryParameterizationType,
        string? StatementSqlHandle);

    internal sealed record SqlStatementQueryTextCandidate(
        string QueryTextShape,
        string QuerySqlText);

    private sealed record RequestedParamType(
        byte? Value,
        string Description);
}
