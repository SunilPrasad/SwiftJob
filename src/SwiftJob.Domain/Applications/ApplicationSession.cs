using SwiftJob.Domain.Common;
using SwiftJob.Domain.Jobs;

namespace SwiftJob.Domain.Applications;

/// <summary>One attempt at applying for a job, with or without AI assistance.</summary>
public class ApplicationSession : Entity
{
    public int JobId { get; set; }
    public Job Job { get; set; } = null!;

    public ApplicationSessionStatus Status { get; set; } = ApplicationSessionStatus.NotStarted;
    public BrowserController Controller { get; set; } = BrowserController.None;

    /// <summary>The URL the session started from (validated before use).</summary>
    public string? ApplicationUrl { get; set; }

    /// <summary>Configured agent adapter, e.g. "Anthropic".</summary>
    public string? AgentProvider { get; set; }
    public string? AgentModel { get; set; }

    public string? CurrentStep { get; set; }
    public string? LastError { get; set; }
    public string? FinalOutcome { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }

    public ICollection<ApplicationActivity> Activities { get; set; } = new List<ApplicationActivity>();
    public ICollection<ApplicationAnswer> Answers { get; set; } = new List<ApplicationAnswer>();
}
