using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Pipeline.Execution;

/// <summary>
/// Describes one ASP.NET Core endpoint operation discovered for replay.
/// </summary>
public sealed class ReplayOperation
{
    /// <summary>
    /// Exact replay selector in the form METHOD /path/template.
    /// </summary>
    [JsonPropertyName("stableOperationKey")]
    public required string StableOperationKey { get; init; }

    /// <summary>
    /// Optional action-level identifier when discovery can derive one.
    /// </summary>
    [JsonPropertyName("operationId")]
    public string? OperationId { get; init; }

    /// <summary>
    /// HTTP verb used when issuing the replay request.
    /// </summary>
    [JsonPropertyName("httpMethod")]
    public required string HttpMethod { get; init; }

    /// <summary>
    /// Normalized ASP.NET Core route template beginning with a slash.
    /// </summary>
    [JsonPropertyName("route")]
    public required string Route { get; init; }

    /// <summary>
    /// Fully qualified controller type that declares the action when source discovery can resolve it.
    /// </summary>
    [JsonPropertyName("controllerType")]
    public string? ControllerType { get; init; }

    /// <summary>
    /// Controller action method that handles the route when source discovery can resolve it.
    /// </summary>
    [JsonPropertyName("methodName")]
    public string? MethodName { get; init; }

    /// <summary>
    /// Roslyn documentation comment identifier for the action method when available.
    /// </summary>
    [JsonPropertyName("methodSymbolId")]
    public string? MethodSymbolId { get; init; }

    /// <summary>
    /// Whether replay should treat the endpoint as authenticated by default.
    /// </summary>
    [JsonPropertyName("requiresAuthentication")]
    public bool RequiresAuthentication { get; init; }

    /// <summary>
    /// Grouping labels used by catalog displays and future endpoint pickers.
    /// </summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Model-bound non-body parameters that replay must resolve before sending the request.
    /// </summary>
    [JsonPropertyName("parameters")]
    public IReadOnlyList<ReplayParameter> Parameters { get; init; } =
        Array.Empty<ReplayParameter>();

    /// <summary>
    /// Whether the action accepts a JSON request body parameter.
    /// </summary>
    [JsonPropertyName("hasJsonRequestBody")]
    public bool HasJsonRequestBody { get; init; }

    /// <summary>
    /// Whether a JSON request body must be supplied before replay can send the request.
    /// </summary>
    [JsonPropertyName("requestBodyRequired")]
    public bool RequestBodyRequired { get; init; }

    /// <summary>
    /// CLR type display name for the JSON request body when source discovery can resolve it.
    /// </summary>
    [JsonPropertyName("requestBodyClrType")]
    public string? RequestBodyClrType { get; init; }

    /// <summary>
    /// Optional JSON body example used as a replay fallback.
    /// </summary>
    [JsonPropertyName("jsonBodyExample")]
    public string? JsonBodyExample { get; init; }
}

/// <summary>
/// Describes one endpoint parameter used during replay.
/// </summary>
public sealed class ReplayParameter
{
    /// <summary>
    /// Model-bound parameter name used in replay value dictionaries.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Model-binding location, such as path, query, or header.
    /// </summary>
    [JsonPropertyName("location")]
    public required string Location { get; init; }

    /// <summary>
    /// Whether replay must supply a value for this parameter.
    /// </summary>
    [JsonPropertyName("required")]
    public bool Required { get; init; }

    /// <summary>
    /// CLR type display name resolved from source.
    /// </summary>
    [JsonPropertyName("clrType")]
    public string? ClrType { get; init; }

    /// <summary>
    /// JSON schema type used by replay value validation.
    /// </summary>
    [JsonPropertyName("schemaType")]
    public string? SchemaType { get; init; }

    /// <summary>
    /// JSON schema format used by replay value validation.
    /// </summary>
    [JsonPropertyName("format")]
    public string? Format { get; init; }
}
