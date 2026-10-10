namespace SwiftJob.Application.Emails;

public interface IJobEmailService
{
    /// <exception cref="Common.NotFoundException">The email does not exist.</exception>
    Task<JobEmailDetailDto> GetEmailAsync(int id, CancellationToken cancellationToken);
}
