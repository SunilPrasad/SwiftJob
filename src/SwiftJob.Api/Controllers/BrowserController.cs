using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SwiftJob.Application.Browser;
using SwiftJob.Application.Common;
using SwiftJob.Application.Jobs;
using Microsoft.Extensions.Options;
using SwiftJob.Infrastructure.Browser;

namespace SwiftJob.Api.Controllers;

/// <summary>The SwiftJob-controlled browser window and the (prototype) form-filling AI.</summary>
[ApiController]
[Route("api/browser")]
public sealed class BrowserController(IBrowserSessionService browser, IFormFillAgent agent) : ControllerBase
{
    [HttpGet("status")]
    public BrowserStatusDto Status() => browser.Status;

    /// <summary>Opens the job's application link in the SwiftJob browser window.</summary>
    [HttpPost("open-job/{jobId:int}")]
    public async Task<BrowserStatusDto> OpenJob(
        int jobId, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] WindowPlacement? placement,
        [FromServices] IJobService jobs, CancellationToken cancellationToken)
    {
        var job = await jobs.GetJobAsync(jobId, cancellationToken);
        if (string.IsNullOrWhiteSpace(job.ApplicationUrl))
        {
            throw new ConflictException("This job has no application link.");
        }

        await browser.OpenAsync(job.ApplicationUrl, placement, cancellationToken);
        return browser.Status;
    }

    /// <summary>Moves the browser window (e.g. to the right half of the screen, beside SwiftJob).</summary>
    [HttpPost("arrange")]
    public async Task<BrowserStatusDto> Arrange(WindowPlacement placement, CancellationToken cancellationToken)
    {
        await browser.ArrangeAsync(placement, cancellationToken);
        return browser.Status;
    }

    [HttpPost("close")]
    public async Task<BrowserStatusDto> Close(CancellationToken cancellationToken)
    {
        await agent.StopAsync();
        await browser.CloseAsync(cancellationToken);
        return browser.Status;
    }

    /// <summary>Prototype: opens the local sample form without the AI, to try the browser by hand.</summary>
    [HttpPost("demo/open")]
    public async Task<BrowserStatusDto> DemoOpen(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] WindowPlacement? placement,
        [FromServices] IOptions<BrowserOptions> options, CancellationToken cancellationToken)
    {
        await browser.OpenAsync(options.Value.DemoFormUrl, placement, cancellationToken);
        return browser.Status;
    }

    /// <summary>Prototype: opens the local sample form and lets the AI fill it with a fictional applicant.</summary>
    [HttpPost("demo/fill")]
    public async Task<BrowserStatusDto> DemoFill(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] WindowPlacement? placement, CancellationToken cancellationToken)
    {
        await agent.StartDemoAsync(placement, cancellationToken);
        return browser.Status;
    }

    [HttpPost("agent/take-control")]
    public async Task<BrowserStatusDto> TakeControl()
    {
        await agent.TakeControlAsync();
        return browser.Status;
    }

    [HttpPost("agent/resume")]
    public async Task<BrowserStatusDto> Resume(CancellationToken cancellationToken)
    {
        await agent.ResumeAsync(cancellationToken);
        return browser.Status;
    }

    [HttpPost("agent/stop")]
    public async Task<BrowserStatusDto> Stop()
    {
        await agent.StopAsync();
        return browser.Status;
    }
}
