using System.Collections.Generic;

namespace Sqloom.AspNetCore.Endpoints;

/// <summary>
/// Captures the HTTP response and SQL evidence for one replayed operation.
/// </summary>
public sealed class EndpointReplayResult
{
    public required string OperationKey { get; init; }

    public required string HttpMethod { get; init; }

    public required string Route { get; init; }

    public string? Persona { get; init; }

    public required string Status { get; init; }

    public int? HttpStatusCode { get; init; }

    public double DurationMilliseconds { get; init; }

    public string ResponseBody { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string[]> ResponseHeaders { get; init; } =
        new Dictionary<string, string[]>();

    public string? ErrorMessage { get; init; }

    public required EndpointReplayRequest Request { get; init; }

    public IReadOnlyList<CapturedSqlCommand> CapturedSqlCommands { get; init; } =
        [];

    public required string ArtifactPath { get; init; }
}
