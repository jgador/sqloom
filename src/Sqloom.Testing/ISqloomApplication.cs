using System.Threading;
using System.Threading.Tasks;
using Sqloom.Pipeline.Execution;

namespace Sqloom.Testing;

/// <summary>
/// Supplies the Sqloom harness for the application under test.
/// </summary>
public interface ISqloomApplication
{
    /// <summary>
    /// Describes the application and its default replay inputs without starting it.
    /// </summary>
    SqloomApplicationManifest Describe(SqloomApplicationContext context);

    /// <summary>
    /// Starts an application session that Sqloom can replay against.
    /// </summary>
    ValueTask<ISqloomApplicationSession> StartAsync(
        SqloomApplicationContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Carries runner-provided inputs to an app harness.
/// </summary>
public sealed class SqloomApplicationContext
{
    /// <summary>
    /// Gets the working directory selected by the runner.
    /// </summary>
    public string CurrentDirectory { get; init; } = string.Empty;

    /// <summary>
    /// Gets the application database connection string supplied to the harness.
    /// </summary>
    public string? ApplicationConnectionString { get; init; }

    /// <summary>
    /// Gets the launch inputs supplied for this replay session.
    /// </summary>
    public ReplayLaunchOptions ReplayLaunchOptions { get; init; } = new();
}
