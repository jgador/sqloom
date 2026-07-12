using System.Threading;
using System.Threading.Tasks;

namespace Sqloom.Core.Execution;

/// <summary>
/// Creates application hosts for Sqloom replay sessions.
/// </summary>
public interface IReplayHostFactory
{
    /// <summary>
    /// Creates and starts a replay host using the supplied launch inputs.
    /// </summary>
    Task<IReplayHost> CreateAsync(
        ReplayLaunchOptions? launchOptions = null,
        CancellationToken cancellationToken = default);
}
