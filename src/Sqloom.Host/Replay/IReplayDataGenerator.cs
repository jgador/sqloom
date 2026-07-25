using System.Threading;
using System.Threading.Tasks;
using Sqloom.Pipeline.Execution;

namespace Sqloom.Host.Replay;

/// <summary>
/// Prepares safe, replay-only request data before the app harness prepares an operation.
/// </summary>
public interface IReplayDataGenerator
{
    /// <summary>
    /// Prepares replay-only request data for a resolved endpoint operation.
    /// </summary>
    Task<ReplayDataGenerationOperation> GenerateAsync(
        ReplayDataGenerationContext context,
        CancellationToken cancellationToken = default);
}
