using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SwiftJob.Application.Common;
using SwiftJob.Application.Jobs;
using SwiftJob.Domain.Jobs;

namespace SwiftJob.Tests.Api;

/// <summary>End-to-end tests through HTTP against a real (temporary) SQLite file seeded with sample data.</summary>
public sealed class JobsApiTests : IClassFixture<JobsApiTests.ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client;

    public JobsApiTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task List_defaults_to_newest_first_and_supports_status_filter()
    {
        var page = await _client.GetFromJsonAsync<PagedResult<JobListItemDto>>("/api/jobs?status=New&pageSize=100", Json);

        Assert.NotNull(page);
        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, j => Assert.Equal(JobStatus.New, j.Status));
        Assert.Equal(page.Items.OrderByDescending(j => j.ReceivedAt).Select(j => j.Id), page.Items.Select(j => j.Id));
    }

    [Fact]
    public async Task Invalid_page_size_returns_400()
    {
        var response = await _client.GetAsync("/api/jobs?pageSize=1000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Status_round_trip_moves_job_between_views()
    {
        var page = await _client.GetFromJsonAsync<PagedResult<JobListItemDto>>("/api/jobs?status=New&pageSize=1", Json);
        var jobId = page!.Items[0].Id;
        var before = await _client.GetFromJsonAsync<JobStatusCountsDto>("/api/jobs/counts", Json);

        var response = await _client.PutAsJsonAsync($"/api/jobs/{jobId}/status", new { status = "NotRelevant" });
        response.EnsureSuccessStatusCode();

        var after = await _client.GetFromJsonAsync<JobStatusCountsDto>("/api/jobs/counts", Json);
        Assert.Equal(before!.New - 1, after!.New);
        Assert.Equal(before.NotRelevant + 1, after.NotRelevant);

        // Reversible: put it back to New.
        (await _client.PutAsJsonAsync($"/api/jobs/{jobId}/status", new { status = "New" })).EnsureSuccessStatusCode();
        var detail = await _client.GetFromJsonAsync<JobDetailDto>($"/api/jobs/{jobId}", Json);
        Assert.Equal(JobStatus.New, detail!.Status);
    }

    [Theory]
    [InlineData("""{"status":"Bogus"}""")]
    [InlineData("""{"status":1}""")]
    [InlineData("""{}""")]
    public async Task Invalid_status_payload_returns_400(string body)
    {
        var response = await _client.PutAsync("/api/jobs/1/status",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_job_returns_404_problem_details()
    {
        var response = await _client.GetAsync("/api/jobs/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Email_endpoint_returns_plain_text_body()
    {
        var page = await _client.GetFromJsonAsync<PagedResult<JobListItemDto>>("/api/jobs?pageSize=1", Json);
        var detail = await _client.GetFromJsonAsync<JobDetailDto>($"/api/jobs/{page!.Items[0].Id}", Json);
        var emailId = Assert.Single(detail!.Emails).Id;

        using var email = await _client.GetFromJsonAsync<JsonDocument>($"/api/emails/{emailId}");

        Assert.False(string.IsNullOrWhiteSpace(email!.RootElement.GetProperty("bodyText").GetString()));
        Assert.False(email.RootElement.TryGetProperty("bodyHtml", out _));
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"swiftjob-test-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:SwiftJob", $"Data Source={_databasePath};Pooling=False");
            builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
            builder.UseSetting("Database:SeedSampleData", "true");
            builder.UseSetting("DataProtection:KeysPath", _databasePath + "-keys");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                File.Delete(_databasePath + suffix);
            }

            if (Directory.Exists(_databasePath + "-keys"))
            {
                Directory.Delete(_databasePath + "-keys", recursive: true);
            }
        }
    }
}
