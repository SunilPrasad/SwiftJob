using SwiftJob.Application.Abstractions;
using SwiftJob.Application.Gmail;

namespace SwiftJob.Tests.Gmail;

internal sealed class FakeProtector : ISecretProtector
{
    public string Protect(string plaintext) => "enc:" + new string(plaintext.Reverse().ToArray());

    public string Unprotect(string protectedValue) => new(protectedValue["enc:".Length..].Reverse().ToArray());
}

internal sealed class FakeStateStore : IOAuthStateStore
{
    private readonly HashSet<string> _states = [];

    public string Create()
    {
        var state = Guid.NewGuid().ToString("N");
        _states.Add(state);
        return state;
    }

    public bool TryConsume(string state) => _states.Remove(state);

    public string Last => _states.Last();
}

internal sealed class FakeGoogle : IGmailOAuthClient, IGmailMailbox
{
    public GmailTokenGrant Grant { get; set; } = new("refresh-123", "https://www.googleapis.com/auth/gmail.readonly");
    public string Email { get; set; } = "me@example.com";
    public List<string> Revoked { get; } = [];
    public bool FailRevoke { get; set; }
    public Exception? MailboxFailure { get; set; }
    public GmailAccess? LastAccess { get; private set; }
    public string? LastQuery { get; private set; }

    public Uri BuildAuthorizationUrl(GoogleOAuthClient client, string redirectUri, string state) =>
        new($"https://accounts.example.com/auth?client_id={client.ClientId}&state={state}");

    public Task<GmailTokenGrant> ExchangeCodeAsync(GoogleOAuthClient client, string redirectUri, string code, CancellationToken ct) =>
        Task.FromResult(Grant);

    public Task RevokeAsync(GoogleOAuthClient client, string refreshToken, CancellationToken ct)
    {
        if (FailRevoke)
        {
            throw new HttpRequestException("network down");
        }

        Revoked.Add(refreshToken);
        return Task.CompletedTask;
    }

    public Task<string> GetEmailAddressAsync(GmailAccess access, CancellationToken ct)
    {
        LastAccess = access;
        return MailboxFailure is null ? Task.FromResult(Email) : Task.FromException<string>(MailboxFailure);
    }

    public Task<GmailSearchResult> SearchAsync(GmailAccess access, string query, int maxPreviews, CancellationToken ct)
    {
        LastQuery = query;
        return Task.FromResult(new GmailSearchResult(42,
            [new GmailMessagePreview("m1", "3 new jobs", "LinkedIn <jobalerts-noreply@linkedin.com>", DateTime.UtcNow)]));
    }
}
