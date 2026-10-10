using System.Net;
using System.Text.RegularExpressions;
using SwiftJob.Application.Emails;

namespace SwiftJob.Application.Extraction;

/// <summary>
/// Builds the text sent to the extractor. Every link is replaced by a short reference such as <c>[L3]</c>
/// placed after its text, and the real URLs are kept in <see cref="EmailContent.Links"/> (L1 = first).
/// Job-alert links carry hundreds of characters of tracking data, so references cut the prompt size
/// (and therefore time and cost) dramatically while still letting the model point at the right link.
/// </summary>
public static partial class EmailTextBuilder
{
    public const int MaxTextLength = 40_000;
    public const int MaxLinks = 300;

    public static EmailContent Build(string? subject, string? sender, DateTime receivedAt, string? bodyText, string? bodyHtml)
    {
        var links = new List<string>();
        var references = new Dictionary<string, string>(StringComparer.Ordinal);

        string Reference(string url)
        {
            if (!references.TryGetValue(url, out var reference))
            {
                if (links.Count >= MaxLinks)
                {
                    return string.Empty;
                }

                links.Add(url);
                reference = $"[L{links.Count}]";
                references[url] = reference;
            }

            return reference;
        }

        string text;
        if (!string.IsNullOrWhiteSpace(bodyHtml))
        {
            var withLinks = Anchor().Replace(bodyHtml, m =>
            {
                var href = WebUtility.HtmlDecode(m.Groups["href"].Value).Trim();
                var label = HtmlText.ToPlainText(m.Groups["text"].Value);
                return IsHttp(href) ? $" {label} {Reference(href)} " : label;
            });
            text = HtmlText.ToPlainText(withLinks);
        }
        else
        {
            text = bodyText ?? string.Empty;
        }

        // Plain-text bodies (and URLs written as text in HTML) carry bare URLs.
        text = BareUrl().Replace(text, m =>
        {
            var url = m.Value.TrimEnd('.', ',', ')', '>');
            var trailing = m.Value[url.Length..];
            return IsHttp(url) ? Reference(url) + trailing : m.Value;
        });

        if (text.Length > MaxTextLength)
        {
            text = text[..MaxTextLength] + "\n[... email truncated ...]";
        }

        return new EmailContent(subject, sender, receivedAt, text, links);
    }

    private static bool IsHttp(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

    [GeneratedRegex("""<a\b[^>]*?\bhref\s*=\s*["'](?<href>[^"']+)["'][^>]*>(?<text>.*?)</a\s*>""",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex Anchor();

    [GeneratedRegex(@"https?://[^\s<>\[\]""']+", RegexOptions.IgnoreCase)]
    private static partial Regex BareUrl();
}
