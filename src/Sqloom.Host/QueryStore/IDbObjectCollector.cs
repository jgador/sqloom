using System.Threading;
using System.Threading.Tasks;
using Sqloom.Pipeline.QueryStore;

namespace Sqloom.Host.QueryStore;

/// <summary>
/// Discovers database objects available to the observed workload.
/// </summary>
public interface IDbObjectCollector
{
    /// <summary>
    /// Captures a filtered catalog of database objects through a readonly connection.
    /// </summary>
    Task<DbObjectCatalog> CaptureAsync(
        string readOnlyConnectionString,
        DbObjectScanOptions options,
        CancellationToken cancellationToken = default);
}
