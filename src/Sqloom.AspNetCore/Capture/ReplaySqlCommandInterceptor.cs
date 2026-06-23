using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Sqloom.Core.Execution;

namespace Sqloom.AspNetCore.Capture;

/// <summary>
/// Captures EF Core SQL commands emitted during replay.
/// </summary>
public sealed class ReplaySqlCommandInterceptor : DbCommandInterceptor
{
    private const string EntityFrameworkSource = "EntityFrameworkCore";

    private readonly ReplaySqlCaptureCollector _collector;

    public ReplaySqlCommandInterceptor(ReplaySqlCaptureCollector collector)
    {
        _collector = collector;
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        Record(command, eventData.Duration, recordsAffected: null);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        Record(command, eventData.Duration, recordsAffected: null);
        return base.ScalarExecuted(command, eventData, result);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        Record(command, eventData.Duration, result);
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        Record(command, eventData.Duration, recordsAffected: null);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        Record(command, eventData.Duration, recordsAffected: null);
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        Record(command, eventData.Duration, result);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    private void Record(DbCommand command, TimeSpan duration, int? recordsAffected)
    {
        _collector.Record(new CapturedSqlCommand
        {
            SourceKind = CapturedSqlSourceKind.EntityFramework,
            Source = EntityFrameworkSource,
            CommandText = command.CommandText,
            NormalizedCommandText = ReplaySqlTextNormalizer.Normalize(command.CommandText),
            Fingerprint = ReplaySqlTextNormalizer.ComputeFingerprint(command.CommandText),
            Parameters = ReadParameters(command),
            Duration = duration,
            RecordsAffected = recordsAffected
        });
    }

    private static IReadOnlyList<CapturedSqlParameter> ReadParameters(DbCommand command)
    {
        List<CapturedSqlParameter> parameters = [];
        foreach (DbParameter parameter in command.Parameters)
        {
            parameters.Add(new CapturedSqlParameter
            {
                Name = parameter.ParameterName,
                DbType = parameter.DbType.ToString(),
                Size = parameter.Size > 0 ? parameter.Size : null,
                Precision = parameter.Precision > 0 ? parameter.Precision : null,
                Scale = parameter.Scale > 0 ? parameter.Scale : null,
                Value = FormatParameterValue(parameter.Value)
            });
        }

        return parameters;
    }

    private static string? FormatParameterValue(object? value)
    {
        return value switch
        {
            null => null,
            DBNull => null,
            byte[] bytes => Convert.ToBase64String(bytes),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
        };
    }
}
