using Sqloom.Core.Execution;
using Sqloom.QueryStore.QueryStore;

namespace Sqloom.Testing;

/// <summary>
/// Declares the application under test and its Sqloom defaults.
/// </summary>
public sealed class SqloomApplicationManifest
{
    public required string Name { get; init; }

    public required string OpenApiPath { get; init; }

    public required ReplayProfile ReplayProfile { get; init; }

    public WorkloadProfile WorkloadProfile { get; init; } =
        WorkloadProfile.Empty;

    public string? SchemaPath { get; init; }
}
