using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Pipeline.QueryStore;

namespace Sqloom.Host.QueryStore;

/// <summary>
/// Matches captured SQL text and parameters to SQL Server statement handles.
/// </summary>
internal interface ISqlHandleResolver
{
    /// <summary>
    /// Finds the best statement-handle match for a captured SQL command.
    /// </summary>
    Task<SqlHandleResolution> ResolveAsync(
        string sqlText,
        IReadOnlyList<SqlHandleParameter> parameters,
        CancellationToken cancellationToken = default);
}
