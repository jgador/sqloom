namespace Sqloom.Core.Execution;

/// <summary>
/// Describes one endpoint workload definition used during replay.
/// </summary>
public sealed class EndpointWorkloadDefinition
{
    public required string Name { get; init; }

    public required string HttpMethod { get; init; }

    public required string Route { get; init; }

    public required string ControllerTypeName { get; init; }

    public required string ServiceEntryPoint { get; init; }

    public required SqlSurfaceKind SqlSurface { get; init; }

    public bool RequiresHintPack { get; init; }

    public string Notes { get; init; } = string.Empty;
}
