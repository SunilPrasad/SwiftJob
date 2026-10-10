using System.Net;
using System.Text.RegularExpressions;
using SwiftJob.Application.Emails;

namespace SwiftJob.Application.Extraction;

/// <summary>
/// Builds the text sent to the extractor. Links are kept inline as <c>text [url]</c> because the
/// application URL is usually only present as a hyperlink. Output is length-limited to control cost.
/// </summary>
public static partial class EmailTextBuilder
{
    public const int MaxTextLength = 40_000;
    public const int MaxLinks = 300;

    public static EmailContent Build(string? subject, string? sender, DateTime receivedAt, string? bodyText, string? bodyHtml)
    {
        string text;
        var links = new List<string>();

        if (!string.IsNullOrWhiteSpace(bodyHtml))
        {
            var withLinks = Anchor().Replace(bodyHtml, m =>
            {
                var href = WebUtility.HtmlDecode(m.Groups["href"].Value).Trim();
                var label = HtmlText.ToPlainText(m.Groups["text"].Value);
                if (!IsHttp(href))
                {
                    return label;
                }

                links.Add(href);
                return string.IsNullOrWhiteSpace(label) ? $" [{href}] " : $" {label} [{href}] ";
            });
            text = HtmlText.ToPlainText(withLinks);
        }
        else
        {
            text = bodyText ?? string.Empty;
        }

        // Plain-text bodies (and text-only links in HTML) carry bare URLs.
        links.AddRange(BareUrl().Matches(text).Select(m => m.Value.TrimEnd('.', ',', ')', '>')));

        if (text.Length > MaxTextLength)
        {
            text = text[..MaxTextLength] + "\n[... email truncated ...]";
        }

        var distinctLinks = links.Where(IsHttp).Distinct(StringComparer.Ordinal).Take(MaxLinks).ToList();
        return new EmailContent(subject, sender, receivedAt, text, distinctLinks);
    }

    private static bool IsHttp(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

    [GeneratedRegex("""<a\b[^>]*?\bhref\s*=\s*["'](?<href>[^"']+)["'][^>]*>(?<text>.*?)</a\s*>""",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex Anchor();

    [GeneratedRegex(@"https?://[^\s<>\[\]""']+", RegexOptions.IgnoreCase)]
    private static partial Regex BareUrl();
}
