using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SwiftJob.Domain.Emails;
using SwiftJob.Domain.Jobs;
using SwiftJob.Infrastructure.Persistence.Seeding;
using SwiftJob.Tests.TestSupport;

namespace SwiftJob.Tests.Infrastructure;

public class PersistenceTests
{
    [Fact]
    public async Task GmailMessageId_is_unique()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddEmailAsync("msg-1");

        await Assert.ThrowsAsync<DbUpdateException>(() => database.AddEmailAsync("msg-1"));
    }

    [Fact]
    public async Task Source_and_SourceJobId_are_unique_together()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddJobAsync("A", source: "LinkedIn", sourceJobId: "123");
        await database.AddJobAsync("B", source: "Indeed", sourceJobId: "123"); // different source: allowed

        await Assert.ThrowsAsync<DbUpdateException>(
            () => database.AddJobAsync("C", source: "LinkedIn", sourceJobId: "123"));
    }

    [Fact]
    public async Task Jobs_without_SourceJobId_are_not_treated_as_duplicates()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddJobAsync("A", source: "Hays");
        await database.AddJobAsync("B", source: "Hays");

        await using var db = database.CreateContext();
        Assert.Equal(2, await db.Jobs.CountAsync());
    }

    [Fact]
    public async Task Enums_are_stored_as_strings()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddJobAsync("A", status: JobStatus.NotRelevant);

        await using var db = database.CreateContext();
        var stored = await db.Database
            .SqlQuery<string>($"SELECT \"Status\" AS \"Value\" FROM \"Jobs\"")
            .SingleAsync();
        Assert.Equal("NotRelevant", stored);
    }

    [Fact]
    public async Task Sample_seeder_seeds_once_and_links_multi_job_emails()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var db = database.CreateContext())
        {
            var seeder = new SampleDataSeeder(db, database.Clock, NullLogger<SampleDataSeeder>.Instance);
            Assert.True(await seeder.SeedAsync() > 0);
            Assert.Equal(0, await seeder.SeedAsync());
        }

        await using var verify = database.CreateContext();
        Assert.All(await verify.JobEmails.ToListAsync(), e =>
        {
            Assert.StartsWith(SampleDataSeeder.SampleMessageIdPrefix, e.GmailMessageId);
            Assert.Equal(EmailSyncStatus.Processed, e.SyncStatus);
        });
        Assert.Contains(await verify.JobEmails.Select(e => e.JobLinks.Count).ToListAsync(), count => count > 1);
        Assert.All(await verify.Jobs.ToListAsync(), j => Assert.StartsWith("https://www.example.com/", j.ApplicationUrl));
    }
}
