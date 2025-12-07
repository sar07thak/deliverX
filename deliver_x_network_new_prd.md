Migration PRD — Convert React SPA + Separate API → ASP.NET Core MVC (Clean Architecture)
1 — Purpose
Migrate the existing application frontend (React SPA) and backend (separate API) into a unified ASP.NET Core MVC web application that follows Clean Architecture principles. The goal is to simplify deployment/maintenance, reduce client-side complexity where appropriate, leverage server-side rendering for SEO / faster first paint for heavy pages (admin/dashboard, DPCM, DBC), and maintain or improve system reliability, security and maintainability.
This PRD covers:
•	architecture design & folder structure
•	migration strategy (big bang vs incremental)
•	feature parity & functional equivalence
•	testing, rollback, and cutover approach
•	acceptance criteria
•	milestone (sprint) plan with tasks and story points
________________________________________
2 — Goals & Success Metrics
Primary goals
•	Maintain full feature parity with existing React + API flows for all user roles (DP, DPCM, DBC, EC, Inspector, Super Admin).
•	Implement Clean Architecture: Domain (Core), Application, Infrastructure, WebUI (ASP.NET Core MVC).
•	Centralize view rendering for business/dashboards into Razor, keep light client-side JS for richer interactions only where needed.
•	Keep or improve performance & SEO for relevant pages.
•	Ensure smooth migration with minimal disruption to existing active users (support backward compatibility for any mobile / API consumers).
Success metrics
•	All critical user journeys (onboarding, create delivery, accept, POD, wallet settlement, complaint->inspection) work end-to-end via MVC.
•	Automated integration/e2e tests covering 90% of critical paths.
•	No regression in matching performance (observed metrics equal or better).
•	Clean architecture code with unit tests and documented module boundaries.
________________________________________
3 — High-Level Strategy
Two-path migration (recommended — incremental and safe)
1.	Side-by-side approach — keep existing API & React in production while building MVC WebUI that calls the same Application layer (or new internal API/mediator) gradually. This reduces risk.
2.	API consolidation — refactor backend into Clean Architecture layers (Domain/Core → Application/Services → Infrastructure) exposing internal interfaces. The existing public API(s) can remain for external consumers while internal controllers call Application services directly.
3.	Page-by-page cutover — move sections to MVC one at a time (Admin & DPCM dashboards first — server heavy; DP mobile flows last if mobile PWA necessary).
4.	Feature flagging & progressive rollout — each feature has a feature flag to toggle MVC vs React.
Alternative (if you want fast cutover)
•	Big-bang rewriting entire UI in Razor — not recommended unless product small and QA bandwidth high.
________________________________________
4 — Clean Architecture proposed layout (codebase)
/src
  /DeliverX.Core            // Domain models, interfaces, enums, value objects
  /DeliverX.Application     // Use cases, DTOs, MediatR handlers, business rules
  /DeliverX.Infrastructure  // EF Core, Repositories, External API clients (KYC, PAN, Police, Payment), Email/SMS, File Storage
  /DeliverX.Web             // ASP.NET Core MVC (Razor Views), Controllers, ViewModels, client JS, static assets
  /DeliverX.Api (opt)       // Lightweight API for 3rd-party/mobile (kept backwards compatible)
  /DeliverX.Workers         // Background services, queue workers (matching, notifications)
  /tests
    /DeliverX.UnitTests
    /DeliverX.IntegrationTests
    /DeliverX.E2ETests
