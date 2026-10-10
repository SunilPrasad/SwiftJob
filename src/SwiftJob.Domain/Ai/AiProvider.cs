namespace SwiftJob.Domain.Ai;

/// <summary>Supported model providers. All are accessed through provider-neutral abstractions.</summary>
public enum AiProvider
{
    /// <summary>Anthropic Claude via the official Anthropic SDK.</summary>
    Anthropic = 0,

    /// <summary>OpenAI via the official OpenAI SDK.</summary>
    OpenAI = 1,

    /// <summary>
    /// Any service exposing the OpenAI Chat Completions API at a custom endpoint
    /// (e.g. Ollama, LM Studio, OpenRouter, Groq, Mistral, Google Gemini).
    /// </summary>
    OpenAICompatible = 2,
}
