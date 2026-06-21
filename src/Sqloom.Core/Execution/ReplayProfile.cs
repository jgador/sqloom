using System;
using System.Collections.Generic;
using System.Net.Http;
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
    public string? SqlServerDacpacPath { get; init; }

    public string? SqlServerSeedSqlPath { get; init; }
}

/// <summary>
/// Captures the bootstrap evidence for a replay host.
/// </summary>
public sealed class ReplayBootstrapReport
{
    public SqlServerDacpacArtifact? SqlServerDacpac { get; init; }

    public SqlServerSeedSqlArtifact? SqlServerSeedSql { get; init; }
}

/// <summary>
/// Describes the DACPAC used to bootstrap a SQL Server replay database.
/// </summary>
public sealed class SqlServerDacpacArtifact
{
    public required string SourcePath { get; init; }

    public required string FileName { get; init; }

    public required string Sha256 { get; init; }
}

/// <summary>
/// Describes the SQL seed script used to populate a SQL Server replay database after DACPAC deploy.
/// </summary>
public sealed class SqlServerSeedSqlArtifact
{
    public required string SourcePath { get; init; }

    public required string FileName { get; init; }

    public required string Sha256 { get; init; }
}

/// <summary>
/// Describes the replay defaults, personas, and overlays for a Sqloom app.
/// </summary>
public sealed class ReplayProfile
{
    public required string DefaultOpenApiDocumentPath { get; init; }

    public bool IncludeAuthenticatedGetOperationsByDefault { get; init; } = true;

    public IReadOnlyList<ReplayPersonaDefinition> Personas { get; init; } =
        Array.Empty<ReplayPersonaDefinition>();

    public IReadOnlyList<ReplayOperationOverlayDefinition> OperationOverlays { get; init; } =
        Array.Empty<ReplayOperationOverlayDefinition>();
}

/// <summary>
/// Describes replay persona.
/// </summary>
public sealed class ReplayPersonaDefinition
{
    public required string Name { get; init; }

    public bool RequiresAuthentication { get; init; } = true;

    public string Notes { get; init; } = string.Empty;
}

/// <summary>
/// Describes replay operation overlay.
/// </summary>
public sealed class ReplayOperationOverlayDefinition
{
    public required string OperationKey { get; init; }

    public string? Persona { get; init; }

    public bool ReplayByDefault { get; init; } = true;

    public bool AllowNonGetReplay { get; init; }

    public string? SkipReason { get; init; }

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
