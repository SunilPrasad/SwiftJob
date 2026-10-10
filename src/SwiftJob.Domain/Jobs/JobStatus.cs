namespace SwiftJob.Domain.Jobs;

/// <summary>The user's triage decision for a job.</summary>
public enum JobStatus
{
    New = 0,
    Applied = 1,
    NotRelevant = 2,
}
