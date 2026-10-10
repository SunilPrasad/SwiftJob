using System.ComponentModel.DataAnnotations;
using SwiftJob.Domain.Jobs;

namespace SwiftJob.Application.Jobs;

public sealed record JobListItemDto(
    int Id,
    DateTime ReceivedAt,
    string Title,
    string? Company,
    string? SalaryText,
    string? Location,
    WorkMode WorkMode,
    string Source,
    JobStatus Status,
    bool HasApplicationUrl);

public sealed record JobEmailSummaryDto(
    int Id,
    string GmailMessageId,
    string? Subject,
    string? Sender,
    DateTime ReceivedAt,
    string GmailUrl);

public sealed record JobDetailDto(
    int Id,
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
    string? EnrichedDescription,
    DateTime? EnrichedAt,
    string? ApplicationUrl,
    string Source,
    string? SourceJobId,
    DateTime ReceivedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    JobStatus Status,
    DateTime? StatusChangedAt,
    IReadOnlyList<JobEmailSummaryDto> Emails);

public sealed record JobStatusCountsDto(int New, int Applied, int NotRelevant)
{
    public int Total => New + Applied + NotRelevant;
}

public sealed record UpdateJobStatusRequest([Required] JobStatus? Status);
