namespace SwiftJob.Application.Sync;

public enum SyncState
{
    Idle = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
}

/// <summary>Live synchronisation status pushed to the browser.</summary>
public sealed record SyncStatusDto(
    SyncState State,
    string Message,
    int Processed,
    int Total,
    int NewJobs,
    DateTime? StartedAt,
    DateTime? CompletedAt)
{
    public static SyncStatusDto Idle { get; } = new(SyncState.Idle, "Not synced yet in this session.", 0, 0, 0, null, null);
}

public enum SyncLogLevel
{
    Info = 0,
    Warning = 1,
    Error = 2,
}

/// <summary>One line of the user-visible activity log (what the background sync is doing).</summary>
public sealed record SyncLogEntry(long Id, DateTime Timestamp, SyncLogLevel Level, string Message);

/// <summary>
/// Publishes progress and activity (e.g. over SignalR) and remembers the latest status and the most
/// recent log lines (kept in memory; cleared when the API restarts).
/// </summary>
public interface ISyncStatusPublisher
{
    SyncStatusDto Current { get; }

    IReadOnlyList<SyncLogEntry> RecentLog { get; }

    Task PublishAsync(SyncStatusDto status, CancellationToken cancellationToken);

    /// <summary>Adds a line to the activity log. Never throws; safe to call from parallel work.</summary>
    Task LogAsync(SyncLogLevel level, string message);
}

/// <summary>Queues a synchronisation to run in the background.</summary>
public interface ISyncTrigger
{
    /// <returns>False when a synchronisation is already running or queued.</returns>
    bool RequestSync();

    bool IsRunning { get; }
}

public sealed record SyncResult(int EmailsDownloaded, int EmailsProcessed, int EmailsFailed, int NewJobs, int LinkedJobs, string Summary);
