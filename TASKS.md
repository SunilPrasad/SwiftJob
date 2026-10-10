# SwiftJob – Task Checklist

Progress tracker for the AI Job Application Manager MVP. Items are ticked when they are built,
tested and pushed. See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the design.

Legend: `[x]` done · `[ ]` to do

---

## Phase 0 — Planning
- [x] Review requirements
- [x] Propose solution architecture (docs/ARCHITECTURE.md)
- [x] Identify technical risks (live browser control, Gmail OAuth, job-board restrictions, prompt injection)
- [x] Define project folder structure
- [x] Select and verify library versions (.NET 10, EF Core 10, Angular 22, Material 22)
- [x] Evaluate live-browser options (headed Playwright, CDP screencast, noVNC) and computer-use approaches
- [x] Classify reliable vs. experimental work

## Phase 1 — Foundation ✅
### Backend
- [x] ASP.NET Core 10 Web API with clean architecture (Domain / Application / Infrastructure / Api)
- [x] Dependency injection composition (`AddApplication`, `AddInfrastructure`)
- [x] SQLite + EF Core 10 configuration (UTC timestamps, enums as strings, PostgreSQL-friendly)
- [x] Entities: Job, JobEmail, JobEmailLink, CandidateProfile (+ WorkExperience, Education, SavedAnswer),
      ApplicationSession, ApplicationActivity, ApplicationAnswer
- [x] Relationships, indexes and unique constraints (Gmail message id, source job id, canonical URL)
- [x] Initial EF Core migration, applied automatically on startup
- [x] Automatic CreatedAt / UpdatedAt auditing
- [x] Development sample-data seeder (clearly marked, includes multi-job emails)
- [x] Jobs API: list with search, source/date/status filters, sorting, paging
- [x] Jobs API: status counts, sources, job details with linked emails
- [x] Jobs API: change status (New / Applied / Not Relevant, reversible)
- [x] Emails API: original email as safe plain text (HTML never sent to browser)
- [x] Validation and RFC 7807 problem-details error handling
- [x] Health check and OpenAPI document

### Frontend
- [x] Angular 22 app with Angular Material 3, standalone components, signals
- [x] API proxy to backend for local development
- [x] Application shell (toolbar + navigation)
- [x] Job dashboard: Received / Title / Company / Salary / Location / Source / Status columns
- [x] Default view = New jobs, newest first
- [x] Search by title or company (debounced)
- [x] Filter by source, date range and status
- [x] Status counts (New / Applied / Not relevant / All)
- [x] Server-side pagination and sorting
- [x] Row actions: Mark Applied, Mark Not Relevant, Move back to New, with Undo
- [x] Refresh action
- [x] Left-side job details panel (all fields, description, enriched description tab, requirements,
      job link, original email(s) + Open in Gmail)
- [x] Details panel docked as a split view (not a pop-up): job list on the left, details on the right
- [x] Resizable split: drag the divider, arrow keys (Shift = bigger steps), double-click to reset
- [x] Chosen panel width remembered between visits
- [x] Job list stays usable while details are open; table columns adapt to the available width
- [x] On phones the details open full screen and Close returns to the list
- [x] Responsive layout (desktop, tablet, phone)
- [x] Untrusted content rendered as plain text; only http(s) links allowed

### Quality
- [x] Backend tests (domain, services on real SQLite, constraints, HTTP integration)
- [x] Frontend tests (API client, store, details panel, split resizer, helpers)
- [x] Manual end-to-end check in Chromium (desktop + mobile)
- [x] GitHub Actions CI (build + tests for backend and frontend)
- [x] README with setup and run instructions

## Phase 2 — Gmail Integration
### Step 1 — Connect Gmail (read-only) ✅
- [x] Google Cloud OAuth setup guide (docs/GMAIL_SETUP.md + in-app guide)
- [x] Settings page with Gmail card (navigation link)
- [x] Save / change / remove Google OAuth Client ID + Secret from the Settings page
- [x] Client secret and refresh token encrypted at rest (ASP.NET Core Data Protection, DPAPI on Windows); never returned to the browser
- [x] OAuth 2.0 sign-in requesting only `gmail.readonly` (offline access, consent prompt)
- [x] CSRF-protected, single-use, expiring OAuth `state`
- [x] Granted scope verified: anything broader than read-only is rejected and revoked
- [x] Connected account email shown; credentials locked while connected
- [x] Disconnect revokes access at Google and deletes the token
- [x] Test connection: estimated match count + latest matching emails
- [x] Editable Gmail search query (default: LinkedIn, Indeed, Reed, Totaljobs, JobServe alerts + `label:jobs`), reset to default
- [x] Clear errors for expired/revoked access, Google API errors and denied consent
- [x] Tests: connection service (fakes for Google), auth URL, encryption, state store, API endpoints, Settings UI

