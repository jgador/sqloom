using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Core.Execution;
using Sqloom.Tests;
using Sqloom.Testing;

namespace Sqloom.Host.Tests;

/// <summary>
/// Provides the first test app harness for resolver tests.
/// </summary>
internal sealed class TestApplicationA : ISqloomApplication
{
    public SqloomApplicationManifest Describe(SqloomApplicationContext context)
    {
        return new SqloomApplicationManifest
        {
            Name = "TestApplicationA",
            OpenApiPath = RepositoryPaths.GetTestAppOpenApiPath(),
            ReplayProfile = ManifestFactory.CreateReplayProfile(),
        };
    }

    public ValueTask<ISqloomApplicationSession> StartAsync(
        SqloomApplicationContext context,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<ISqloomApplicationSession>(new ApplicationSessionFake());
    }
}

/// <summary>
/// Provides the second test app harness for resolver tests.
/// </summary>
internal sealed class TestApplicationB : ISqloomApplication
{
    public SqloomApplicationManifest Describe(SqloomApplicationContext context)
    {
        return new SqloomApplicationManifest
        {
            Name = "TestApplicationB",
            OpenApiPath = RepositoryPaths.GetTestAppOpenApiPath(),
            ReplayProfile = ManifestFactory.CreateReplayProfile(),
        };
    }

    public ValueTask<ISqloomApplicationSession> StartAsync(
        SqloomApplicationContext context,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<ISqloomApplicationSession>(new ApplicationSessionFake());
    }
}

/// <summary>
/// Provides manifest defaults for parser and dispatch tests.
/// </summary>
internal static class ManifestFactory
{
    public static SqloomApplicationManifest CreateManifest()
    {
        return new SqloomApplicationManifest
        {
            Name = "Sqloom Test Harness",
            OpenApiPath = RepositoryPaths.GetTestAppOpenApiPath(),
            ReplayProfile = CreateReplayProfile(),
        };
    }

    public static ReplayProfile CreateReplayProfile()
    {
        return new ReplayProfile();
    }
}

/// <summary>
/// Provides a test application session for parser and dispatch tests.
/// </summary>
internal sealed class ApplicationSessionFake : ISqloomApplicationSession
{
    public IReplayHost ReplayHost { get; } = new ReplayHostFake();

    public string? ReadOnlyConnection => "Server=localhost;Database=Sqloom;Trusted_Connection=True;";

    public ReplayBootstrapReport Bootstrap => ReplayHost.Bootstrap;

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Provides a test replay host for parser and dispatch tests.
/// </summary>
internal sealed class ReplayHostFake : IReplayHost
{
    public HttpClient Client { get; } = new()
    {
        BaseAddress = new Uri("http://localhost"),
    };

    public IServiceProvider Services => EmptyServiceProvider.Instance;

    public ReplayBootstrapReport Bootstrap { get; } = new();

    public Task<PreparedReplayOperation> PrepareOperationAsync(
        ResolvedReplayOperation operation,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Test app resolution does not execute replay operations.");
    }

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        return ValueTask.CompletedTask;
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new();

        public object? GetService(Type serviceType)
        {
            return null;
        }
    }
}
