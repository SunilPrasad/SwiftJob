using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    IOptions<SyncOptions> options,
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
            await status.LogAsync(SyncLogLevel.Error, $"Sync failed: {message}");
            if (await db.GmailConnections.OrderBy(c => c.Id).FirstOrDefaultAsync(CancellationToken.None) is { } saved)
            {
                await RecordFailureAsync(saved, message);
            }

            await Report(SyncState.Failed, message, 0, 0, 0, startedAt, CancellationToken.None, Now);
            throw;
        }

        await Report(SyncState.Running, "Checking Gmail for new emails…", 0, 0, 0, startedAt, cancellationToken);
        await status.LogAsync(SyncLogLevel.Info, $"Sync started for {connection.ConnectedEmail}.");

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
            await status.LogAsync(extractionNote is null ? SyncLogLevel.Info : SyncLogLevel.Warning,
                $"Sync finished in {Elapsed(startedAt)}: {summary}");
            logger.LogInformation("Sync completed: {Summary}", summary);
            return new SyncResult(downloaded, processed, failed, newJobs, linkedJobs, summary);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await status.LogAsync(SyncLogLevel.Warning, "Sync was cancelled (the API is shutting down).");
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
            await status.LogAsync(SyncLogLevel.Error, $"Sync failed after {Elapsed(startedAt)}: {message}");
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

        await status.LogAsync(SyncLogLevel.Info,
            $"Searching Gmail for emails since {since.ToLocalTime():d MMM yyyy HH:mm} matching: {connection.SearchQuery}");
        var ids = await mailbox.ListMessageIdsAsync(access, query, MaxMessagesPerSync, cancellationToken);
        var known = await db.JobEmails
            .Where(e => ids.Contains(e.GmailMessageId))
            .Select(e => e.GmailMessageId)
            .ToListAsync(cancellationToken);
        var newIds = ids.Except(known, StringComparer.Ordinal).ToList();
        await status.LogAsync(SyncLogLevel.Info,
            $"Found {ids.Count} matching email{Plural(ids.Count)}: {newIds.Count} new, {known.Count} already imported." +
            (ids.Count >= MaxMessagesPerSync ? $" (limited to {MaxMessagesPerSync} per sync)" : string.Empty));

        // Downloads run in parallel; saving stays sequential on this thread (DbContext is not thread-safe).
        var downloaded = 0;
        await foreach (var (_, message) in BoundedParallel.RunAsync(
                           newIds, options.Value.DownloadConcurrency,
                           (id, ct) => mailbox.GetMessageAsync(access, id, ct), cancellationToken))
        {
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
            downloaded++;
            await status.LogAsync(SyncLogLevel.Info, $"Downloaded {Describe(message.Subject, message.From)}");
            await Report(SyncState.Running, $"Downloaded {downloaded} of {newIds.Count} emails…",
                downloaded, newIds.Count, 0, startedAt, cancellationToken);
        }

        return downloaded;
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
            _ = await aiConfiguration.GetRequiredAsync(cancellationToken);
        }
        catch (AiNotConfiguredException ex)
        {
            await status.LogAsync(SyncLogLevel.Warning, $"{pending.Count} email{Plural(pending.Count)} not processed: {ex.Message}");
            return (0, 0, 0, 0, $"{pending.Count} email{Plural(pending.Count)} waiting: {ex.Message}");
        }

        var model = await aiConfiguration.GetRequiredAsync(cancellationToken);
        await status.LogAsync(SyncLogLevel.Info,
            $"Extracting jobs from {pending.Count} email{Plural(pending.Count)} with {model} " +
            $"({options.Value.ExtractionConcurrency} at a time).");

        await Report(SyncState.Running, $"Extracting jobs from {pending.Count} email{Plural(pending.Count)}…",
            0, pending.Count, 0, startedAt, cancellationToken);

        // Text is prepared up front so the parallel AI calls never touch the DbContext.
        var work = pending
            .Select(email => (Email: email, Content: EmailTextBuilder.Build(
                email.Subject, email.Sender, email.ReceivedAt, email.BodyText, email.BodyHtml)))
            .ToList();

        int processed = 0, failed = 0, newJobs = 0, linked = 0, consecutiveFailures = 0;
        string? stopNote = null;

        await foreach (var (item, outcome) in BoundedParallel.RunAsync(
                           work, options.Value.ExtractionConcurrency,
                           (w, ct) => ExtractSafelyAsync(w.Content, ct), cancellationToken))
        {
            var email = item.Email;
            email.ProcessingAttempts++;
            if (outcome.Result is { } extraction)
            {
                var result = await importer.ImportAsync(email, item.Content, extraction, cancellationToken);
                email.SyncStatus = extraction.IsJobNotification || extraction.Jobs.Count > 0
                    ? EmailSyncStatus.Processed
                    : EmailSyncStatus.Ignored;
                email.ProcessingError = null;
                email.ProcessedAt = Now;
                newJobs += result.Created;
                linked += result.Linked;
                processed++;
                consecutiveFailures = 0;
                await status.LogAsync(SyncLogLevel.Info, email.SyncStatus == EmailSyncStatus.Ignored
                    ? $"Not a job email: {Describe(email.Subject, email.Sender)} ({outcome.Seconds:0.0}s)"
                    : $"{extraction.Jobs.Count} job{Plural(extraction.Jobs.Count)} found ({result.Created} new" +
                      (result.Linked > 0 ? $", {result.Linked} already known" : string.Empty) +
                      $") in {Describe(email.Subject, email.Sender)} ({outcome.Seconds:0.0}s)");
            }
            else
            {
                email.SyncStatus = EmailSyncStatus.Failed;
                email.ProcessingError = Truncate(outcome.Error, 4000);
                failed++;
                consecutiveFailures++;
                await status.LogAsync(SyncLogLevel.Error,
                    $"AI extraction failed for {Describe(email.Subject, email.Sender)} ({outcome.Seconds:0.0}s): {outcome.Error}");
            }

            await db.SaveChangesAsync(cancellationToken);
            await Report(SyncState.Running, $"Extracted {processed + failed} of {pending.Count} emails…",
                processed + failed, pending.Count, newJobs, startedAt, cancellationToken);

            if (consecutiveFailures >= MaxConsecutiveFailures)
            {
                // Leaving the loop cancels the remaining AI calls; their emails stay pending for the next sync.
                stopNote = $"Extraction stopped after {MaxConsecutiveFailures} consecutive AI errors: {email.ProcessingError}";
                await status.LogAsync(SyncLogLevel.Warning,
                    "Stopping extraction: several AI errors in a row. Remaining emails will be retried on the next sync.");
                break;
            }
        }

        return (processed, failed, newJobs, linked, stopNote);
    }

    private async Task<(EmailExtractionResult? Result, string? Error, double Seconds)> ExtractSafelyAsync(
        EmailContent content, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            return (await extractor.ExtractAsync(content, cancellationToken), null, Stopwatch.GetElapsedTime(started).TotalSeconds);
        }
        catch (ExternalServiceException ex)
        {
            logger.LogWarning(ex, "Extraction failed for an email from {Sender}", content.Sender);
            return (null, ex.Message, Stopwatch.GetElapsedTime(started).TotalSeconds);
        }
    }

    /// <summary>Short, single-line description of an email for the activity log.</summary>
    private static string Describe(string? subject, string? from)
    {
        var title = string.IsNullOrWhiteSpace(subject) ? "(no subject)" : subject.ReplaceLineEndings(" ").Trim();
        if (title.Length > 90)
        {
            title = title[..90] + "…";
        }

        var sender = JobSourceDetector.ExtractAddress(from) ?? from;
        return $"“{title}”" + (string.IsNullOrWhiteSpace(sender) ? string.Empty : $" from {sender}");
    }

    private string Elapsed(DateTime startedAt)
    {
        var elapsed = Now - startedAt;
        return elapsed.TotalMinutes >= 1 ? $"{(int)elapsed.TotalMinutes}m {elapsed.Seconds}s" : $"{elapsed.TotalSeconds:0}s";
    }

    private Task Report(
        SyncState state, string message, int processed, int total, int newJobs, DateTime startedAt,
        CancellationToken cancellationToken, DateTime? completedAt = null) =>
        status.PublishAsync(new SyncStatusDto(state, message, processed, total, newJobs, startedAt, completedAt), cancellationToken);

    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    private static string Plural(int count) => count == 1 ? string.Empty : "s";

    private static string? Truncate(string? value, int max) => value is { Length: var l } && l > max ? value[..max] : value;
}