### Step 2 — Sync and extraction ✅
- [x] Gmail sync using the saved query; first sync reads the last 30 days, later syncs only new emails (1-day overlap)
- [x] Idempotent storage of emails (unique Gmail message id); full text + HTML bodies retained, attachments ignored
- [x] AI extraction with **Microsoft Agent Framework** (`ChatClientAgent`, structured output) behind `IJobExtractionService`
- [x] Provider-neutral AI: Anthropic, OpenAI, any OpenAI-compatible endpoint (Ollama, OpenRouter, Groq, Mistral, Gemini…); no Azure AI Foundry
- [x] Settings → AI model: provider, any model id, endpoint, encrypted API key, Test model
- [x] Provider capability info supplied by the backend (key/endpoint requirements, suggested models, notes)
- [x] Multiple jobs per email; nulls for anything not stated; non-job emails marked Ignored
- [x] Prompt-injection safeguards: email passed as delimited untrusted data, agent has no tools, outputs validated
- [x] Application links must appear in the email (fabricated/injected links discarded)
- [x] Source detected from sender domain (LinkedIn, Indeed, Reed, Totaljobs, JobServe, agencies…)
- [x] Tracking-URL canonicalisation without network calls (LinkedIn, Indeed, Reed, Totaljobs, JobServe, generic `utm_*` etc.)
- [x] De-duplication: source job id → canonical URL → source + title + company within 21 days; later emails linked, missing fields filled, existing values never overwritten
- [x] Scheduled background sync (toggle + interval in Settings, default 30 minutes)
- [x] Manual **Sync jobs** button enabled
- [x] SignalR live progress (downloading / extracting n of m) and completion notification; dashboard refreshes
- [x] Failed emails retried up to 3 times; extraction pauses after 3 consecutive AI errors; Gmail/AI errors shown in Settings
- [x] Remove sample data from Settings; sample data no longer seeded by default
- [x] Faster sync: parallel downloads (5) and AI extraction (4), one reused Google connection, links sent as short `[Ln]` references, shorter AI answers
- [x] Background-sync fix: no retry loop after failures (5-minute back-off); every failure recorded and shown
- [x] Activity log on the Jobs page: live view of each sync step, per-email result and timing, warnings and errors
- [x] Tests: URL canonicalisation, source detection, email text/link extraction, importer/dedup, sync idempotency and retries, Gmail parser, AI settings, Agent Framework extractor with a scripted model, API endpoints, Angular AI settings and sync status

## Prototype — SwiftJob browser & AI form filling ✅
- [x] SwiftJob browser: visible Chromium window controlled through Microsoft **Playwright MCP** (started by the backend via the MCP C# SDK)
- [x] Chrome → Edge → Playwright Chromium fallback; private persistent profile so job-site logins are remembered
- [x] **Open job posting** opens the job in the SwiftJob browser ("My browser" link kept)
- [x] Local sample job form (`/demo/job-form`) — nothing is stored or sent anywhere
- [x] Browser lab page: open sample form, **AI fill (demo)**, **Take control**, **Resume AI**, **Stop AI**, close browser, live status
- [x] Agent Framework agent with guarded MCP tools: only read/fill tools offered (no navigation, scripts, tabs)
- [x] Submit protection based on the real element from the page snapshot (not the AI's description); Enter-to-submit disabled
- [x] One controller at a time: Take control cancels the AI immediately; Resume re-reads the page and continues
- [x] Activity log shows every AI browser action; browser status pushed over SignalR
- [x] Tests: guard rules, snapshot tracker, URL validation, demo CV; opt-in end-to-end test with a real browser (fills fields, attaches CV, submit blocked, zero submissions)

## Phase 3 — Candidate Profile
- [ ] Profile API (personal, contact, summary, skills, work authorisation, preferences)
- [ ] Work history and education management
- [ ] Saved screening-question answers
- [ ] CV upload / replace (safe storage outside web root, type and size validation)
- [ ] Profile settings page in Angular
- [ ] Tests for profile and CV handling

## Phase 4 — AI Application Prototype
- [ ] Provider-neutral AI interfaces (`IFormUnderstandingService`, `IApplicationAnswerService`,
      `IApplicationAgent`, `IBrowserSessionService`, `IJobMatchingService`)
- [x] Provider configuration (provider, model, credentials) — done in Phase 2 (Settings → AI model)
- [x] Working AI provider adapters (Anthropic, OpenAI, OpenAI-compatible via Microsoft.Extensions.AI + Agent Framework)
- [ ] Provider capability flags for browser work (vision, computer-use)
- [ ] Playwright browser session service
- [ ] Locally hosted sample multi-step job application form
- [ ] Page inspection → field mapping → form filling
- [ ] CV upload during application
- [ ] Screening answers from verified data only; ask user when missing/ambiguous
- [ ] Multi-step navigation and validation
- [ ] Stop before final submission; explicit user confirmation required
- [ ] Application sessions and activity log persisted
- [ ] Auto Apply button enabled
- [ ] Tests against the sample form

## Phase 5 — Interactive Agent
- [ ] Live browser viewing (headed Chromium + in-dashboard view)
- [ ] Pause AI
- [ ] Take Control (single authoritative controller lock)
- [ ] Resume AI (re-inspect page and continue)
- [ ] Stop Session (preserve history)
- [ ] Submit Application with confirmation
- [ ] Real-time activity stream via SignalR
- [ ] Manual intervention / CAPTCHA hand-off
- [ ] Test with a real, permitted application workflow

## Phase 6 — Refinement
- [ ] Application history page (job, URL, provider/model, times, interventions, errors, outcome)
- [ ] Auto-mark job Applied only after verified submission
- [ ] Extraction reliability improvements
- [ ] Error handling, retries and structured logging review
- [ ] Prompt-injection safeguards review
- [ ] Provider configuration UI / settings
- [ ] Optional cloud deployment preparation (PostgreSQL migration, containerised browser)
