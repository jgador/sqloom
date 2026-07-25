using System;
using Sqloom.Pipeline.OpenAI.Advice;
using Sqloom.Pipeline.QueryStore;

namespace Sqloom.Host;

/// <summary>
/// Builds OpenAI request payloads from Sqloom correlation evidence.
/// </summary>
internal static class OpenAIAdviceRequestBuilder
{
    public static OpenAITuningAdviceRequest Build(
        string appName,
        OperationCorrelationSummary operation,
        string artifactManifestJson,
        string sourceEvidenceJson,
        string schemaSql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appName);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactManifestJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceEvidenceJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaSql);

        return new OpenAITuningAdviceRequest
        {
            AppName = appName,
            OperationKey = operation.OperationKey,
            HttpMethod = operation.HttpMethod,
            Route = operation.Route,
            ArtifactManifestJson = artifactManifestJson,
            SourceEvidenceJson = sourceEvidenceJson,
            SchemaSql = schemaSql,
        };
    }
}
