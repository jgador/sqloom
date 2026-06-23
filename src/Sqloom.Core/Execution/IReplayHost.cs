using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sqloom.Core.Execution;

public interface IReplayHost : IAsyncDisposable
{
    HttpClient Client { get; }

    IServiceProvider Services { get; }

    ReplayBootstrapReport Bootstrap { get; }

    Task<PreparedReplayOperation> PrepareOperationAsync(
        ResolvedReplayOperation operation,
        CancellationToken cancellationToken = default);
}
