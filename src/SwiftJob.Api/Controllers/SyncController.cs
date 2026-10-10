using Microsoft.AspNetCore.Mvc;
using SwiftJob.Application.Sync;

namespace SwiftJob.Api.Controllers;

[ApiController]
[Route("api/sync")]
public sealed class SyncController(ISyncTrigger trigger, ISyncStatusPublisher status) : ControllerBase
{
    [HttpGet("status")]
    public SyncStatusDto GetStatus() => status.Current;

    /// <summary>Recent activity-log lines (newest last). New lines are pushed over SignalR as "syncLog".</summary>
    [HttpGet("log")]
    public IReadOnlyList<SyncLogEntry> GetLog() => status.RecentLog;

    /// <summary>Starts a synchronisation in the background. Progress is pushed over SignalR (/hubs/sync).</summary>
    [HttpPost]
    [ProducesResponseType<SyncStatusDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Start()
    {
        if (!trigger.RequestSync())
        {
            return Problem(title: "A sync is already running", statusCode: StatusCodes.Status409Conflict);
        }

        await status.LogAsync(SyncLogLevel.Info, "Sync requested from the dashboard.");
        return Accepted(status.Current);
    }
}
