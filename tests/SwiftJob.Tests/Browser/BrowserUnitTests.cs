using Microsoft.Extensions.AI;
using SwiftJob.Application.Browser;
using SwiftJob.Infrastructure.Browser;

namespace SwiftJob.Tests.Browser;

public class BrowserUnitTests
{
    [Theory]
    [InlineData("browser_click", "Submit application button", true)]
    [InlineData("browser_click", "Apply now link", true)]
    [InlineData("browser_click", "Send button", true)]
    [InlineData("browser_click", "Confirm email textbox", false)]
    [InlineData("browser_click", "First name textbox", false)]
    [InlineData("browser_click", "CV file input", false)]
    [InlineData("browser_type", "Submit application button", false)]
    public void Submit_like_clicks_are_detected(string tool, string element, bool expected) =>
        Assert.Equal(expected, GuardedBrowserTool.IsSubmitAttempt(tool, element, new AIFunctionArguments()));

    [Fact]
    public void Pressing_enter_counts_as_submitting() =>
        Assert.True(GuardedBrowserTool.IsSubmitAttempt("browser_press_key", null,
            new AIFunctionArguments { ["key"] = "Enter" }));

    [Fact]
    public void Agent_is_only_offered_page_reading_and_form_filling_tools()
    {
        Assert.DoesNotContain("browser_navigate", GuardedBrowserTool.AllowedTools);
        Assert.DoesNotContain("browser_evaluate", GuardedBrowserTool.AllowedTools);
        Assert.DoesNotContain("browser_run_code", GuardedBrowserTool.AllowedTools);
        Assert.DoesNotContain("browser_tabs", GuardedBrowserTool.AllowedTools);
        Assert.DoesNotContain("browser_press_key", GuardedBrowserTool.AllowedTools);
        Assert.Contains("browser_fill_form", GuardedBrowserTool.AllowedTools);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com")]
    [InlineData("not a url")]
    [InlineData(null)]
    public void Only_http_urls_can_be_opened(string? url) =>
        Assert.Throws<ArgumentException>(() => BrowserUrl.Validate(url));

    [Fact]
    public void Snapshot_tracker_knows_what_each_reference_is()
    {
        var tracker = new PageSnapshotTracker();
        tracker.Update("""
            ### Snapshot
            - generic [active] [ref=e1]:
              - textbox "First name *" [ref=e4]: Alex
              - button "Submit application" [ref=e8]
              - link "Apply now" [ref=e9] [cursor=pointer]
            """);

        Assert.Equal(new PageSnapshotTracker.Element("button", "Submit application"), tracker.Lookup("e8"));
        Assert.Equal("textbox", tracker.Lookup("e4")!.Role);
        Assert.Equal("Apply now", tracker.Lookup("e9")!.Name);
        Assert.Null(tracker.Lookup("e99"));
        Assert.True(PageSnapshotTracker.IsReference("e12"));
        Assert.False(PageSnapshotTracker.IsReference("button:has-text('Submit')"));

        tracker.Update("No snapshot in this output");
        Assert.NotNull(tracker.Lookup("e8")); // unrelated output keeps the last snapshot
    }

    [Fact]
    public void Demo_cv_is_a_pdf()
    {
        var bytes = DemoPdf.Create("Alex Demo (CV)");
        var text = System.Text.Encoding.ASCII.GetString(bytes);

        Assert.StartsWith("%PDF-1.4", text);
        Assert.EndsWith("%%EOF\n", text);
        Assert.Contains("\\(CV\\)", text);
    }
}
