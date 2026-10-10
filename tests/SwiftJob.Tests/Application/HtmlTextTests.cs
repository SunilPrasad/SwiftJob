using SwiftJob.Application.Emails;

namespace SwiftJob.Tests.Application;

public class HtmlTextTests
{
    [Fact]
    public void Removes_scripts_styles_and_tags_and_decodes_entities()
    {
        const string html = """
            <html><head><title>x</title><style>p{color:red}</style></head>
            <body><script>alert('x')</script><p>Senior&nbsp;Developer &amp; Lead</p><br><div>£80,000</div>
            <img src="https://tracker.example.com/pixel.gif"></body></html>
            """;

        var text = HtmlText.ToPlainText(html);

        Assert.Equal("Senior Developer & Lead\n\n£80,000", text);
        Assert.DoesNotContain("alert", text);
        Assert.DoesNotContain("color", text);
        Assert.DoesNotContain("tracker", text);
    }
}
