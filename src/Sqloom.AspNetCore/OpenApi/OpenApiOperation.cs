using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.AspNetCore.OpenApi;

/// <summary>
/// Describes one OpenAPI operation discovered for replay.
/// </summary>
public sealed class OpenApiOperation
{
    [JsonPropertyName("stableOperationKey")]
    public required string StableOperationKey { get; init; }

    [JsonPropertyName("operationId")]
    public string? OperationId { get; init; }

    [JsonPropertyName("httpMethod")]
    public required string HttpMethod { get; init; }

    [JsonPropertyName("route")]
    public required string Route { get; init; }

    [JsonPropertyName("requiresAuthentication")]
    public bool RequiresAuthentication { get; init; }

    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    [JsonPropertyName("parameters")]
    public IReadOnlyList<OpenApiParameter> Parameters { get; init; } =
        Array.Empty<OpenApiParameter>();

    [JsonPropertyName("hasJsonRequestBody")]
    public bool HasJsonRequestBody { get; init; }

    [JsonPropertyName("requestBodyRequired")]
    public bool RequestBodyRequired { get; init; }

    [JsonPropertyName("jsonBodyExample")]
    public string? JsonBodyExample { get; init; }
}

/// <summary>
/// Describes one OpenAPI parameter used during replay.
/// </summary>
public sealed class OpenApiParameter
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("location")]
    public required string Location { get; init; }

    [JsonPropertyName("required")]
    public bool Required { get; init; }

    [JsonPropertyName("schemaType")]
    public string? SchemaType { get; init; }

    [JsonPropertyName("format")]
    public string? Format { get; init; }
}
