using SwiftJob.Domain.Common;
using SwiftJob.Domain.Applications;

namespace SwiftJob.Domain.Jobs;

/// <summary>
/// A single job opportunity extracted from one or more notification emails.
/// Fields that could not be extracted are left null; values are never invented.
/// </summary>
public class Job : AuditableEntity
{
    public required string Title { get; set; }
    public string? Company { get; set; }

    /// <summary>Salary exactly as advertised, e.g. "£70,000 - £85,000 per annum".</summary>
    public string? SalaryText { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }

    /// <summary>ISO 4217 currency code, e.g. "GBP".</summary>
    public string? SalaryCurrency { get; set; }

    public string? Location { get; set; }
    public WorkMode WorkMode { get; set; } = WorkMode.Unknown;

    /// <summary>Description as extracted from the source email. Never overwritten by enrichment.</summary>
    public string? Description { get; set; }
    public string? Requirements { get; set; }

    /// <summary>Optional richer description fetched later from the job website. Stored separately from the original.</summary>
    public string? EnrichedDescription { get; set; }
    public DateTime? EnrichedAt { get; set; }

    /// <summary>Link to apply, as found in the email (may be a tracking URL).</summary>
    public string? ApplicationUrl { get; set; }

    /// <summary>Normalised job URL (tracking parameters and redirects removed) used for de-duplication.</summary>
    public string? CanonicalUrl { get; set; }

    /// <summary>Job board or agency the job came from, e.g. "LinkedIn".</summary>
    public required string Source { get; set; }

    /// <summary>The job board's own identifier for the job, when one can be reliably extracted.</summary>
    public string? SourceJobId { get; set; }

    /// <summary>When the (first) email containing this job was received.</summary>
    public DateTime ReceivedAt { get; set; }

    public JobStatus Status { get; private set; } = JobStatus.New;
    public DateTime? StatusChangedAt { get; private set; }

    public ICollection<JobEmailLink> EmailLinks { get; set; } = new List<JobEmailLink>();
    public ICollection<ApplicationSession> ApplicationSessions { get; set; } = new List<ApplicationSession>();

    /// <summary>Changes the triage status. Any status may be changed again later.</summary>
    /// <returns>True if the status actually changed.</returns>
    public bool ChangeStatus(JobStatus newStatus, DateTime utcNow)
    {
        if (!Enum.IsDefined(newStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(newStatus), newStatus, "Unknown job status.");
        }

        if (Status == newStatus)
        {
            return false;
        }

        Status = newStatus;
        StatusChangedAt = utcNow;
        return true;
    }
}
