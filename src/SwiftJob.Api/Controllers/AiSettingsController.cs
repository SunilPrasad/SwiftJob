using Microsoft.AspNetCore.Mvc;
using SwiftJob.Application.Ai;

namespace SwiftJob.Api.Controllers;

/// <summary>Choose the AI provider and model. The API key is write-only.</summary>
[ApiController]
[Route("api/settings/ai")]
public sealed class AiSettingsController(IAiSettingsService ai) : ControllerBase
{
    [HttpGet]
    public Task<AiSettingsDto> Get(CancellationToken cancellationToken) => ai.GetAsync(cancellationToken);

    [HttpPut]
    public Task<AiSettingsDto> Save(SaveAiSettingsRequest request, CancellationToken cancellationToken) =>
        ai.SaveAsync(request, cancellationToken);

    /// <summary>Sends a tiny request to the configured model to verify provider, model and key.</summary>
    [HttpPost("test")]
    public Task<AiTestResultDto> Test(CancellationToken cancellationToken) => ai.TestAsync(cancellationToken);
}
