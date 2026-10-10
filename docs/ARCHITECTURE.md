# SwiftJob – Solution Architecture

This document is the architecture proposal for the AI Job Application Manager. It records the
decisions behind Phase 1 and the plan for later phases. Each later phase will update this file.

## 1. Goals and constraints

* Personal, locally run tool. No paid infrastructure is required (AI API calls may cost money).
* One deployable backend and one SPA. No microservices.
* Clean architecture with clear boundaries so the database, AI provider and browser technology can
  each be swapped without touching the Angular UI or the core domain.
* All external content (emails, job ads, web pages) is **untrusted**.

## 2. High-level architecture

```
┌──────────────────────────┐   HTTP (REST) + SignalR (Phase 2+)   ┌─────────────────────────────────────────┐
│  Angular 22 SPA (web/)   │ ───────────────────────────────────▶ │  ASP.NET Core 10 Web API (SwiftJob.Api)  │
│  Material 3, signals     │ ◀─────────────────────────────────── │  controllers, hubs, composition root     │
└──────────────────────────┘                                      └───────────────────┬─────────────────────┘
                                                                                      │ DI
                                     ┌────────────────────────────────────────────────┼───────────────────────┐
                                     ▼                                                ▼                       ▼
                       ┌──────────────────────────┐   ports (interfaces)   ┌─────────────────────────────────────┐
                       │ SwiftJob.Application     │ ◀───────────────────── │ SwiftJob.Infrastructure             │
                       │ use cases, DTOs, ports   │      implemented by    │ EF Core/SQLite, seeding             │
                       └────────────┬─────────────┘                        │ (later) Gmail, AI adapters,         │
                                    ▼                                      │ Playwright browser sessions         │
                       ┌──────────────────────────┐                        └─────────────────────────────────────┘
                       │ SwiftJob.Domain          │
                       │ entities, enums, rules   │
                       └──────────────────────────┘
```

Dependency direction: `Api → Infrastructure → Application → Domain`. The Domain has no package
dependencies. Application references only `Microsoft.EntityFrameworkCore` (for `DbSet`/LINQ through
`IAppDbContext`), never a database provider.

### Why four projects (and not more)

Four projects give the boundaries that matter (domain rules, use cases, external technology,
HTTP surface) while staying easy to navigate. AI and browser adapters will live in
`SwiftJob.Infrastructure` sub-folders; if an adapter grows heavy dependencies (e.g. Playwright), it can be
split into its own project later without changing any interface.

## 3. Folder structure

```
SwiftJob/
├── SwiftJob.slnx                     # solution (new XML format)
├── Directory.Build.props             # net10.0, nullable, warnings-as-errors for all projects
├── dotnet-tools.json                 # dotnet-ef pinned locally
├── docs/ARCHITECTURE.md
├── src/
│   ├── SwiftJob.Domain/              # Jobs/, Emails/, Candidates/, Applications/, Common/
│   ├── SwiftJob.Application/         # Abstractions/ (ports), Jobs/, Emails/, Common/
│   ├── SwiftJob.Infrastructure/      # Persistence/ (DbContext, Configurations/, Migrations/, Seeding/)
│   └── SwiftJob.Api/                 # Controllers/, Infrastructure/ (error handling), Program.cs
├── tests/
│   └── SwiftJob.Tests/               # Domain/, Application/, Infrastructure/, Api/ (integration)
└── web/                              # Angular workspace
    └── src/app/
        ├── core/                     # api/ (typed HTTP clients), models/, http/ (interceptors)
        ├── layout/                   # application shell
        ├── shared/                   # small framework-free helpers
        └── features/jobs/            # data/ (signal store), dashboard/, filters/, table/, details/, shared/
```

## 4. Selected versions

