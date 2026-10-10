using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SwiftJob.Tests.Api;

public sealed class AiAndSyncApiTests(JobsApiTests.ApiFactory factory) : IClassFixture<JobsApiTests.ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Ai_settings_round_trip_without_exposing_the_key()
    {
        var put = await _client.PutAsJsonAsync("/api/settings/ai",
            new { provider = "Anthropic", model = "claude-opus-5-5", apiKey = "sk-ant-very-secret" });
        put.EnsureSuccessStatusCode();

        var body = await _client.GetStringAsync("/api/settings/ai");

        Assert.DoesNotContain("sk-ant-very-secret", body);
        using var json = JsonDocument.Parse(body);
        Assert.Equal("Anthropic", json.RootElement.GetProperty("provider").GetString());
        Assert.True(json.RootElement.GetProperty("hasApiKey").GetBoolean());
        Assert.True(json.RootElement.GetProperty("isConfigured").GetBoolean());
        Assert.Equal(3, json.RootElement.GetProperty("providers").GetArrayLength());
    }

    [Fact]
    public async Task Invalid_ai_settings_return_400()
    {
        var response = await _client.PutAsJsonAsync("/api/settings/ai", new { provider = "OpenAI", model = "gpt-5" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Sync_status_is_available_and_sync_can_be_requested()
    {
        var status = await _client.GetFromJsonAsync<JsonDocument>("/api/sync/status");
        Assert.True(status!.RootElement.TryGetProperty("state", out _));

        var response = await _client.PostAsync("/api/sync", null);

        Assert.True(response.StatusCode is HttpStatusCode.Accepted or HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Sample_data_can_be_removed()
    {
        var before = await _client.GetFromJsonAsync<JsonDocument>("/api/settings/sample-data");
        Assert.True(before!.RootElement.GetProperty("sampleEmails").GetInt32() > 0);

        var response = await _client.DeleteAsync("/api/settings/sample-data");
        response.EnsureSuccessStatusCode();

        var after = await _client.GetFromJsonAsync<JsonDocument>("/api/settings/sample-data");
        Assert.Equal(0, after!.RootElement.GetProperty("sampleEmails").GetInt32());
    }
}