Key design points
•	Use MediatR (or own application service layer) for commands/queries to keep controllers thin.
•	Domain entities contain business invariants; Application layer exposes use-cases.
•	Infrastructure layer implements repository interfaces; use EF Core with migrations.
•	Use DTOs / ViewModels separate from domain models. Map with AutoMapper (or hand-map in critical flows).
•	Razor Views use strongly-typed ViewModels; where complex interactions exist, use server-side partials + small React/Vue components (optional) or Alpine/HTMX for lightweight client behavior.
•	Keep a compatibility adapter if external mobile apps still call old public APIs.
________________________________________
5 — Technical decisions & libraries
•	.NET Version: .NET 8+ (use your supported LTS in infra)
•	ASP.NET Core MVC for Web UI with Razor Pages / Views.
•	EF Core for database access (use repository / unit-of-work patterns) or Dapper for performance-critical queries.
•	MediatR (CQRS style) for application layer; or classic Service + Repository pattern if preferred.
•	Identity: ASP.NET Core Identity with JWT and cookie auth for web. RBAC using policy-based authorization.
•	Caching: Redis for session/cache.
•	Background/Queue: RabbitMQ / Azure Service Bus and a Worker project for matching & notifications.
•	Real-time: SignalR for delivery status updates (optional).
•	Map / Geo: use SQL Server spatial types and server-side geospatial queries.
•	Front-end helper: use unobtrusive AJAX (jQuery) only where needed for partial updates. Optionally keep small React components for DP mobile app flows but render server-side.
•	CI/CD: Extend to build .NET solution, run tests, publish to hosting (IIS/Azure App Service/Kubernetes).
________________________________________
6 — Migration considerations (functionality mapping)
You need a clear mapping from React components + API endpoints → MVC pages/controllers + Application calls.
Map examples
•	React DP_Onboarding page → MVC DpController.Onboard (GET/POST), Views/Dp/Onboard.cshtml
•	React DPCM_Dashboard SPA → MVC DpcmController.Index (server-rendered dashboard with server-side pagination & charts)
•	React DeliveryCreate form → DeliveryController.Create POST; server validates and calls application layer to run matching pipeline asynchronously.
API compatibility
•	Preserve public API endpoints for external mobile clients. Provide a migration adapter or minimal API to proxy to application layer.
•	Mark deprecated endpoints in API docs, and plan for eventual removal.
________________________________________
7 — UX & UI changes and decisions
•	Existing React UI can be ported to Razor templates for consistent styling, reuse styles (CSS, Tailwind or Bootstrap). Keep look/feel identical initially to reduce user retraining.
•	For dashboard heavy pages (data grids, charts), generate server-side HTML and use minimal client-side for interactivity (sorting, filters via AJAX).
•	Make DP mobile flows PWA-friendly: either keep a thin SPA for DP mobile app or build responsive Razor pages with client-side enhancements. Decision depends on DP usage patterns — if DPs use mobile web primarily, keep a small SPA-like experience (use progressive enhancement).
________________________________________
8 — Backward compatibility & coexistence
Strategy
•	Keep public API available for mobile/3rd-party until migration complete.
•	Implement feature flags to switch users to MVC interface gradually.
•	Maintain DB schema compatibility; migration scripts must be idempotent.
•	Introduce a compatibility middleware to route requests to legacy API or new app depending on headers/flags.
________________________________________
9 — Security & compliance considerations during migration
•	Maintain encryption & PII handling rules (same as PRD). Don’t expose new vulnerabilities via server-rendered HTML (XSS mitigations).
•	CSRF: Use anti-forgery tokens for all state-changing MVC posts.
•	Input validation: Validate at both controller and application level.
•	Audit: ensure audit logs capture actor, action, timestamp across new controllers.
________________________________________
10 — Acceptance Criteria (global)
•	All active business-critical flows have equivalent behavior in MVC (user can complete tasks end-to-end).
•	No data loss when switching flows.
•	Matching & settlement outputs match previous system within expected tolerance.
•	All new pages are secured by role-based authorization.
•	Automated test suite (unit + integration + e2e) covers core flows and executes successfully in CI.
•	Feature flags allow immediate rollback per feature without DB restore.
________________________________________
Migration Milestones (Sprint-style Plan)
Below is a sequenced plan divided into 12 sprints (logical units). Each sprint lists objectives, tasks, deliverables, story points (estimate granularity for capacity planning), test focus, and acceptance criteria. Use your team’s velocity to map sprints to calendar dates.
Note: story points are relative effort units (T-shirt size style in Fibonacci). Adjust to your team’s scale.
________________________________________
Sprint 0 — Discovery, Code Audit & Planning (Spike) — 5 SP
Objectives
•	Audit current React app & API codebase.
•	Inventory React components, API endpoints, data contracts, and third-party integrations.
•	Identify pages to migrate first (priority: Admin/DPCM → DBC → DP → EC → Inspector).
•	Define compatibility plan & feature flags.
•	Create migration checklist and test matrix.
Tasks
•	Static code analysis & architecture review.
•	Mapping matrix: React component ↔ MVC view/controller ↔ API endpoints.
•	Define required infra changes (CI/CD, deployment).
•	Define DB migration plan and schema changes.
•	Create risk register & rollback plan.
Deliverables
•	Migration plan doc + mapping spreadsheet.
•	Prioritized feature list for sprints.
•	Test plan & acceptance criteria.
Acceptance
•	Mapping doc completed and reviewed by stakeholders.
•	CI changes outlined.
________________________________________
Sprint 1 — Project Scaffolding & Clean Architecture Baseline — 8 SP
Objectives
•	Create project skeleton (Core, Application, Infrastructure, Web).
•	Setup common infra (DI, logging, configuration, secrets).
•	Setup CI pipeline skeleton to build solution and run unit tests.
Tasks
•	Initialize Git branch & repository structure.
•	Implement layers and sample domain model & a skeleton controller.
•	Configure DI container & MediatR wiring.
•	Implement logging (Serilog) & config (appsettings with env overrides).
•	Setup Unit Test project and basic test template.
Deliverables
•	Working scaffolding that builds and runs.
•	CI pipeline triggers builds and tests.
Acceptance
•	Solution builds in CI; minimal smoke test passes (GET /health returns 200).
________________________________________
Sprint 2 — Authentication, IAM & User Management — 8 SP
Objectives
•	Migrate Authentication flows: OTP login, ASP.NET Identity or hybrid cookie + JWT for APIs.
•	Implement RBAC & role management (Super Admin, DPCM, DP, DBC, EC, Inspector).
Tasks
•	Implement Identity setup (store users in DB, role seeding).
•	OTP infrastructure (send/verify — tie into existing SMS provider).
•	Controllers & Views for Login, Logout, Profile.
•	Policy-based authorization & middleware.
Deliverables
•	Login / logout / role-based access working.
•	Admin can manage roles via MVC UI.
Acceptance
•	Roles enforced on sample protected routes.
•	OTP login reproduces previous auth tokens (or provides mapped session).
________________________________________
Sprint 3 — KYC & Onboarding (DP & DPCM) — 13 SP
Objectives
•	Migrate DP & DPCM registration flows to MVC.
•	Implement KYC request creation & display in UI (Aadhaar/PAN/Police verification stubs).
•	Implement rules to prevent duplicate Aadhaar/phone/PAN registrations.
Tasks
•	Build DpController, DpcmController and their views (wizard-style onboarding).
•	Backend services in Application layer for KYC request creation and status tracking.
•	Add file upload handling for documents (store in blob storage).
•	Validation & duplicate checks with hashed fields.
Deliverables
•	Onboarding wizard available in MVC.
•	KYC status page & admin queue.
Acceptance
•	New DP can onboard end-to-end via MVC with KYC request created. Duplicate registration blocked as per rule.
________________________________________
Sprint 4 — Service Area, Pricing & Mapping — 8 SP
Objectives
•	Implement service area setup UI (map + radius slider).
•	Persist spatial data (center lat/lng + radius).
•	Ensure spatial queries work from server side.
Tasks
•	Integrate map provider (Google Maps / MapMyIndia) in Razor pages.
•	Implement ServiceArea domain model + repository.
•	Implement spatial queries (SQL geography) for matching service areas.
Deliverables
•	Service area UI & server persisted data.
•	Endpoint to compute whether a point is inside service area.
Acceptance
•	Admin/tester can create service area and server correctly checks inclusion.
________________________________________
Sprint 5 — Delivery Creation & Matching Engine Integration (Part 1) — 13 SP
Objectives
•	Migrate delivery creation UI and integrate with matching service.
•	Implement server-side matching trigger and candidate suggestions.
Tasks
•	Create DeliveryController with Create form (server-rendered form + AJAX).
•	Implement Application use-case for create-delivery and match-candidates.
•	Implement candidate search logic using spatial queries and price calculations in Application layer (reuse existing matching logic where possible).
•	Basic notifications (via push or SMS stub).
Deliverables
•	Delivery creation page with candidate list shown.
•	Matching logs and visibility in admin.
Acceptance
•	Create delivery, matching returns candidate list equal to previous API behavior for tested cases.
________________________________________
Sprint 6 — Delivery Lifecycle & POD — 13 SP
Objectives
•	Implement assignment, accept/reject flows, delivery state machine and POD capture in MVC.
•	Implement server-side validation for state transitions.
Tasks
•	Add endpoints for Accept/Reject/Pickup/Deliver with anti-forgery tokens.
•	POD upload: photo store & metadata (recipientName, OTP).
•	Delivery events logging & admin inspector interface to view state history.
Deliverables
•	MVC pages for assigned deliveries and active delivery view.
•	POD handling with stored evidence.
Acceptance
•	Full delivery flow from creation to POD works in MVC for end-to-end cases.
________________________________________
Sprint 7 — Wallet, Billing & Subscriptions (Part 1) — 13 SP
Objectives
•	Migrate wallet UI & implement wallet ledger, top-up stub, and settlement entries.
•	Implement subscription model & billing engine hooks.
Tasks
•	Implement Wallet domain model, ledger transactions & DB views for reconciliation.
•	MVC pages for wallet balance, transactions & top-up flow (gateway integration stub).
•	Subscription plan management pages for DBC.
Deliverables
•	Wallet UI + test top-up flow (sandbox).
•	Invoice generation stub.
Acceptance
•	Ledger transactions recorded; sample top-up increases wallet balance in DB.
________________________________________
Sprint 8 — Complaints & Inspector Flow — 13 SP
Objectives
•	Implement complaint filing UI & inspector assignment UI.
•	Implement inspector result submission & penalty application flow.
Tasks
•	Build ComplaintsController & InspectorController with views and file uploads.
•	Application logic to apply penalties (wallet debits) when inspector verdict valid.
•	Notifications & audit logs for complaint lifecycle.
Deliverables
•	Full complaint -> inspector -> resolution flow in MVC.
•	Penalty application shown in wallet ledger.
Acceptance
•	Complaints can be filed and inspected; verdict applies wallet change.
________________________________________
Sprint 9 — DPCM & Admin Dashboards (Analytics) — 13 SP
Objectives
•	Port DPCM and Super Admin dashboards to server-side MVC with charts & paginated lists.
•	Implement server-side filtering and export (CSV/Excel).
Tasks
•	Build dashboards (deliveries summary, KYC queue, earnings, complaints).
•	Integrate charting libraries (server-fed data) and UI optimizations.
•	Implement export endpoints.
Deliverables
•	Usable DPCM & Admin dashboards in MVC.
•	Exportable reports and admin actions (ban/unban, KYC approve).
Acceptance
•	Dashboards render correct data & export functions produce valid CSV.
________________________________________
Sprint 10 — Migration, Backwards Compatibility & API Adapter — 13 SP
Objectives
•	Provide adapter layer for old API endpoints (if needed).
•	Route/feature flagging to switch users to MVC flows.
•	Finalize data migrations or schema changes.
Tasks
•	Build compatibility controllers or light API (keeps public contracts).
•	Implement feature flags in config & admin UI to flip between React & MVC.
•	Run migration scripts to update DB if necessary (idempotent).
•	Smoke tests & blue/green deployment plan (or canary).
Deliverables
•	Adapter endpoints and feature-flag toggles.
•	Migration scripts.
Acceptance
•	When feature flag toggled, target users served by MVC with identical behavior.
________________________________________
Sprint 11 — Testing, E2E & Performance Tuning — 13 SP
Objectives
•	Implement automated E2E tests for critical flows.
•	Perform load testing & performance tuning for matching & DB queries.
•	Security testing & remediation (SAST/DAST findings).
Tasks
•	Create E2E tests (Playwright / Selenium) for onboarding, create delivery, accept, deliver, complaint.
•	Load test matching engine scenarios; identify bottlenecks; optimize queries & caching.
•	Run SAST/DAST and fix issues.
Deliverables
•	Passing E2E suite in CI.
•	Performance report and tuning changes.
Acceptance
•	E2E tests pass; matching latency within prior baseline; no critical security issues.
________________________________________
Sprint 12 — Cutover, Monitoring & Hardening — 13 SP
Objectives
•	Final cutover plan, monitoring, runbooks & production hardening.
•	Training for support teams; documentation & rollback runbook.
Tasks
•	Final smoke tests on pre-prod.
•	Create runbooks for incidents (KYC fail, matching fail, payment problems).
•	Doc for support: how to toggle flags, how to revert to old API.
•	Release candidate and production monitoring dashboards.
Deliverables
•	Production deployment artifacts & runbooks.
•	Post-migration support plan.
Acceptance
•	Team can switch traffic to MVC safely; runbooks validated.
________________________________________
Story Points Summary (quick view)
•	Sprint 0: 5 SP
•	Sprint 1: 8 SP
•	Sprint 2: 8 SP
•	Sprint 3: 13 SP
•	Sprint 4: 8 SP
•	Sprint 5: 13 SP
•	Sprint 6: 13 SP
•	Sprint 7: 13 SP
•	Sprint 8: 13 SP
•	Sprint 9: 13 SP
•	Sprint10: 13 SP
•	Sprint11: 13 SP
•	Sprint12: 13 SP
Total: ~144 story points (adjust to your team’s velocity). You can split or combine sprints based on resource constraints.
________________________________________
Testing & Rollback Strategy
Testing
•	Unit tests for Application & Domain logic (fast feedback).
•	Integration tests for DB & queue interactions.
•	Contract tests to ensure API parity if keeping public API.
•	E2E tests for each role’s critical path (onboarding, create-delivery, accept, POD).
•	Load tests for matching engine and DB hotspots.
•	Security scans (SAST/DAST) before production.
Rollback
•	Use feature flags to instantly revert a page to React or the old API for user subset.
•	Database migrations must be backwards-compatible or have down-migration scripts (avoid destructive changes without feature gating).
•	Maintain ability to route traffic to the legacy API during rollback.
•	Maintain snapshot backups for quick DB restore if needed.
________________________________________
Risks & Mitigations Specific to Migration
1.	Risk: Loss of parity / regression in matching logic.
Mitigation: Reuse existing matching logic (copy to Application layer); add contract/integration tests comparing outputs for sample dataset.
2.	Risk: User disruption (DPs rely on mobile-first React).
Mitigation: Keep DP mobile flows as PWA or keep small React components for DP; migrate DP flows last; use feature flags.
3.	Risk: Increasing surface for XSS or CSRF with server-side rendering.
Mitigation: Use anti-forgery tokens, encode outputs, Content-Security-Policy headers, input validation.
4.	Risk: Large refactor creates long-lived branches.
Mitigation: Use short-lived feature branches, trunk-based development, and incremental merges per sprint.
5.	Risk: Performance degradation due to server-side rendering of heavy pages.
Mitigation: Cache rendered fragments, use Redis, lazy-load heavy JS, and offload charts to server pre-computed APIs.
________________________________________
Operational items & CI/CD changes
•	Build pipeline to:
o	compile .NET solution, run unit tests
o	build static assets (SCSS, client-side modules) if kept
o	produce artifacts (zip/docker image)
o	deploy to staging (blue/green or canary)
•	DB migrations automated (EF migrations with validation in pre-prod).
•	Health checks endpoints for liveness & readiness.
•	Observability: instrument middleware for request durations, Serilog sinks, error reporting.
•	Feature flag management: use configuration service (AppConfig, LaunchDarkly, or DB-driven flags).
________________________________________
Deliverables for handoff to team
1.	Migration mapping spreadsheet (component → view/controller → APIs).
2.	Clean Architecture scaffold in repo (Core/Application/Infra/Web).
3.	Sprint backlog with user stories & acceptance criteria (ready for sprint planning).
4.	Test plan and E2E test cases (Playwright / Selenium scripts).
5.	Runbooks for release and rollback.
6.	Monitoring & dashboard list.
________________________________________
Acceptance Criteria (Project Done)
•	All critical user journeys operate end-to-end in ASP.NET Core MVC with parity.
•	CI pipeline runs full test suite & deployment is automated.
•	Feature flagging in place with tested rollback path.
•	No regression in matching / settlement logic (verified via contract tests).
•	Documentation and runbooks completed and support team trained.
