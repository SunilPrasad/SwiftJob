using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SwiftJob.Application.Common;
using SwiftJob.Application.Gmail;

namespace SwiftJob.Api.Controllers;

/// <summary>Connects SwiftJob to Gmail with read-only access.</summary>
[ApiController]
[Route("api/settings/gmail")]
public sealed class GmailSettingsController(
    IGmailConnectionService gmail,
    IOptions<GmailOptions> options,
    ILogger<GmailSettingsController> logger) : ControllerBase
{
    [HttpGet]
    public Task<GmailSettingsDto> Get(CancellationToken cancellationToken) =>
        gmail.GetSettingsAsync(cancellationToken);

    /// <summary>Saves the user's Google OAuth client. The secret is encrypted and never returned.</summary>
    [HttpPut("credentials")]
    public Task<GmailSettingsDto> SaveCredentials(SaveGmailCredentialsRequest request, CancellationToken cancellationToken) =>
        gmail.SaveCredentialsAsync(request, cancellationToken);

    [HttpDelete("credentials")]
    public Task<GmailSettingsDto> RemoveCredentials(CancellationToken cancellationToken) =>
        gmail.RemoveCredentialsAsync(cancellationToken);

    [HttpPut("search-query")]
    public Task<GmailSettingsDto> UpdateSearchQuery(UpdateGmailSearchQueryRequest request, CancellationToken cancellationToken) =>
        gmail.UpdateSearchQueryAsync(request.SearchQuery, cancellationToken);

    [HttpPut("sync-schedule")]
    public Task<GmailSettingsDto> UpdateSyncSchedule(UpdateSyncScheduleRequest request, CancellationToken cancellationToken) =>
        gmail.UpdateSyncScheduleAsync(request.AutoSyncEnabled, request.SyncIntervalMinutes, cancellationToken);

    /// <summary>Returns the Google sign-in URL the browser should open.</summary>
    [HttpPost("connect")]
    public Task<GmailAuthorizationDto> Connect(CancellationToken cancellationToken) =>
        gmail.BeginAuthorizationAsync(cancellationToken);

    /// <summary>OAuth redirect target. Completes sign-in, then sends the browser back to the Settings page.</summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(error))
        {
            return RedirectToSettings("error", error == "access_denied"
                ? "Access was not granted."
                : $"Google returned an error: {error}");
        }

        try
        {
            await gmail.CompleteAuthorizationAsync(code ?? string.Empty, state ?? string.Empty, cancellationToken);
            return RedirectToSettings("connected");
        }
        catch (Exception ex) when (ex is ConflictException or ExternalServiceException or ArgumentException)
        {
            logger.LogWarning(ex, "Gmail connection failed");
            return RedirectToSettings("error", ex.Message);
        }
    }

    [HttpPost("disconnect")]
    public Task<GmailSettingsDto> Disconnect(CancellationToken cancellationToken) =>
        gmail.DisconnectAsync(cancellationToken);

    /// <summary>Verifies the connection and previews the latest emails matching the search query.</summary>
    [HttpPost("test")]
    public Task<GmailTestResultDto> Test(CancellationToken cancellationToken) =>
        gmail.TestConnectionAsync(cancellationToken);

    private RedirectResult RedirectToSettings(string result, string? message = null)
    {
        var url = $"{options.Value.FrontendUrl.TrimEnd('/')}/settings?gmail={result}";
        if (message is not null)
        {
            url += "&message=" + Uri.EscapeDataString(message);
        }

        return Redirect(url);
    }
}
