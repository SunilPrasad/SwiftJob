using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SwiftJob.Application.Ai;
using SwiftJob.Application.Common;
using SwiftJob.Domain.Ai;
using SwiftJob.Infrastructure.Ai;
using SwiftJob.Infrastructure.Persistence;
using SwiftJob.Tests.Gmail;
using SwiftJob.Tests.TestSupport;

namespace SwiftJob.Tests.Ai;

public sealed class AiSettingsServiceTests : IAsyncLifetime
{
    private readonly FakeProbe _probe = new();
    private TestDatabase _database = null!;
    private AppDbContext _db = null!;
    private AiSettingsService _service = null!;

    public async Task InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        _db = _database.CreateContext();
        _service = new AiSettingsService(_db, new FakeProtector(), _probe, NullLogger<AiSettingsService>.Instance);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _database.DisposeAsync();
    }

    private Task<AiSettingsDto> Save(AiProvider provider, string model = "m", string? key = null, string? endpoint = null, bool clear = false) =>
        _service.SaveAsync(new SaveAiSettingsRequest(provider, model, endpoint, key, clear), default);

    [Fact]
    public async Task Not_configured_by_default_and_lists_providers()
    {
        var settings = await _service.GetAsync(default);

        Assert.False(settings.IsConfigured);
        Assert.Equal(3, settings.Providers.Count);
        await Assert.ThrowsAsync<AiNotConfiguredException>(() => _service.GetRequiredAsync(default));
    }

    [Fact]
    public async Task Api_key_is_encrypted_never_returned_and_kept_when_omitted()
    {
        await Save(AiProvider.Anthropic, "claude-opus-5-5", "sk-ant-secret");
        var updated = await Save(AiProvider.Anthropic, "claude-sonnet-5-5");

        Assert.True(updated.HasApiKey);
        Assert.Equal("claude-sonnet-5-5", updated.Model);
        await using var db = _database.CreateContext();
        Assert.DoesNotContain("sk-ant-secret", (await db.AiSettings.SingleAsync()).ProtectedApiKey);
        var config = await _service.GetRequiredAsync(default);
        Assert.Equal("sk-ant-secret", config.ApiKey);
        Assert.DoesNotContain("sk-ant-secret", config.ToString());
    }

    [Fact]
    public async Task Hosted_providers_require_a_key()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Save(AiProvider.OpenAI, "gpt-5"));
    }

    [Fact]
    public async Task Switching_provider_without_a_new_key_drops_the_old_key()
    {
        await Save(AiProvider.Anthropic, "claude-opus-5-5", "sk-ant-secret");

        await Assert.ThrowsAsync<ArgumentException>(() => Save(AiProvider.OpenAI, "gpt-5"));
        var switched = await Save(AiProvider.OpenAICompatible, "llama3.1", endpoint: "http://localhost:11434/v1/");

        Assert.False(switched.HasApiKey);
        Assert.True(switched.IsConfigured);
        Assert.Equal("http://localhost:11434/v1", switched.Endpoint);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not a url")]
    [InlineData("ftp://example.com")]
    public async Task Compatible_provider_requires_a_valid_endpoint(string? endpoint)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Save(AiProvider.OpenAICompatible, "llama", endpoint: endpoint));
    }

    [Fact]
    public async Task Test_reports_success_and_failure_without_throwing()
    {
        await Save(AiProvider.Anthropic, "claude-opus-5-5", "sk-ant-secret");

        Assert.True((await _service.TestAsync(default)).Success);
        _probe.Fail = true;
        var failed = await _service.TestAsync(default);
        Assert.False(failed.Success);
        Assert.Contains("invalid x-api-key", failed.Message);
    }

    [Theory]
    [InlineData(AiProvider.Anthropic, "sk-ant-x", null)]
    [InlineData(AiProvider.OpenAI, "sk-x", null)]
    [InlineData(AiProvider.OpenAICompatible, null, "http://localhost:11434/v1")]
    public void Factory_creates_a_chat_client_for_every_provider(AiProvider provider, string? key, string? endpoint)
    {
        using var client = new AiChatClientFactory().Create(new AiConnectionConfig(provider, "some-model", key, endpoint));

        Assert.NotNull(client);
    }

    private sealed class FakeProbe : IAiConnectionProbe
    {
        public bool Fail { get; set; }

        public Task<string> ProbeAsync(AiConnectionConfig config, CancellationToken cancellationToken) =>
            Fail ? throw new ExternalServiceException("invalid x-api-key") : Task.FromResult("OK");
    }
}
