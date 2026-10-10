using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SwiftJob.Application.Common;
using SwiftJob.Application.Gmail;
using SwiftJob.Domain.Settings;
using SwiftJob.Infrastructure.Persistence;
using SwiftJob.Tests.TestSupport;

namespace SwiftJob.Tests.Gmail;

public sealed class GmailConnectionServiceTests : IAsyncLifetime
{
    private const string ReadOnly = "https://www.googleapis.com/auth/gmail.readonly";

    private readonly FakeGoogle _google = new();
    private readonly FakeStateStore _states = new();
    private TestDatabase _database = null!;
    private AppDbContext _db = null!;
    private GmailConnectionService _service = null!;

    public async Task InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        _db = _database.CreateContext();
        _service = new GmailConnectionService(
            _db, new FakeProtector(), _google, _google, _states,
            Options.Create(new GmailOptions()), _database.Clock, NullLogger<GmailConnectionService>.Instance);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _database.DisposeAsync();
    }

    private Task SaveCredentials() =>
        _service.SaveCredentialsAsync(new SaveGmailCredentialsRequest("client-id-123.apps", "secret-abcdef"), CancellationToken.None);

    private async Task<string> Connect()
    {
        await SaveCredentials();
        await _service.BeginAuthorizationAsync(CancellationToken.None);
        return await _service.CompleteAuthorizationAsync("auth-code", _states.Last, CancellationToken.None);
    }

    private async Task<GmailConnection> Stored()
    {
        await using var db = _database.CreateContext();
        return await db.GmailConnections.SingleAsync();
    }

    [Fact]
    public async Task Initial_settings_are_not_configured_with_default_query_and_read_only_scope()
    {
        var settings = await _service.GetSettingsAsync(CancellationToken.None);

        Assert.False(settings.HasClientCredentials);
        Assert.False(settings.IsConnected);
        Assert.Equal(GmailConnection.DefaultSearchQuery, settings.SearchQuery);
        Assert.Equal(ReadOnly, settings.Scope);
        Assert.Equal("http://localhost:5080/api/settings/gmail/callback", settings.RedirectUri);
    }

    [Fact]
    public async Task Client_secret_is_stored_encrypted_and_never_returned()
    {
        await SaveCredentials();

        var stored = await Stored();
        Assert.Equal("client-id-123.apps", stored.ClientId);
        Assert.NotEqual("secret-abcdef", stored.ProtectedClientSecret);
        Assert.DoesNotContain("secret-abcdef", stored.ProtectedClientSecret);
        var settings = await _service.GetSettingsAsync(CancellationToken.None);
        Assert.True(settings.HasClientCredentials);
        Assert.DoesNotContain(typeof(GmailSettingsDto).GetProperties(), p => p.Name.Contains("Secret"));
    }

    [Fact]
    public async Task Connecting_requires_credentials()
    {
        await Assert.ThrowsAsync<ConflictException>(() => _service.BeginAuthorizationAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Successful_connection_stores_encrypted_refresh_token_and_email()
    {
        var email = await Connect();

        Assert.Equal("me@example.com", email);
        var stored = await Stored();
        Assert.True(stored.IsConnected);
        Assert.Equal("me@example.com", stored.ConnectedEmail);
        Assert.NotEqual("refresh-123", stored.ProtectedRefreshToken);
        Assert.Equal(ReadOnly, stored.GrantedScopes);
        Assert.Equal("secret-abcdef", _google.LastAccess!.Client.ClientSecret);
    }

    [Fact]
    public async Task Invalid_or_reused_state_is_rejected()
    {
        await SaveCredentials();
        await _service.BeginAuthorizationAsync(CancellationToken.None);
        var state = _states.Last;

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.CompleteAuthorizationAsync("code", "forged-state", CancellationToken.None));
        await _service.CompleteAuthorizationAsync("code", state, CancellationToken.None);
        await Assert.ThrowsAsync<ConflictException>(
            () => _service.CompleteAuthorizationAsync("code", state, CancellationToken.None));
    }

    [Theory]
    [InlineData("https://mail.google.com/")]
    [InlineData("https://www.googleapis.com/auth/gmail.readonly https://www.googleapis.com/auth/gmail.send")]
    [InlineData("https://www.googleapis.com/auth/gmail.readonly https://www.googleapis.com/auth/gmail.modify")]
    [InlineData("openid email")]
    [InlineData(null)]
    public async Task Grants_other_than_read_only_are_rejected_and_revoked(string? scope)
    {
        _google.Grant = new GmailTokenGrant("refresh-xyz", scope);

        await Assert.ThrowsAsync<ConflictException>(Connect);

        Assert.Contains("refresh-xyz", _google.Revoked);
        Assert.False((await Stored()).IsConnected);
    }

    [Fact]
    public async Task Missing_refresh_token_is_rejected()
    {
        _google.Grant = new GmailTokenGrant(null, ReadOnly);

        await Assert.ThrowsAsync<ConflictException>(Connect);
        Assert.False((await Stored()).IsConnected);
    }

    [Fact]
    public async Task Credentials_cannot_change_while_connected()
    {
        await Connect();

        await Assert.ThrowsAsync<ConflictException>(SaveCredentials);
        await Assert.ThrowsAsync<ConflictException>(() => _service.RemoveCredentialsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Disconnect_revokes_and_forgets_the_token()
    {
        await Connect();

        var settings = await _service.DisconnectAsync(CancellationToken.None);

        Assert.False(settings.IsConnected);
        Assert.Equal(["refresh-123"], _google.Revoked);
        var stored = await Stored();
        Assert.Null(stored.ProtectedRefreshToken);
        Assert.Null(stored.ConnectedEmail);
        Assert.True(stored.HasClientCredentials);
    }

    [Fact]
    public async Task Disconnect_succeeds_locally_even_if_revocation_fails()
    {
        await Connect();
        _google.FailRevoke = true;

        var settings = await _service.DisconnectAsync(CancellationToken.None);

        Assert.False(settings.IsConnected);
    }

    [Fact]
    public async Task Test_connection_uses_the_saved_search_query()
    {
        await Connect();
        await _service.UpdateSearchQueryAsync("  label:jobs  ", CancellationToken.None);

        var result = await _service.TestConnectionAsync(CancellationToken.None);

        Assert.Equal("label:jobs", _google.LastQuery);
        Assert.Equal(42, result.EstimatedMatches);
        Assert.Single(result.LatestMessages);
        Assert.Equal("refresh-123", _google.LastAccess!.RefreshToken);
    }

    [Fact]
    public async Task Test_connection_requires_a_connection()
    {
        await Assert.ThrowsAsync<ConflictException>(() => _service.TestConnectionAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Revoked_access_surfaces_as_a_conflict()
    {
        await Connect();
        _google.MailboxFailure = new GmailAccessRevokedException();

        var ex = await Assert.ThrowsAsync<GmailAccessRevokedException>(() => _service.TestConnectionAsync(CancellationToken.None));
        Assert.IsAssignableFrom<ConflictException>(ex);
    }

    [Fact]
    public async Task Empty_search_query_is_rejected()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateSearchQueryAsync("  ", CancellationToken.None));
    }
}
