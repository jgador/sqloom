using Sqloom.Pipeline.Execution;
using Sqloom.Pipeline.QueryStore;

namespace Sqloom.Testing;

/// <summary>
/// Declares the application under test and its Sqloom defaults.
/// </summary>
public sealed class SqloomApplicationManifest
{
    /// <summary>
    /// Gets the display name of the application under test.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the application-owned defaults for replay planning.
    /// </summary>
    public required ReplayProfile ReplayProfile { get; init; }

    /// <summary>
    /// Gets the application-owned Query Store classification profile.
    /// </summary>
    public WorkloadProfile WorkloadProfile { get; init; } =
        WorkloadProfile.Empty;

    /// <summary>
    /// Gets the optional SQL Server DACPAC used to prepare replay data.
    /// </summary>
    public string? SqlServerDacpacPath { get; init; }
}
