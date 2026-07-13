using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.Pipeline.Execution;

/// <summary>
/// Describes the planned operations for one replay run.
/// </summary>
public sealed class EndpointReplayPlan
{
    /// <summary>
    /// Gets the app name.
    /// </summary>
    [JsonPropertyName("appName")]
    public required string AppName { get; init; }

    /// <summary>
    /// Gets the open api path.
    /// </summary>
    [JsonPropertyName("openApiPath")]
    public required string OpenApiPath { get; init; }

    /// <summary>
    /// Gets the planned at utc.
    /// </summary>
    [JsonPropertyName("plannedAtUtc")]
    public DateTimeOffset PlannedAtUtc { get; init; }

    /// <summary>
    /// Gets the operations.
    /// </summary>
    [JsonPropertyName("operations")]
    public required IReadOnlyList<EndpointReplayPlanItem> Operations { get; init; }
}

/// <summary>
/// Describes one operation in a replay plan.
/// </summary>
public sealed class EndpointReplayPlanItem
{
    /// <summary>
    /// Gets the operation key.
    /// </summary>
    [JsonPropertyName("operationKey")]
    public required string OperationKey { get; init; }

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
    /// Gets the persona.
    /// </summary>
    [JsonPropertyName("persona")]
    public string? Persona { get; init; }

    /// <summary>
    /// Gets the requires authentication.
    /// </summary>
    [JsonPropertyName("requiresAuthentication")]
    public bool RequiresAuthentication { get; init; }

    /// <summary>
    /// Gets the has json request body.
    /// </summary>
    [JsonPropertyName("hasJsonRequestBody")]
    public bool HasJsonRequestBody { get; init; }

    /// <summary>
    /// Gets the replay safe.
    /// </summary>
    [JsonPropertyName("replaySafe")]
    public bool ReplaySafe { get; init; }

    /// <summary>
    /// Gets the status.
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>
    /// Gets the reason.
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    /// <summary>
    /// Gets the notes.
    /// </summary>
    [JsonPropertyName("notes")]
    public string Notes { get; init; } = string.Empty;
}
