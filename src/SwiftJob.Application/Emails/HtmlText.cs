using System.Net;
using System.Text.RegularExpressions;

namespace SwiftJob.Application.Emails;

/// <summary>Minimal, dependency-free HTML-to-text conversion for displaying untrusted email bodies as plain text.</summary>
public static partial class HtmlText
{
    public static string ToPlainText(string html)
    {
        var text = NonContentBlocks().Replace(html, string.Empty);
        text = LineBreakTags().Replace(text, "\n");
        text = Tags().Replace(text, string.Empty);
        text = WebUtility.HtmlDecode(text);
        text = HorizontalWhitespace().Replace(text, " ");
        text = BlankLines().Replace(text, "\n\n");
        return text.Trim();
    }

    [GeneratedRegex(@"<(script|style|head)\b[^>]*>.*?</\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex NonContentBlocks();

    [GeneratedRegex(@"<\s*(br|/p|/div|/tr|/li|/h[1-6])\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreakTags();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex Tags();

    [GeneratedRegex(@"[ \t ]+")]
    private static partial Regex HorizontalWhitespace();

    [GeneratedRegex(@"\s*\n\s*\n\s*")]
    private static partial Regex BlankLines();
}
