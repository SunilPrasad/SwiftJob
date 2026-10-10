using Microsoft.EntityFrameworkCore;
using SwiftJob.Application.Abstractions;
using SwiftJob.Application.Extraction;
using SwiftJob.Domain.Emails;
using SwiftJob.Domain.Jobs;

namespace SwiftJob.Application.Sync;

public sealed record ImportResult(int Created, int Linked, int Skipped);

/// <summary>
/// Validates extractor output against the email itself and stores jobs, de-duplicating against existing
/// jobs. Never trusts the extractor blindly: links must exist in the email and values are bounded.
/// </summary>
public sealed class JobImporter(IAppDbContext db, TimeProvider timeProvider)
{
    /// <summary>Jobs with the same source, title and company within this window are treated as the same job.</summary>
    public static readonly TimeSpan FuzzyDuplicateWindow = TimeSpan.FromDays(21);

    public async Task<ImportResult> ImportAsync(
        JobEmail email, EmailContent content, EmailExtractionResult extraction, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var created = 0;
        var linked = 0;
        var skipped = 0;
        var handled = new HashSet<Job>();

        foreach (var extracted in extraction.Jobs)
        {
            var candidate = ToJob(extracted, email, content);
            if (candidate is null)
            {
                skipped++;
                continue;
            }

            var existing = handled.FirstOrDefault(j => IsSame(j, candidate))
                           ?? await FindDuplicateAsync(candidate, cancellationToken);
            if (existing is not null)
            {
                if (handled.Add(existing))
                {
                    FillMissing(existing, candidate);
                    if (!existing.EmailLinks.Any(l => l.JobEmail == email || (email.Id != 0 && l.JobEmailId == email.Id)))
                    {
                        existing.EmailLinks.Add(new JobEmailLink { JobEmail = email, LinkedAt = now });
                    }

                    linked++;
                }
                else
                {
                    skipped++; // duplicate within the same email
                }

                continue;
            }

            candidate.EmailLinks.Add(new JobEmailLink { JobEmail = email, LinkedAt = now });
            db.Jobs.Add(candidate);
            handled.Add(candidate);
            created++;
        }

        return new ImportResult(created, linked, skipped);
    }

    internal static Job? ToJob(ExtractedJob extracted, JobEmail email, EmailContent content)
    {
        var title = Clean(extracted.Title, 500);
        if (title is null)
        {
            return null;
        }

        // Only accept a link that actually appears in the email: prevents fabricated or injected links.
        var applicationUrl = content.ResolveLink(extracted.ApplicationUrl);
        if (applicationUrl is { Length: > 2048 })
        {
            applicationUrl = null;
        }

        var canonical = JobUrl.Canonicalise(applicationUrl);
        var (min, max) = SalaryRange(extracted.SalaryMin, extracted.SalaryMax);

        return new Job
        {
            Title = title,
            Company = Clean(extracted.Company, 300),
            SalaryText = Clean(extracted.SalaryText, 300),
            SalaryMin = min,
            SalaryMax = max,
            SalaryCurrency = Currency(extracted.SalaryCurrency),
            Location = Clean(extracted.Location, 300),
            WorkMode = Enum.IsDefined(extracted.WorkMode) ? extracted.WorkMode : WorkMode.Unknown,
            Description = Clean(extracted.Description, 20_000),
            Requirements = Clean(extracted.Requirements, 10_000),
            ApplicationUrl = canonical is null ? null : applicationUrl,
            CanonicalUrl = canonical?.Url,
            SourceJobId = canonical?.SourceJobId,
            Source = JobSourceDetector.Detect(email.Sender, extracted.SourceName),
            ReceivedAt = email.ReceivedAt,
        };
    }

    private async Task<Job?> FindDuplicateAsync(Job candidate, CancellationToken cancellationToken)
    {
        var jobs = db.Jobs.Include(j => j.EmailLinks);

        if (candidate.SourceJobId is not null)
        {
            var bySourceId = await jobs.FirstOrDefaultAsync(
                j => j.Source == candidate.Source && j.SourceJobId == candidate.SourceJobId, cancellationToken);
            if (bySourceId is not null)
            {
                return bySourceId;
            }
        }

        if (candidate.CanonicalUrl is not null)
        {
            var byUrl = await jobs.FirstOrDefaultAsync(j => j.CanonicalUrl == candidate.CanonicalUrl, cancellationToken);
            if (byUrl is not null)
            {
                return byUrl;
            }
        }

        // Fallback for emails without stable links (e.g. recruiters): same source, title and company, recently.
        if (candidate.Company is null)
        {
            return null;
        }

        var title = candidate.Title.ToLower();
        var company = candidate.Company.ToLower();
        var from = candidate.ReceivedAt - FuzzyDuplicateWindow;
        var to = candidate.ReceivedAt + FuzzyDuplicateWindow;
        return await jobs.FirstOrDefaultAsync(j =>
            j.Source == candidate.Source
            && j.Title.ToLower() == title
            && j.Company != null && j.Company.ToLower() == company
            && j.ReceivedAt >= from && j.ReceivedAt <= to, cancellationToken);
    }

    private static bool IsSame(Job a, Job b) =>
        (a.SourceJobId is not null && a.Source == b.Source && a.SourceJobId == b.SourceJobId)
        || (a.CanonicalUrl is not null && a.CanonicalUrl == b.CanonicalUrl)
        || (a.CanonicalUrl is null && b.CanonicalUrl is null && a.Source == b.Source
            && string.Equals(a.Title, b.Title, StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.Company, b.Company, StringComparison.OrdinalIgnoreCase));

    /// <summary>Adds information the existing job lacks. Never overwrites values that were already extracted.</summary>
    private static void FillMissing(Job existing, Job update)
    {
        existing.Company ??= update.Company;
        existing.SalaryText ??= update.SalaryText;
        existing.SalaryMin ??= update.SalaryMin;
        existing.SalaryMax ??= update.SalaryMax;
        existing.SalaryCurrency ??= update.SalaryCurrency;
        existing.Location ??= update.Location;
        existing.Description ??= update.Description;
        existing.Requirements ??= update.Requirements;
        existing.ApplicationUrl ??= update.ApplicationUrl;
        if (existing.WorkMode == WorkMode.Unknown)
        {
            existing.WorkMode = update.WorkMode;
        }

        if (existing.CanonicalUrl is null && update.CanonicalUrl is not null)
        {
            existing.CanonicalUrl = update.CanonicalUrl;
            existing.SourceJobId ??= update.SourceJobId;
        }
    }

    private static string? Clean(string? value, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    private static string? Currency(string? value)
    {
        var code = value?.Trim().ToUpperInvariant();
        return code is { Length: 3 } && code.All(char.IsAsciiLetterUpper) ? code : null;
    }

    private static (decimal? Min, decimal? Max) SalaryRange(decimal? min, decimal? max)
    {
        min = min is > 0 and < 100_000_000 ? min : null;
        max = max is > 0 and < 100_000_000 ? max : null;
        return min > max ? (max, min) : (min, max);
    }
}
