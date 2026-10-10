using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftJob.Application.Abstractions;
using SwiftJob.Application.Common;
using SwiftJob.Domain.Settings;

namespace SwiftJob.Application.Gmail;

public sealed class GmailConnectionService(
    IAppDbContext db,
    ISecretProtector protector,
    IGmailOAuthClient oauthClient,
    IGmailMailbox mailbox,
    IOAuthStateStore stateStore,
    IOptions<GmailOptions> options,
    TimeProvider timeProvider,
    ILogger<GmailConnectionService> logger) : IGmailConnectionService
{
    /// <summary>Scopes that would allow more than reading. Their presence aborts the connection.</summary>
    private static readonly string[] ForbiddenScopes =
    [
        "https://mail.google.com/",
        "https://www.googleapis.com/auth/gmail.modify",
        "https://www.googleapis.com/auth/gmail.compose",
        "https://www.googleapis.com/auth/gmail.send",
        "https://www.googleapis.com/auth/gmail.insert",
        "https://www.googleapis.com/auth/gmail.labels",
        "https://www.googleapis.com/auth/gmail.settings.basic",
        "https://www.googleapis.com/auth/gmail.settings.sharing",
    ];

    private readonly GmailOptions _options = options.Value;

    public async Task<GmailSettingsDto> GetSettingsAsync(CancellationToken cancellationToken) =>
        ToDto(await FindAsync(cancellationToken) ?? new GmailConnection());

    public async Task<GmailSettingsDto> SaveCredentialsAsync(
        SaveGmailCredentialsRequest request, CancellationToken cancellationToken)
    {
        var connection = await GetOrCreateAsync(cancellationToken);
        if (connection.IsConnected)
        {
            throw new ConflictException("Disconnect Gmail before changing the Google OAuth client.");
        }

        connection.ClientId = request.ClientId.Trim();
        connection.ProtectedClientSecret = protector.Protect(request.ClientSecret.Trim());
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Google OAuth client credentials saved");
        return ToDto(connection);
    }

    public async Task<GmailSettingsDto> RemoveCredentialsAsync(CancellationToken cancellationToken)
    {
        var connection = await GetOrCreateAsync(cancellationToken);
        if (connection.IsConnected)
        {
            throw new ConflictException("Disconnect Gmail before removing the Google OAuth client.");
        }

        connection.ClientId = null;
        connection.ProtectedClientSecret = null;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(connection);
    }

    public async Task<GmailSettingsDto> UpdateSearchQueryAsync(string searchQuery, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            throw new ArgumentException("The Gmail search query cannot be empty.", nameof(searchQuery));
        }

        var connection = await GetOrCreateAsync(cancellationToken);
        connection.SearchQuery = searchQuery.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(connection);
    }

    public async Task<GmailAuthorizationDto> BeginAuthorizationAsync(CancellationToken cancellationToken)
    {
        var connection = await FindAsync(cancellationToken);
        if (connection is null || !connection.HasClientCredentials)
        {
            throw new ConflictException("Save your Google OAuth Client ID and Client Secret first.");
        }

        var url = oauthClient.BuildAuthorizationUrl(ClientOf(connection), _options.RedirectUri, stateStore.Create());
        return new GmailAuthorizationDto(url.ToString());
    }

    public async Task<string> CompleteAuthorizationAsync(string code, string state, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(state) || !stateStore.TryConsume(state))
        {
            throw new ConflictException("The sign-in request has expired or is invalid. Please try connecting again.");
        }

        if (string.IsNullOrEmpty(code))
        {
            throw new ArgumentException("Google did not return an authorization code.", nameof(code));
        }

        var connection = await FindAsync(cancellationToken);
        if (connection is null || !connection.HasClientCredentials)
        {
            throw new ConflictException("Google OAuth client credentials are not configured.");
        }

        var client = ClientOf(connection);
        var grant = await oauthClient.ExchangeCodeAsync(client, _options.RedirectUri, code, cancellationToken);

        if (string.IsNullOrEmpty(grant.RefreshToken))
        {
            throw new ConflictException(
                "Google did not return a long-lived token. Remove SwiftJob at https://myaccount.google.com/permissions and connect again.");
        }

        if (!IsReadOnlyGrant(grant.Scope))
        {
            // Never keep a token that can do more than read, or that cannot read at all.
            await TryRevokeAsync(client, grant.RefreshToken, cancellationToken);
            throw new ConflictException(
                "Google granted different permissions than read-only Gmail access, so the connection was rejected. " +
                "Please connect again and allow \"View your email messages and settings\".");
        }

        var email = await mailbox.GetEmailAddressAsync(new GmailAccess(client, grant.RefreshToken), cancellationToken);

        connection.Connect(
            protector.Protect(grant.RefreshToken), email, grant.Scope!, timeProvider.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Gmail connected (read-only)");
        return email;
    }

    public async Task<GmailSettingsDto> DisconnectAsync(CancellationToken cancellationToken)
    {
        var connection = await GetOrCreateAsync(cancellationToken);
        if (connection is { IsConnected: true, HasClientCredentials: true })
        {
            try
            {
                await TryRevokeAsync(ClientOf(connection), UnprotectStored(connection.ProtectedRefreshToken!), cancellationToken);
            }
            catch (ConflictException ex)
            {
                // Unreadable token: nothing can be revoked from here; the local copy is still removed.
                logger.LogWarning(ex, "Skipping token revocation");
            }
        }

        connection.Disconnect();
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Gmail disconnected");
        return ToDto(connection);
    }

    public async Task<GmailSettingsDto> UpdateSyncScheduleAsync(
        bool autoSyncEnabled, int intervalMinutes, CancellationToken cancellationToken)
    {
        if (intervalMinutes is < 5 or > 1440)
        {
            throw new ArgumentException("The sync interval must be between 5 and 1440 minutes.", nameof(intervalMinutes));
        }

        var connection = await GetOrCreateAsync(cancellationToken);
        connection.AutoSyncEnabled = autoSyncEnabled;
        connection.SyncIntervalMinutes = intervalMinutes;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(connection);
    }

    /// <summary>Decrypted access for the connected account (used by synchronisation).</summary>
    /// <exception cref="ConflictException">Gmail is not connected.</exception>
    public async Task<(GmailConnection Connection, GmailAccess Access)> GetAccessAsync(CancellationToken cancellationToken)
    {
        var connection = await FindAsync(cancellationToken);
        if (connection is not { IsConnected: true, HasClientCredentials: true })
        {
            throw new ConflictException("Gmail is not connected. Connect it in Settings.");
        }

        return (connection, new GmailAccess(ClientOf(connection), UnprotectStored(connection.ProtectedRefreshToken!)));
    }

    public async Task<GmailTestResultDto> TestConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = await FindAsync(cancellationToken);
        if (connection is not { IsConnected: true, HasClientCredentials: true })
        {
            throw new ConflictException("Gmail is not connected.");
        }

        var access = new GmailAccess(ClientOf(connection), UnprotectStored(connection.ProtectedRefreshToken!));
        var email = await mailbox.GetEmailAddressAsync(access, cancellationToken);
        var result = await mailbox.SearchAsync(access, connection.SearchQuery, _options.TestPreviewCount, cancellationToken);
        return new GmailTestResultDto(email, connection.SearchQuery, result.EstimatedMatches, result.LatestMessages);
    }

    internal static bool IsReadOnlyGrant(string? scope)
    {
        var scopes = (scope ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return scopes.Contains(GmailConnection.ReadOnlyScope)
               && !scopes.Any(s => ForbiddenScopes.Contains(s, StringComparer.OrdinalIgnoreCase));
    }

    private async Task TryRevokeAsync(GoogleOAuthClient client, string refreshToken, CancellationToken cancellationToken)
    {
        try
        {
            await oauthClient.RevokeAsync(client, refreshToken, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The local token is discarded regardless; access can also be removed at myaccount.google.com.
            logger.LogWarning(ex, "Could not revoke the Gmail token at Google");
        }
    }

    private GoogleOAuthClient ClientOf(GmailConnection connection) =>
        new(connection.ClientId!, UnprotectStored(connection.ProtectedClientSecret!));

    /// <summary>Stored secrets become unreadable if the encryption keys (App_Data/keys) are lost or replaced.</summary>
    private string UnprotectStored(string protectedValue)
    {
        try
        {
            return protector.Unprotect(protectedValue);
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            throw new ConflictException(
                "The saved Gmail credentials can no longer be decrypted (the encryption keys in App_Data/keys changed). " +
                "Disconnect Gmail, save the Client ID and Secret again, and reconnect.", ex);
        }
    }

    private Task<GmailConnection?> FindAsync(CancellationToken cancellationToken) =>
        db.GmailConnections.OrderBy(c => c.Id).FirstOrDefaultAsync(cancellationToken);

    private async Task<GmailConnection> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var connection = await FindAsync(cancellationToken);
        if (connection is null)
        {
            connection = new GmailConnection();
            db.GmailConnections.Add(connection);
        }

        return connection;
    }

    private GmailSettingsDto ToDto(GmailConnection connection) => new(
        connection.HasClientCredentials,
        connection.ClientId,
        _options.RedirectUri,
        GmailConnection.ReadOnlyScope,
        connection.IsConnected,
        connection.ConnectedEmail,
        connection.ConnectedAt,
        connection.SearchQuery,
        GmailConnection.DefaultSearchQuery,
        connection.AutoSyncEnabled,
        connection.SyncIntervalMinutes,
        connection.LastSyncCompletedAt,
        connection.LastSyncSummary,
        connection.LastSyncError);
}
