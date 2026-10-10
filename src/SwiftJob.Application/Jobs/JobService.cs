using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SwiftJob.Application.Abstractions;
using SwiftJob.Application.Common;
using SwiftJob.Domain.Jobs;

namespace SwiftJob.Application.Jobs;

public sealed class JobService(IAppDbContext db, TimeProvider timeProvider, ILogger<JobService> logger) : IJobService
{
    public async Task<PagedResult<JobListItemDto>> GetJobsAsync(JobQuery query, CancellationToken cancellationToken)
    {
        var jobs = ApplyFilters(db.Jobs.AsNoTracking(), query);
        if (query.Status is { } status)
        {
            jobs = jobs.Where(j => j.Status == status);
        }

        var totalCount = await jobs.CountAsync(cancellationToken);

        var items = await ApplySort(jobs, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(j => new JobListItemDto(
                j.Id,
                j.ReceivedAt,
                j.Title,
                j.Company,
                j.SalaryText,
                j.Location,
                j.WorkMode,
                j.Source,
                j.Status,
                j.ApplicationUrl != null))
            .ToListAsync(cancellationToken);

        return new PagedResult<JobListItemDto>(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<JobStatusCountsDto> GetStatusCountsAsync(JobQuery query, CancellationToken cancellationToken)
    {
        var counts = await ApplyFilters(db.Jobs.AsNoTracking(), query)
            .GroupBy(j => j.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        return new JobStatusCountsDto(
            counts.GetValueOrDefault(JobStatus.New),
            counts.GetValueOrDefault(JobStatus.Applied),
            counts.GetValueOrDefault(JobStatus.NotRelevant));
    }

    public async Task<IReadOnlyList<string>> GetSourcesAsync(CancellationToken cancellationToken) =>
        await db.Jobs.AsNoTracking()
            .Select(j => j.Source)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(cancellationToken);

    public async Task<JobDetailDto> GetJobAsync(int id, CancellationToken cancellationToken)
    {
        var job = await db.Jobs.AsNoTracking()
            .Include(j => j.EmailLinks).ThenInclude(l => l.JobEmail)
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken)
            ?? throw new NotFoundException("Job", id);

        return ToDetail(job);
    }

    public async Task<JobDetailDto> UpdateStatusAsync(int id, JobStatus status, CancellationToken cancellationToken)
    {
        var job = await db.Jobs
            .Include(j => j.EmailLinks).ThenInclude(l => l.JobEmail)
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken)
            ?? throw new NotFoundException("Job", id);

        var previous = job.Status;
        if (job.ChangeStatus(status, timeProvider.GetUtcNow().UtcDateTime))
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Job {JobId} status changed from {PreviousStatus} to {Status}", id, previous, status);
        }

        return ToDetail(job);
    }

    private static IQueryable<Job> ApplyFilters(IQueryable<Job> jobs, JobQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // ToLower().Contains() translates on SQLite and PostgreSQL alike, giving case-insensitive matching on both.
            var term = query.Search.Trim().ToLower();
            jobs = jobs.Where(j => j.Title.ToLower().Contains(term)
                                   || (j.Company != null && j.Company.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            var source = query.Source.Trim();
            jobs = jobs.Where(j => j.Source == source);
        }

        if (query.ReceivedFrom is { } from)
        {
            var fromUtc = ToUtc(from);
            jobs = jobs.Where(j => j.ReceivedAt >= fromUtc);
        }

        if (query.ReceivedBefore is { } before)
        {
            var beforeUtc = ToUtc(before);
            jobs = jobs.Where(j => j.ReceivedAt < beforeUtc);
        }

        return jobs;
    }

    private static IQueryable<Job> ApplySort(IQueryable<Job> jobs, JobQuery query)
    {
        var ascending = query.SortDirection == SortDirection.Asc;
        IOrderedQueryable<Job> ordered = query.SortBy switch
        {
            JobSortField.Title => ascending ? jobs.OrderBy(j => j.Title) : jobs.OrderByDescending(j => j.Title),
            JobSortField.Company => ascending ? jobs.OrderBy(j => j.Company) : jobs.OrderByDescending(j => j.Company),
            _ => ascending ? jobs.OrderBy(j => j.ReceivedAt) : jobs.OrderByDescending(j => j.ReceivedAt),
        };

        // Secondary keys keep paging stable when the primary key ties.
        return ordered.ThenByDescending(j => j.ReceivedAt).ThenByDescending(j => j.Id);
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    private static JobDetailDto ToDetail(Job job) => new(
        job.Id,
        job.Title,
        job.Company,
        job.SalaryText,
        job.SalaryMin,
        job.SalaryMax,
        job.SalaryCurrency,
        job.Location,
        job.WorkMode,
        job.Description,
        job.Requirements,
        job.EnrichedDescription,
        job.EnrichedAt,
        job.ApplicationUrl,
        job.Source,
        job.SourceJobId,
        job.ReceivedAt,
        job.CreatedAt,
        job.UpdatedAt,
        job.Status,
        job.StatusChangedAt,
        job.EmailLinks
            .Select(l => l.JobEmail)
            .OrderBy(e => e.ReceivedAt)
            .Select(e => new JobEmailSummaryDto(
                e.Id, e.GmailMessageId, e.Subject, e.Sender, e.ReceivedAt, GmailLinks.ForMessage(e.GmailMessageId)))
            .ToList());
}
