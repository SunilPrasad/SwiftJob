using SwiftJob.Application.Extraction;

namespace SwiftJob.Tests.Extraction;

public class EmailTextBuilderTests
{
    [Fact]
    public void Html_links_are_kept_inline_and_collected()
    {
        const string html = """
            <html><body><h1>3 new jobs</h1>
            <a href="https://www.linkedin.com/comm/jobs/view/111?trk=a">Senior .NET Developer</a><br>
            <a href='https://example.com/unsubscribe'>Unsubscribe</a>
            <a href="javascript:alert(1)">Bad</a>
            <script>evil()</script></body></html>
            """;

        var content = EmailTextBuilder.Build("Subject", "a@b.com", DateTime.UtcNow, null, html);

        Assert.Contains("Senior .NET Developer [https://www.linkedin.com/comm/jobs/view/111?trk=a]", content.Text);
        Assert.Contains("https://www.linkedin.com/comm/jobs/view/111?trk=a", content.Links);
        Assert.Contains("https://example.com/unsubscribe", content.Links);
        Assert.DoesNotContain(content.Links, l => l.StartsWith("javascript", StringComparison.Ordinal));
        Assert.DoesNotContain("evil", content.Text);
    }

    [Fact]
    public void Plain_text_urls_are_collected()
    {
        var content = EmailTextBuilder.Build(null, null, DateTime.UtcNow,
            "Apply here: https://jobs.example.com/apply/42. Thanks", null);

        Assert.Equal(["https://jobs.example.com/apply/42"], content.Links);
    }

    [Fact]
    public void Very_long_bodies_are_truncated()
    {
        var content = EmailTextBuilder.Build(null, null, DateTime.UtcNow, new string('x', 100_000), null);

        Assert.True(content.Text.Length < EmailTextBuilder.MaxTextLength + 100);
        Assert.EndsWith("[... email truncated ...]", content.Text);
    }
}
