using System.ComponentModel.DataAnnotations;
using SwiftJob.Domain.Ai;

namespace SwiftJob.Application.Ai;

/// <summary>AI settings as shown in the UI. The API key itself is never returned.</summary>
public sealed record AiSettingsDto(
    AiProvider Provider,
    string? Model,
    string? Endpoint,
    bool HasApiKey,
    bool IsConfigured,
    IReadOnlyList<AiProviderInfo> Providers);

public sealed record SaveAiSettingsRequest(
    [Required] AiProvider? Provider,
    [Required, StringLength(200, MinimumLength = 1)] string Model,
    [StringLength(500)] string? Endpoint,
    /// <summary>Null or empty keeps the stored key (when the provider is unchanged).</summary>
    [StringLength(500)] string? ApiKey,
    /// <summary>True removes the stored key.</summary>
    bool ClearApiKey = false);

public sealed record AiTestResultDto(bool Success, string Provider, string Model, string Message);
