using System;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Core.Contracts;
using Sqloom.Core.Execution;

namespace Sqloom.Host.Tests;

/// <summary>
/// Provides the first test app integration for resolver tests.
/// </summary>
public sealed class MultipleTestAppIntegrationA : IAppIntegration
{
    public string AppName => "MultipleTestAppIntegrationA";

    public IReplayHostFactory CreateReplayHostFactory()
    {
        return new TestReplayHostFactory();
    }

    public ReplayProfile GetReplayProfile()
    {
        return new ReplayProfile
        {
            DefaultOpenApiDocumentPath = "openapi.json",
        };
    }
}

/// <summary>
/// Provides the second test app integration for resolver tests.
/// </summary>
public sealed class MultipleTestAppIntegrationB : IAppIntegration
{
    public string AppName => "MultipleTestAppIntegrationB";

    public IReplayHostFactory CreateReplayHostFactory()
    {
        return new TestReplayHostFactory();
    }

    public ReplayProfile GetReplayProfile()
    {
        return new ReplayProfile
        {
            DefaultOpenApiDocumentPath = "openapi.json",
        };
    }
}

/// <summary>
/// Provides a test replay host factory for resolver tests.
/// </summary>
internal sealed class TestReplayHostFactory : IReplayHostFactory
{
    public Task<IReplayHost> CreateAsync(
        ReplayLaunchOptions? launchOptions = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Test app resolution does not create replay hosts.");
    }
}
