namespace Sqloom.Host;

/// <summary>
/// Carries arguments for the Sqloom advise command.
/// </summary>
internal sealed class AdviseArguments
{
    public required string ReplayArtifactDir { get; init; }

    public required string QueryStoreCorrelationPath { get; init; }

    public required string SchemaPath { get; init; }

    public required string JsonOutputPath { get; init; }

    public required ModelProviderKind ModelProvider { get; init; }

    public OpenAIAdviceOptions? OpenAIOptions { get; init; }

    public HostDebugWriter DebugWriter { get; set; } = HostDebugWriter.Disabled;
}
