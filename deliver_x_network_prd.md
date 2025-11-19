# DeliverX Network — Product Requirements Document (PRD)

**Version:** 1.0  
**Date:** 2025-11-01  
**Prepared by:** Senior .NET Core Solution Architect & Product Strategist

---

## Executive Summary

DeliverX Network (codename) is an India-wide Open Network for Delivery (OND) that connects micro-delivery partners (students, rickshaw drivers, SHG members, gig workers) with businesses and end-consumers who need local last-mile, intra-city and hyperlocal deliveries.

The platform focuses on: rapid onboarding (Aadhaar eKYC, PAN verification, Police verification), configurable pricing per KM/per KG, aggregator-driven territory management, transparent commissions and settlements, wallet & donation flows, inspector-based complaint resolution, and scalable matching of orders to serviceable delivery partners.

This PRD is designed to be exhaustive for product, engineering, QA, and operations teams and covers MVP scope, architecture, APIs, data model overview, business rules, compliance, and roadmap.

---

# Table of Contents

1. Vision & Objectives
2. Business Objectives & Success Metrics
3. Product Scope (MVP vs Later)
4. Target Users & Personas
5. Functional Requirements (detailed modules)
6. Non-functional Requirements (NFR)
7. Data Model Overview
8. API Design (selected endpoints & payloads)
9. UI / UX — Screens & Interactions
10. Workflows & Sequence Diagrams (textual)
11. Security & Compliance
12. Architecture & Infrastructure
13. Testing & Quality Assurance
14. Monitoring & Operations
15. Legal, Privacy & Policy
16. Operational Model & Go-To-Market
17. Business Model & Revenue Projections
18. Roadmap & Release Plan
19. Metrics to Monitor Post-Launch
20. Risks & Mitigations
21. Acceptance Criteria & DoD
22. Documentation & Handover
23. Edge Cases & Business Rules
24. Glossary
25. Next Steps & Deliverables

---

# 1. Vision & Objectives

**Vision:** Build a trusted, scalable, and monetizable open delivery grid across India enabling micro-entrepreneurs to earn and businesses to get reliable hyperlocal delivery.

**Objectives:**
- Rapid, secure onboarding at scale (Aadhaar, PAN, Police verification).  
- Fair, transparent pricing per KM & per KG with aggregator commission layers.  
- High fulfillment & low complaint rates via rating & inspector processes.  
- Multiple monetization channels: subscriptions, per-delivery fees, API-hits, wallet flows, donations.

---

# 2. Business Objectives & Success Metrics

**Business Objectives**
- Recruit 100,000 Delivery Partners (DP) in Year 1 (target).  
- Onboard 10,000 active Delivery Business Consumers (DBC) within 12 months.  
- Achieve break-even within 18 months through subscription + per-delivery fees.

**Key Metrics (KPIs)**
- KYC completion rate (%).  
- DP activation (registered → active) within 7 days.  
- Deliveries per day/week/month.  
- Average revenue per delivery (platform commission + subscription).  
- Acceptance rate of assigned deliveries.  
- Complaint rate per 1,000 deliveries.  
- Time to resolution for disputes (target < 72 hours).  
- DAU (Delivery Partners) and DBC churn.

---

# 3. Product Scope

## MVP (Core)
- Role-based user management (Super Admin, DPCM, DP, DBC, EC, Inspector).  
- Registration & KYC stubs (Aadhaar, PAN, Police verification placeholders).  
- DP and DPCM onboarding flows, service area setup, pricing.  
- Delivery creation, basic matching (price + rating), assignment flow.  
- DP acceptance, POD upload, wallet basics, referral, donation module.  
- Basic complaint + inspector assignment workflow.  
- Admin & DPCM dashboards, notifications.

## Phase 2+ (Post-MVP)
- Real-time Aadhaar & PAN integrations, Police verification.  
- Advanced matching (proximity + price + rating + availability).  
- Live GPS tracking & ETA, surge pricing, advanced inspector tools, ML for fraud detection.  
- Enterprise APIs & webhooks, multilingual support, ONDC integration option.

---

# 4. Target Users & Personas

### Roles
- **Super Admin:** Platform owner with full control.  
- **Delivery Partner Channel Manager (DPCM):** Aggregators across regions; can onboard DPs under them, set commission & areas.  
- **Delivery Partner (DP):** Micro-entrepreneur delivering goods.  
- **Delivery Business Consumer (DBC):** Retailers, quick-commerce, e-commerce operators creating deliveries.  
- **End Consumer (EC):** Recipients or senders for personal deliveries; OTP login.  
- **Inspector:** Field investigator validating complaints.

