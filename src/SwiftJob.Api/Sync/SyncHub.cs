using Microsoft.AspNetCore.SignalR;
using SwiftJob.Application.Sync;

namespace SwiftJob.Api.Sync;

/// <summary>Pushes synchronisation progress to the browser. Clients only listen; they cannot invoke actions.</summary>
public sealed class SyncHub(ISyncStatusPublisher status) : Hub
{
    public const string Path = "/hubs/sync";
    public const string StatusMethod = "syncStatus";

    /// <summary>Sends the latest status as soon as a client connects.</summary>
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync(StatusMethod, status.Current);
        await base.OnConnectedAsync();
    }
}

internal sealed class SignalRSyncStatusPublisher(IHubContext<SyncHub> hub) : ISyncStatusPublisher
{
    private volatile SyncStatusDto _current = SyncStatusDto.Idle;

    public SyncStatusDto Current => _current;

    public Task PublishAsync(SyncStatusDto status, CancellationToken cancellationToken)
    {
        _current = status;
        return hub.Clients.All.SendAsync(SyncHub.StatusMethod, status, cancellationToken);
    }
}
