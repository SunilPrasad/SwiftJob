namespace SwiftJob.Application.Extraction;

/// <summary>Identifies the job board from the sender address (reliable) before falling back to the extractor's guess.</summary>
public static class JobSourceDetector
{
    private static readonly (string Domain, string Source)[] KnownSenders =
    [
        ("linkedin.com", "LinkedIn"),
        ("indeed.com", "Indeed"),
        ("indeed.co.uk", "Indeed"),
        ("reed.co.uk", "Reed"),
        ("totaljobs.com", "Totaljobs"),
        ("jobserve.com", "JobServe"),
        ("cv-library.co.uk", "CV-Library"),
        ("glassdoor.com", "Glassdoor"),
        ("monster.co.uk", "Monster"),
        ("cwjobs.co.uk", "CWJobs"),
        ("hays.co.uk", "Hays"),
        ("hays.com", "Hays"),
        ("michaelpage.co.uk", "Michael Page"),
        ("roberthalf.co.uk", "Robert Half"),
    ];

    public const string Unknown = "Email";

    public static string Detect(string? sender, string? extractorGuess)
    {
        var address = ExtractAddress(sender);
        var domain = address is null ? null : address[(address.IndexOf('@') + 1)..];
        if (domain is not null)
        {
            foreach (var (known, source) in KnownSenders)
            {
                if (domain.Equals(known, StringComparison.OrdinalIgnoreCase)
                    || domain.EndsWith("." + known, StringComparison.OrdinalIgnoreCase))
                {
                    return source;
                }
            }
        }

        var guess = extractorGuess?.Trim();
        return string.IsNullOrEmpty(guess) ? Unknown : guess.Length > 100 ? guess[..100] : guess;
    }

    /// <summary>"Name &lt;a@b.com&gt;" or "a@b.com" → "a@b.com".</summary>
    public static string? ExtractAddress(string? sender)
    {
        if (string.IsNullOrWhiteSpace(sender))
        {
            return null;
        }

        var start = sender.LastIndexOf('<');
        var end = sender.LastIndexOf('>');
        var address = start >= 0 && end > start ? sender[(start + 1)..end] : sender;
        address = address.Trim();
        return address.Contains('@') ? address.ToLowerInvariant() : null;
    }
}
