using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using SwiftJob.Application.Sync;

namespace SwiftJob.Infrastructure.Browser;

/// <summary>
/// Wraps a Playwright MCP tool before the AI may use it. Every call is logged, stops immediately when the
/// user takes control, and anything that could submit an application is refused: submitting is the user's job.
/// </summary>
internal sealed partial class GuardedBrowserTool(
    McpClientTool inner, CancellationToken runToken, ISyncStatusPublisher activity, PageSnapshotTracker page)
    : DelegatingAIFunction(inner)
{
    /// <summary>The only tools the AI is offered: read the page and fill in fields. No navigation, scripts or tabs.</summary>
    public static readonly HashSet<string> AllowedTools = new(StringComparer.Ordinal)
    {
        "browser_snapshot",
        "browser_click",
        "browser_type",
        "browser_fill_form",
        "browser_select_option",
        "browser_file_upload",
        "browser_wait_for",
        "browser_hover",
    };

    internal const string SubmitBlockedMessage =
        "BLOCKED by SwiftJob: submitting the application is reserved for the user. Do not try again. " +
        "Stop now and report which fields you filled and which still need the user.";

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        runToken.ThrowIfCancellationRequested(); // the user took control: no further actions

        var target = Argument(arguments, "target");
        var actual = page.Lookup(target);
        // Describe the element from the page itself when possible; the AI's own description is optional.
        var element = actual is null ? Argument(arguments, "element") : $"{actual.Name} {actual.Role}".Trim();

        if (Name is "browser_click" or "browser_hover" or "browser_type" or "browser_select_option"
            && !PageSnapshotTracker.IsReference(target))
        {
            return "BLOCKED by SwiftJob: use an element reference (ref) from the latest browser_snapshot as the target.";
        }

        if (IsSubmitAttempt(Name, element, arguments) || (Name == "browser_click" && actual is null && target is not null))
        {
            var reason = actual is null ? "unknown element" : "submitting is yours";
            await activity.LogAsync(SyncLogLevel.Warning, $"AI ▸ tried to {Describe(Name, element)} — blocked ({reason}).");
            return actual is null
                ? "BLOCKED by SwiftJob: that element is not in the latest snapshot. Call browser_snapshot first."
                : SubmitBlockedMessage;
        }

        if (Name == "browser_type")
        {
            arguments["submit"] = false; // never press Enter after typing
        }

        if (Name != "browser_snapshot" && Name != "browser_wait_for")
        {
            await activity.LogAsync(SyncLogLevel.Info, $"AI ▸ {Describe(Name, element)}");
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, runToken);
        var result = await base.InvokeCoreAsync(arguments, linked.Token);
        page.Update(result is JsonElement json ? json.ToString() : result?.ToString());
        return result;
    }

    internal static bool IsSubmitAttempt(string tool, string? element, AIFunctionArguments arguments)
    {
        // Clicking into a text field (e.g. "Confirm email textbox") is fine; buttons and links that submit are not.
        // For known elements the description comes from the page snapshot ("Submit application button").
        if (tool == "browser_click" && element is not null && SubmitWords().IsMatch(element) && !FieldWords().IsMatch(element))
        {
            return true;
        }

        if (tool == "browser_press_key" && string.Equals(Argument(arguments, "key"), "Enter", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static string Describe(string tool, string? element) => tool switch
    {
        "browser_click" => $"click {element ?? "an element"}",
        "browser_type" => $"type into {element ?? "a field"}",
        "browser_fill_form" => "fill form fields",
        "browser_select_option" => $"choose an option in {element ?? "a list"}",
        "browser_file_upload" => "attach a file",
        "browser_hover" => $"hover {element ?? "an element"}",
        _ => tool,
    };

    private static string? Argument(AIFunctionArguments arguments, string name) =>
        arguments.TryGetValue(name, out var value) ? value switch
        {
            string s => s,
            JsonElement { ValueKind: JsonValueKind.String } j => j.GetString(),
            null => null,
            _ => value.ToString(),
        } : null;

    [GeneratedRegex(@"\b(submit|send|apply|finish|complete( my)? application|confirm)\b", RegexOptions.IgnoreCase)]
    private static partial Regex SubmitWords();

    [GeneratedRegex(@"\b(textbox|text box|text field|input|field|checkbox|combobox|dropdown)\b", RegexOptions.IgnoreCase)]
    private static partial Regex FieldWords();
}
