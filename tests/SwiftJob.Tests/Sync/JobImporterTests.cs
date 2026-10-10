using Microsoft.EntityFrameworkCore;
using SwiftJob.Application.Extraction;
using SwiftJob.Application.Sync;
using SwiftJob.Domain.Emails;
using SwiftJob.Domain.Jobs;
using SwiftJob.Tests.TestSupport;

namespace SwiftJob.Tests.Sync;

public class JobImporterTests
{
    private const string LinkedInLink = "https://www.linkedin.com/comm/jobs/view/4012345671/?trackingId=abc";

    private static ExtractedJob Job(string title = "Senior .NET Developer", string? company = "Fintech Ltd",
        string? url = LinkedInLink, string? salary = "£75,000") =>
        new(title, company, salary, 75000, 90000, "gbp", "London", WorkMode.Hybrid, "Desc", null, url, null);

    private static async Task<(JobEmail Email, EmailContent Content)> EmailAsync(TestDatabase database, string id, params string[] links)
    {
        await using var db = database.CreateContext();
        var email = new JobEmail
        {
            GmailMessageId = id, Sender = "LinkedIn <jobalerts-noreply@linkedin.com>", Subject = "Jobs",
            ReceivedAt = database.Clock.GetUtcNow().UtcDateTime, SyncedAt = database.Clock.GetUtcNow().UtcDateTime,
        };
        db.JobEmails.Add(email);
        await db.SaveChangesAsync();
        return (email, new EmailContent("Jobs", email.Sender, email.ReceivedAt, "text", links));
    }

    private static async Task<ImportResult> Import(TestDatabase database, string emailId, EmailContent content, params ExtractedJob[] jobs)
    {
        await using var db = database.CreateContext();
        var email = await db.JobEmails.SingleAsync(e => e.GmailMessageId == emailId);
        var result = await new JobImporter(db, database.Clock).ImportAsync(email, content, new EmailExtractionResult(true, jobs), default);
        await db.SaveChangesAsync();
        return result;
    }

    [Fact]
    public async Task Creates_job_with_canonical_url_source_and_normalised_values()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (_, content) = await EmailAsync(database, "m1", LinkedInLink);

        var result = await Import(database, "m1", content, Job());

        Assert.Equal(1, result.Created);
        await using var db = database.CreateContext();
        var job = await db.Jobs.Include(j => j.EmailLinks).SingleAsync();
        Assert.Equal("LinkedIn", job.Source);
        Assert.Equal("4012345671", job.SourceJobId);
        Assert.Equal("https://www.linkedin.com/jobs/view/4012345671", job.CanonicalUrl);
        Assert.Equal(LinkedInLink, job.ApplicationUrl);
        Assert.Equal("GBP", job.SalaryCurrency);
        Assert.Single(job.EmailLinks);
    }

    [Fact]
    public async Task Url_not_present_in_email_is_discarded()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (_, content) = await EmailAsync(database, "m1", "https://example.com/other");

        await Import(database, "m1", content, Job(url: "https://evil.example.com/phish"));

        await using var db = database.CreateContext();
        var job = await db.Jobs.SingleAsync();
        Assert.Null(job.ApplicationUrl);
        Assert.Null(job.CanonicalUrl);
    }

    [Fact]
    public async Task Same_job_in_a_later_email_is_linked_not_duplicated_and_fills_only_missing_fields()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (_, c1) = await EmailAsync(database, "m1", LinkedInLink);
        await Import(database, "m1", c1, Job(salary: null) with { SalaryMin = null, SalaryMax = null, Location = "London" });

        var otherTracking = "https://www.linkedin.com/comm/jobs/view/4012345671/?trackingId=zzz";
        var (_, c2) = await EmailAsync(database, "m2", otherTracking);
        var result = await Import(database, "m2", c2, Job(url: otherTracking, salary: "£80k") with { Location = "Remote" });

        Assert.Equal(0, result.Created);
        Assert.Equal(1, result.Linked);
        await using var db = database.CreateContext();
        var job = await db.Jobs.Include(j => j.EmailLinks).SingleAsync();
        Assert.Equal(2, job.EmailLinks.Count);
        Assert.Equal("£80k", job.SalaryText);  // was missing → filled
        Assert.Equal("London", job.Location);  // already known → not overwritten
    }

    [Fact]
    public async Task Jobs_without_links_are_deduplicated_by_title_company_and_source()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (_, c1) = await EmailAsync(database, "m1");
        await Import(database, "m1", c1, Job(url: null));
        var (_, c2) = await EmailAsync(database, "m2");

        var result = await Import(database, "m2", c2, Job(title: "senior .net developer", url: null));

        Assert.Equal(1, result.Linked);
        await using var db = database.CreateContext();
        Assert.Equal(1, await db.Jobs.CountAsync());
    }

    [Fact]
    public async Task Different_jobs_and_in_email_duplicates_are_handled()
    {
        await using var database = await TestDatabase.CreateAsync();
        var (_, content) = await EmailAsync(database, "m1");

        var result = await Import(database, "m1", content,
            Job(title: "A", url: null), Job(title: "B", url: null), Job(title: "A", url: null), Job(title: "  "));

        Assert.Equal(2, result.Created);
        Assert.Equal(2, result.Skipped);
    }
}
