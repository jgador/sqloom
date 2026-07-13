using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sqloom.Pipeline.Execution;

/// <summary>
/// Represents a running application host that can prepare and execute replay operations.
/// </summary>
public interface IReplayHost : IAsyncDisposable
{
    /// <summary>
    /// Gets the client.
    /// </summary>
    HttpClient Client { get; }

    /// <summary>
    /// Gets the services.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Gets the bootstrap.
    /// </summary>
    ReplayBootstrapReport Bootstrap { get; }

    /// <summary>
    /// Allows the application harness to finalize a resolved operation before execution.
    /// </summary>
    Task<PreparedReplayOperation> PrepareOperationAsync(
        ResolvedReplayOperation operation,
        CancellationToken cancellationToken = default);
}
