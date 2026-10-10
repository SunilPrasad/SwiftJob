using System.Text;
using Google.Apis.Gmail.v1.Data;
using SwiftJob.Application.Gmail;

namespace SwiftJob.Infrastructure.Gmail;

/// <summary>Converts a Gmail API message (format=full) into subject, sender, date and text/HTML bodies.</summary>
internal static class GmailMessageParser
{
    /// <summary>Bodies larger than this are truncated before storage.</summary>
    public const int MaxBodyLength = 512 * 1024;

    public static GmailMessage Parse(Message message)
    {
        string? Header(string name) =>
            message.Payload?.Headers?.FirstOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value;

        string? text = null;
        string? html = null;
        Walk(message.Payload, ref text, ref html);

        var receivedAt = message.InternalDate is { } ms
            ? DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime
            : DateTime.UtcNow;

        return new GmailMessage(message.Id, message.ThreadId, Header("Subject"), Header("From"), receivedAt, Limit(text), Limit(html));
    }

    private static void Walk(MessagePart? part, ref string? text, ref string? html)
    {
        if (part is null)
        {
            return;
        }

        // Attachments are ignored: only inline text bodies are read.
        var isAttachment = !string.IsNullOrEmpty(part.Filename);
        if (!isAttachment && part.Body?.Data is { Length: > 0 } data)
        {
            if (part.MimeType?.Equals("text/plain", StringComparison.OrdinalIgnoreCase) == true)
            {
                text ??= Decode(data);
            }
            else if (part.MimeType?.Equals("text/html", StringComparison.OrdinalIgnoreCase) == true)
            {
                html ??= Decode(data);
            }
        }

        foreach (var child in part.Parts ?? [])
        {
            Walk(child, ref text, ref html);
        }
    }

    /// <summary>Gmail bodies are base64url-encoded UTF-8.</summary>
    internal static string Decode(string base64Url)
    {
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + ((4 - (base64.Length % 4)) % 4), '=');
        return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }

    private static string? Limit(string? value) => value is { Length: > MaxBodyLength } ? value[..MaxBodyLength] : value;
}
