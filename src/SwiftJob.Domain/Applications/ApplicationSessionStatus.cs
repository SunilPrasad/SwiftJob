namespace SwiftJob.Domain.Applications;

public enum ApplicationSessionStatus
{
    NotStarted = 0,
    InProgress = 1,
    WaitingForUser = 2,
    Paused = 3,
    ReadyForReview = 4,
    Submitted = 5,
    Failed = 6,
    Cancelled = 7,
}
