using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sqloom.AspNetCore.Endpoints;

/// <summary>
/// Describes the planned operations for one replay run.
/// </summary>
public sealed class EndpointReplayPlan
{
    [JsonPropertyName("appName")]
    public required string AppName { get; init; }

    [JsonPropertyName("openApiPath")]
    public required string OpenApiPath { get; init; }

    [JsonPropertyName("plannedAtUtc")]
    public DateTimeOffset PlannedAtUtc { get; init; }

    [JsonPropertyName("operations")]
    public required IReadOnlyList<EndpointReplayPlanItem> Operations { get; init; }
}

/// <summary>
/// Describes one operation in a replay plan.
/// </summary>
public sealed class EndpointReplayPlanItem
{
    [JsonPropertyName("operationKey")]
    public required string OperationKey { get; init; }

    [JsonPropertyName("operationId")]
    public string? OperationId { get; init; }

    [JsonPropertyName("httpMethod")]
    public required string HttpMethod { get; init; }

    [JsonPropertyName("route")]
    public required string Route { get; init; }

    [JsonPropertyName("persona")]
    public string? Persona { get; init; }

    [JsonPropertyName("requiresAuthentication")]
    public bool RequiresAuthentication { get; init; }

    [JsonPropertyName("hasJsonRequestBody")]
    public bool HasJsonRequestBody { get; init; }

    [JsonPropertyName("replaySafe")]
    public bool ReplaySafe { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    [JsonPropertyName("notes")]
    public string Notes { get; init; } = string.Empty;
}
