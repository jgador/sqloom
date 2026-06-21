using System;
using System.Collections.Generic;

namespace Sqloom.AspNetCore.OpenApi;

/// <summary>
/// Describes one OpenAPI operation discovered for replay.
/// </summary>
public sealed class DiscoveredOpenApiOperation
{
    public required string StableOperationKey { get; init; }

    public string? OperationId { get; init; }

    public required string HttpMethod { get; init; }

    public required string Route { get; init; }

    public bool RequiresAuthentication { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    public IReadOnlyList<OpenApiParameterDefinition> Parameters { get; init; } =
        Array.Empty<OpenApiParameterDefinition>();

    public bool HasJsonRequestBody { get; init; }

    public bool RequestBodyRequired { get; init; }

    public string? JsonRequestBodyExample { get; init; }
}

/// <summary>
/// Describes one OpenAPI parameter used during replay.
/// </summary>
public sealed class OpenApiParameterDefinition
{
    public required string Name { get; init; }

    public required string Location { get; init; }

    public bool Required { get; init; }

    public string? SchemaType { get; init; }

    public string? Format { get; init; }
}
