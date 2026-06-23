using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Core.QueryStore;

namespace Sqloom.Host.QueryStore;

public interface ISqlHandleResolver
{
    Task<SqlHandleResolution> ResolveAsync(
        string sqlText,
        IReadOnlyList<SqlHandleParameter> parameters,
        CancellationToken cancellationToken = default);
}
