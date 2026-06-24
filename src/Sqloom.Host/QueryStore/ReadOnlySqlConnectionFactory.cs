using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Sqloom.Host.QueryStore;

/// <summary>
/// Creates readonly SQL connection.
/// </summary>
public sealed class ReadOnlySqlConnectionFactory
{
    public SqlConnectionStringBuilder CreateBuilder(string readOnlyConnectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(readOnlyConnectionString);

        DbConnectionStringBuilder sourceBuilder = new()
        {
            ConnectionString = readOnlyConnectionString,
        };

        SqlConnectionStringBuilder builder = new(readOnlyConnectionString);
        if (!sourceBuilder.ContainsKey("Application Name") && !sourceBuilder.ContainsKey("App"))
        {
            builder.ApplicationName = "Sqloom";
        }

        return builder;
    }

    public async Task<SqlConnection> CreateOpenConnectionAsync(
        string readOnlyConnectionString,
        CancellationToken cancellationToken = default)
    {
        var builder = CreateBuilder(readOnlyConnectionString);
        SqlConnection connection = new(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }
}
