using SwiftJob.Domain.Common;

namespace SwiftJob.Domain.Applications;

public enum ActivityActionType
{
    Info = 0,
    Navigate = 1,
    InspectPage = 2,
    FillField = 3,
    Click = 4,
    UploadFile = 5,
    AnswerQuestion = 6,
    RequestUserInput = 7,
    Pause = 8,
    Resume = 9,
    TakeControl = 10,
    ReleaseControl = 11,
    Submit = 12,
    Stop = 13,
    Error = 14,
}

public enum ActivityOutcome
{
    Pending = 0,
    Succeeded = 1,
    Failed = 2,
    Skipped = 3,
}

public enum ActivityActor
{
    System = 0,
    Agent = 1,
    User = 2,
}

/// <summary>An append-only audit entry describing one step of an application session.</summary>
public class ApplicationActivity : Entity
{
    public int ApplicationSessionId { get; set; }
    public ApplicationSession ApplicationSession { get; set; } = null!;

    public DateTime Timestamp { get; set; }
    public ActivityActionType ActionType { get; set; }
    public ActivityActor Actor { get; set; }
    public required string Description { get; set; }
    public ActivityOutcome Outcome { get; set; }
}
