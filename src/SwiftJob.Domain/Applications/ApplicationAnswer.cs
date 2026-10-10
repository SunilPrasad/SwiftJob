using SwiftJob.Domain.Common;

namespace SwiftJob.Domain.Applications;

public enum AnswerSource
{
    /// <summary>Taken directly from a candidate profile field.</summary>
    Profile = 0,

    /// <summary>Taken from a user-verified saved answer.</summary>
    SavedAnswer = 1,

    /// <summary>Proposed by the AI from verified data; must be confirmed before submission.</summary>
    AiSuggested = 2,

    /// <summary>Entered by the user during the session.</summary>
    User = 3,
}

/// <summary>A question asked by an application form and the answer given in this session.</summary>
public class ApplicationAnswer : Entity
{
    public int ApplicationSessionId { get; set; }
    public ApplicationSession ApplicationSession { get; set; } = null!;

    public required string Question { get; set; }
    public string? Answer { get; set; }
    public AnswerSource Source { get; set; }
    public bool ConfirmedByUser { get; set; }
    public DateTime CreatedAt { get; set; }
}
