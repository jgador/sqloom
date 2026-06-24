using System;
using Sqloom.Core.Execution;

namespace Sqloom.Testing;

/// <summary>
/// Represents one running app-under-test session owned by a Sqloom harness.
/// </summary>
public interface ISqloomApplicationSession : IAsyncDisposable
{
    IReplayHost ReplayHost { get; }

    string? ReadOnlyConnection { get; }

    ReplayBootstrapReport Bootstrap { get; }
}
