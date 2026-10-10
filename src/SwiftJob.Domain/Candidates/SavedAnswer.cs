using SwiftJob.Domain.Common;

namespace SwiftJob.Domain.Candidates;

/// <summary>A user-verified answer to a common screening question, reusable across applications.</summary>
public class SavedAnswer : AuditableEntity
{
    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public required string Question { get; set; }
    public required string Answer { get; set; }

    /// <summary>Optional grouping key, e.g. "right-to-work", "notice-period".</summary>
    public string? Category { get; set; }
}
