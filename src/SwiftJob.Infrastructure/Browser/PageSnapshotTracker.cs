using System.Text.RegularExpressions;

namespace SwiftJob.Infrastructure.Browser;

/// <summary>
/// Remembers what each element reference (e.g. <c>e8</c>) in the latest page snapshot is, so a click can be
/// checked against the real element rather than the AI's own description of it.
/// </summary>
internal sealed partial class PageSnapshotTracker
{
    private readonly Lock _lock = new();
    private Dictionary<string, (string Role, string Name)> _elements = new(StringComparer.Ordinal);

    public sealed record Element(string Role, string Name);

    /// <summary>Updates from any tool output that contains a page snapshot.</summary>
    public void Update(string? toolOutput)
    {
        if (toolOutput is null || !toolOutput.Contains("[ref=", StringComparison.Ordinal))
        {
            return;
        }

        var elements = new Dictionary<string, (string, string)>(StringComparer.Ordinal);
        foreach (Match m in SnapshotLine().Matches(toolOutput))
        {
            elements[m.Groups["ref"].Value] = (m.Groups["role"].Value, m.Groups["name"].Value);
        }

        if (elements.Count > 0)
        {
            lock (_lock)
            {
                _elements = elements;
            }
        }
    }

    public Element? Lookup(string? reference)
    {
        if (reference is null)
        {
            return null;
        }

        lock (_lock)
        {
            return _elements.TryGetValue(reference, out var e) ? new Element(e.Role, e.Name) : null;
        }
    }

    public static bool IsReference(string? value) => value is not null && Reference().IsMatch(value);

    // e.g. "- button "Submit application" [ref=e8]" or "- textbox "First name *" [ref=e4]: Alex"
    [GeneratedRegex("""-\s+(?<role>[a-z]+)(?:\s+"(?<name>(?:[^"\\]|\\.)*)")?[^\n]*?\[ref=(?<ref>e\d+)\]""")]
    private static partial Regex SnapshotLine();

    [GeneratedRegex(@"^e\d+$")]
    private static partial Regex Reference();
}
