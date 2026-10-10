using SwiftJob.Domain.Ai;

namespace SwiftJob.Application.Ai;

/// <summary>
/// Describes each provider for the UI, including capability differences, so the frontend never
/// hard-codes provider knowledge. Model suggestions are only hints: any model id can be entered.
/// </summary>
public sealed record AiProviderInfo(
    AiProvider Provider,
    string DisplayName,
    bool RequiresApiKey,
    bool RequiresEndpoint,
    string? DefaultEndpoint,
    IReadOnlyList<string> SuggestedModels,
    string ApiKeyHelpUrl,
    string Notes);

public static class AiProviderCatalog
{
    public static IReadOnlyList<AiProviderInfo> All { get; } =
    [
        new(AiProvider.Anthropic, "Anthropic (Claude)", RequiresApiKey: true, RequiresEndpoint: false, null,
            ["claude-opus-5-5", "claude-sonnet-5-5", "claude-haiku-5-5"],
            "https://console.anthropic.com/settings/keys",
            "Uses the official Anthropic SDK. Structured output is supported natively."),
        new(AiProvider.OpenAI, "OpenAI", RequiresApiKey: true, RequiresEndpoint: false, null,
            ["gpt-5", "gpt-5-mini"],
            "https://platform.openai.com/api-keys",
            "Uses the official OpenAI SDK. Structured output is supported natively."),
        new(AiProvider.OpenAICompatible, "OpenAI-compatible endpoint", RequiresApiKey: false, RequiresEndpoint: true,
            "http://localhost:11434/v1",
            [],
            "https://github.com/ollama/ollama/blob/main/docs/openai.md",
            "Ollama, LM Studio, OpenRouter, Groq, Mistral, Google Gemini and others. The API key is optional " +
            "(not needed for local Ollama). Structured-output support depends on the service and model."),
    ];

    public static AiProviderInfo Get(AiProvider provider) => All.Single(p => p.Provider == provider);
}
