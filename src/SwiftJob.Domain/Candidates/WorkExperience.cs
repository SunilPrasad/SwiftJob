using SwiftJob.Domain.Common;

namespace SwiftJob.Domain.Candidates;

public class WorkExperience : Entity
{
    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public required string JobTitle { get; set; }
    public required string Employer { get; set; }
    public string? Location { get; set; }
    public DateOnly? StartDate { get; set; }

    /// <summary>Null when this is the current role.</summary>
    public DateOnly? EndDate { get; set; }
    public string? Summary { get; set; }
    public int SortOrder { get; set; }
}
