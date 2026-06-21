using System.Threading;
using System.Threading.Tasks;

namespace Sqloom.QueryStore.QueryStore;

public interface IDiscoveredDatabaseObjectCollector
{
    Task<DiscoveredDatabaseObjectCatalog> CaptureAsync(
        string readOnlyConnectionString,
        DiscoveredDatabaseObjectObservationOptions options,
        CancellationToken cancellationToken = default);
}
