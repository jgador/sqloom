using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Sqloom.Core.Execution;

public interface IReplayHostFactory
{
    Task<IReplayHost> CreateAsync(
        ReplayLaunchOptions? launchOptions = null,
        CancellationToken cancellationToken = default);
}

public interface IReplayHost : IAsyncDisposable
{
    HttpClient Client { get; }

    IServiceProvider Services { get; }

    ReplayBootstrapReport Bootstrap { get; }

    Task<PreparedReplayOperation> PrepareOperationAsync(
        ResolvedReplayOperation operation,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Carries app-owned launch inputs for replay host bootstrap.
/// </summary>
public sealed class ReplayLaunchOptions
{
    [JsonPropertyName("sqlServerDacpacPath")]
    public string? SqlServerDacpacPath { get; init; }

    [JsonPropertyName("sqlServerSeedSqlPath")]
    public string? SqlServerSeedSqlPath { get; init; }
}

/// <summary>
/// Captures the bootstrap evidence for a replay host.
/// </summary>
public sealed class ReplayBootstrapReport
{
    [JsonPropertyName("sqlServerDacpac")]
    public SqlServerDacpacArtifact? SqlServerDacpac { get; init; }

    [JsonPropertyName("sqlServerSeedSql")]
    public SqlServerSeedSqlArtifact? SqlServerSeedSql { get; init; }
}

/// <summary>
/// Describes the DACPAC used to bootstrap a SQL Server replay database.
/// </summary>
public sealed class SqlServerDacpacArtifact
{
    [JsonPropertyName("sourcePath")]
    public required string SourcePath { get; init; }

    [JsonPropertyName("fileName")]
    public required string FileName { get; init; }

    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }
}

/// <summary>
/// Describes the SQL seed script used to populate a SQL Server replay database after DACPAC deploy.
/// </summary>
public sealed class SqlServerSeedSqlArtifact
{
    [JsonPropertyName("sourcePath")]
    public required string SourcePath { get; init; }

    [JsonPropertyName("fileName")]
    public required string FileName { get; init; }

    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }
}

/// <summary>
/// Describes the replay defaults, personas, and overlays for a Sqloom app.
/// </summary>
public sealed class ReplayProfile
{
    [JsonPropertyName("includeAuthenticatedGetOperationsByDefault")]
    public bool IncludeAuthenticatedGetOperationsByDefault { get; init; } = true;

    [JsonPropertyName("personas")]
    public IReadOnlyList<ReplayPersonaDefinition> Personas { get; init; } =
        Array.Empty<ReplayPersonaDefinition>();

    [JsonPropertyName("operationOverlays")]
    public IReadOnlyList<ReplayOperationOverlayDefinition> OperationOverlays { get; init; } =
        Array.Empty<ReplayOperationOverlayDefinition>();
}

/// <summary>
/// Describes replay persona.
/// </summary>
public sealed class ReplayPersonaDefinition
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("requiresAuthentication")]
    public bool RequiresAuthentication { get; init; } = true;

    [JsonPropertyName("notes")]
    public string Notes { get; init; } = string.Empty;
}

/// <summary>
/// Describes replay operation overlay.
/// </summary>
public sealed class ReplayOperationOverlayDefinition
{
    [JsonPropertyName("operationKey")]
    public required string OperationKey { get; init; }

    [JsonPropertyName("persona")]
    public string? Persona { get; init; }

    [JsonPropertyName("replayByDefault")]
    public bool ReplayByDefault { get; init; } = true;

    [JsonPropertyName("allowNonGetReplay")]
    public bool AllowNonGetReplay { get; init; }

    [JsonPropertyName("skipReason")]
    public string? SkipReason { get; init; }

    [JsonPropertyName("requestBodyJson")]
    public string? RequestBodyJson { get; init; }

    [JsonPropertyName("pathValues")]
    public IReadOnlyDictionary<string, string> PathValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("queryValues")]
    public IReadOnlyDictionary<string, string> QueryValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("headerValues")]
    public IReadOnlyDictionary<string, string> HeaderValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("notes")]
    public string Notes { get; init; } = string.Empty;
}

/// <summary>
/// Captures one replay operation after Sqloom overlays and defaults are resolved.
/// </summary>
public sealed class ResolvedReplayOperation
{
    public required string OperationKey { get; init; }

    public string? OperationId { get; init; }

    public required string HttpMethod { get; init; }

    public required string Route { get; init; }

    public string? Persona { get; init; }

    public string? RequestBodyJson { get; init; }

    public IReadOnlyDictionary<string, string> PathValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> QueryValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> HeaderValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public string Notes { get; init; } = string.Empty;
}

/// <summary>
/// Carries the inputs prepared for one replay operation.
/// </summary>
public sealed class PreparedReplayOperation
{
    public string? Persona { get; init; }

    public string? AccessToken { get; init; }

    public string? RequestBodyJson { get; init; }

    public IReadOnlyDictionary<string, string> PathValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> QueryValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> HeaderValues { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public string Notes { get; init; } = string.Empty;
}
