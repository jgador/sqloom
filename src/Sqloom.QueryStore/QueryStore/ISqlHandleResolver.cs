using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sqloom.QueryStore.QueryStore;

public interface ISqlHandleResolver
{
    Task<SqlHandleResolution> ResolveAsync(
        string sqlText,
        IReadOnlyList<SqlHandleParameter> parameters,
        CancellationToken cancellationToken = default);
}
