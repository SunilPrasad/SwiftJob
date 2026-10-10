using SwiftJob.Domain.Jobs;

namespace SwiftJob.Application.Extraction;

/// <summary>An email prepared for extraction: readable text with links inline, plus every link found.</summary>
public sealed record EmailContent(
    string? Subject,
    string? Sender,
    DateTime ReceivedAt,
    string Text,
    IReadOnlyList<string> Links);

/// <summary>One job as reported by the extractor. Every field is optional except the title; unknowns are null.</summary>
public sealed record ExtractedJob(
    string Title,
    string? Company,
    string? SalaryText,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string? SalaryCurrency,
    string? Location,
    WorkMode WorkMode,
    string? Description,
    string? Requirements,
    string? ApplicationUrl,
    string? SourceName);

public sealed record EmailExtractionResult(bool IsJobNotification, IReadOnlyList<ExtractedJob> Jobs);

/// <summary>
/// Turns one (untrusted) email into structured jobs. Implementations must not invent values:
/// anything not stated in the email must be null.
/// </summary>
public interface IJobExtractionService
{
    /// <exception cref="Common.ExternalServiceException">The model call failed or returned unusable output.</exception>
    Task<EmailExtractionResult> ExtractAsync(EmailContent email, CancellationToken cancellationToken);
}
