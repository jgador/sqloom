namespace Sqloom.Host;

/// <summary>
/// Carries the OpenAI settings for Sqloom advice generation.
/// </summary>
internal sealed class OpenAIAdviceOptions
{
    public required string ApiKey { get; init; }

    public string BaseUrl { get; init; } = "https://api.openai.com";

    public string Model { get; init; } = "gpt-5.4-mini";
}
