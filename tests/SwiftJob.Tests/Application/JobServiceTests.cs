using Microsoft.Extensions.Logging.Abstractions;
using SwiftJob.Application.Common;
using SwiftJob.Application.Jobs;
using SwiftJob.Domain.Jobs;
using SwiftJob.Infrastructure.Persistence;
using SwiftJob.Tests.TestSupport;

namespace SwiftJob.Tests.Application;

public sealed class JobServiceTests : IAsyncLifetime
{
    private TestDatabase _database = null!;
    private AppDbContext _db = null!;
    private JobService _service = null!;

    private DateTime Now => _database.Clock.GetUtcNow().UtcDateTime;

    public async Task InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        _db = _database.CreateContext();
        _service = new JobService(_db, _database.Clock, NullLogger<JobService>.Instance);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task GetJobs_returns_newest_first_by_default()
    {
        await _database.AddJobAsync("Old", receivedAt: Now.AddDays(-3));
        await _database.AddJobAsync("Newest", receivedAt: Now);
        await _database.AddJobAsync("Middle", receivedAt: Now.AddDays(-1));

        var result = await _service.GetJobsAsync(new JobQuery(), CancellationToken.None);

        Assert.Equal(["Newest", "Middle", "Old"], result.Items.Select(j => j.Title));
    }

    [Fact]
    public async Task GetJobs_can_sort_oldest_first()
    {
        await _database.AddJobAsync("Old", receivedAt: Now.AddDays(-3));
        await _database.AddJobAsync("Newest", receivedAt: Now);

        var result = await _service.GetJobsAsync(new JobQuery { SortDirection = SortDirection.Asc }, CancellationToken.None);

        Assert.Equal(["Old", "Newest"], result.Items.Select(j => j.Title));
    }

    [Fact]
    public async Task GetJobs_filters_by_status()
    {
        await _database.AddJobAsync("New one");
        await _database.AddJobAsync("Applied one", status: JobStatus.Applied);
        await _database.AddJobAsync("Irrelevant one", status: JobStatus.NotRelevant);

        var result = await _service.GetJobsAsync(new JobQuery { Status = JobStatus.New }, CancellationToken.None);

        var job = Assert.Single(result.Items);
        Assert.Equal("New one", job.Title);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetJobs_without_status_returns_all_statuses()
    {
        await _database.AddJobAsync("A");
        await _database.AddJobAsync("B", status: JobStatus.Applied);

        var result = await _service.GetJobsAsync(new JobQuery { Status = null }, CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
    }

    [Theory]
    [InlineData("senior")]
    [InlineData("SENIOR")]
    [InlineData("contoso")]
    public async Task GetJobs_search_matches_title_or_company_case_insensitively(string term)
    {
        await _database.AddJobAsync("Senior .NET Developer", "Fabrikam");
        await _database.AddJobAsync("Angular Developer", "Contoso");
        await _database.AddJobAsync("Data Engineer", null);

        var result = await _service.GetJobsAsync(new JobQuery { Search = term }, CancellationToken.None);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetJobs_filters_by_source_and_date_range()
    {
        await _database.AddJobAsync("Too old", source: "Reed", receivedAt: Now.AddDays(-10));
        await _database.AddJobAsync("In range", source: "Reed", receivedAt: Now.AddDays(-2));
        await _database.AddJobAsync("Other source", source: "Indeed", receivedAt: Now.AddDays(-2));
        await _database.AddJobAsync("Upper bound is exclusive", source: "Reed", receivedAt: Now);

        var result = await _service.GetJobsAsync(new JobQuery
        {
            Source = "Reed",
            ReceivedFrom = Now.AddDays(-5),
            ReceivedBefore = Now,
        }, CancellationToken.None);

        Assert.Equal("In range", Assert.Single(result.Items).Title);
    }

    [Fact]
    public async Task GetJobs_pages_results_with_stable_order()
    {
        for (var i = 0; i < 7; i++)
        {
            await _database.AddJobAsync($"Job {i}", receivedAt: Now); // identical timestamps: ordering falls back to id
        }

        var page1 = await _service.GetJobsAsync(new JobQuery { Page = 1, PageSize = 3 }, CancellationToken.None);
        var page3 = await _service.GetJobsAsync(new JobQuery { Page = 3, PageSize = 3 }, CancellationToken.None);

        Assert.Equal(7, page1.TotalCount);
        Assert.Equal(["Job 6", "Job 5", "Job 4"], page1.Items.Select(j => j.Title));
        Assert.Equal(["Job 0"], page3.Items.Select(j => j.Title));
    }

    [Fact]
    public async Task GetStatusCounts_ignores_status_filter_but_honours_other_filters()
    {
        await _database.AddJobAsync("Dev 1");
        await _database.AddJobAsync("Dev 2", status: JobStatus.Applied);
        await _database.AddJobAsync("Dev 3", status: JobStatus.NotRelevant);
        await _database.AddJobAsync("Dev 4", status: JobStatus.NotRelevant);
        await _database.AddJobAsync("Tester", status: JobStatus.Applied);

        var counts = await _service.GetStatusCountsAsync(
            new JobQuery { Status = JobStatus.New, Search = "dev" }, CancellationToken.None);

        Assert.Equal(new JobStatusCountsDto(1, 1, 2), counts);
        Assert.Equal(4, counts.Total);
    }

    [Fact]
    public async Task GetSources_returns_distinct_sorted_sources()
    {
        await _database.AddJobAsync("A", source: "Reed");
        await _database.AddJobAsync("B", source: "Indeed");
        await _database.AddJobAsync("C", source: "Reed");

        var sources = await _service.GetSourcesAsync(CancellationToken.None);

        Assert.Equal(["Indeed", "Reed"], sources);
    }

    [Fact]
    public async Task GetJob_includes_linked_emails_with_gmail_links()
    {
        var job = await _database.AddJobAsync("A");
        await _database.AddEmailAsync("18c2f0a1b2c3d4e5", job);

        var detail = await _service.GetJobAsync(job.Id, CancellationToken.None);

        var email = Assert.Single(detail.Emails);
        Assert.Equal("18c2f0a1b2c3d4e5", email.GmailMessageId);
        Assert.Equal("https://mail.google.com/mail/u/0/#all/18c2f0a1b2c3d4e5", email.GmailUrl);
    }

    [Fact]
    public async Task GetJob_throws_NotFound_for_unknown_id()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetJobAsync(12345, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateStatus_persists_status_and_timestamps_but_keeps_the_job()
    {
        var job = await _database.AddJobAsync("A");
        _database.Clock.Advance(TimeSpan.FromHours(2));

        var result = await _service.UpdateStatusAsync(job.Id, JobStatus.Applied, CancellationToken.None);

        Assert.Equal(JobStatus.Applied, result.Status);
        await using var verify = _database.CreateContext();
        var stored = await verify.Jobs.FindAsync(job.Id);
        Assert.NotNull(stored);
        Assert.Equal(JobStatus.Applied, stored.Status);
        Assert.Equal(Now, stored.StatusChangedAt);
        Assert.Equal(Now, stored.UpdatedAt);
        Assert.Equal(Now.AddHours(-2), stored.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, stored.ReceivedAt.Kind);
    }

    [Fact]
    public async Task UpdateStatus_throws_NotFound_for_unknown_id()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateStatusAsync(12345, JobStatus.Applied, CancellationToken.None));
    }
}
