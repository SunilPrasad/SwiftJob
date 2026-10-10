using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SwiftJob.Application.Ai;
using SwiftJob.Application.Browser;
using SwiftJob.Application.Sync;
using SwiftJob.Domain.Ai;
using SwiftJob.Infrastructure.Ai;
using SwiftJob.Infrastructure.Browser;

namespace SwiftJob.Tests.Browser;

/// <summary>
/// End-to-end: Agent Framework agent → guarded tools → Playwright MCP → real browser → a local form, with a
/// scripted model standing in for the AI. Needs Node.js and a browser, so it only runs when
/// SWIFTJOB_BROWSER_TESTS=1 (optionally SWIFTJOB_BROWSER_EXECUTABLE for a specific Chromium).
/// </summary>
public sealed class FormFillAgentIntegrationTests
{
    private static bool Enabled => Environment.GetEnvironmentVariable("SWIFTJOB_BROWSER_TESTS") == "1";

    [Fact]
    public async Task Agent_fills_the_form_attaches_the_cv_and_cannot_submit()
    {
        if (!Enabled)
        {
            return; // opt-in: requires Node.js/npx and a browser
        }

        using var site = new SampleSite();
        var root = Path.Combine(Path.GetTempPath(), $"swiftjob-browser-{Guid.NewGuid():N}");
        var paths = new BrowserPaths(Path.Combine(root, "profile"), Path.Combine(root, "workspace"));
        var options = Options.Create(new BrowserOptions
        {
            Headless = true,
            ExecutablePath = Environment.GetEnvironmentVariable("SWIFTJOB_BROWSER_EXECUTABLE"),
            ExtraArguments = OperatingSystem.IsLinux() ? ["--no-sandbox"] : [],
            DemoFormUrl = site.Url,
        });
        var activity = new ListActivity();
        await using var browser = new PlaywrightMcpBrowser(options, paths, new NoStatus(), activity, NullLoggerFactory.Instance);

        var services = new ServiceCollection()
            .AddSingleton<IAiConfigurationSource>(new FixedConfig())
            .BuildServiceProvider();
        var agent = new FormFillAgent(browser, paths, services.GetRequiredService<IServiceScopeFactory>(),
            new FixedFactory(new ScriptedFormModel()), activity, options, NullLoggerFactory.Instance);


        await agent.StartDemoAsync(CancellationToken.None);
        var deadline = DateTime.UtcNow.AddMinutes(3);
        while (!activity.Lines.Any(l => l.StartsWith("AI finished", StringComparison.Ordinal) || l.StartsWith("AI stopped", StringComparison.Ordinal)))
        {
            Assert.True(DateTime.UtcNow < deadline, "Agent did not finish: " + string.Join(" | ", activity.Lines));
            await Task.Delay(200);
        }

        var snapshot = await browser.CallToolAsync("browser_snapshot", new Dictionary<string, object?>(), CancellationToken.None);

        Assert.Contains("AI finished: Filled first name, last name, email and CV.", activity.Lines);
        Assert.Contains(activity.Lines, l => l.Contains("Submit application button — blocked (submitting is yours)", StringComparison.Ordinal));
        Assert.Contains("Alex", snapshot);
        Assert.Contains("alex.demo@example.com", snapshot);
        // The page outline does not show file names, so ask the page directly (test-only; the AI has no such tool).
        var attached = await browser.CallToolAsync("browser_evaluate", new Dictionary<string, object?>
        {
            ["function"] = "() => document.querySelector('#cv').files[0]?.name ?? 'none'",
        }, CancellationToken.None);
        Assert.Contains(DemoApplicant.CvFileName, attached);
        Assert.DoesNotContain("Application received", snapshot); // never submitted
        Assert.Equal(0, site.Submissions);
        Assert.Equal(BrowserOwner.User, browser.Status.Controller);

        await browser.CloseAsync(CancellationToken.None);
        try { Directory.Delete(root, true); } catch (IOException) { }
    }

    /// <summary>Plays the role of the LLM: snapshot, then fill + upload, then try to submit, then report.</summary>
    private sealed partial class ScriptedFormModel : IChatClient
    {
        private int _turn;

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var history = messages.ToList();
            var toolOutput = string.Join("\n", history.SelectMany(m => m.Contents).OfType<FunctionResultContent>()
                .Select(r => r.Result is JsonElement j ? j.ToString() : r.Result?.ToString()));
            _turn++;

