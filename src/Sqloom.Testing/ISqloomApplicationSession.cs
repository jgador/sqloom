using System;
using Sqloom.Core.Execution;

namespace Sqloom.Testing;

/// <summary>
/// Represents one running app-under-test session owned by a Sqloom harness.
/// </summary>
public interface ISqloomApplicationSession : IAsyncDisposable
{
    /// <summary>
    /// Gets the running host used to execute replay operations.
    /// </summary>
    IReplayHost ReplayHost { get; }

    /// <summary>
    /// Gets the read-only database connection available to Sqloom observation stages.
    /// </summary>
    string? ReadOnlyConnection { get; }

    /// <summary>
    /// Gets metadata captured while starting the application session.
    /// </summary>
    ReplayBootstrapReport Bootstrap { get; }
}
