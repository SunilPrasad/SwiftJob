using SwiftJob.Domain.Common;

namespace SwiftJob.Domain.Candidates;

public class Education : Entity
{
    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public required string Institution { get; set; }
    public string? Qualification { get; set; }
    public string? FieldOfStudy { get; set; }
    public string? Grade { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int SortOrder { get; set; }
}
