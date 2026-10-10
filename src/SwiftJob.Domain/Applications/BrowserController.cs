namespace SwiftJob.Domain.Applications;

/// <summary>Who currently holds the (single, authoritative) control of a session's browser.</summary>
public enum BrowserController
{
    None = 0,
    Agent = 1,
    User = 2,
}
