using SwiftJob.Application.Common;
using SwiftJob.Domain.Jobs;

namespace SwiftJob.Application.Jobs;

public interface IJobService
{
    Task<PagedResult<JobListItemDto>> GetJobsAsync(JobQuery query, CancellationToken cancellationToken);

    /// <summary>Counts per status, honouring every filter in <paramref name="query"/> except status and paging.</summary>
    Task<JobStatusCountsDto> GetStatusCountsAsync(JobQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetSourcesAsync(CancellationToken cancellationToken);

    /// <exception cref="NotFoundException">The job does not exist.</exception>
    Task<JobDetailDto> GetJobAsync(int id, CancellationToken cancellationToken);

    /// <exception cref="NotFoundException">The job does not exist.</exception>
    Task<JobDetailDto> UpdateStatusAsync(int id, JobStatus status, CancellationToken cancellationToken);
}
