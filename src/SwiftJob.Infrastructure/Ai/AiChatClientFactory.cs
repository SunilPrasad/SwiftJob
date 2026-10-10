using System.ClientModel;
using Anthropic;
using Microsoft.Extensions.AI;
using OpenAI;
using SwiftJob.Application.Ai;
using SwiftJob.Domain.Ai;

namespace SwiftJob.Infrastructure.Ai;

/// <summary>
/// The only place that knows about concrete AI SDKs. Everything else works with the provider-neutral
/// <see cref="IChatClient"/> (Microsoft.Extensions.AI), on which Microsoft Agent Framework agents are built.
/// </summary>
public interface IAiChatClientFactory
{
    IChatClient Create(AiConnectionConfig config);
}

internal sealed class AiChatClientFactory : IAiChatClientFactory
{
    public const int DefaultMaxOutputTokens = 16_000;

    public IChatClient Create(AiConnectionConfig config) => config.Provider switch
    {
        AiProvider.Anthropic => new AnthropicClient { ApiKey = Required(config.ApiKey, "Anthropic API key") }
            .AsIChatClient(config.Model, DefaultMaxOutputTokens),

        AiProvider.OpenAI => new OpenAIClient(Required(config.ApiKey, "OpenAI API key"))
            .GetChatClient(config.Model)
            .AsIChatClient(),

        AiProvider.OpenAICompatible => new OpenAIClient(
                new ApiKeyCredential(string.IsNullOrWhiteSpace(config.ApiKey) ? "not-required" : config.ApiKey),
                new OpenAIClientOptions { Endpoint = new Uri(Required(config.Endpoint, "endpoint")) })
            .GetChatClient(config.Model)
            .AsIChatClient(),

        _ => throw new NotSupportedException($"AI provider '{config.Provider}' is not supported."),
    };

    private static string Required(string? value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new AiNotConfiguredException() : value;
}
