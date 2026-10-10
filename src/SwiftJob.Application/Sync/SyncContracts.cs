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

/// <summary>Publishes progress (e.g. over SignalR) and remembers the latest status.</summary>
public interface ISyncStatusPublisher
{
    SyncStatusDto Current { get; }

    Task PublishAsync(SyncStatusDto status, CancellationToken cancellationToken);
}

/// <summary>Queues a synchronisation to run in the background.</summary>
public interface ISyncTrigger
{
    /// <returns>False when a synchronisation is already running or queued.</returns>
    bool RequestSync();

    bool IsRunning { get; }
}

public sealed record SyncResult(int EmailsDownloaded, int EmailsProcessed, int EmailsFailed, int NewJobs, int LinkedJobs, string Summary);
