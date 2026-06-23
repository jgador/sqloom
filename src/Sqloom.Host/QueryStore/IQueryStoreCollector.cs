using System.Threading;
using System.Threading.Tasks;
using Sqloom.Core.QueryStore;

namespace Sqloom.Host.QueryStore;

/// <summary>
/// Captures Query Store evidence from a readonly SQL Server or Azure SQL connection.
/// </summary>
public interface IQueryStoreCollector
{
    Task<QueryStoreSnapshot> CaptureAsync(
        string readOnlyConnectionString,
        QueryStoreOptions options,
        CancellationToken cancellationToken = default);
}
