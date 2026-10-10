using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using SwiftJob.Application.Abstractions;
using SwiftJob.Application.Common;
using SwiftJob.Application.Sync;

namespace SwiftJob.Api.Sync;

/// <summary>
/// Runs Gmail synchronisation in the background: on the configured schedule and whenever the user presses
/// "Sync jobs". Only one synchronisation runs at a time.
/// </summary>
internal sealed class GmailSyncWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<GmailSyncWorker> logger) : BackgroundService, ISyncTrigger
{
    /// <summary>How often the schedule is re-checked when nothing is due.</summary>
    private static readonly TimeSpan ScheduleCheckInterval = TimeSpan.FromMinutes(1);

    private readonly Channel<bool> _requests = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    private volatile bool _running;

    public bool IsRunning => _running;

    public bool RequestSync() => !_running && _requests.Writer.TryWrite(true);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the application finish starting (migrations, etc.).
        await Task.Delay(TimeSpan.FromSeconds(5), timeProvider, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var manual = _requests.Reader.TryRead(out _);
            if (manual || await IsScheduledSyncDueAsync(stoppingToken))
            {
                await RunOnceAsync(stoppingToken);
                continue;
            }

            using var wait = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            wait.CancelAfter(ScheduleCheckInterval);
            try
            {
                await _requests.Reader.WaitToReadAsync(wait.Token);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                // Timed out: re-check the schedule.
            }
        }
    }

    private async Task<bool> IsScheduledSyncDueAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var connection = await db.GmailConnections.AsNoTracking().OrderBy(c => c.Id).FirstOrDefaultAsync(cancellationToken);
            if (connection is not { IsConnected: true, AutoSyncEnabled: true })
            {
                return false;
            }

            var last = connection.LastSyncCompletedAt;
            return last is null || timeProvider.GetUtcNow().UtcDateTime - last >= TimeSpan.FromMinutes(connection.SyncIntervalMinutes);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Could not read the sync schedule");
            return false;
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        _running = true;
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IJobSyncService>().SyncAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down.
        }
        catch (Exception ex) when (ex is ConflictException or ExternalServiceException)
        {
            logger.LogWarning("Sync did not complete: {Reason}", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Sync failed unexpectedly");
        }
        finally
        {
            _running = false;
        }
    }
}