### Example Personas (short)
- *Ravi*: 20-year-old student, signs up as DP for pocket money; prefers ASAP gigs within 5km radius.  
- *Mrs. Sharma*: Owner of local kirana store; needs daily multiple deliveries; uses web portal and subscribes to monthly plan.  
- *Amit*: DPCM managing 250 DPs in a city; needs dashboards and settlement reports.

---

# 5. Functional Requirements (Detailed Modules)

> Each submodule includes a description, detailed behavior, and acceptance criteria.

## 5.1 Identity & Access Management (IAM)
- RBAC with fine-grained permissions.  
- Authentication methods: OTP (phone) for EC & DP; email/password (+2FA) for DBC, DPCM, Admin.  
- JWT-based tokens with refresh tokens.  
- Device/session management and token revocation.

**Acceptance:** Only authorized roles access protected endpoints; refresh token rotation implemented.

## 5.2 Registration & KYC

### DP Registration fields (MVP)
- Full name, phone (OTP), DOB, profile photo, Aadhaar hash, PAN, address, bank details, vehicle type, languages, availability, perKM, perKg, minCharge, service radius, police verification consent.

### DPCM registration
- Org name, contact person, PAN, Aadhaar hash, bank details, service regions, commission rate.

### DBC registration
- Business name, contact, GSTIN (optional), API key generation (for enterprise), plan/subscription.

### EC registration
- Phone OTP; saved addresses.

### KYC Verifications
- Aadhaar eKYC (UIDAI/DigiLocker) workflow (or manual upload & human verification).  
- PAN verification via API.  
- Police verification via API or third-party vendor.  
- Bank verification via micro-deposit or NPCI Mandate.

**Business rule:** If a KYC is registered under a DPCM, identical Aadhaar/phone/PAN cannot self-register as DP.

**Acceptance:** KYC flow must be auditable; duplicate registration blocked.

## 5.3 Service Area & Geofencing
- DP or DPCM define service area as circle: {centerLat, centerLng, radiusKm}.  
- Optional polygon support later.  
- Geospatial queries use SQL Server geography or PostGIS.

**Acceptance:** Deliveries only match DPs whose area covers pickup/drop per configured rule.

## 5.4 Pricing & Commission Model
- Pricing = perKM * distance + perKg * weight + minCharge + surcharges + taxes.  
- DPCM can set a perKM/perKg add-on; platform commission configurable (flat or %).  
- Surcharges configurable (e.g., consumer misbehavior).  

**Acceptance:** Billing engine provides detailed breakdown; admin-configurable rules apply.

## 5.5 Delivery Creation & Matching
- Delivery payload: requesterId, pickup, drop, weight, packageType, scheduledAt, priority.  
- Matching algorithm (MVP): filter DPs by service area & availability → sort by price asc, rating desc, proximity tie-breaker → notify top N.

**Acceptance:** Matching returns candidate list and triggers notification within configured timeout.

## 5.6 Delivery State Machine & POD
- States: CREATED → ASSIGNED → ACCEPTED → PICKED_UP → IN_TRANSIT → DELIVERED → CLOSED.  
- POD options: Photo + Recipient Name + OTP.  
- EC can hide deliveries in UI; system keeps records for audit with retention policy.

**Acceptance:** All deliveries must store POD; transitions audited.

## 5.7 Ratings & Behavior Index
- Ratings (1–5) with tags; behavior index computed from ratings, punctuality and complaints.  
- Consumer reliability index to deter false complaints.

**Acceptance:** Ratings influence matching rank for configured period.

## 5.8 Complaints & Inspector Flow
- Complaints logged with attachments; auto-triaged and assigned to DPCM/Inspector.  
- Inspector creates inspection record with evidence; outcome triggers penalties/refunds.

**Acceptance:** Inspector results actionable by admin; penalties applied automatically per policy.

## 5.9 Wallet, Payments & Settlements
- Wallet ledgered per user.  
- Top-ups via UPI/gateway; payouts via payout APIs (payout batch for settlements).  
- Settlement cadence configurable (daily/weekly) with threshold checks.

**Acceptance:** Wallet reconciliation accurate; payouts include audit trail.

