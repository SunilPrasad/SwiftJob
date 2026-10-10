using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using SwiftJob.Application.Common;
using SwiftJob.Application.Gmail;

namespace SwiftJob.Infrastructure.Gmail;

/// <summary>
/// Google OAuth + Gmail API adapter. Only ever requests <c>gmail.readonly</c>, and only uses read
/// endpoints (profile, messages.list, messages.get with metadata).
/// </summary>
internal sealed class GoogleGmailClient : IGmailOAuthClient, IGmailMailbox
{
    private const string UserId = "swiftjob";
    private const string ApplicationName = "SwiftJob";

    public Uri BuildAuthorizationUrl(GoogleOAuthClient client, string redirectUri, string state)
    {
        var request = (GoogleAuthorizationCodeRequestUrl)CreateFlow(client).CreateAuthorizationCodeRequest(redirectUri);
        request.State = state;
        request.AccessType = "offline"; // needed to receive a refresh token
        request.Prompt = "consent";     // always show the consent screen so a refresh token is issued
        request.IncludeGrantedScopes = "false";
        return request.Build();
    }

    public async Task<GmailTokenGrant> ExchangeCodeAsync(
        GoogleOAuthClient client, string redirectUri, string code, CancellationToken cancellationToken)
    {
        try
        {
            var token = await CreateFlow(client).ExchangeCodeForTokenAsync(UserId, code, redirectUri, cancellationToken);
            return new GmailTokenGrant(token.RefreshToken, token.Scope);
        }
        catch (TokenResponseException ex)
        {
            throw new ExternalServiceException(
                $"Google rejected the sign-in ({ex.Error?.Error}). Check the Client ID, Client Secret and redirect URI.", ex);
        }
    }

    public Task RevokeAsync(GoogleOAuthClient client, string refreshToken, CancellationToken cancellationToken) =>
        CreateFlow(client).RevokeTokenAsync(UserId, refreshToken, cancellationToken);

    public Task<string> GetEmailAddressAsync(GmailAccess access, CancellationToken cancellationToken) =>
        CallAsync(access, async service =>
        {
            var profile = await service.Users.GetProfile("me").ExecuteAsync(cancellationToken);
            return profile.EmailAddress;
        });

    public Task<GmailSearchResult> SearchAsync(
        GmailAccess access, string query, int maxPreviews, CancellationToken cancellationToken) =>
        CallAsync(access, async service =>
        {
            var list = service.Users.Messages.List("me");
            list.Q = query;
            list.MaxResults = Math.Max(1, maxPreviews);
            list.IncludeSpamTrash = false;
            var response = await list.ExecuteAsync(cancellationToken);

            var previews = new List<GmailMessagePreview>();
            foreach (var message in response.Messages ?? [])
            {
                var get = service.Users.Messages.Get("me", message.Id);
                get.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Metadata;
                get.MetadataHeaders = new Google.Apis.Util.Repeatable<string>(["Subject", "From"]);
                var detail = await get.ExecuteAsync(cancellationToken);

                string? Header(string name) =>
                    detail.Payload?.Headers?.FirstOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value;

                previews.Add(new GmailMessagePreview(
                    detail.Id,
                    Header("Subject"),
                    Header("From"),
                    detail.InternalDate is { } ms ? DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime : null));
            }

            return new GmailSearchResult((int)(response.ResultSizeEstimate ?? previews.Count), previews);
        });

    private static async Task<T> CallAsync<T>(GmailAccess access, Func<GmailService, Task<T>> call)
    {
        var flow = CreateFlow(access.Client);
        var credential = new UserCredential(flow, UserId, new TokenResponse { RefreshToken = access.RefreshToken });
        using var service = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName,
        });

        try
        {
            return await call(service);
        }
        catch (TokenResponseException ex) when (ex.Error?.Error == "invalid_grant")
        {
            throw new GmailAccessRevokedException(ex);
        }
        catch (TokenResponseException ex)
        {
            throw new ExternalServiceException($"Google refused the stored credentials ({ex.Error?.Error}).", ex);
        }
        catch (GoogleApiException ex)
        {
            var reason = ex.Error?.Message ?? ex.Message;
            throw new ExternalServiceException($"The Gmail API returned an error: {reason}", ex);
        }
    }

    private static GoogleAuthorizationCodeFlow CreateFlow(GoogleOAuthClient client) =>
        new(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = client.ClientId, ClientSecret = client.ClientSecret },
            Scopes = [GmailService.Scope.GmailReadonly],
            DataStore = new NullDataStore(), // tokens are persisted (encrypted) by SwiftJob, not by the Google library
        });
}
