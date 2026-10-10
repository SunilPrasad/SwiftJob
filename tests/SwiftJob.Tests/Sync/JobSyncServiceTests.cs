using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SwiftJob.Application.Ai;
using SwiftJob.Application.Common;
using SwiftJob.Application.Extraction;
using SwiftJob.Application.Gmail;
using SwiftJob.Application.Sync;
using SwiftJob.Domain.Ai;
using SwiftJob.Domain.Emails;
using SwiftJob.Domain.Jobs;
using SwiftJob.Infrastructure.Persistence;
using SwiftJob.Tests.Gmail;
using SwiftJob.Tests.TestSupport;

namespace SwiftJob.Tests.Sync;

public sealed class JobSyncServiceTests : IAsyncLifetime
{
    private readonly FakeGoogle _google = new();
    private readonly FakeExtractor _extractor = new();
    private readonly FakeAiConfiguration _ai = new();
    private readonly RecordingPublisher _publisher = new();
    private TestDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        var states = new FakeStateStore();
        await using var db = _database.CreateContext();
        var gmail = GmailService(db, states);
        await gmail.SaveCredentialsAsync(new SaveGmailCredentialsRequest("client-id-123.apps", "secret-abcdef"), default);
        await gmail.BeginAuthorizationAsync(default);
        await gmail.CompleteAuthorizationAsync("code", states.Last, default);
    }

    public Task DisposeAsync() => _database.DisposeAsync().AsTask();

    private GmailConnectionService GmailService(AppDbContext db, FakeStateStore? states = null) =>
        new(db, new FakeProtector(), _google, _google, states ?? new FakeStateStore(), Options.Create(new GmailOptions()),
            _database.Clock, NullLogger<GmailConnectionService>.Instance);

    private async Task<SyncResult> Sync()
    {
        await using var db = _database.CreateContext();
        var service = new JobSyncService(db, GmailService(db), _google, _ai, _extractor, new JobImporter(db, _database.Clock),
            _publisher, Options.Create(new SyncOptions()), _database.Clock, NullLogger<JobSyncService>.Instance);
        return await service.SyncAsync(default);
    }

    private void AddMessage(string id, string subject, int hoursAgo = 1) =>
        _google.Messages[id] = new GmailMessage(id, "t-" + id, subject, "LinkedIn <jobalerts-noreply@linkedin.com>",
            _database.Clock.GetUtcNow().UtcDateTime.AddHours(-hoursAgo), $"Body of {subject}", null);

    [Fact]
    public async Task First_sync_reads_last_30_days_and_imports_jobs()
    {
        AddMessage("a", "Job A");
        AddMessage("b", "Job B");

        var result = await Sync();

        Assert.Equal(2, result.EmailsDownloaded);
        Assert.Equal(2, result.NewJobs);
        var expectedAfter = _database.Clock.GetUtcNow().AddDays(-30).ToUnixTimeSeconds();
        Assert.Contains($"after:{expectedAfter}", _google.ListQueries.Single());
        Assert.StartsWith("(" + SwiftJob.Domain.Settings.GmailConnection.DefaultSearchQuery + ")", _google.ListQueries.Single());
        Assert.Equal(SyncState.Completed, _publisher.Current.State);
        await using var db = _database.CreateContext();
        Assert.Equal(2, await db.Jobs.CountAsync());
        Assert.All(await db.JobEmails.ToListAsync(), e => Assert.Equal(EmailSyncStatus.Processed, e.SyncStatus));
    }

    [Fact]
    public async Task Sync_is_idempotent_and_later_syncs_only_read_since_the_previous_one()
    {
        AddMessage("a", "Job A");
        await Sync();
        _database.Clock.Advance(TimeSpan.FromHours(2));
        AddMessage("c", "Job C");

        var second = await Sync();

        Assert.Equal(1, second.EmailsDownloaded);
        Assert.Equal(2, _extractor.Calls); // "a" was not extracted again
        var expectedAfter = _database.Clock.GetUtcNow().AddHours(-2).AddDays(-1).ToUnixTimeSeconds();
        Assert.Contains($"after:{expectedAfter}", _google.ListQueries.Last());
        await using var db = _database.CreateContext();
        Assert.Equal(2, await db.JobEmails.CountAsync());
    }

    [Fact]
    public async Task Without_ai_configuration_emails_are_stored_and_wait()
    {
        _ai.Configured = false;
        AddMessage("a", "Job A");

        var result = await Sync();

        Assert.Equal(1, result.EmailsDownloaded);
        Assert.Equal(0, _extractor.Calls);
        Assert.Contains("AI is not configured", result.Summary);
        await using var db = _database.CreateContext();
        Assert.Equal(EmailSyncStatus.Pending, (await db.JobEmails.SingleAsync()).SyncStatus);

        _ai.Configured = true;
        var later = await Sync();
        Assert.Equal(1, later.NewJobs);
    }

    [Fact]
    public async Task Non_job_emails_are_ignored()
    {
        AddMessage("a", "Your application was received");
        _extractor.NotAJob.Add("Your application was received");

        await Sync();

        await using var db = _database.CreateContext();
        Assert.Equal(EmailSyncStatus.Ignored, (await db.JobEmails.SingleAsync()).SyncStatus);
        Assert.Equal(0, await db.Jobs.CountAsync());
    }

    [Fact]
    public async Task Failed_extractions_are_retried_up_to_the_limit()
    {
        AddMessage("a", "Job A");
        _extractor.Fail = true;

        for (var i = 0; i < JobEmail.MaxProcessingAttempts + 1; i++)
        {
            await Sync();
        }

        Assert.Equal(JobEmail.MaxProcessingAttempts, _extractor.Calls);
        await using var db = _database.CreateContext();
        var email = await db.JobEmails.SingleAsync();
        Assert.Equal(EmailSyncStatus.Failed, email.SyncStatus);
        Assert.Contains("model unavailable", email.ProcessingError);
    }

    [Fact]
    public async Task Extraction_stops_after_consecutive_failures()
    {
        for (var i = 0; i < 6; i++)
        {
            AddMessage($"m{i}", $"Job {i}", hoursAgo: i + 1);
        }

        _extractor.Fail = true;

        var result = await Sync();

        Assert.Contains("consecutive AI errors", result.Summary);
        Assert.Equal(JobSyncService.MaxConsecutiveFailures, result.EmailsFailed);
        // Calls already in flight may finish, but the rest are never started and wait for the next sync.
        await using var db = _database.CreateContext();
        Assert.True(await db.JobEmails.CountAsync(e => e.SyncStatus == EmailSyncStatus.Pending) >= 6 - new SyncOptions().ExtractionConcurrency);
    }

    [Fact]
    public async Task Gmail_failures_are_recorded_and_reported()
    {
        AddMessage("a", "Job A");
        _google.MailboxFailure = new GmailAccessRevokedException();
        _google.FailListing = true;

        await Assert.ThrowsAsync<GmailAccessRevokedException>(Sync);

        Assert.Equal(SyncState.Failed, _publisher.Current.State);
        await using var db = _database.CreateContext();
        Assert.Contains("reconnect", (await db.GmailConnections.SingleAsync()).LastSyncError);
    }

    [Fact]
    public async Task Sync_without_a_gmail_connection_reports_failure()
    {
        await using (var db = _database.CreateContext())
        {
            await GmailService(db).DisconnectAsync(default);
        }

        await Assert.ThrowsAsync<ConflictException>(Sync);

        Assert.Equal(SyncState.Failed, _publisher.Current.State);
        Assert.Contains("not connected", _publisher.Current.Message);
    }

    [Fact]
    public async Task Unexpected_errors_are_recorded_and_reported_not_swallowed()
    {
        AddMessage("a", "Job A");
        _google.MailboxFailure = new InvalidOperationException("boom");
        _google.FailListing = true;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(Sync);

        Assert.Equal("boom", ex.Message);
        Assert.Equal(SyncState.Failed, _publisher.Current.State);
        Assert.Contains("Unexpected error during sync: boom", _publisher.Current.Message);
        await using var db = _database.CreateContext();
        var connection = await db.GmailConnections.SingleAsync();
        Assert.Contains("boom", connection.LastSyncError);
        Assert.NotNull(connection.LastSyncCompletedAt);
    }

    [Fact]
    public async Task Undecryptable_credentials_give_a_clear_message()
    {
        await using (var db = _database.CreateContext())
        {
            var connection = await db.GmailConnections.SingleAsync();
            connection.ProtectedRefreshToken = "not-encrypted-by-this-machine";
            await db.SaveChangesAsync();
        }

        var ex = await Assert.ThrowsAsync<ConflictException>(Sync);

        Assert.Contains("can no longer be decrypted", ex.Message);
        await using var verify = _database.CreateContext();
        Assert.Contains("can no longer be decrypted", (await verify.GmailConnections.SingleAsync()).LastSyncError);
    }

    [Fact]
    public async Task Extraction_runs_in_parallel_but_every_email_is_recorded()
    {
        for (var i = 0; i < 12; i++)
        {
            AddMessage($"m{i}", $"Job {i}", hoursAgo: i + 1);
        }

        _extractor.Delay = TimeSpan.FromMilliseconds(50);

        var result = await Sync();

        Assert.Equal(12, result.NewJobs);
        Assert.True(_extractor.MaxConcurrent > 1, "extraction should overlap");
        Assert.True(_extractor.MaxConcurrent <= new SyncOptions().ExtractionConcurrency);
        await using var db = _database.CreateContext();
        Assert.All(await db.JobEmails.ToListAsync(), e => Assert.Equal(EmailSyncStatus.Processed, e.SyncStatus));
    }

    [Fact]
    public async Task Activity_log_describes_each_step()
    {
        AddMessage("a", "3 new .NET jobs");
        AddMessage("b", "Your application was received", hoursAgo: 2);
        _extractor.NotAJob.Add("Your application was received");

        await Sync();

        var lines = _publisher.RecentLog.Select(l => l.Message).ToList();
        Assert.Contains(lines, l => l.StartsWith("Sync started for me@example.com", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.StartsWith("Found 2 matching emails: 2 new, 0 already imported", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.StartsWith("Downloaded “3 new .NET jobs” from jobalerts-noreply@linkedin.com", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.Contains("with Anthropic/claude-opus-5-5 (4 at a time)", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.StartsWith("1 job found (1 new) in “3 new .NET jobs”", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.StartsWith("Not a job email: “Your application was received”", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.StartsWith("Sync finished in", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, l => l.Contains("key", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Activity_log_records_ai_errors()
    {
        AddMessage("a", "Job A");
        _extractor.Fail = true;

        await Sync();

        Assert.Contains(_publisher.RecentLog, l => l.Level == SyncLogLevel.Error && l.Message.Contains("model unavailable"));
    }

    private sealed class FakeExtractor : IJobExtractionService
    {
        private int _calls;
        private int _concurrent;
        private int _maxConcurrent;

        public int Calls => _calls;
        public int MaxConcurrent => _maxConcurrent;
        public bool Fail { get; set; }
        public TimeSpan Delay { get; set; }
        public HashSet<string> NotAJob { get; } = [];

        public async Task<EmailExtractionResult> ExtractAsync(EmailContent email, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            var now = Interlocked.Increment(ref _concurrent);
            int seen;
            while (now > (seen = _maxConcurrent) && Interlocked.CompareExchange(ref _maxConcurrent, now, seen) != seen)
            {
            }

            try
            {
                if (Delay > TimeSpan.Zero)
                {
                    await Task.Delay(Delay, cancellationToken);
                }

                if (Fail)
                {
                    throw new ExternalServiceException("model unavailable");
                }

                return NotAJob.Contains(email.Subject!)
                    ? new EmailExtractionResult(false, [])
                    : new EmailExtractionResult(true,
                    [
                        new ExtractedJob(email.Subject!, "Company", null, null, null, null, null, WorkMode.Unknown,
                            null, null, null, null),
                    ]);
            }
            finally
            {
                Interlocked.Decrement(ref _concurrent);
            }
        }
    }

    private sealed class FakeAiConfiguration : IAiConfigurationSource
    {
        public bool Configured { get; set; } = true;

        public Task<AiConnectionConfig> GetRequiredAsync(CancellationToken cancellationToken) =>
            Configured
                ? Task.FromResult(new AiConnectionConfig(AiProvider.Anthropic, "claude-opus-5-5", "key", null))
                : throw new AiNotConfiguredException();
    }

    private sealed class RecordingPublisher : ISyncStatusPublisher
    {
        private readonly List<SyncLogEntry> _log = [];

        public SyncStatusDto Current { get; private set; } = SyncStatusDto.Idle;

        public IReadOnlyList<SyncLogEntry> RecentLog
        {
            get
            {
                lock (_log)
                {
                    return _log.ToList();
                }
            }
        }

        public Task LogAsync(SyncLogLevel level, string message)
        {
            lock (_log)
            {
                _log.Add(new SyncLogEntry(_log.Count + 1, DateTime.UtcNow, level, message));
            }

            return Task.CompletedTask;
        }

        public Task PublishAsync(SyncStatusDto status, CancellationToken cancellationToken)
        {
            Current = status;
            return Task.CompletedTask;
        }
    }
}