## 5.10 Subscription & Billing Engine
- Plans: Free (per-delivery), Monthly/Quarterly/Annual with reduced commissions.  
- API hit billing for enterprise users.  
- Proration and auto-renewal supported.

**Acceptance:** Invoice generation automated and GST-ready.

## 5.11 Referral & Donation Modules
- Referral codes with multi-level configuration and reward triggers post-validation.  
- Donation/tip flows after delivery; optional fee on donations.

**Acceptance:** Referral & donation ledger entries accurate and traceable.

## 5.12 Admin & DPCM Dashboards
- Admin: full metrics, user management, KYC queues, income reports, inspector assignments.  
- DPCM: DP list, pending KYC, top performers, settlement reports.

**Acceptance:** Dashboards display key metrics under 5 seconds with pagination.

---

# 6. Non-functional Requirements (NFR)

**Performance:**
- Scale target: 1M users, 100k daily deliveries in 12 months.  
- Read API latency P95 < 300ms; matching < 1s under normal load.

**Availability:**
- Target uptime 99.9% for core APIs.

**Security:**
- OWASP Top 10 mitigations, TLS 1.2+, encryption at rest (AES-256) for PII, JWT tokens with refresh rotation, audit logs, RBAC.

**Compliance:**
- DPDP Act mapping, UIDAI rules (don’t store raw Aadhaar), GST invoicing.

**Observability:**
- Centralized logs (ELK/Application Insights), metrics in Grafana, distributed tracing (OpenTelemetry).

**Backup & DR:**
- Daily DB backups with PITR; region-based replication optional.

---

# 7. Data Model Overview (High level)

> The schema below is conceptual; implement with normalization and indexes.

**Core entities:** Users, Profiles, DeliveryPartners, DPCManagers, BusinessConsumers, EndConsumers, Inspectors, KYCRequests, ServiceAreas, Deliveries, DeliveryEvents, PODs, Ratings, Complaints, Inspections, Wallets, WalletTransactions, Subscriptions, Plans, Referrals, AuditLogs, Invoices.

**Spatial data:** ServiceAreas store centerLat/centerLng/radius or polygon. Use geography type for indexing.

**Example relationships:**
- Users 1--1 Profiles, Users 1--N WalletTransactions, DPCManagers 1--N DeliveryPartners, Delivery 1--1 POD, Delivery 1--N DeliveryEvents.

---

# 8. API Design (Selected Endpoints & Sample Payloads)

**Base path:** `/api/v1/`

### Authentication
- `POST /auth/otp/send`  
  Request: `{ "phone": "XXXXXXXXXX" }`  
  Response: `{ "status": "OTP_SENT" }`

- `POST /auth/otp/verify`  
  Request: `{ "phone": "...", "otp": "123456" }`  
  Response: `{ "accessToken": "...", "refreshToken": "..." }`

- `POST /auth/login` — email/password for DBC/DPCM/Admin

### KYC
- `POST /kyc/aadhaar/initiate`  
  Request: `{ "userId": "...", "aadhaarNumberHash": "sha256(...)" }`

- `POST /kyc/pan/verify`  
  Request: `{ "userId": "...", "pan": "ABCDE1234F" }`

### DP Registration
- `POST /dp/register`  
  Payload: DP profile + rates + serviceArea

### DPCM Add DP
- `POST /dpcm/{id}/add-dp`  
  Payload: DP user object + pricing + serviceArea

### Delivery Creation
- `POST /deliveries`  
  Payload:

```json
{
  "requesterId":"dbc-123",
  "pickup": { "lat": 26.9124, "lng": 75.7873, "address": "ABC" },
  "drop": { "lat": 26.9050, "lng":75.7840, "address":"XYZ" },
  "weightKg": 2.5,
  "packageType": "parcel",
  "scheduledAt": "2025-11-02T10:00:00Z",
  "priority": "ASAP"
}
```

### Matching
- `POST /deliveries/{id}/match` — triggers matching; returns candidates.

### Delivery Lifecycle
- `POST /deliveries/{id}/accept` — DP accepts
- `POST /deliveries/{id}/pickup` — mark picked up
- `POST /deliveries/{id}/deliver` — include POD: `{ "recipientName": "Ram", "otp":"4321", "podPhotoUrl":"..." }`

### Wallet
- `GET /wallets/{id}` — fetch wallet
- `POST /wallets/{id}/topup` — initiate top-up
- `POST /wallets/{id}/withdraw` — request withdrawal

