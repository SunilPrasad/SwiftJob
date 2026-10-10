using Microsoft.EntityFrameworkCore;
using SwiftJob.Application.Abstractions;
using SwiftJob.Application.Common;

namespace SwiftJob.Application.Emails;

public sealed class JobEmailService(IAppDbContext db) : IJobEmailService
{
    public async Task<JobEmailDetailDto> GetEmailAsync(int id, CancellationToken cancellationToken)
    {
        var email = await db.JobEmails.AsNoTracking()
            .Include(e => e.JobLinks)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException("Email", id);

        var bodyText = !string.IsNullOrWhiteSpace(email.BodyText)
            ? email.BodyText
            : email.BodyHtml is { Length: > 0 } html ? HtmlText.ToPlainText(html) : null;

        return new JobEmailDetailDto(
            email.Id,
            email.GmailMessageId,
            email.Subject,
            email.Sender,
            email.ReceivedAt,
            email.SyncStatus,
            bodyText,
            GmailLinks.ForMessage(email.GmailMessageId),
            email.JobLinks.Select(l => l.JobId).Order().ToList());
    }
}
