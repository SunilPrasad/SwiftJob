using System.ComponentModel.DataAnnotations;

namespace SwiftJob.Application.Gmail;

/// <summary>Gmail settings as shown in the UI. The client secret and tokens are never returned.</summary>
public sealed record GmailSettingsDto(
    bool HasClientCredentials,
    string? ClientId,
    string RedirectUri,
    string Scope,
    bool IsConnected,
    string? ConnectedEmail,
    DateTime? ConnectedAt,
    string SearchQuery,
    string DefaultSearchQuery,
    bool AutoSyncEnabled,
    int SyncIntervalMinutes,
    DateTime? LastSyncCompletedAt,
    string? LastSyncSummary,
    string? LastSyncError);

public sealed record SaveGmailCredentialsRequest(
    [Required, StringLength(300, MinimumLength = 10)] string ClientId,
    [Required, StringLength(300, MinimumLength = 10)] string ClientSecret);

public sealed record UpdateGmailSearchQueryRequest([Required, StringLength(1000, MinimumLength = 1)] string SearchQuery);

public sealed record UpdateSyncScheduleRequest(bool AutoSyncEnabled, [Range(5, 1440)] int SyncIntervalMinutes);

public sealed record GmailAuthorizationDto(string AuthorizationUrl);

public sealed record GmailTestResultDto(
    string ConnectedEmail,
    string SearchQuery,
    int EstimatedMatches,
    IReadOnlyList<GmailMessagePreview> LatestMessages);
