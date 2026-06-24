using Sqloom.Core.Execution;

namespace Sqloom.TestApp.Harness;

internal sealed class ReplayBootstrapResult
{
    public required string ApplicationConnectionString { get; init; }

    public required ReplayBootstrapReport Bootstrap { get; init; }
}
