using SwiftJob.Application.Extraction;

namespace SwiftJob.Tests.Extraction;

public class EmailTextBuilderTests
{
    [Fact]
    public void Html_links_become_short_references_and_resolve_back_to_urls()
    {
        const string html = """
            <html><body><h1>3 new jobs</h1>
            <a href="https://www.linkedin.com/comm/jobs/view/111?trk=a">Senior .NET Developer</a><br>
            <a href='https://example.com/unsubscribe'>Unsubscribe</a>
            <a href="javascript:alert(1)">Bad</a>
            <script>evil()</script></body></html>
            """;

        var content = EmailTextBuilder.Build("Subject", "a@b.com", DateTime.UtcNow, null, html);

        Assert.Contains("Senior .NET Developer [L1]", content.Text);
        Assert.Contains("Unsubscribe [L2]", content.Text);
        Assert.DoesNotContain("linkedin.com", content.Text); // long tracking URLs are not sent to the model
        Assert.Equal(["https://www.linkedin.com/comm/jobs/view/111?trk=a", "https://example.com/unsubscribe"], content.Links);
        Assert.Equal("https://www.linkedin.com/comm/jobs/view/111?trk=a", content.ResolveLink("L1"));
        Assert.Equal("https://example.com/unsubscribe", content.ResolveLink("[L2]"));
        Assert.Equal("https://example.com/unsubscribe", content.ResolveLink("https://example.com/unsubscribe"));
        Assert.Null(content.ResolveLink("L3"));
        Assert.Null(content.ResolveLink("L0"));
        Assert.Null(content.ResolveLink("https://evil.example.com"));
        Assert.Null(content.ResolveLink(null));
        Assert.DoesNotContain("evil", content.Text);
    }

    [Fact]
    public void Plain_text_urls_are_collected()
    {
        var content = EmailTextBuilder.Build(null, null, DateTime.UtcNow,
            "Apply here: https://jobs.example.com/apply/42. Thanks", null);

        Assert.Equal(["https://jobs.example.com/apply/42"], content.Links);
        Assert.Equal("Apply here: [L1]. Thanks", content.Text);
    }

    [Fact]
    public void Repeated_links_share_one_reference()
    {
        const string html = """<a href="https://x.example.com/j/1">Title</a> <a href="https://x.example.com/j/1">Apply</a>""";

        var content = EmailTextBuilder.Build(null, null, DateTime.UtcNow, null, html);

        Assert.Single(content.Links);
        Assert.Equal("Title [L1] Apply [L1]", content.Text);
    }

    [Fact]
    public void Very_long_bodies_are_truncated()
    {
        var content = EmailTextBuilder.Build(null, null, DateTime.UtcNow, new string('x', 100_000), null);

        Assert.True(content.Text.Length < EmailTextBuilder.MaxTextLength + 100);
        Assert.EndsWith("[... email truncated ...]", content.Text);
    }
}
