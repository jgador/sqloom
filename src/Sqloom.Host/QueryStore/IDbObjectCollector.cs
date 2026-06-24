using System.Threading;
using System.Threading.Tasks;
using Sqloom.Core.QueryStore;

namespace Sqloom.Host.QueryStore;

public interface IDbObjectCollector
{
    Task<DbObjectCatalog> CaptureAsync(
        string readOnlyConnectionString,
        DbObjectScanOptions options,
        CancellationToken cancellationToken = default);
}
