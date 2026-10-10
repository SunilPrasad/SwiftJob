using Microsoft.AspNetCore.Mvc;
using SwiftJob.Application.Common;
using SwiftJob.Application.Jobs;

namespace SwiftJob.Api.Controllers;

[ApiController]
[Route("api/jobs")]
public sealed class JobsController(IJobService jobService) : ControllerBase
{
    /// <summary>Lists jobs with filtering, sorting and paging. Defaults to newest first.</summary>
    [HttpGet]
    public Task<PagedResult<JobListItemDto>> GetJobs([FromQuery] JobQuery query, CancellationToken cancellationToken) =>
        jobService.GetJobsAsync(query, cancellationToken);

    /// <summary>Number of jobs per status, using the same filters as the list (status and paging ignored).</summary>
    [HttpGet("counts")]
    public Task<JobStatusCountsDto> GetCounts([FromQuery] JobQuery query, CancellationToken cancellationToken) =>
        jobService.GetStatusCountsAsync(query, cancellationToken);

    [HttpGet("sources")]
    public Task<IReadOnlyList<string>> GetSources(CancellationToken cancellationToken) =>
        jobService.GetSourcesAsync(cancellationToken);

    [HttpGet("{id:int}")]
    [ProducesResponseType<JobDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<JobDetailDto> GetJob(int id, CancellationToken cancellationToken) =>
        jobService.GetJobAsync(id, cancellationToken);

    /// <summary>Sets the job's triage status (New, Applied or NotRelevant). Can be changed again at any time.</summary>
    [HttpPut("{id:int}/status")]
    [ProducesResponseType<JobDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<JobDetailDto> UpdateStatus(int id, UpdateJobStatusRequest request, CancellationToken cancellationToken) =>
        jobService.UpdateStatusAsync(id, request.Status!.Value, cancellationToken);
}
