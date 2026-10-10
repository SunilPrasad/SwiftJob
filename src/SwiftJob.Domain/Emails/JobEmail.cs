using SwiftJob.Domain.Common;
using SwiftJob.Domain.Jobs;

namespace SwiftJob.Domain.Emails;

/// <summary>An original job-notification email, retained verbatim for reference. Treated as untrusted content.</summary>
public class JobEmail : Entity
{
    /// <summary>Gmail's immutable message id. Unique; makes synchronisation idempotent.</summary>
    public required string GmailMessageId { get; set; }
    public string? GmailThreadId { get; set; }

    public string? Subject { get; set; }
    public string? Sender { get; set; }
    public DateTime ReceivedAt { get; set; }

    public string? BodyText { get; set; }
    public string? BodyHtml { get; set; }

    public EmailSyncStatus SyncStatus { get; set; } = EmailSyncStatus.Pending;
    public string? ProcessingError { get; set; }

    /// <summary>Number of extraction attempts; failed emails are retried a limited number of times.</summary>
    public int ProcessingAttempts { get; set; }
    public const int MaxProcessingAttempts = 3;
    public DateTime SyncedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public ICollection<JobEmailLink> JobLinks { get; set; } = new List<JobEmailLink>();
}
