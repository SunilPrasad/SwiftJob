using Microsoft.AspNetCore.Mvc;
using SwiftJob.Application.Emails;

namespace SwiftJob.Api.Controllers;

[ApiController]
[Route("api/emails")]
public sealed class EmailsController(IJobEmailService emailService) : ControllerBase
{
    /// <summary>The original email as plain text (HTML bodies are never returned).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<JobEmailDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<JobEmailDetailDto> GetEmail(int id, CancellationToken cancellationToken) =>
        emailService.GetEmailAsync(id, cancellationToken);
}
