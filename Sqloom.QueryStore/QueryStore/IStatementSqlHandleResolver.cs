using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sqloom.QueryStore.QueryStore;

public interface IStatementSqlHandleResolver
{
    Task<SqlStatementHandleResolution> ResolveAsync(
        string sqlText,
        IReadOnlyList<SqlStatementHandleParameterDescriptor> parameters,
        CancellationToken cancellationToken = default);
}
