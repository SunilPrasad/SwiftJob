using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using SwiftJob.Application.Browser;
using SwiftJob.Application.Common;
using SwiftJob.Application.Sync;

namespace SwiftJob.Infrastructure.Browser;

/// <summary>
/// One visible browser window controlled through Microsoft's Playwright MCP server, started on demand as a
/// child process (<c>npx @playwright/mcp</c>). The same browser is used by the user and, later, the AI.
/// </summary>
internal sealed partial class PlaywrightMcpBrowser(
    IOptions<BrowserOptions> options,
    BrowserPaths paths,
    IBrowserStatusPublisher statusPublisher,
    ISyncStatusPublisher activity,
    ILoggerFactory loggerFactory) : IBrowserSessionService, IAsyncDisposable
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<PlaywrightMcpBrowser>();
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private McpClient? _client;
    private int _channelIndex;
    private volatile BrowserStatusDto _status = new(false, null, BrowserOwner.None, "Browser not started.");

    public BrowserStatusDto Status => _status;

    public async Task OpenAsync(string url, CancellationToken cancellationToken)
    {
        var uri = BrowserUrl.Validate(url);
        await activity.LogAsync(SyncLogLevel.Info, $"Browser: opening {uri.Host}…");

        var result = await CallToolAsync("browser_navigate", new Dictionary<string, object?> { ["url"] = uri.ToString() }, cancellationToken);
        var currentUrl = PageUrl().Match(result) is { Success: true } m ? m.Groups[1].Value : uri.ToString();
        await SetStatusAsync(new BrowserStatusDto(true, currentUrl, _status.Controller == BrowserOwner.Agent
            ? BrowserOwner.Agent : BrowserOwner.User, "Page open in the SwiftJob browser window."));
        await activity.LogAsync(SyncLogLevel.Info, $"Browser: opened {currentUrl}");
    }

    public async Task CloseAsync(CancellationToken cancellationToken)
    {
        if (_client is not null)
        {
            try
            {
                await CallToolAsync("browser_close", new Dictionary<string, object?>(), cancellationToken, startIfNeeded: false);
            }
            catch (ExternalServiceException ex)
            {
                _logger.LogDebug(ex, "Closing the browser failed");
            }
        }

        await SetStatusAsync(new BrowserStatusDto(false, null, BrowserOwner.None, "Browser closed."));
        await activity.LogAsync(SyncLogLevel.Info, "Browser: closed.");
    }

    /// <summary>The browser tools offered by Playwright MCP (for the agent).</summary>
    public async Task<IReadOnlyList<McpClientTool>> GetToolsAsync(CancellationToken cancellationToken)
    {
        var client = await EnsureStartedAsync(cancellationToken);
        return (await client.ListToolsAsync(cancellationToken: cancellationToken)).ToList();
    }

    public Task SetControllerAsync(BrowserOwner controller, string message) =>
        SetStatusAsync(_status with { Controller = controller, Message = message });

    /// <summary>Calls a Playwright MCP tool and returns its text output.</summary>
    /// <exception cref="ExternalServiceException">The tool failed.</exception>
    public async Task<string> CallToolAsync(
        string tool, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken, bool startIfNeeded = true)
    {
        for (var attempt = 0; ; attempt++)
        {
            var client = startIfNeeded ? await EnsureStartedAsync(cancellationToken) : _client!;
            CallToolResult result;
            try
            {
                result = await client.CallToolAsync(tool, arguments, cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await ResetClientAsync();
                throw new ExternalServiceException($"The browser did not respond: {ex.Message}", ex);
            }

            var text = TextOf(result);
            if (result.IsError != true)
            {
                return text;
            }

            // The configured browser is not installed: try the next one (Chrome → Edge → Chromium).
            if (startIfNeeded && IsBrowserMissing(text) && _channelIndex + 1 < options.Value.Channels.Length
                && options.Value.ExecutablePath is null && attempt < options.Value.Channels.Length)
            {
                await activity.LogAsync(SyncLogLevel.Warning,
                    $"Browser: '{options.Value.Channels[_channelIndex]}' is not installed, trying '{options.Value.Channels[_channelIndex + 1]}'.");
                _channelIndex++;
                await ResetClientAsync();
                continue;
            }

            throw new ExternalServiceException(
                IsBrowserMissing(text)
                    ? "No supported browser was found. Install Google Chrome or Microsoft Edge, or run 'npx playwright install chromium'."
                    : NetworkError().Match(text) is { Success: true } net
                        ? $"The page could not be loaded ({net.Value}). Check the link and your internet connection."
                        : $"Browser action '{tool}' failed: {Shorten(text)}");
        }
    }

    private async Task<McpClient> EnsureStartedAsync(CancellationToken cancellationToken)
    {
        if (_client is not null)
        {
            return _client;
        }

        await _startLock.WaitAsync(cancellationToken);
        try
        {
            if (_client is not null)
            {
                return _client;
            }

            var o = options.Value;
            Directory.CreateDirectory(paths.Profile);
            Directory.CreateDirectory(paths.Workspace);

            var arguments = new List<string>
            {
                "-y", o.McpPackage,
                "--user-data-dir", paths.Profile,
                "--output-dir", Path.Combine(paths.Workspace, "output"),
            };
            if (o.ExecutablePath is { Length: > 0 } exe)
            {
                arguments.AddRange(["--executable-path", exe]);
            }
            else
            {
                arguments.AddRange(["--browser", o.Channels[Math.Min(_channelIndex, o.Channels.Length - 1)]]);
            }

            if (o.Headless)
            {
                arguments.Add("--headless");
            }

            arguments.AddRange(o.ExtraArguments);

            // npx is a script on Windows, so it is started through cmd.exe there.
            var (command, prefix) = OperatingSystem.IsWindows() ? ("cmd.exe", new[] { "/c", "npx" }) : ("npx", []);
            var transport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = "playwright",
                Command = command,
                Arguments = [.. prefix, .. arguments],
                WorkingDirectory = paths.Workspace, // the only folder the browser may read files from
            }, loggerFactory);

            await activity.LogAsync(SyncLogLevel.Info, "Browser: starting Playwright (first start can take a minute)…");
            try
            {
                _client = await McpClient.CreateAsync(transport, loggerFactory: loggerFactory, cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new ExternalServiceException(
                    $"Could not start Playwright MCP ({ex.Message}). Make sure Node.js (npx) is installed and on PATH.", ex);
            }

            return _client;
        }
        finally
        {
            _startLock.Release();
        }
    }

    private async Task ResetClientAsync()
    {
        var client = Interlocked.Exchange(ref _client, null);
        if (client is not null)
        {
            try
            {
                await client.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Disposing the MCP client failed");
            }
        }
    }

    private async Task SetStatusAsync(BrowserStatusDto status)
    {
        _status = status;
        try
        {
            await statusPublisher.PublishAsync(status);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not publish browser status");
        }
    }

    internal static string TextOf(CallToolResult result) =>
        string.Join("\n", result.Content.OfType<TextContentBlock>().Select(c => c.Text));

    private static bool IsBrowserMissing(string text) =>
        text.Contains("is not found", StringComparison.OrdinalIgnoreCase)
        || text.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase)
        || text.Contains("not installed", StringComparison.OrdinalIgnoreCase);

    private static string Shorten(string text) => text.Length > 400 ? text[..400] + "…" : text;

    public async ValueTask DisposeAsync()
    {
        await ResetClientAsync();
        _startLock.Dispose();
    }

    [GeneratedRegex(@"Page URL:\s*(\S+)")]
    private static partial Regex PageUrl();

    [GeneratedRegex(@"net::ERR_[A-Z_]+")]
    private static partial Regex NetworkError();
}

/// <summary>Absolute folders used by the browser.</summary>
internal sealed record BrowserPaths(string Profile, string Workspace);
