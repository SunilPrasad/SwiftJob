using SwiftJob.Domain.Common;
using SwiftJob.Domain.Jobs;

namespace SwiftJob.Domain.Candidates;

/// <summary>
/// The user's verified information. The application agent may only use facts stored here
/// (or confirmed by the user during a session); it must never invent candidate details.
/// </summary>
public class CandidateProfile : AuditableEntity
{
    // Personal information
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    // Contact details
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? PortfolioUrl { get; set; }

    // Professional
    public string? ProfessionalSummary { get; set; }
    public List<string> TechnicalSkills { get; set; } = [];

    // Work authorisation
    public string? WorkAuthorisation { get; set; }
    public bool? RequiresVisaSponsorship { get; set; }
    public string? NoticePeriod { get; set; }

    // Application preferences
    public decimal? PreferredSalaryMin { get; set; }
    public decimal? PreferredSalaryMax { get; set; }
    public string? PreferredSalaryCurrency { get; set; }
    public List<string> PreferredLocations { get; set; } = [];
    public List<WorkMode> PreferredWorkModes { get; set; } = [];

    // CV file reference (the file itself is stored outside the database)
    public string? CvFileName { get; set; }
    public string? CvStoragePath { get; set; }
    public string? CvContentType { get; set; }
    public DateTime? CvUploadedAt { get; set; }

    public ICollection<WorkExperience> WorkExperiences { get; set; } = new List<WorkExperience>();
    public ICollection<Education> Educations { get; set; } = new List<Education>();
    public ICollection<SavedAnswer> SavedAnswers { get; set; } = new List<SavedAnswer>();
}
