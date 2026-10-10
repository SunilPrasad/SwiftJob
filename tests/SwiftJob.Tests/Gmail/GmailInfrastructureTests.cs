using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using SwiftJob.Application.Gmail;
using SwiftJob.Infrastructure.Gmail;
using SwiftJob.Infrastructure.Security;

namespace SwiftJob.Tests.Gmail;

public class GmailInfrastructureTests
{
    [Fact]
    public void Authorization_url_requests_only_read_only_gmail_with_offline_access()
    {
        var url = new GoogleGmailClient().BuildAuthorizationUrl(
            new GoogleOAuthClient("my-client.apps.googleusercontent.com", "secret"),
            "http://localhost:5080/api/settings/gmail/callback",
            "state-abc");

        var query = System.Web.HttpUtility.ParseQueryString(url.Query);
        Assert.Equal("accounts.google.com", url.Host);
        Assert.Equal("https://www.googleapis.com/auth/gmail.readonly", query["scope"]);
        Assert.Equal("offline", query["access_type"]);
        Assert.Equal("consent", query["prompt"]);
        Assert.Equal("state-abc", query["state"]);
        Assert.Equal("code", query["response_type"]);
        Assert.Equal("my-client.apps.googleusercontent.com", query["client_id"]);
        Assert.Equal("http://localhost:5080/api/settings/gmail/callback", query["redirect_uri"]);
        Assert.DoesNotContain("secret", url.ToString());
    }

    [Fact]
    public void Secret_protector_round_trips_and_hides_plaintext()
    {
        var protector = new DataProtectionSecretProtector(new EphemeralDataProtectionProvider());

        var encrypted = protector.Protect("refresh-token-value");

        Assert.DoesNotContain("refresh-token-value", encrypted);
        Assert.Equal("refresh-token-value", protector.Unprotect(encrypted));
    }

    [Fact]
    public void OAuth_state_is_random_and_single_use()
    {
        var store = new MemoryOAuthStateStore(new MemoryCache(new MemoryCacheOptions()));

        var a = store.Create();
        var b = store.Create();

        Assert.NotEqual(a, b);
        Assert.True(a.Length >= 32);
        Assert.True(store.TryConsume(a));
        Assert.False(store.TryConsume(a));
        Assert.False(store.TryConsume("unknown"));
    }
}
