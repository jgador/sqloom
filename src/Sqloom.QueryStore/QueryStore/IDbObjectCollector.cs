using System.Threading;
using System.Threading.Tasks;

namespace Sqloom.QueryStore.QueryStore;

public interface IDbObjectCollector
{
    Task<DbObjectCatalog> CaptureAsync(
        string readOnlyConnectionString,
        DbObjectScanOptions options,
        CancellationToken cancellationToken = default);
}
