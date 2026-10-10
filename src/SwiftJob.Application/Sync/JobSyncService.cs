using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SwiftJob.Application.Abstractions;
using SwiftJob.Application.Ai;
using SwiftJob.Application.Common;
using SwiftJob.Application.Extraction;
using SwiftJob.Application.Gmail;
using SwiftJob.Domain.Emails;
using SwiftJob.Domain.Settings;

namespace SwiftJob.Application.Sync;

public interface IJobSyncService
{
    /// <summary>Downloads new matching emails and extracts jobs from every email not yet processed.</summary>
    Task<SyncResult> SyncAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Idempotent Gmail → jobs synchronisation. Safe to re-run: emails are keyed by Gmail message id, and
/// only pending or retryable failed emails are sent for extraction.
/// </summary>
public sealed class JobSyncService(
    IAppDbContext db,
    IGmailConnectionService gmail,
    IGmailMailbox mailbox,
    IAiConfigurationSource aiConfiguration,
    IJobExtractionService extractor,
    JobImporter importer,
    ISyncStatusPublisher status,
    TimeProvider timeProvider,
    ILogger<JobSyncService> logger) : IJobSyncService
{
    public const int MaxMessagesPerSync = 500;

    /// <summary>Stop extracting after this many consecutive failures (e.g. invalid API key) to avoid wasted calls.</summary>
    public const int MaxConsecutiveFailures = 3;

    /// <summary>Re-reads a little before the last sync so late-arriving messages are not missed.</summary>
    public static readonly TimeSpan Overlap = TimeSpan.FromDays(1);

    public async Task<SyncResult> SyncAsync(CancellationToken cancellationToken)
    {
        var startedAt = Now;
        GmailConnection connection;
        GmailAccess access;
        try
        {
            (connection, access) = await gmail.GetAccessAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var message = ex is ConflictException ? ex.Message : $"Unexpected error during sync: {ex.Message}";
            if (await db.GmailConnections.OrderBy(c => c.Id).FirstOrDefaultAsync(CancellationToken.None) is { } saved)
            {
                await RecordFailureAsync(saved, message);
            }

            await Report(SyncState.Failed, message, 0, 0, 0, startedAt, CancellationToken.None, Now);
            throw;
        }

        await Report(SyncState.Running, "Checking Gmail for new emails…", 0, 0, 0, startedAt, cancellationToken);

        try
        {
            var downloaded = await DownloadNewEmailsAsync(connection, access, startedAt, cancellationToken);
            var (processed, failed, newJobs, linkedJobs, extractionNote) = await ExtractPendingAsync(startedAt, cancellationToken);

            var summary = $"{downloaded} new email{Plural(downloaded)}, {newJobs} new job{Plural(newJobs)}" +
                          (linkedJobs > 0 ? $", {linkedJobs} already known" : string.Empty) +
                          (failed > 0 ? $", {failed} email{Plural(failed)} failed" : string.Empty) +
                          (extractionNote is null ? "." : $". {extractionNote}");

            connection.LastSuccessfulSyncStartedAt = startedAt;
            connection.LastSyncCompletedAt = Now;
            connection.LastSyncSummary = summary;
            connection.LastSyncError = extractionNote;
            await db.SaveChangesAsync(CancellationToken.None);

            await Report(SyncState.Completed, summary, processed, processed, newJobs, startedAt, CancellationToken.None, Now);
            logger.LogInformation("Sync completed: {Summary}", summary);
            return new SyncResult(downloaded, processed, failed, newJobs, linkedJobs, summary);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await Report(SyncState.Failed, "Sync was cancelled.", 0, 0, 0, startedAt, CancellationToken.None, Now);
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Every failure is recorded and shown to the user; unexpected ones are reported generically.
            var known = ex is ConflictException or ExternalServiceException;
            var message = known ? ex.Message : $"Unexpected error during sync: {ex.Message}";
            if (!known)
            {
                logger.LogError(ex, "Unexpected sync failure");
            }

            await RecordFailureAsync(connection, message);
            await Report(SyncState.Failed, message, 0, 0, 0, startedAt, CancellationToken.None, Now);
            throw;
        }
    }

    private async Task RecordFailureAsync(GmailConnection connection, string message)
    {
        try
        {
            connection.LastSyncCompletedAt = Now;
            connection.LastSyncError = Truncate(message, 4000);
            await db.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception saveError)
        {
            logger.LogError(saveError, "Could not record the sync failure");
        }
    }

    private async Task<int> DownloadNewEmailsAsync(
        GmailConnection connection, GmailAccess access, DateTime startedAt, CancellationToken cancellationToken)
    {
        var since = connection.LastSuccessfulSyncStartedAt is { } last
            ? last - Overlap
            : startedAt.AddDays(-GmailConnection.InitialSyncDays);
        var query = $"({connection.SearchQuery}) after:{new DateTimeOffset(since).ToUnixTimeSeconds()}";

        var ids = await mailbox.ListMessageIdsAsync(access, query, MaxMessagesPerSync, cancellationToken);
        var known = await db.JobEmails
            .Where(e => ids.Contains(e.GmailMessageId))
            .Select(e => e.GmailMessageId)
            .ToListAsync(cancellationToken);
        var newIds = ids.Except(known, StringComparer.Ordinal).ToList();

        for (var i = 0; i < newIds.Count; i++)
        {
            await Report(SyncState.Running, $"Downloading email {i + 1} of {newIds.Count}…", i, newIds.Count, 0, startedAt, cancellationToken);
            var message = await mailbox.GetMessageAsync(access, newIds[i], cancellationToken);
            db.JobEmails.Add(new JobEmail
            {
                GmailMessageId = message.Id,
                GmailThreadId = message.ThreadId,
                Subject = Truncate(message.Subject, 1000),
                Sender = Truncate(message.From, 500),
                ReceivedAt = message.ReceivedAt,
                BodyText = message.BodyText,
                BodyHtml = message.BodyHtml,
                SyncStatus = EmailSyncStatus.Pending,
                SyncedAt = Now,
            });
            await db.SaveChangesAsync(cancellationToken); // persist progress per email
        }

        return newIds.Count;
    }

    private async Task<(int Processed, int Failed, int NewJobs, int LinkedJobs, string? Note)> ExtractPendingAsync(
        DateTime startedAt, CancellationToken cancellationToken)
    {
        var pending = await db.JobEmails
            .Where(e => e.SyncStatus == EmailSyncStatus.Pending
                        || (e.SyncStatus == EmailSyncStatus.Failed && e.ProcessingAttempts < JobEmail.MaxProcessingAttempts))
            .OrderBy(e => e.ReceivedAt)
            .ToListAsync(cancellationToken);
        if (pending.Count == 0)
        {
            return (0, 0, 0, 0, null);
        }

        try
        {
            await aiConfiguration.GetRequiredAsync(cancellationToken);
        }
        catch (AiNotConfiguredException ex)
        {
            return (0, 0, 0, 0, $"{pending.Count} email{Plural(pending.Count)} waiting: {ex.Message}");
        }

        int processed = 0, failed = 0, newJobs = 0, linked = 0, consecutiveFailures = 0;
        foreach (var email in pending)
        {
            await Report(SyncState.Running, $"Extracting jobs from email {processed + failed + 1} of {pending.Count}…",
                processed + failed, pending.Count, newJobs, startedAt, cancellationToken);

            email.ProcessingAttempts++;
            try
            {
                var content = EmailTextBuilder.Build(email.Subject, email.Sender, email.ReceivedAt, email.BodyText, email.BodyHtml);
                var extraction = await extractor.ExtractAsync(content, cancellationToken);
                var result = await importer.ImportAsync(email, content, extraction, cancellationToken);

                email.SyncStatus = extraction.IsJobNotification || extraction.Jobs.Count > 0
                    ? EmailSyncStatus.Processed
                    : EmailSyncStatus.Ignored;
                email.ProcessingError = null;
                email.ProcessedAt = Now;
                newJobs += result.Created;
                linked += result.Linked;
                processed++;
                consecutiveFailures = 0;
            }
            catch (ExternalServiceException ex)
            {
                logger.LogWarning(ex, "Extraction failed for email {EmailId}", email.Id);
                email.SyncStatus = EmailSyncStatus.Failed;
                email.ProcessingError = Truncate(ex.Message, 4000);
                failed++;
                consecutiveFailures++;
            }

            await db.SaveChangesAsync(cancellationToken);

            if (consecutiveFailures >= MaxConsecutiveFailures)
            {
                return (processed, failed, newJobs, linked,
                    $"Extraction stopped after {MaxConsecutiveFailures} consecutive AI errors: {email.ProcessingError}");
            }
        }

        return (processed, failed, newJobs, linked, null);
    }

    private Task Report(
        SyncState state, string message, int processed, int total, int newJobs, DateTime startedAt,
        CancellationToken cancellationToken, DateTime? completedAt = null) =>
        status.PublishAsync(new SyncStatusDto(state, message, processed, total, newJobs, startedAt, completedAt), cancellationToken);

    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    private static string Plural(int count) => count == 1 ? string.Empty : "s";

    private static string? Truncate(string? value, int max) => value is { Length: var l } && l > max ? value[..max] : value;
}
