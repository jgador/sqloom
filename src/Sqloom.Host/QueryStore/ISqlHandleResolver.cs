using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Core.QueryStore;

namespace Sqloom.Host.QueryStore;

/// <summary>
/// Resolves captured SQL text and parameters to SQL Server statement handles.
/// </summary>
public interface ISqlHandleResolver
{
    /// <summary>
    /// Resolves the best statement-handle match for a captured SQL command.
    /// </summary>
    Task<SqlHandleResolution> ResolveAsync(
        string sqlText,
        IReadOnlyList<SqlHandleParameter> parameters,
        CancellationToken cancellationToken = default);
}
