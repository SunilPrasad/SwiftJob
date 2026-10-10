namespace SwiftJob.Application.Gmail;

/// <summary>The user's own Google OAuth client (from Google Cloud Console).</summary>
public sealed record GoogleOAuthClient(string ClientId, string ClientSecret);

/// <summary>Everything needed to call the Gmail API on the user's behalf.</summary>
public sealed record GmailAccess(GoogleOAuthClient Client, string RefreshToken);

/// <summary>Result of exchanging an authorization code.</summary>
public sealed record GmailTokenGrant(string? RefreshToken, string? Scope);

public sealed record GmailMessagePreview(string Id, string? Subject, string? From, DateTime? ReceivedAt);

public sealed record GmailSearchResult(int EstimatedMatches, IReadOnlyList<GmailMessagePreview> LatestMessages);

/// <summary>A full email as downloaded from Gmail (untrusted content).</summary>
public sealed record GmailMessage(
    string Id,
    string? ThreadId,
    string? Subject,
    string? From,
    DateTime ReceivedAt,
    string? BodyText,
    string? BodyHtml);

/// <summary>Google OAuth 2.0 operations. Implementations must request only the read-only Gmail scope.</summary>
public interface IGmailOAuthClient
{
    Uri BuildAuthorizationUrl(GoogleOAuthClient client, string redirectUri, string state);

    /// <exception cref="Common.ExternalServiceException">Google rejected the exchange.</exception>
    Task<GmailTokenGrant> ExchangeCodeAsync(
        GoogleOAuthClient client, string redirectUri, string code, CancellationToken cancellationToken);

    Task RevokeAsync(GoogleOAuthClient client, string refreshToken, CancellationToken cancellationToken);
}

/// <summary>Read-only access to a Gmail mailbox.</summary>
public interface IGmailMailbox
{
    /// <exception cref="GmailAccessRevokedException">The refresh token is no longer valid.</exception>
    /// <exception cref="Common.ExternalServiceException">The Gmail API call failed.</exception>
    Task<string> GetEmailAddressAsync(GmailAccess access, CancellationToken cancellationToken);

    /// <exception cref="GmailAccessRevokedException">The refresh token is no longer valid.</exception>
    /// <exception cref="Common.ExternalServiceException">The Gmail API call failed.</exception>
    Task<GmailSearchResult> SearchAsync(
        GmailAccess access, string query, int maxPreviews, CancellationToken cancellationToken);

    /// <summary>Ids of messages matching <paramref name="query"/>, newest first, up to <paramref name="maxResults"/>.</summary>
    Task<IReadOnlyList<string>> ListMessageIdsAsync(
        GmailAccess access, string query, int maxResults, CancellationToken cancellationToken);

    Task<GmailMessage> GetMessageAsync(GmailAccess access, string messageId, CancellationToken cancellationToken);
}

/// <summary>One-time, short-lived values that protect the OAuth redirect against CSRF.</summary>
public interface IOAuthStateStore
{
    string Create();

    /// <summary>True exactly once for a state created recently by <see cref="Create"/>.</summary>
    bool TryConsume(string state);
}

/// <summary>Google no longer accepts the stored refresh token (revoked, expired or password changed).</summary>
public sealed class GmailAccessRevokedException(Exception? innerException = null)
    : Common.ConflictException("Gmail access has expired or was revoked. Please reconnect Gmail.", innerException);
