using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Pipeline.Execution;

/// <summary>
/// Describes one OpenAPI operation discovered for replay.
/// </summary>
public sealed class OpenApiOperation
{
    /// <summary>
    /// Gets the stable operation key.
    /// </summary>
    [JsonPropertyName("stableOperationKey")]
    public required string StableOperationKey { get; init; }

    /// <summary>
    /// Gets the operation id.
    /// </summary>
    [JsonPropertyName("operationId")]
    public string? OperationId { get; init; }

    /// <summary>
    /// Gets the http method.
    /// </summary>
    [JsonPropertyName("httpMethod")]
    public required string HttpMethod { get; init; }

    /// <summary>
    /// Gets the route.
    /// </summary>
    [JsonPropertyName("route")]
    public required string Route { get; init; }

    /// <summary>
    /// Gets the requires authentication.
    /// </summary>
    [JsonPropertyName("requiresAuthentication")]
    public bool RequiresAuthentication { get; init; }

    /// <summary>
    /// Gets the tags.
    /// </summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the parameters.
    /// </summary>
    [JsonPropertyName("parameters")]
    public IReadOnlyList<OpenApiParameter> Parameters { get; init; } =
        Array.Empty<OpenApiParameter>();

    /// <summary>
    /// Gets the has json request body.
    /// </summary>
    [JsonPropertyName("hasJsonRequestBody")]
    public bool HasJsonRequestBody { get; init; }

    /// <summary>
    /// Gets the request body required.
    /// </summary>
    [JsonPropertyName("requestBodyRequired")]
    public bool RequestBodyRequired { get; init; }

    /// <summary>
    /// Gets the json body example.
    /// </summary>
    [JsonPropertyName("jsonBodyExample")]
    public string? JsonBodyExample { get; init; }
}

/// <summary>
/// Describes one OpenAPI parameter used during replay.
/// </summary>
public sealed class OpenApiParameter
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the location.
    /// </summary>
    [JsonPropertyName("location")]
    public required string Location { get; init; }

    /// <summary>
    /// Gets the required.
    /// </summary>
    [JsonPropertyName("required")]
    public bool Required { get; init; }

    /// <summary>
    /// Gets the schema type.
    /// </summary>
    [JsonPropertyName("schemaType")]
    public string? SchemaType { get; init; }

    /// <summary>
    /// Gets the format.
    /// </summary>
    [JsonPropertyName("format")]
    public string? Format { get; init; }
}
