using Microsoft.AspNetCore.Mvc;
using SwiftJob.Application.Sync;

namespace SwiftJob.Api.Controllers;

[ApiController]
[Route("api/sync")]
public sealed class SyncController(ISyncTrigger trigger, ISyncStatusPublisher status) : ControllerBase
{
    [HttpGet("status")]
    public SyncStatusDto GetStatus() => status.Current;

    /// <summary>Starts a synchronisation in the background. Progress is pushed over SignalR (/hubs/sync).</summary>
    [HttpPost]
    [ProducesResponseType<SyncStatusDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public IActionResult Start() =>
        trigger.RequestSync()
            ? Accepted(status.Current)
            : Problem(title: "A sync is already running", statusCode: StatusCodes.Status409Conflict);
}
