using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SwiftJob.Application.Abstractions;
using SwiftJob.Application.Common;
using SwiftJob.Domain.Ai;

namespace SwiftJob.Application.Ai;

public interface IAiSettingsService
{
    Task<AiSettingsDto> GetAsync(CancellationToken cancellationToken);
    Task<AiSettingsDto> SaveAsync(SaveAiSettingsRequest request, CancellationToken cancellationToken);
    Task<AiTestResultDto> TestAsync(CancellationToken cancellationToken);
}

public sealed class AiSettingsService(
    IAppDbContext db,
    ISecretProtector protector,
    IAiConnectionProbe probe,
    ILogger<AiSettingsService> logger) : IAiSettingsService, IAiConfigurationSource
{
    public async Task<AiSettingsDto> GetAsync(CancellationToken cancellationToken) =>
        ToDto(await FindAsync(cancellationToken) ?? new AiSettings());

    public async Task<AiSettingsDto> SaveAsync(SaveAiSettingsRequest request, CancellationToken cancellationToken)
    {
        var provider = request.Provider ?? throw new ArgumentException("Provider is required.");
        var info = AiProviderCatalog.Get(provider);
        var endpoint = NormaliseEndpoint(request.Endpoint, info.RequiresEndpoint);

        var settings = await FindAsync(cancellationToken);
        if (settings is null)
        {
            settings = new AiSettings();
            db.AiSettings.Add(settings);
        }

        // A key belongs to one provider: switching provider without a new key drops the old one.
        if (settings.Provider != provider && string.IsNullOrWhiteSpace(request.ApiKey))
        {
            settings.ProtectedApiKey = null;
        }

        settings.Provider = provider;
        settings.Model = request.Model.Trim();
        settings.Endpoint = endpoint;

        if (request.ClearApiKey)
        {
            settings.ProtectedApiKey = null;
        }
        else if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            settings.ProtectedApiKey = protector.Protect(request.ApiKey.Trim());
        }

        if (info.RequiresApiKey && settings.ProtectedApiKey is null)
        {
            throw new ArgumentException($"{info.DisplayName} requires an API key.");
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("AI settings saved: {Provider}/{Model}", settings.Provider, settings.Model);
        return ToDto(settings);
    }

    public async Task<AiTestResultDto> TestAsync(CancellationToken cancellationToken)
    {
        var config = await GetRequiredAsync(cancellationToken);
        try
        {
            var reply = await probe.ProbeAsync(config, cancellationToken);
            return new AiTestResultDto(true, config.Provider.ToString(), config.Model, reply);
        }
        catch (ExternalServiceException ex)
        {
            return new AiTestResultDto(false, config.Provider.ToString(), config.Model, ex.Message);
        }
    }

    public async Task<AiConnectionConfig> GetRequiredAsync(CancellationToken cancellationToken)
    {
        var settings = await FindAsync(cancellationToken);
        if (settings is not { IsConfigured: true })
        {
            throw new AiNotConfiguredException();
        }

        string? apiKey;
        try
        {
            apiKey = settings.ProtectedApiKey is null ? null : protector.Unprotect(settings.ProtectedApiKey);
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            throw new ConflictException(
                "The saved AI API key can no longer be decrypted (encryption keys changed). Enter the key again in Settings → AI model.", ex);
        }

        return new AiConnectionConfig(settings.Provider, settings.Model!, apiKey, settings.Endpoint);
    }

    private static string? NormaliseEndpoint(string? endpoint, bool required)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return required ? throw new ArgumentException("An endpoint URL is required for this provider.") : null;
        }

        if (!Uri.TryCreate(endpoint.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http"))
        {
            throw new ArgumentException("The endpoint must be an absolute http(s) URL.");
        }

        return uri.ToString().TrimEnd('/');
    }

    private Task<AiSettings?> FindAsync(CancellationToken cancellationToken) =>
        db.AiSettings.OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);

    private static AiSettingsDto ToDto(AiSettings settings) => new(
        settings.Provider,
        settings.Model,
        settings.Endpoint,
        settings.ProtectedApiKey is not null,
        settings.IsConfigured,
        AiProviderCatalog.All);
}
