using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using SwiftJob.Application.Ai;
using SwiftJob.Application.Common;

namespace SwiftJob.Infrastructure.Ai;

/// <summary>Verifies provider, model and key with a tiny agent run.</summary>
internal sealed class AgentConnectionProbe(IAiChatClientFactory chatClientFactory) : IAiConnectionProbe
{
    public async Task<string> ProbeAsync(AiConnectionConfig config, CancellationToken cancellationToken)
    {
        try
        {
            var agent = new ChatClientAgent(
                chatClientFactory.Create(config),
                new ChatClientAgentOptions { Name = "ConnectionProbe", ChatOptions = new ChatOptions { MaxOutputTokens = 2000 } });
            var response = await agent.RunAsync("Reply with exactly: OK", cancellationToken: cancellationToken);
            var text = response.Text?.Trim();
            return string.IsNullOrEmpty(text)
                ? $"{config} responded."
                : $"{config} responded: \"{(text.Length > 80 ? text[..80] + "…" : text)}\"";
        }
        catch (Exception ex) when (ex is not (OperationCanceledException or ConflictException))
        {
            throw new ExternalServiceException($"{config} could not be reached: {ex.Message}", ex);
        }
    }
}
