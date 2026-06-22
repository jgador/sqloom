using Sqloom.Core.Execution;
using Sqloom.QueryStore.QueryStore;

namespace Sqloom.Testing;

/// <summary>
/// Declares the application under test and its Sqloom defaults.
/// </summary>
public sealed class SqloomApplicationManifest
{
    public required string Name { get; init; }

    public required ReplayProfile ReplayProfile { get; init; }

    public QueryStoreWorkloadProfile QueryStoreWorkloadProfile { get; init; } =
        QueryStoreWorkloadProfile.Empty;

    public string? SqlServerSchemaPath { get; init; }
}
