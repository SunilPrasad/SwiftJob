namespace SwiftJob.Application.Gmail;

public sealed class GmailOptions
{
    public const string SectionName = "Gmail";

    /// <summary>
    /// OAuth redirect URI. Must exactly match an "Authorised redirect URI" on the Google OAuth client.
    /// </summary>
    public string RedirectUri { get; set; } = "http://localhost:5080/api/settings/gmail/callback";

    /// <summary>Where the browser is sent after Google sign-in completes.</summary>
    public string FrontendUrl { get; set; } = "http://localhost:4200";

    /// <summary>Number of matching emails shown by "Test connection".</summary>
    public int TestPreviewCount { get; set; } = 5;
}
