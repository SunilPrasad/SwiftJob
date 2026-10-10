using SwiftJob.Domain.Ai;

namespace SwiftJob.Application.Ai;

/// <summary>Everything needed to talk to the configured model. Contains the decrypted key: never log or return it.</summary>
public sealed record AiConnectionConfig(AiProvider Provider, string Model, string? ApiKey, string? Endpoint)
{
    public override string ToString() => $"{Provider}/{Model}"; // keep the key out of logs
}

/// <summary>Supplies the active AI configuration to adapters.</summary>
public interface IAiConfigurationSource
{
    /// <exception cref="AiNotConfiguredException">No usable AI configuration is saved.</exception>
    Task<AiConnectionConfig> GetRequiredAsync(CancellationToken cancellationToken);
}

/// <summary>Sends a trivial request to verify that provider, model and credentials work.</summary>
public interface IAiConnectionProbe
{
    /// <exception cref="Common.ExternalServiceException">The provider rejected the request.</exception>
    Task<string> ProbeAsync(AiConnectionConfig config, CancellationToken cancellationToken);
}

public sealed class AiNotConfiguredException()
    : Common.ConflictException("AI is not configured. Choose a provider and model in Settings → AI.");
