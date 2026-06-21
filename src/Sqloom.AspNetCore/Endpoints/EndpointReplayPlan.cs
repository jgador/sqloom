using System;
using System.Collections.Generic;

namespace Sqloom.AspNetCore.Endpoints;

/// <summary>
/// Describes the planned operations for one replay run.
/// </summary>
public sealed class EndpointReplayPlan
{
    public required string AppName { get; init; }

    public required string OpenApiDocumentPath { get; init; }

    public DateTimeOffset PlannedAtUtc { get; init; }

    public required IReadOnlyList<EndpointReplayPlanItem> Operations { get; init; }
}

/// <summary>
/// Describes one operation in a replay plan.
/// </summary>
public sealed class EndpointReplayPlanItem
{
    public required string OperationKey { get; init; }

    public string? OperationId { get; init; }

    public required string HttpMethod { get; init; }

    public required string Route { get; init; }

    public string? Persona { get; init; }

    public bool RequiresAuthentication { get; init; }

    public bool HasJsonRequestBody { get; init; }

    public bool ReplaySafe { get; init; }

    public required string Status { get; init; }

    public string? Reason { get; init; }

    public string Notes { get; init; } = string.Empty;
}
