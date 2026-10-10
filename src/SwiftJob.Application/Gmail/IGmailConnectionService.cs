namespace SwiftJob.Application.Gmail;

/// <summary>Connects SwiftJob to one Gmail account with read-only access.</summary>
public interface IGmailConnectionService
{
    Task<GmailSettingsDto> GetSettingsAsync(CancellationToken cancellationToken);

    /// <exception cref="Common.ConflictException">Gmail is connected; disconnect before changing credentials.</exception>
    Task<GmailSettingsDto> SaveCredentialsAsync(SaveGmailCredentialsRequest request, CancellationToken cancellationToken);

    /// <exception cref="Common.ConflictException">Gmail is connected; disconnect first.</exception>
    Task<GmailSettingsDto> RemoveCredentialsAsync(CancellationToken cancellationToken);

    Task<GmailSettingsDto> UpdateSearchQueryAsync(string searchQuery, CancellationToken cancellationToken);

    Task<GmailSettingsDto> UpdateSyncScheduleAsync(bool autoSyncEnabled, int intervalMinutes, CancellationToken cancellationToken);

    /// <exception cref="Common.ConflictException">Gmail is not connected.</exception>
    Task<(Domain.Settings.GmailConnection Connection, GmailAccess Access)> GetAccessAsync(CancellationToken cancellationToken);

    /// <summary>Starts Google sign-in: returns the URL the browser must open.</summary>
    /// <exception cref="Common.ConflictException">Client credentials are not configured.</exception>
    Task<GmailAuthorizationDto> BeginAuthorizationAsync(CancellationToken cancellationToken);

    /// <summary>Completes Google sign-in from the OAuth redirect.</summary>
    /// <returns>The connected Gmail address.</returns>
    Task<string> CompleteAuthorizationAsync(string code, string state, CancellationToken cancellationToken);

    /// <summary>Revokes access at Google (best effort) and forgets the stored token.</summary>
    Task<GmailSettingsDto> DisconnectAsync(CancellationToken cancellationToken);

    /// <summary>Checks the connection and previews the latest emails matching the search query.</summary>
    /// <exception cref="Common.ConflictException">Gmail is not connected.</exception>
    Task<GmailTestResultDto> TestConnectionAsync(CancellationToken cancellationToken);
}
