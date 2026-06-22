using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Core.Execution;
using Sqloom.Testing;

namespace Sqloom.Host.Tests;

/// <summary>
/// Provides the first test app harness for resolver tests.
/// </summary>
internal sealed class MultipleTestApplicationA : ISqloomApplication
{
    public SqloomApplicationDescriptor Describe(SqloomApplicationContext context)
    {
        return new SqloomApplicationDescriptor
        {
            Name = "MultipleTestApplicationA",
            ReplayProfile = TestApplicationDescriptorFactory.CreateReplayProfile(),
        };
    }

    public ValueTask<ISqloomApplicationSession> StartAsync(
        SqloomApplicationContext context,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<ISqloomApplicationSession>(new TestSqloomApplicationSession());
    }
}

/// <summary>
/// Provides the second test app harness for resolver tests.
/// </summary>
internal sealed class MultipleTestApplicationB : ISqloomApplication
{
    public SqloomApplicationDescriptor Describe(SqloomApplicationContext context)
    {
        return new SqloomApplicationDescriptor
        {
            Name = "MultipleTestApplicationB",
            ReplayProfile = TestApplicationDescriptorFactory.CreateReplayProfile(),
        };
    }

    public ValueTask<ISqloomApplicationSession> StartAsync(
        SqloomApplicationContext context,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<ISqloomApplicationSession>(new TestSqloomApplicationSession());
    }
}

/// <summary>
/// Provides descriptor defaults for parser and dispatch tests.
/// </summary>
internal static class TestApplicationDescriptorFactory
{
    public static SqloomApplicationDescriptor CreateDescriptor()
    {
        return new SqloomApplicationDescriptor
        {
            Name = "Sqloom Test Harness",
            ReplayProfile = CreateReplayProfile(),
        };
    }

    public static ReplayProfile CreateReplayProfile()
    {
        return new ReplayProfile
        {
            DefaultOpenApiDocumentPath = "openapi.json",
        };
    }
}

/// <summary>
/// Provides a test application session for parser and dispatch tests.
/// </summary>
internal sealed class TestSqloomApplicationSession : ISqloomApplicationSession
{
    public IReplayHost ReplayHost { get; } = new TestReplayHost();

    public string? ReadOnlyConnectionString => "Server=localhost;Database=Sqloom;Trusted_Connection=True;";

    public ReplayBootstrapReport Bootstrap => ReplayHost.Bootstrap;

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Provides a test replay host for parser and dispatch tests.
/// </summary>
internal sealed class TestReplayHost : IReplayHost
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
