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
- [x] Details panel docked as a split view (not a pop-up): details on the left, job list on the right
- [x] Resizable split: drag the divider, arrow keys (Shift = bigger steps), double-click to reset
- [x] Chosen panel width remembered between visits
- [x] Job list stays usable while details are open; table columns adapt to the available width
- [x] On phones the details open full screen and Close returns to the list
- [x] Responsive layout (desktop, tablet, phone)
- [x] Untrusted content rendered as plain text; only http(s) links allowed

### Quality
- [x] 36 backend tests (domain, services on real SQLite, constraints, HTTP integration)
- [x] 29 frontend tests (API client, store, details panel, split resizer, helpers)
- [x] Manual end-to-end check in Chromium (desktop + mobile)
- [x] GitHub Actions CI (build + tests for backend and frontend)
- [x] README with setup and run instructions

## Phase 2 — Gmail Integration
- [ ] Google Cloud OAuth setup guide (gmail.readonly scope)
- [ ] OAuth 2.0 sign-in flow and secure token storage (Data Protection)
- [ ] Gmail sync service (configurable query / labels / senders)
- [ ] Idempotent storage of emails (Gmail message id)
- [ ] Job extraction: deterministic hints for LinkedIn, Indeed, Reed, Totaljobs, JobServe, agencies
- [ ] AI-assisted structured extraction (`IJobExtractionService`) with nulls for unknown fields
- [ ] Multiple jobs per email
- [ ] Tracking-URL canonicalisation and safe redirect resolution
- [ ] De-duplication (source job id, canonical URL, other reliable attributes)
- [ ] Scheduled background sync (configurable interval)
- [ ] Manual "Sync Jobs" button enabled
- [ ] SignalR sync progress / new-job notifications
- [ ] Retry policies and failed-email reprocessing
- [ ] Tests for parsing, dedup and sync idempotency

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
- [ ] Provider configuration (provider, model, credentials via user secrets / env vars)
- [ ] One working AI provider adapter (Microsoft.Extensions.AI)
- [ ] Provider capability flags (structured output, vision, computer-use)
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
