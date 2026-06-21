using System.Collections.Generic;

namespace Sqloom.AspNetCore.Endpoints;

/// <summary>
/// Describes one replayable HTTP request.
/// </summary>
public sealed class EndpointReplayRequest
{
    public required string OperationKey { get; init; }

    public required string HttpMethod { get; init; }

    public required string Route { get; init; }

    public string? Persona { get; init; }

    public required string RelativePathAndQuery { get; init; }

    public string? RequestBodyJson { get; init; }

    public IReadOnlyDictionary<string, string> PathValues { get; init; } =
        new Dictionary<string, string>();

    public IReadOnlyDictionary<string, string> QueryValues { get; init; } =
        new Dictionary<string, string>();

    public IReadOnlyDictionary<string, string> HeaderValues { get; init; } =
        new Dictionary<string, string>();
}