### Complaints & Inspections
- `POST /complaints` — lodge complaint
- `POST /complaints/{id}/assign-inspector` — admin assign
- `POST /inspections/{id}/resolve` — inspector verdict

### Admin
- `GET /admin/metrics` — KPI dashboard
- `PATCH /admin/settings` — update fee rules

> Implement OpenAPI (Swagger) spec for all endpoints. Ensure consistent error format: `{ code, message, details }`.

---

# 9. UI / UX — Screens & Interactions

**Principles:** Mobile-first PWA for DPs and ECs; responsive web for DBC, DPCM and Admin. Clean flow, minimal typing for DPs, progressive disclosures for KYC.

### DP (Mobile)
- Onboarding wizard (phone → OTP → profile → KYC upload)  
- Service area map (pin + radius slider)  
- Earnings / Today & Week  
- Assigned deliveries list (accept/reject)  
- Active delivery map (nav + contact + call)  
- POD capture (camera), OTP/recipient capture  
- Wallet & withdraws  
- Ratings & complaints history

### DPCM (Web & Mobile)
- DP onboarding page (single + bulk CSV import)  
- DPs list with filters & KYC pending  
- Pricing & commission settings  
- Settlement & payout summary  
- Complaints & inspector view

### DBC (Web Portal)
- Delivery creation (single & bulk)  
- API key management & billing overview  
- Subscription & invoices  
- Tracking view (realtime)

### EC (Mobile)
- OTP login  
- My deliveries with privacy controls (hide/delete)  
- Quick-send flow (small parcel)  
- Tip / Donate & Rate

### Inspector (Mobile)
- Assigned cases list  
- Evidence capture (photos + geo)  
- Verdict submission  
- Payment record

### Super Admin (Web)
- Full KPI dashboards  
- KYC & complaint queues  
- Financial reports & settlement controls  
- Inspector management & audit logs

---

# 10. Workflows (Textual Sequence)

## A. DP Onboarding via DPCM
1. DPCM creates DP from UI or CSV.  
2. DP receives OTP to confirm phone.  
3. DP completes Aadhaar eKYC or uploads docs.  
4. PAN & Police verifications initiated async.  
5. KYC record updated; DP activated when all mandatory verifications pass.

## B. Delivery Creation → Completion
1. DBC/EC creates delivery.  
2. Matching engine finds candidates → notifies top N.  
3. DP accepts → picks up → updates status → delivers with POD.  
4. DBC/EC rates; funds moved to DP wallet per settlement rules.

## C. Complaint Resolution
1. Complaint lodged with evidence.  
2. System triages and assigns to Inspector if needed.  
3. Inspector uploads evidence & verdict.  
4. If valid, penalty & refund applied; if invalid, closed.

---

# 11. Security & Compliance

## KYC & PII handling
- Store only hashed Aadhaar/PAN and externalRef IDs.  
- Encrypt PII at rest using KMS (Azure KeyVault / AWS KMS).  
- Restrict access via RBAC and log all access in AuditLogs.

## Payment & Wallet
- Use PCI-compliant payment gateway.  
- Tokenize bank account/payout details.  

## Legal & Policy
- Terms of service, privacy policy, DP independent contractor clause.  
- Recommended insurance options for high-value goods.

---

# 12. Architecture & Infrastructure

**Suggested Stack**
- Frontend: ASP.NET Core MVC (Razor) + PWA; React for data-heavy dashboards.  
- Backend: ASP.NET Core Web API (Clean Architecture), C# 11, .NET 8/9.  
- DB: SQL Server 2022 (primary) + Redis cache. Optionally PostGIS for complex spatial queries.  
- Queue: RabbitMQ or Azure Service Bus.  
- Object Storage: Azure Blob / AWS S3 for images & PODs.  
- Auth: ASP.NET Identity + JWT.  
- Payments: Razorpay/Stripe/Paytm.  
- Monitoring: AppInsights + ELK + Prometheus/Grafana.

**Deployment**
- Containers on AKS/EKS or Azure App Service.  
- CI/CD with Azure DevOps/GitHub Actions; infra as code (Terraform/ARM).  

**Scaling**
- Match service horizontally scalable; use queue workers for async tasks.  
- DB read replicas & partitioning by region when necessary.

**DR & Backup**
- Daily backups and cross-region replication; runbook for failover.

---

# 13. Testing & Quality Assurance

