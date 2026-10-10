using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using SwiftJob.Domain.Emails;
using SwiftJob.Domain.Jobs;
using SwiftJob.Infrastructure.Persistence;

namespace SwiftJob.Tests.TestSupport;

/// <summary>A migrated in-memory SQLite database that lives as long as this object.</summary>
public sealed class TestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero));

    private TestDatabase(SqliteConnection connection) => _connection = connection;

    public static async Task<TestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var database = new TestDatabase(connection);
        await using var db = database.CreateContext();
        await db.Database.MigrateAsync();
        return database;
    }

    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new AuditingInterceptor(Clock))
            .Options);

    public async Task<Job> AddJobAsync(
        string title,
        string? company = null,
        string source = "LinkedIn",
        DateTime? receivedAt = null,
        JobStatus status = JobStatus.New,
        string? sourceJobId = null)
    {
        await using var db = CreateContext();
        var job = new Job
        {
            Title = title,
            Company = company,
            Source = source,
            SourceJobId = sourceJobId,
            ReceivedAt = receivedAt ?? Clock.GetUtcNow().UtcDateTime,
        };
        if (status != JobStatus.New)
        {
            job.ChangeStatus(status, Clock.GetUtcNow().UtcDateTime);
        }

        db.Jobs.Add(job);
        await db.SaveChangesAsync();
        return job;
    }

    public async Task<JobEmail> AddEmailAsync(string gmailMessageId, params Job[] jobs)
    {
        await using var db = CreateContext();
        var email = new JobEmail
        {
            GmailMessageId = gmailMessageId,
            Subject = "Subject " + gmailMessageId,
            Sender = "alerts@example.com",
            ReceivedAt = Clock.GetUtcNow().UtcDateTime,
            SyncedAt = Clock.GetUtcNow().UtcDateTime,
        };
        foreach (var job in jobs)
        {
            email.JobLinks.Add(new JobEmailLink { JobId = job.Id, LinkedAt = Clock.GetUtcNow().UtcDateTime });
        }

        db.JobEmails.Add(email);
        await db.SaveChangesAsync();
        return email;
    }

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
}
