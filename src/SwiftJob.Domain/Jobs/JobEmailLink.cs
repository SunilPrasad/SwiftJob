using SwiftJob.Domain.Emails;

namespace SwiftJob.Domain.Jobs;

/// <summary>Many-to-many link: one email can announce several jobs and one job can appear in several emails.</summary>
public class JobEmailLink
{
    public int JobId { get; set; }
    public Job Job { get; set; } = null!;

    public int JobEmailId { get; set; }
    public JobEmail JobEmail { get; set; } = null!;

    public DateTime LinkedAt { get; set; }
}