| Area | Choice | Notes |
|------|--------|-------|
| Backend runtime | .NET 10 (LTS), C# 14 | LTS until Nov 2028 |
| Web API | ASP.NET Core 10, controllers | familiar structure for a .NET developer |
| ORM | EF Core 10.0.12 + `Microsoft.EntityFrameworkCore.Sqlite` | Npgsql provider can replace SQLite later |
| Tests (.NET) | xUnit 2.9, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.Extensions.TimeProvider.Testing` | real SQLite, no mocks of EF |
| Frontend | Angular 22.2, Angular Material 22.2 (Material 3) | standalone components, signals, zoneless |
| Frontend tests | Vitest 5 via `ng test` (Angular CLI default) | jsdom environment |
| Node.js | 24 LTS (≥ 24.15) or 22 (≥ 22.22.3) | required by Angular CLI 22 |
| Real-time (Phase 2+) | ASP.NET Core SignalR + `@microsoft/signalr` 10 | |
| Email (Phase 2) | `Google.Apis.Gmail.v1` + `Google.Apis.Auth` | `gmail.readonly` scope |
| AI (Phase 4) | `Microsoft.Extensions.AI` (`IChatClient`) with OpenAI / Anthropic adapters | versions verified when Phase 4 starts |
| Browser (Phase 4/5) | `Microsoft.Playwright` for .NET, Chromium | see §8 |

## 5. Data model

All entities are created in Phase 1 so later phases add behaviour, not schema churn.

| Entity | Purpose | Key constraints / indexes |
|--------|---------|---------------------------|
| `Job` | One opportunity. Original `Description` is never overwritten; enrichment goes to `EnrichedDescription`. | `(Status, ReceivedAt)`; unique `(Source, SourceJobId)` where not null; unique `CanonicalUrl` where not null |
| `JobEmail` | Original email retained for reference (text + HTML). | unique `GmailMessageId` (idempotent sync) |
| `JobEmailLink` | Many-to-many: one email → many jobs, one job ← many emails. | composite PK |
| `CandidateProfile` (+ `WorkExperience`, `Education`, `SavedAnswer`) | Verified candidate facts the agent may use. CV stored on disk, referenced by path. | skills/locations as primitive collections |
| `ApplicationSession` | One application attempt incl. provider/model, current controller (Agent/User). | `(JobId, CreatedAt)`, `Status`; jobs with history cannot be deleted |
| `ApplicationActivity` | Append-only audit log (who did what, outcome). | `(ApplicationSessionId, Timestamp)` |
| `ApplicationAnswer` | Questions asked by a form and the answer/source/confirmation. | |

Portability decisions (SQLite → PostgreSQL):

* All timestamps are UTC `DateTime` (SQLite cannot order `DateTimeOffset`; Npgsql `timestamptz` requires UTC).
* Enums are stored as strings.
* Partial unique-index filters use quoted identifiers valid on both engines.
* Case-insensitive search uses `ToLower().Contains()`, which translates on both.
* Migrations are provider-specific: switching to PostgreSQL means generating a fresh initial migration for Npgsql.

## 6. AI provider independence (Phase 4 design)

Domain-level ports (in `SwiftJob.Application`), each with one responsibility:

| Interface | Responsibility |
|-----------|----------------|
| `IJobExtractionService` | email → zero or more structured jobs (schema-validated, nulls for unknowns) |
| `IJobMatchingService` | optional relevance scoring against preferences |
| `IFormUnderstandingService` | page snapshot (form fields, labels, options) → field semantics |
| `IApplicationAnswerService` | field semantics + *only the relevant* verified profile facts → proposed answers with provenance, or "needs user" |
| `IApplicationAgent` | orchestrates one session step-by-step: inspect → plan → act → validate |
| `IBrowserSessionService` | browser lifecycle and actions (navigate, fill, click, upload, snapshot), controller lock |

Implementation approach:

* `Microsoft.Extensions.AI.IChatClient` is the provider seam. Concrete clients (OpenAI, Anthropic, Azure OpenAI,
  Ollama, …) are chosen by configuration (`Ai:Provider`, `Ai:Model`, secrets via user-secrets/env vars).
* Structured outputs: JSON schema generated from C# records; responses are validated and rejected on mismatch.
* Provider capabilities are explicit (`AgentCapabilities { SupportsStructuredOutput, SupportsVision, SupportsComputerUseTool }`).
  The orchestrator branches on capabilities rather than on provider names.
* Data minimisation: the answer service sends only the profile fields needed for the current question.

## 7. Security model

* Emails and web pages are rendered **as plain text** in the UI (no `innerHTML`), links are only created for absolute
  http(s) URLs and open with `rel="noopener noreferrer"`. The email HTML body is never sent to the browser.
* Prompt-injection defence (Phase 2/4): untrusted text is passed to models only as clearly delimited data, never as
  instructions; model outputs are constrained to schemas; the agent can only perform whitelisted browser actions
  (no shell, no arbitrary navigation outside the application's origin without confirmation).
* Final submission always requires explicit user confirmation; jobs are marked Applied automatically only after
  verified submission.
* Secrets: user-secrets in development, environment variables otherwise. OAuth refresh tokens encrypted at rest with
  ASP.NET Core Data Protection. No keys in the frontend.
* External URLs are validated (scheme, host) before the agent navigates; tracking redirects are resolved without
  cookies, with a hop limit and timeouts.

## 8. Evaluation: live browser viewing and control

The critical requirement is that the user can watch the agent, pause it, take over (including CAPTCHA) and resume.
Screenshots or a headless server-side Playwright instance do **not** provide this on their own.

| Option | How it works | Fidelity of manual control | Effort | Fits |
|--------|-------------|----------------------------|--------|------|
| A. Headed local Chromium driven by Playwright | Backend launches a visible Chromium window on the user's machine (persistent profile). The user sees and uses the real window. | Full (real browser: typing, file pickers, CAPTCHA, password managers) | Low | Local MVP ✅ |
| B. CDP screencast embedded in Angular | `Page.startScreencast` frames streamed over SignalR/WebSocket; mouse/keyboard forwarded with `Input.dispatch*Event`. | Partial: native dialogs, file choosers, IME, some CAPTCHA iframes are awkward | Medium | In-dashboard watching; remote |
| C. Browser in a container + noVNC | Xvfb + Chromium + x11vnc + websockify; noVNC embedded via iframe; Playwright connects over CDP. | Full | Medium/High (Docker) | Cloud deployment later |

**Recommendation:** Phase 5 starts with **A** (reliable, zero extra infrastructure) and adds **B** as an
in-dashboard live view (watch + light interaction). "Take control" moves the controller lock to the user — the agent
stops issuing actions and the user works in the real window. "Resume" re-snapshots the page and continues from
the current state. **C** stays an option behind `IBrowserSessionService` for a future hosted version.

Controller arbitration: a per-session `SemaphoreSlim`-guarded state machine owns the lock
(`Agent | User | None`). Every agent action checks the lock and a cancellation token immediately before execution,
so a Pause/Take-control request takes effect before the next click or keystroke.

Computer-use options for the agent:

* **DOM-first (recommended baseline):** Playwright extracts form fields/labels/accessibility tree; the LLM maps them to
  profile data with structured output; Playwright performs the actions deterministically. Works with any provider
  that supports structured output, cheaper and auditable.
* **Vision computer-use tools** (e.g. Anthropic's computer-use tool, OpenAI's computer-use model): screenshot →
  coordinate actions. Useful as a fallback for non-standard UIs, but provider-specific, slower, costlier, and the
  tool APIs are still evolving. Exposed as an optional adapter with declared capabilities, not assumed.
* Consumer desktop AI apps are not used as an automation API.

## 9. Technical risks

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Gmail OAuth app in Google "Testing" mode issues refresh tokens that expire after 7 days | Sync silently stops weekly | Publish the OAuth app (personal, unverified is acceptable for own account) or re-consent flow with clear UI |
| Job boards (LinkedIn, Indeed) block automation, require login, or forbid it in their terms | Auto-apply cannot complete | Respect boundaries: hand control to the user for login/CAPTCHA; agent focuses on employer ATS forms (Workday, Greenhouse, Lever, …) |
| Email formats vary widely and change | Missed or wrong fields | LLM extraction with strict schema + nulls, keep raw email, re-processing support, sender-specific deterministic hints |
| Tracking/redirect URLs | Duplicates, privacy leaks | Canonicalisation (strip tracking params), bounded redirect resolution, dedup on source id + canonical URL |
| Prompt injection from pages/emails | Agent misbehaviour | Data/instruction separation, schema-constrained outputs, action whitelist, human confirmation before submit |
| Live control fidelity (option B) | Frustrating takeover | Real headed window (option A) is always available as the takeover surface |
| AI cost/latency | Slow or expensive sessions | DOM-first, small models for extraction, caching of form understanding per page |

## 10. What is reliable vs. experimental

Reliable (well-trodden): REST API, EF Core/SQLite, dashboard, Gmail API sync with `gmail.readonly`, idempotent storage,
SignalR progress events, profile/CV management, headed Playwright automation, deterministic form filling.

Requires experimentation: robust multi-sender extraction, form understanding on unfamiliar ATS sites, multi-step
navigation heuristics, CDP screencast interaction quality, vision computer-use adapters, submission verification.

## 11. Phases

1. **Foundation (this delivery):** solution, entities + migration, seeder, jobs API, dashboard, status management, details panel, tests.
2. Gmail integration: OAuth, scheduled + manual sync, extraction, dedup, SignalR sync progress.
3. Candidate profile + CV upload + saved answers.
4. AI application prototype: AI ports + one adapter, local sample job form, multi-step + CV upload, confirm-before-submit.
5. Interactive agent: live view, Pause / Take control / Resume / Stop, activity streaming.
6. Refinement: history UI, extraction reliability, retries, provider configuration, deployment preparation.
