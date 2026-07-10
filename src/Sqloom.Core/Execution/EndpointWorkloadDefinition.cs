namespace Sqloom.Core.Execution;

/// <summary>
/// Describes one endpoint workload definition used during replay.
/// </summary>
public sealed class EndpointWorkloadDefinition
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the http method.
    /// </summary>
    public required string HttpMethod { get; init; }

    /// <summary>
    /// Gets the route.
    /// </summary>
    public required string Route { get; init; }

    /// <summary>
    /// Gets the controller type name.
    /// </summary>
    public required string ControllerTypeName { get; init; }

    /// <summary>
    /// Gets the service entry point.
    /// </summary>
    public required string ServiceEntryPoint { get; init; }

    /// <summary>
    /// Gets the sql surface.
    /// </summary>
    public required SqlSurfaceKind SqlSurface { get; init; }

    /// <summary>
    /// Gets the requires hint pack.
    /// </summary>
    public bool RequiresHintPack { get; init; }

    /// <summary>
    /// Gets the notes.
    /// </summary>
    public string Notes { get; init; } = string.Empty;
}
