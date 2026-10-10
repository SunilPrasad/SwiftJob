namespace SwiftJob.Application.Common;

public static class GmailLinks
{
    /// <summary>Web link that opens a message in the signed-in user's Gmail, given its API message id.</summary>
    public static string ForMessage(string gmailMessageId) =>
        $"https://mail.google.com/mail/u/0/#all/{Uri.EscapeDataString(gmailMessageId)}";
}
