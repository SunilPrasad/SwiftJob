# SwiftJob — AI Job Application Manager

SwiftJob reads job-notification emails, turns them into a searchable job list, and (in later phases) uses an
AI browser agent — which you can watch, pause and take over — to help complete applications.

> **Status: Phase 1 (Foundation) complete; Phase 2 in progress — Gmail can be connected (read-only).**
> Email synchronisation, profile management and the AI agent follow; their buttons are visible but disabled.
> See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) and [TASKS.md](TASKS.md).

## What works in Phase 1

* ASP.NET Core 10 Web API with clean architecture (Domain / Application / Infrastructure / Api).
* SQLite database via EF Core 10 with the full data model and an initial migration (applied automatically on startup).
* Development seeder with clearly-marked sample jobs (`sample-…` Gmail ids, `example.com` URLs), including emails
  that contain several jobs.
* Angular 22 + Material dashboard:
  * New jobs by default, newest first; sort by received date, title or company.
  * Search by title/company, filter by source, date range and status; server-side paging.
  * Live counts for New / Applied / Not relevant / All (respecting the current search and filters).
  * Mark **Applied** / **Not relevant** from the row or the panel, with **Undo**; move any job back to **New**.
  * Split view: clicking a job keeps the list on the left and docks the details on the right. Drag the divider
    (or focus it and use the arrow keys) to resize; double-click resets. The width is remembered.
  * Details panel with salary, location, work mode, source, received date, description (original and,
    when present, enriched), requirements, job link and the original email(s) — viewable as plain text or opened in Gmail.
  * Responsive layout down to phone width.
* **Settings → Gmail** (Phase 2, step 1): connect one Gmail account with **read-only** access using your own
  Google OAuth client, test the connection, edit which emails are read (a Gmail search query) and disconnect.
  Setup guide: [docs/GMAIL_SETUP.md](docs/GMAIL_SETUP.md).
* Automated tests: 63 .NET tests (domain, services on real SQLite, constraints, Gmail connection, HTTP integration) and
  33 Angular tests (API client, signal store, details panel, split resizer, Gmail settings, helpers).

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 10.0 |
| Node.js | 24 LTS (≥ 24.15) or 22 (≥ 22.22.3) — required by Angular CLI 22 |
| npm | 10+ |

No database server is needed: SQLite is a file at `src/SwiftJob.Api/App_Data/swiftjob.db`.

## Run locally

```bash
# 1. Backend (http://localhost:5080)
dotnet tool restore
dotnet run --project src/SwiftJob.Api
```

On first start in the Development environment the API creates the database, applies migrations and seeds the
sample data. Useful URLs: `http://localhost:5080/health`, `http://localhost:5080/openapi/v1.json`.

```bash
# 2. Frontend (http://localhost:4200) — in a second terminal
cd web
npm install
npm start
```

The Angular dev server proxies `/api` to the backend (`web/proxy.conf.json`), so no CORS setup is needed.
Open **http://localhost:4200**.

### Visual Studio / Rider

Open `SwiftJob.slnx`, set `SwiftJob.Api` as the startup project and run the `http` profile. Run the frontend with
`npm start` from `web/`.

## Tests

```bash
dotnet test                         # backend
cd web && npx ng test --watch=false # frontend
```

## Configuration

`src/SwiftJob.Api/appsettings.json` (overridable with environment variables, e.g. `Database__SeedSampleData=false`):

| Key | Default | Description |
|-----|---------|-------------|
| `ConnectionStrings:SwiftJob` | `Data Source=App_Data/swiftjob.db` | SQLite file; relative paths are resolved from the API project folder |
| `Database:ApplyMigrationsOnStartup` | `true` | Apply pending EF Core migrations at startup |
| `Database:SeedSampleData` | `false` (`true` in Development) | Seed sample jobs into an **empty** database only |
| `Gmail:RedirectUri` | `http://localhost:5080/api/settings/gmail/callback` | OAuth redirect; must match the Google OAuth client |
| `Gmail:FrontendUrl` | `http://localhost:4200` | Where the browser returns after Google sign-in |
| `DataProtection:KeysPath` | `App_Data/keys` | Encryption keys for stored secrets (keep private, back up with the database) |
| `Cors:AllowedOrigins` | `["http://localhost:4200"]` | Origins allowed to call the API directly |

To start with an empty database (e.g. before connecting Gmail in Phase 2), set `Database:SeedSampleData` to `false`
and delete `App_Data/swiftjob.db`.

The Gmail OAuth client secret and refresh token are entered in the Settings page and stored **encrypted** in the
local database (ASP.NET Core Data Protection; keys in `App_Data/keys`, DPAPI-protected on Windows). They are never
returned to the browser. AI API keys (later phases) will come from
[user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) or environment variables.

The API listens on `localhost` only and has no login, as it is a single-user local tool. Do not expose it to a network.

## Database migrations

```bash
dotnet tool restore
dotnet ef migrations add <Name> \
  --project src/SwiftJob.Infrastructure --startup-project src/SwiftJob.Api \
  --output-dir Persistence/Migrations
```

## API

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/jobs?status=&search=&source=&receivedFrom=&receivedBefore=&sortBy=&sortDirection=&page=&pageSize=` | Paged job list (default: all statuses, newest first; the UI requests `status=New`) |
| GET | `/api/jobs/counts?search=&source=&receivedFrom=&receivedBefore=` | Counts per status |
| GET | `/api/jobs/sources` | Distinct sources |
| GET | `/api/jobs/{id}` | Job details with linked emails |
| PUT | `/api/jobs/{id}/status` | Body `{ "status": "New" \| "Applied" \| "NotRelevant" }` |
| GET | `/api/emails/{id}` | Original email as plain text |
| GET | `/api/settings/gmail` | Gmail connection status, redirect URI, search query (never secrets) |
| PUT / DELETE | `/api/settings/gmail/credentials` | Save (`{ clientId, clientSecret }`) or remove the Google OAuth client |
| PUT | `/api/settings/gmail/search-query` | `{ searchQuery }` — which emails may be read |
| POST | `/api/settings/gmail/connect` | Returns the Google sign-in URL (read-only scope) |
| GET | `/api/settings/gmail/callback` | OAuth redirect target; returns the browser to Settings |
| POST | `/api/settings/gmail/disconnect` | Revoke access and delete the stored token |
| POST | `/api/settings/gmail/test` | Verify access; count and preview matching emails |

Errors use RFC 7807 problem details (`404` unknown id, `400` invalid input, `409` conflicts with current state, `502` Google API failures).

## Project structure

```
src/SwiftJob.Domain           entities, enums, domain rules (no dependencies)
src/SwiftJob.Application      use cases, DTOs, ports (IAppDbContext, later AI/Gmail/browser interfaces)
src/SwiftJob.Infrastructure   EF Core DbContext, configurations, migrations, seeding (later: Gmail, AI, Playwright)
src/SwiftJob.Api              ASP.NET Core host: controllers, error handling, composition root
tests/SwiftJob.Tests          unit + integration tests
web/                          Angular 22 application
docs/ARCHITECTURE.md          architecture, technology evaluation, risks and phase plan
docs/GMAIL_SETUP.md           step-by-step Google OAuth setup for Gmail
TASKS.md                      progress checklist for all phases
```
