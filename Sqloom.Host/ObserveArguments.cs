using Sqloom.QueryStore.QueryStore;

namespace Sqloom.Host;

/// <summary>
/// Carries arguments for the Sqloom observe command.
/// </summary>
internal sealed class ObserveArguments
{
    public required string ReadOnlyConnectionString { get; init; }

    public required QueryStoreObservationOptions ObservationOptions { get; init; }

    public required QueryStoreWorkloadProfile BaseWorkloadProfile { get; init; }

    public string? JsonOutputPathOverride { get; init; }

    public required string CurrentDirectory { get; init; }

    public bool AppOnly { get; init; }

    public bool ShowClassification { get; init; }

    public HostDebugWriter DebugWriter { get; set; } = HostDebugWriter.Disabled;
}