**Test types**
- Unit tests, integration tests, contract tests, E2E tests, load tests, security scans.

**Performance Testing**
- Simulate 10k match requests/min in load test.  
- Validate latencies & scaling behavior.

**Security Testing**
- SAST/DAST scans and dependency checks.

---

# 14. Monitoring & Ops

**Key metrics**
- API latency & error rates.  
- Queue backlog.  
- KYC success/failure rates.  
- Payment & settlement success rate.

**Alerts & Runbooks**
- High error rates, queue depth alerts, payment gateway downtimes.  
- Runbooks for incident response, rollback, and DB restores.

---

# 15. Legal, Privacy & Policy Considerations

- Explicit user consent for KYC & data usage.  
- Retention policy for KYC & financial data as per regulations.  
- Legal clause clarifying DP as independent contractor.  
- Clear policy on complaint resolution and penalties.

---

# 16. Operational Model & Go-To-Market

**Onboarding Channels**
- Partner DPCMs, SHGs, municipal bodies, educational institutions.  
- Digital marketing + local field teams for city-level rollouts.

**Incentives**
- Signup bonuses for early DPs; referral rewards; guaranteed minimums during pilot.

**Target Geography**
- Phase 1 target Tier-2 & Tier-3 cities, then expand to metros.

---

# 17. Business Model & Revenue Streams

- Per-delivery commission.  
- Subscription plans for businesses.  
- API hits billing.  
- Verification fees.  
- Ad and promotion revenues.

**Sample split (illustrative):** Customer pays ₹100; DP receives ₹65; DPCM commission ₹10; Platform ₹15.

---

# 18. Roadmap & Release Plan (6–12 months)

**Phase 0 (2 weeks):** Finalize PRD, infra setup, wireframes.  
**Phase 1 (0–8 weeks):** MVP core (auth, onboarding, matching, POD, wallet).  
**Phase 2 (9–16 weeks):** KYC integrations, billing, inspector flows, dashboards.  
**Phase 3 (17–28 weeks):** Live tracking, advanced matching, ML fraud detection, APIs.  
**Phase 4 (29–40 weeks):** Scale, enterprise onboarding, compliance & audits.

---

# 19. Metrics to Monitor Post-Launch

- KYC completion %, DP activation rate, acceptance rate, avg deliveries per DP/day, complaints per 1k deliveries, dispute resolution time, revenue per delivery.

---

# 20. Risks & Mitigations

1. **Slow KYC providers** — allow manual verification queue.  
2. **Fraudulent complaints** — inspector flow + evidence capture + consumer reliability index.  
3. **Regulatory changes** — store hashed Aadhaar & adaptable data model.  
4. **Peak DP shortage** — surge pricing & partner incentives.

---

# 21. Acceptance Criteria & Definition of Done (DoD)

- Unit tests & integration tests passing in CI.  
- Swagger/OpenAPI docs published.  
- Manual QA pass for core flows.  
- Performance tests meet thresholds.  
- Security scans clear.

---

# 22. Documentation & Handover

Deliverables:  
- OpenAPI spec + Postman collection.  
- ER diagrams & schema scripts.  
- Deployment & runbooks.  
- Admin & user manuals.

---

# 23. Edge Cases & Business Rules (Selected)

- DP accepts beyond capacity → system rejects and offers to alternate DP.  
- Delivery outside service area → prompt to expand area or assign DPCM-managed DP.  
- EC deletes delivery → hides from UI but kept in audit log for retention period.  
- Repeated invalid complaints → consumer surcharge & possible suspension.

---

# 24. Glossary

- DP — Delivery Partner.  
- DPCM — Delivery Partner Channel Manager.  
- DBC — Delivery Business Consumer.  
- EC — End Consumer.  
- POD — Proof of Delivery.  
- KYC — Know Your Customer.  
- OTP — One Time Password.  
- T+1 — Settlement one business day after completion.

---

# 25. Next Steps & Deliverables

I recommend the immediate next deliverables be:
1. **System Design Document (SDD)** — ER diagram, detailed API (OpenAPI spec), DTOs (C#), database schema SQL, matching engine pseudocode, sequence diagrams.  
2. **Sprint Plan for MVP** — 2-week sprints with stories, acceptance criteria, and resource assignment.  
3. **Wireframes / Clickable Prototype** — for DP mobile onboarding and DBC order flows.

---

*End of PRD v1.0*

