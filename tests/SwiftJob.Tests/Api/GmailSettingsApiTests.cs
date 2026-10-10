using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SwiftJob.Tests.Api;

public sealed class GmailSettingsApiTests(JobsApiTests.ApiFactory factory) : IClassFixture<JobsApiTests.ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Settings_never_expose_secrets()
    {
        var put = await _client.PutAsJsonAsync("/api/settings/gmail/credentials",
            new { clientId = "abc123.apps.googleusercontent.com", clientSecret = "GOCSPX-super-secret" });
        put.EnsureSuccessStatusCode();

        var body = await _client.GetStringAsync("/api/settings/gmail");

        Assert.DoesNotContain("GOCSPX-super-secret", body);
        using var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.GetProperty("hasClientCredentials").GetBoolean());
        Assert.Equal("https://www.googleapis.com/auth/gmail.readonly", json.RootElement.GetProperty("scope").GetString());
    }

    [Fact]
    public async Task Connect_returns_google_sign_in_url_once_credentials_exist()
    {
        await _client.PutAsJsonAsync("/api/settings/gmail/credentials",
            new { clientId = "abc123.apps.googleusercontent.com", clientSecret = "GOCSPX-super-secret" });

        var response = await _client.PostAsync("/api/settings/gmail/connect", null);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var url = json.RootElement.GetProperty("authorizationUrl").GetString();
        Assert.StartsWith("https://accounts.google.com/", url);
        Assert.Contains("gmail.readonly", url);
    }

    [Fact]
    public async Task Callback_with_forged_state_redirects_back_with_an_error()
    {
        var response = await _client.GetAsync("/api/settings/gmail/callback?code=abc&state=forged");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("http://localhost:4200/settings?gmail=error", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Callback_when_user_denies_access_redirects_back_with_an_error()
    {
        var response = await _client.GetAsync("/api/settings/gmail/callback?error=access_denied");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("gmail=error", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Test_without_connection_returns_409()
    {
        var response = await _client.PostAsync("/api/settings/gmail/test", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_credentials_payload_returns_400()
    {
        var response = await _client.PutAsJsonAsync("/api/settings/gmail/credentials", new { clientId = "", clientSecret = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
