using Microsoft.AspNetCore.SignalR;
using SwiftJob.Application.Sync;

namespace SwiftJob.Api.Sync;

/// <summary>Pushes synchronisation progress to the browser. Clients only listen; they cannot invoke actions.</summary>
public sealed class SyncHub(ISyncStatusPublisher status) : Hub
{
    public const string Path = "/hubs/sync";
    public const string StatusMethod = "syncStatus";
    public const string LogMethod = "syncLog";

    /// <summary>Sends the latest status as soon as a client connects.</summary>
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync(StatusMethod, status.Current);
        await base.OnConnectedAsync();
    }
}

internal sealed class SignalRSyncStatusPublisher(
    IHubContext<SyncHub> hub, TimeProvider timeProvider, ILogger<SignalRSyncStatusPublisher> logger) : ISyncStatusPublisher
{
    public const int MaxLogEntries = 500;

    private readonly Queue<SyncLogEntry> _log = new();
    private readonly Lock _logLock = new();
    private long _nextId;
    private volatile SyncStatusDto _current = SyncStatusDto.Idle;

    public SyncStatusDto Current => _current;

    public IReadOnlyList<SyncLogEntry> RecentLog
    {
        get
        {
            lock (_logLock)
            {
                return _log.ToList();
            }
        }
    }

    public Task PublishAsync(SyncStatusDto status, CancellationToken cancellationToken)
    {
        _current = status;
        return hub.Clients.All.SendAsync(SyncHub.StatusMethod, status, cancellationToken);
    }

    public async Task LogAsync(SyncLogLevel level, string message)
    {
        SyncLogEntry entry;
        lock (_logLock)
        {
            entry = new SyncLogEntry(++_nextId, timeProvider.GetUtcNow().UtcDateTime, level, message);
            _log.Enqueue(entry);
            while (_log.Count > MaxLogEntries)
            {
                _log.Dequeue();
            }
        }

        try
        {
            await hub.Clients.All.SendAsync(SyncHub.LogMethod, entry);
        }
        catch (Exception ex)
        {
            // The log is best-effort: never let a disconnected client break a sync.
            logger.LogDebug(ex, "Could not broadcast a sync log entry");
        }
    }
}
