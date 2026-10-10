# SwiftJob — AI Job Application Manager

SwiftJob reads job-notification emails, turns them into a searchable job list, and (in later phases) uses an
AI browser agent — which you can watch, pause and take over — to help complete applications.

> **Status: Phase 1 (Foundation) complete.** Gmail sync, profile management and the AI agent are planned for
> Phases 2–5; their buttons are visible but disabled in the UI. See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

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
  * Left-side details panel with salary, location, work mode, source, received date, description (original and,
    when present, enriched), requirements, job link and the original email(s) — viewable as plain text or opened in Gmail.
  * Responsive layout down to phone width.
* Automated tests: 36 .NET tests (domain, services on real SQLite, constraints, HTTP integration) and
  23 Angular tests (API client, signal store, details panel, URL/date helpers).

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
| `Cors:AllowedOrigins` | `["http://localhost:4200"]` | Origins allowed to call the API directly |

To start with an empty database (e.g. before connecting Gmail in Phase 2), set `Database:SeedSampleData` to `false`
and delete `App_Data/swiftjob.db`.

Secrets (Gmail OAuth client, AI API keys) arrive in later phases and will be read from
[user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) or environment variables — never from
committed files or the frontend.

## Database migrations

```bash
dotnet tool restore
dotnet ef migrations add <Name> \
  --project src/SwiftJob.Infrastructure --startup-project src/SwiftJob.Api \
  --output-dir Persistence/Migrations
```

## API (Phase 1)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/jobs?status=&search=&source=&receivedFrom=&receivedBefore=&sortBy=&sortDirection=&page=&pageSize=` | Paged job list (default: all statuses, newest first; the UI requests `status=New`) |
| GET | `/api/jobs/counts?search=&source=&receivedFrom=&receivedBefore=` | Counts per status |
| GET | `/api/jobs/sources` | Distinct sources |
| GET | `/api/jobs/{id}` | Job details with linked emails |
| PUT | `/api/jobs/{id}/status` | Body `{ "status": "New" \| "Applied" \| "NotRelevant" }` |
| GET | `/api/emails/{id}` | Original email as plain text |

Errors use RFC 7807 problem details (`404` for unknown ids, `400` for invalid input).

## Project structure

```
src/SwiftJob.Domain           entities, enums, domain rules (no dependencies)
src/SwiftJob.Application      use cases, DTOs, ports (IAppDbContext, later AI/Gmail/browser interfaces)
src/SwiftJob.Infrastructure   EF Core DbContext, configurations, migrations, seeding (later: Gmail, AI, Playwright)
src/SwiftJob.Api              ASP.NET Core host: controllers, error handling, composition root
tests/SwiftJob.Tests          unit + integration tests
web/                          Angular 22 application
docs/ARCHITECTURE.md          architecture, technology evaluation, risks and phase plan
TASKS.md                      progress checklist for all phases
```
