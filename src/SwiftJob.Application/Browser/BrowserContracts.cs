namespace SwiftJob.Application.Browser;

/// <summary>Who may act in the browser right now. Only one controller at a time.</summary>
public enum BrowserOwner
{
    /// <summary>No session, or nobody is acting.</summary>
    None = 0,

    /// <summary>The AI agent is acting; the user should not type.</summary>
    Agent = 1,

    /// <summary>The user has control; the AI does nothing.</summary>
    User = 2,
}

public sealed record BrowserStatusDto(
    bool IsOpen,
    string? CurrentUrl,
    BrowserOwner Controller,
    string Message);

/// <summary>
/// The SwiftJob-controlled browser (one visible window). Implementations must validate URLs and never
/// navigate to non-http(s) addresses.
/// </summary>
public interface IBrowserSessionService
{
    BrowserStatusDto Status { get; }

    /// <summary>
    /// Opens <paramref name="url"/> in the SwiftJob browser, starting it if necessary. A new browser window is
    /// placed at <paramref name="placement"/> (e.g. the right half of the screen); an open window is not moved.
    /// </summary>
    /// <exception cref="ArgumentException">The URL is not an absolute http(s) URL.</exception>
    /// <exception cref="Common.ExternalServiceException">The browser could not be started or navigated.</exception>
    Task OpenAsync(string url, WindowPlacement? placement, CancellationToken cancellationToken);

    /// <summary>Moves the browser window to <paramref name="placement"/> by reopening it (the page and logins are kept).</summary>
    Task ArrangeAsync(WindowPlacement placement, CancellationToken cancellationToken);

    Task CloseAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Prototype application agent: fills the form in the SwiftJob browser using supplied applicant data,
/// never submits, and hands control back to the user. Pause/resume give one authoritative controller.
/// </summary>
public interface IFormFillAgent
{
    /// <summary>Opens the local sample job form and starts filling it with demo applicant data.</summary>
    Task StartDemoAsync(WindowPlacement? placement, CancellationToken cancellationToken);

    /// <summary>Stops the AI immediately and gives control to the user (browser state is kept).</summary>
    Task TakeControlAsync();

    /// <summary>Lets the AI continue from the page as it is now.</summary>
    Task ResumeAsync(CancellationToken cancellationToken);

    /// <summary>Stops the AI; the browser stays open.</summary>
    Task StopAsync();
}

/// <summary>Where the browser window should appear, in screen pixels (typically the right half of the screen).</summary>
public sealed record WindowPlacement(int X, int Y, int Width, int Height)
{
    /// <summary>Rejects nonsensical sizes so a bad request cannot produce an invisible window.</summary>
    public WindowPlacement Validated() =>
        Width is >= 400 and <= 10_000 && Height is >= 300 and <= 10_000 && X is > -20_000 and < 20_000 && Y is > -20_000 and < 20_000
            ? this
            : throw new ArgumentException("The window placement is out of range.");
}

/// <summary>Clearly fictional applicant used only with the local sample form in the prototype.</summary>
public static class DemoApplicant
{
    public const string FirstName = "Alex";
    public const string LastName = "Demo";
    public const string Email = "alex.demo@example.com";
    public const string Phone = "+44 7700 900123";
    public const string CvFileName = "Demo_CV.pdf";
}

/// <summary>Validates addresses before the browser is pointed at them.</summary>
public static class BrowserUrl
{
    public static Uri Validate(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)
            || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new ArgumentException("Only absolute http(s) links can be opened.", nameof(url));
        }

        return uri;
    }
}

/// <summary>Pushes browser status changes to the UI (e.g. over SignalR).</summary>
public interface IBrowserStatusPublisher
{
    Task PublishAsync(BrowserStatusDto status);
}
