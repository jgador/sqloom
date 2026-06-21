using Sqloom.Core.Contracts;

namespace Sqloom.QueryStore.QueryStore;

/// <summary>
/// Supplies app-specific Query Store classification context when a Sqloom app integration can provide it.
/// </summary>
public interface IQueryStoreAppIntegration : IAppIntegration
{
    QueryStoreWorkloadProfile GetQueryStoreWorkloadProfile();
}
