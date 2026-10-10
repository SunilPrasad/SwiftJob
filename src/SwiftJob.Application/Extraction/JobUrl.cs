using System.Text.RegularExpressions;
using System.Web;

namespace SwiftJob.Application.Extraction;

/// <summary>Canonical form of a job link plus the job board's own id when it can be read from the URL.</summary>
public sealed record CanonicalJobUrl(string Url, string? SourceJobId);

/// <summary>
/// Removes tracking parameters and normalises well-known job-board URLs <b>without making any network
/// request</b> (following tracking redirects would notify trackers and could hit unsafe hosts).
/// </summary>
public static partial class JobUrl
{
    private static readonly HashSet<string> TrackingParameters = new(StringComparer.OrdinalIgnoreCase)
    {
        "trk", "trkEmail", "trackingId", "refId", "lipi", "midToken", "midSig", "eid", "otpToken", "ssid",
        "from", "src", "source", "ref", "referrer", "cmp", "campaign", "mc_cid", "mc_eid", "gclid", "fbclid",
        "msclkid", "_hsenc", "_hsmi", "sid", "tk", "alid", "rgtk", "acct", "camk", "xkcb",
    };

    public static CanonicalJobUrl? Canonicalise(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)
            || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return null;
        }

        var host = uri.Host.ToLowerInvariant();
        var query = HttpUtility.ParseQueryString(uri.Query);
        var keys = query.AllKeys.Where(k => k is not null).Cast<string>().ToList();

        if (host.EndsWith("linkedin.com", StringComparison.Ordinal) && LinkedInJob().Match(uri.AbsolutePath) is { Success: true } li)
        {
            var id = li.Groups["id"].Value;
            return new CanonicalJobUrl($"https://www.linkedin.com/jobs/view/{id}", id);
        }

        if (host.Contains("indeed.", StringComparison.Ordinal) && query["jk"] is { Length: > 0 } jk)
        {
            return new CanonicalJobUrl($"https://{host}/viewjob?jk={Uri.EscapeDataString(jk)}", jk);
        }

        if (host.EndsWith("reed.co.uk", StringComparison.Ordinal) && TrailingNumericId().Match(uri.AbsolutePath) is { Success: true } reed)
        {
            return new CanonicalJobUrl($"https://www.reed.co.uk/jobs/{reed.Groups["id"].Value}", reed.Groups["id"].Value);
        }

        if (host.EndsWith("totaljobs.com", StringComparison.Ordinal) && TrailingNumericId().Match(uri.AbsolutePath) is { Success: true } tj)
        {
            return new CanonicalJobUrl($"https://www.totaljobs.com/job/{tj.Groups["id"].Value}", tj.Groups["id"].Value);
        }

        if (host.EndsWith("jobserve.com", StringComparison.Ordinal) && JobServeId().Match(uri.PathAndQuery) is { Success: true } js)
        {
            return new CanonicalJobUrl($"https://www.jobserve.com/gb/en/{js.Groups["id"].Value}.jsjob", js.Groups["id"].Value);
        }

        // Generic: lower-case host, no fragment, no tracking parameters, stable parameter order.
        var kept = keys
            .Where(k => !TrackingParameters.Contains(k) && !k.StartsWith("utm_", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal)
            .SelectMany(k => (query.GetValues(k) ?? []).Select(v => $"{Uri.EscapeDataString(k)}={Uri.EscapeDataString(v)}"))
            .ToList();
        var path = uri.AbsolutePath.Length > 1 ? uri.AbsolutePath.TrimEnd('/') : uri.AbsolutePath;
        var port = uri.IsDefaultPort ? string.Empty : $":{uri.Port}";
        var canonical = $"{uri.Scheme}://{host}{port}{path}{(kept.Count > 0 ? "?" + string.Join('&', kept) : string.Empty)}";
        return new CanonicalJobUrl(canonical, null);
    }

    [GeneratedRegex(@"/jobs/view/(?:[^/]*-)?(?<id>\d{6,})", RegexOptions.IgnoreCase)]
    private static partial Regex LinkedInJob();

    [GeneratedRegex(@"/(?<id>\d{5,})/?$")]
    private static partial Regex TrailingNumericId();

    [GeneratedRegex(@"(?<id>[A-F0-9]{16,})\.jsjob", RegexOptions.IgnoreCase)]
    private static partial Regex JobServeId();
}
