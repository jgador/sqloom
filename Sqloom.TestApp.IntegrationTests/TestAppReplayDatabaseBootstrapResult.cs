using Sqloom.Core.Execution;

namespace Sqloom.TestApp.IntegrationTests;

internal sealed class TestAppReplayDatabaseBootstrapResult
{
    public required string ApplicationConnectionString { get; init; }

    public required ReplayBootstrapReport Bootstrap { get; init; }
}
