using SwiftJob.Domain.Emails;

namespace SwiftJob.Application.Emails;

/// <summary>
/// The original email. Only a plain-text rendering is exposed: the HTML is untrusted
/// (tracking pixels, scripts) and is never sent to the browser.
/// </summary>
public sealed record JobEmailDetailDto(
    int Id,
    string GmailMessageId,
    string? Subject,
    string? Sender,
    DateTime ReceivedAt,
    EmailSyncStatus SyncStatus,
    string? BodyText,
    string GmailUrl,
    IReadOnlyList<int> JobIds);
