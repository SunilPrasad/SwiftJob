# Connecting Gmail (read-only)

SwiftJob reads job notification emails using **your own** Google OAuth client. It asks Google only for the
`https://www.googleapis.com/auth/gmail.readonly` scope, so it can read messages but can never send, delete,
label or modify anything. If Google grants anything broader, SwiftJob rejects and revokes the token.

This is a one-time setup of about 10 minutes.

## 1. Create a Google Cloud project

1. Go to <https://console.cloud.google.com/projectcreate>.
2. Name it e.g. `SwiftJob` and click **Create**. Make sure the new project is selected in the top bar.

## 2. Enable the Gmail API

1. Open <https://console.cloud.google.com/apis/library/gmail.googleapis.com>.
2. Click **Enable**.

## 3. Configure the OAuth consent screen

In **Google Auth Platform** (left menu → *APIs & Services* → *OAuth consent screen*):

1. **Branding:** app name `SwiftJob`, your email as support and developer contact. Save.
2. **Audience:** user type **External**. Under *Test users* add the Gmail address you want to connect.
3. **Data Access:** *Add or remove scopes* → find **Gmail API – `.../auth/gmail.readonly`** ("View your email
   messages and settings") → *Update* → *Save*.

### Avoid weekly reconnects (recommended)

While the app's publishing status is **Testing**, Google expires refresh tokens after **7 days**, so you would
have to reconnect every week. For a personal tool you can avoid this:

* **Audience → Publish app** (status becomes *In production*). Google does not require verification for a
  personal app used by its owner; when you connect you will see *"Google hasn't verified this app"* — click
  **Advanced → Go to SwiftJob (unsafe)**. This warning is expected because the app is yours and unverified.

## 4. Create the OAuth client

1. **Clients → Create client**.
2. Application type: **Web application**. Name: `SwiftJob local`.
3. **Authorised redirect URIs → Add URI**:

   ```
   http://localhost:5080/api/settings/gmail/callback
   ```

   (Shown with a copy button on the SwiftJob Settings page. It must match exactly.)
4. Click **Create** and copy the **Client ID** and **Client secret**.

## 5. Connect in SwiftJob

1. Start the API and the web app (see the main README) and open <http://localhost:4200/settings>.
2. Paste the **Client ID** and **Client secret** and click **Save client**.
   The secret is encrypted with ASP.NET Core Data Protection (keys in `src/SwiftJob.Api/App_Data/keys`,
   additionally protected with Windows DPAPI on Windows) and is never shown again.
3. Click **Connect Gmail (read-only)**, sign in with the test-user account and approve.
4. You are returned to Settings showing **Connected as you@gmail.com**.
5. Click **Test connection** to see how many emails match the filter and the latest few subjects.

## Choosing which emails are read

The **Gmail search query** uses the same syntax as Gmail's search box. The default is:

```
from:(jobalerts-noreply@linkedin.com OR jobs-noreply@linkedin.com OR indeed.com OR reed.co.uk OR totaljobs.com OR jobserve.com) OR label:jobs
```

Tips:

* Add a recruiter or agency: `... OR from:recruiter@agency.co.uk`.
* Create a Gmail filter that applies the label **Jobs** to anything job-related — it is included automatically.
* Use **Test connection** after saving to check what matches.

## Disconnecting

**Disconnect** revokes SwiftJob's access at Google and deletes the stored token. You can also remove access at
<https://myaccount.google.com/permissions>. To change the OAuth client, disconnect first.

## Troubleshooting

| Message | Fix |
|---------|-----|
| `redirect_uri_mismatch` on Google's page | The redirect URI in the Google client must be exactly `http://localhost:5080/api/settings/gmail/callback`. |
| `access_denied` / "app is blocked" | Add your address as a test user (Audience), or publish the app. |
| "Gmail API has not been used in project … or it is disabled" | Enable the Gmail API for the same project (step 2). |
| "Gmail access has expired or was revoked" | Click **Disconnect**, then **Connect** again. If it happens weekly, publish the app (step 3). |
| "Google did not return a long-lived token" | Remove SwiftJob at <https://myaccount.google.com/permissions> and connect again. |
| Running the API on another port/host | Set `Gmail:RedirectUri` (and `Gmail:FrontendUrl`) in `appsettings.json` and update the Google client's redirect URI to match. |
