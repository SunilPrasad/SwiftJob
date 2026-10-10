using SwiftJob.Domain.Common;

namespace SwiftJob.Domain.Settings;

/// <summary>
/// The single Gmail account SwiftJob may read from, plus the user's own Google OAuth client.
/// Secrets are stored only in encrypted ("protected") form; the persistence layer never sees plain text.
/// </summary>
public class GmailConnection : AuditableEntity
{
    /// <summary>The only OAuth scope SwiftJob requests: read-only access to Gmail.</summary>
    public const string ReadOnlyScope = "https://www.googleapis.com/auth/gmail.readonly";

    /// <summary>
    /// Default Gmail search: common job-alert senders plus anything the user labels "Jobs".
    /// Editable in Settings.
    /// </summary>
    public const string DefaultSearchQuery =
        "from:(jobalerts-noreply@linkedin.com OR jobs-noreply@linkedin.com OR indeed.com OR reed.co.uk " +
        "OR totaljobs.com OR jobserve.com) OR label:jobs";

    public string? ClientId { get; set; }
    public string? ProtectedClientSecret { get; set; }

    public string? ProtectedRefreshToken { get; set; }
    public string? ConnectedEmail { get; set; }
    public string? GrantedScopes { get; set; }
    public DateTime? ConnectedAt { get; set; }

    public string SearchQuery { get; set; } = DefaultSearchQuery;

    /// <summary>How far back the first synchronisation reads.</summary>
    public const int InitialSyncDays = 30;
    public const int DefaultSyncIntervalMinutes = 30;

    public bool AutoSyncEnabled { get; set; } = true;
    public int SyncIntervalMinutes { get; set; } = DefaultSyncIntervalMinutes;

    /// <summary>Start time of the last synchronisation that completed without a fatal error.</summary>
    public DateTime? LastSuccessfulSyncStartedAt { get; set; }
    public DateTime? LastSyncCompletedAt { get; set; }
    public string? LastSyncSummary { get; set; }
    public string? LastSyncError { get; set; }

    public bool HasClientCredentials => !string.IsNullOrWhiteSpace(ClientId) && ProtectedClientSecret is not null;

    public bool IsConnected => ProtectedRefreshToken is not null;

    public void Connect(string protectedRefreshToken, string email, string grantedScopes, DateTime utcNow)
    {
        ProtectedRefreshToken = protectedRefreshToken;
        ConnectedEmail = email;
        GrantedScopes = grantedScopes;
        ConnectedAt = utcNow;
    }

    public void Disconnect()
    {
        // Sync history is kept; already imported emails and jobs remain in the database.
        ProtectedRefreshToken = null;
        ConnectedEmail = null;
        GrantedScopes = null;
        ConnectedAt = null;
    }
}
