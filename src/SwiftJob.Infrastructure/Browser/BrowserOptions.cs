namespace SwiftJob.Infrastructure.Browser;

/// <summary>Settings for the SwiftJob-controlled browser (Playwright MCP).</summary>
public sealed class BrowserOptions
{
    public const string SectionName = "Browser";

    /// <summary>npm package that provides the Playwright MCP server (version pinned).</summary>
    public string McpPackage { get; set; } = "@playwright/mcp@0.0.82";

    /// <summary>
    /// Browsers to try in order: installed Google Chrome, Microsoft Edge, then Playwright's own Chromium
    /// (the last needs a one-time <c>npx playwright install chromium</c>).
    /// </summary>
    public string[] Channels { get; set; } = ["chrome", "msedge", "chromium"];

    /// <summary>Optional explicit browser executable (overrides <see cref="Channels"/>).</summary>
    public string? ExecutablePath { get; set; }

    public bool Headless { get; set; }

    /// <summary>Private browser profile, so logins to job sites are remembered. Relative to the content root.</summary>
    public string ProfilePath { get; set; } = Path.Combine("App_Data", "browser-profile");

    /// <summary>The only folder the browser may read files from (e.g. CVs to upload). Relative to the content root.</summary>
    public string WorkspacePath { get; set; } = Path.Combine("App_Data", "browser-workspace");

    /// <summary>Additional Playwright MCP arguments (e.g. "--no-sandbox" on Linux as root).</summary>
    public string[] ExtraArguments { get; set; } = [];

    /// <summary>URL of the local sample job form used by the prototype.</summary>
    public string DemoFormUrl { get; set; } = "http://localhost:5080/demo/job-form";
}
