namespace Sqloom.OpenAI.Advice;

/// <summary>
/// Carries the OpenAI request payload used for Sqloom advice generation.
/// </summary>
public sealed class OpenAITuningAdviceRequest
{
    public required string AppName { get; init; }

    public required string OperationKey { get; init; }

    public required string HttpMethod { get; init; }

    public required string Route { get; init; }

    public required string ArtifactManifestJson { get; init; }

    public required string SourceEvidenceJson { get; init; }

    public required string SqlServerSchemaText { get; init; }
}
