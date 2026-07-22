using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Pipeline.Execution;

namespace Sqloom.Host.Replay;

/// <summary>
/// Loads replayable endpoint operations from application source metadata.
/// </summary>
internal interface IReplayOperationCatalogLoader
{
    Task<IReadOnlyList<ReplayOperation>> LoadAsync(
        string sourceProjectPath,
        CancellationToken cancellationToken = default);
}
