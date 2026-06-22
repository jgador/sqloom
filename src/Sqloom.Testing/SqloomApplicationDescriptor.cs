using Sqloom.Core.Execution;
using Sqloom.QueryStore.QueryStore;

namespace Sqloom.Testing;

/// <summary>
/// Describes the application under test and its Sqloom defaults.
/// </summary>
public sealed class SqloomApplicationDescriptor
{
    public required string Name { get; init; }

    public required ReplayProfile ReplayProfile { get; init; }

    public QueryStoreWorkloadProfile QueryStoreWorkloadProfile { get; init; } =
        QueryStoreWorkloadProfile.Empty;

    public string? SqlServerSchemaPath { get; init; }
}
