using Microsoft.AspNetCore.Mvc;
using SwiftJob.Infrastructure.Persistence.Seeding;

namespace SwiftJob.Api.Controllers;

/// <summary>Development sample data (emails with ids starting "sample-"). Real data is never affected.</summary>
[ApiController]
[Route("api/settings/sample-data")]
public sealed class SampleDataController(SampleDataSeeder seeder) : ControllerBase
{
    public sealed record SampleDataDto(int SampleEmails);

    public sealed record RemovedDto(int RemovedJobs);

    [HttpGet]
    public async Task<SampleDataDto> Get(CancellationToken cancellationToken) =>
        new(await seeder.CountAsync(cancellationToken));

    [HttpDelete]
    public async Task<RemovedDto> Remove(CancellationToken cancellationToken) =>
        new(await seeder.RemoveAsync(cancellationToken));
}
