using SwiftJob.Domain.Jobs;

namespace SwiftJob.Application.Extraction;

/// <summary>
/// An email prepared for extraction: readable text in which each link appears as a reference such as
/// <c>[L3]</c>, plus the real URLs (<see cref="Links"/>[0] is L1).
/// </summary>
public sealed record EmailContent(
    string? Subject,
    string? Sender,
    DateTime ReceivedAt,
    string Text,
    IReadOnlyList<string> Links)
{
    /// <summary>
    /// Turns the extractor's answer ("L3", "[L3]" or a URL copied verbatim) into a URL from this email.
    /// Anything that does not refer to a link actually present in the email gives null.
    /// </summary>
    public string? ResolveLink(string? reference)
    {
        var value = reference?.Trim().Trim('[', ']');
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (value.Length > 1 && (value[0] == 'L' || value[0] == 'l')
            && int.TryParse(value.AsSpan(1), System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out var index))
        {
            return index >= 1 && index <= Links.Count ? Links[index - 1] : null;
        }

        return Links.Contains(value, StringComparer.Ordinal) ? value : null;
    }
}

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
