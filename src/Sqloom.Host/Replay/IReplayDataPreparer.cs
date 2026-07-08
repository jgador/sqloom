using System.Threading;
using System.Threading.Tasks;
using Sqloom.Core.Execution;

namespace Sqloom.Host.Replay;

/// <summary>
/// Prepares safe, replay-only request data before the app harness prepares an operation.
/// </summary>
public interface IReplayDataPreparer
{
    Task<ReplayDataPreparationOperation> PrepareAsync(
        ReplayDataPreparationContext context,
        CancellationToken cancellationToken = default);
}
