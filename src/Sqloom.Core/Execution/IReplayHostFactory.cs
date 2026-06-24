using System.Threading;
using System.Threading.Tasks;

namespace Sqloom.Core.Execution;

public interface IReplayHostFactory
{
    Task<IReplayHost> CreateAsync(
        ReplayLaunchOptions? launchOptions = null,
        CancellationToken cancellationToken = default);
}
