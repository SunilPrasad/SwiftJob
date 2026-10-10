namespace SwiftJob.Domain.Emails;

public enum EmailSyncStatus
{
    /// <summary>Downloaded but not yet processed for jobs.</summary>
    Pending = 0,

    /// <summary>Processed; zero or more jobs were extracted.</summary>
    Processed = 1,

    /// <summary>Processing failed; see <see cref="JobEmail.ProcessingError"/>. Eligible for retry.</summary>
    Failed = 2,

    /// <summary>Not a job notification; intentionally skipped.</summary>
    Ignored = 3,
}
