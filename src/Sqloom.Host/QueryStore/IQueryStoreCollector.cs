using System.Threading;
using System.Threading.Tasks;
using Sqloom.Pipeline.QueryStore;

namespace Sqloom.Host.QueryStore;

/// <summary>
/// Captures Query Store evidence from a readonly SQL Server or Azure SQL connection.
/// </summary>
public interface IQueryStoreCollector
{
    /// <summary>
    /// Captures a bounded Query Store snapshot through a readonly connection.
    /// </summary>
    Task<QueryStoreSnapshot> CaptureAsync(
        string readOnlyConnectionString,
        QueryStoreOptions options,
        CancellationToken cancellationToken = default);
}