            ChatMessage reply = _turn switch
            {
                1 => Calls(new FunctionCallContent("c1", "browser_snapshot", new Dictionary<string, object?>())),
                2 => Calls(
                    Type("c2", "First name", Ref(toolOutput, "First name"), DemoApplicant.FirstName),
                    Type("c3", "Last name", Ref(toolOutput, "Last name"), DemoApplicant.LastName),
                    Type("c4", "Email", Ref(toolOutput, "Email"), DemoApplicant.Email),
                    new FunctionCallContent("c5", "browser_click", new Dictionary<string, object?>
                    {
                        ["element"] = "CV file input", ["target"] = ButtonRef(toolOutput, "CV"),
                    })),
                3 => Calls(new FunctionCallContent("c6", "browser_file_upload", new Dictionary<string, object?>
                {
                    ["paths"] = new[] { CvPath(history) },
                })),
                4 => Calls(new FunctionCallContent("c7", "browser_click", new Dictionary<string, object?>
                {
                    ["target"] = ButtonRef(toolOutput, "Submit application"), // no description: the guard must still see it is Submit
                })),
                _ => new ChatMessage(ChatRole.Assistant, "Filled first name, last name, email and CV."),
            };
            return Task.FromResult(new ChatResponse(reply));
        }

        private static ChatMessage Calls(params AIContent[] calls) => new(ChatRole.Assistant, calls);

        private static FunctionCallContent Type(string id, string label, string reference, string text) =>
            new(id, "browser_type", new Dictionary<string, object?> { ["element"] = $"{label} textbox", ["target"] = reference, ["text"] = text });

        private static string Ref(string snapshot, string label) =>
            Regex.Match(snapshot, $"textbox \"{Regex.Escape(label)}[^\"]*\"[^\\n]*?\\[ref=(e\\d+)\\]").Groups[1].Value is { Length: > 0 } r
                ? r : throw new InvalidOperationException($"No ref for {label}: {snapshot}");

        private static string ButtonRef(string snapshot, string label) =>
            Regex.Match(snapshot, $"(?:button|textbox)? ?\"{Regex.Escape(label)}[^\"]*\"[^\\n]*?\\[ref=(e\\d+)\\]").Groups[1].Value is { Length: > 0 } r
                ? r : throw new InvalidOperationException($"No ref for {label}: {snapshot}");

        private static string CvPath(IEnumerable<ChatMessage> history) =>
            Regex.Match(string.Join("\n", history.Select(m => m.Text)), @"CV file to upload: (.+)").Groups[1].Value.Trim();

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var update in (await GetResponseAsync(messages, options, cancellationToken)).ToChatResponseUpdates())
            {
                yield return update;
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    private sealed class SampleSite : IDisposable
    {
        private readonly HttpListener _listener = new();
        private int _submissions;

        public SampleSite()
        {
            var port = Random.Shared.Next(20000, 40000);
            Url = $"http://localhost:{port}/apply";
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _listener.Start();
            _ = Task.Run(Serve);
        }

        public string Url { get; }
        public int Submissions => _submissions;

        private async Task Serve()
        {
            while (_listener.IsListening)
            {
                HttpListenerContext ctx;
                try { ctx = await _listener.GetContextAsync(); } catch { return; }
                if (ctx.Request.HttpMethod == "POST")
                {
                    Interlocked.Increment(ref _submissions);
                }

                var html = ctx.Request.HttpMethod == "POST"
                    ? "<h1>Application received</h1>"
                    : """
                      <!doctype html><html><body><h1>Apply</h1>
                      <form method="post" enctype="multipart/form-data">
                        <label for="f">First name *</label><input id="f" name="f" type="text">
                        <label for="l">Last name *</label><input id="l" name="l" type="text">
                        <label for="e">Email *</label><input id="e" name="e" type="email">
                        <label for="cv">CV</label><input id="cv" name="cv" type="file">
                        <button type="submit">Submit application</button>
                      </form></body></html>
                      """;
                var bytes = Encoding.UTF8.GetBytes(html);
                ctx.Response.ContentType = "text/html";
                await ctx.Response.OutputStream.WriteAsync(bytes);
                ctx.Response.Close();
            }
        }

        public void Dispose() => _listener.Close();
    }

    private sealed class ListActivity : ISyncStatusPublisher
    {
        private readonly List<string> _lines = [];

        public IReadOnlyList<string> Lines { get { lock (_lines) { return _lines.ToList(); } } }
        public SyncStatusDto Current => SyncStatusDto.Idle;
        public IReadOnlyList<SyncLogEntry> RecentLog => [];
        public Task PublishAsync(SyncStatusDto status, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task LogAsync(SyncLogLevel level, string message)
        {
            lock (_lines) { _lines.Add(message); }
            return Task.CompletedTask;
        }
    }

    private sealed class NoStatus : IBrowserStatusPublisher
    {
        public Task PublishAsync(BrowserStatusDto status) => Task.CompletedTask;
    }

    private sealed class FixedConfig : IAiConfigurationSource
    {
        public Task<AiConnectionConfig> GetRequiredAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new AiConnectionConfig(AiProvider.Anthropic, "scripted", "key", null));
    }

    private sealed class FixedFactory(IChatClient client) : IAiChatClientFactory
    {
        public IChatClient Create(AiConnectionConfig config) => client;
    }
}
