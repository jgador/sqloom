using System.Threading;
using System.Threading.Tasks;
using Sqloom.Core.Execution;

namespace Sqloom.Testing;

/// <summary>
/// Supplies the Sqloom harness for the application under test.
/// </summary>
public interface ISqloomApplication
{
    SqloomApplicationDescriptor Describe(SqloomApplicationContext context);

    ValueTask<ISqloomApplicationSession> StartAsync(
        SqloomApplicationContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Carries runner-provided inputs to an app harness.
/// </summary>
public sealed class SqloomApplicationContext
{
    public string CurrentDirectory { get; init; } = string.Empty;

    public ReplayLaunchOptions ReplayLaunchOptions { get; init; } = new();
}
