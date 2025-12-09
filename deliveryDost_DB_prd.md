Here’s a **backend + database PRD** you can drop straight into Cursor as `deliverx_backend_db_prd.md`.
I’ll align it with your stack and both PRDs.

---

    # DeliverX Backend & Database PRD

**Scope:** MS SQL + Dapper + ASP.NET Core MVC/API (Clean Architecture)
**UI Flow:**
UI (HTML5 + Bootstrap 4.8 + jQuery)
  ↓ AJAX
Controller (ASP.NET Core)
  ↓
Service (Business Logic)
  ↓
Repository (Dapper)
  ↓
SQL Stored Procedures
  ↓
Database Tables

---

## 1. Objectives

1. Design **robust SQL schema** supporting all core flows: onboarding, KYC, service area, delivery, matching, wallet, complaints, subscriptions, dashboards. 
2. Standardize **stored procedures, functions, views, TVPs, and jobs** to keep logic centralized and testable.
3. Provide a **Dapper-first repository pattern** that calls stored procedures only (no ad-hoc inline SQL in services).
4. Provide a **development tracker** to implement DB + Repositories systematically.

---

## 2. High-Level Backend Architecture (Clean + Dapper)

* **DeliverX.Core**

  * Entities: User, DeliveryPartner, DPCM, DBC, EC, Inspector, ServiceArea, Delivery, Wallet, Complaint, Subscription, etc. 

* **DeliverX.Application**

  * Services / Use Cases: `CreateDelivery`, `MatchDelivery`, `CompleteKyc`, `TopupWallet`, `ResolveComplaint`, etc.
  * DTOs & Commands/Queries.

* **DeliverX.Infrastructure**

  * Dapper-based repositories.
  * DB connection factory, transaction handling, mapping.
  * Implementation of repository interfaces.

* **DeliverX.Web / DeliverX.Api**

  * Controllers, ViewModels, JSON endpoints calling Application services.

---

## 3. Database Design Principles

1. **Primary keys:** `BIGINT IDENTITY` or `UNIQUEIDENTIFIER` (for external/public IDs).
2. **Soft delete:** `IsDeleted BIT`, `DeletedAt DATETIME2`.
3. **Audit:** `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`.
4. **PII:** Aadhaar/PAN only stored as **hash + externalRefId** as per main PRD. 
5. **No business logic in triggers** unless absolutely necessary (use stored procs + services instead).
6. Use **schemas** to group: `iam`, `kyc`, `user`, `delivery`, `billing`, `complaint`, `admin`, `ref`, etc.

---

## 4. Core Schemas & Tables

### 4.1 IAM & Users

**Schema:** `iam`

* `iam.Users`

  * `UserId (PK, BIGINT)`
  * `UserType (TINYINT)` — enum: DP, DPCM, DBC, EC, Inspector, Admin
  * `Phone`, `Email`, `PasswordHash` (for non-OTP roles)
  * `IsActive`, `LastLoginAt`, audit fields

* `iam.Roles`

  * `RoleId`, `Name` (Admin, DPCM, DP, DBC, EC, Inspector)

* `iam.UserRoles`

  * `UserId`, `RoleId`

* `iam.RefreshTokens`

  * `Token`, `UserId`, `ExpiresAt`, `RevokedAt`

* `iam.OtpLogs`

  * `OtpId`, `Phone`, `OtpCodeHash`, `Purpose`, `ExpiresAt`, `ConsumedAt`

### 4.2 Profiles & KYC

**Schema:** `user`, `kyc`

* `user.DeliveryPartners`

  * `DeliveryPartnerId (PK, BIGINT, FK -> Users)`
  * `FullName`, `Dob`, `ProfilePhotoUrl`
  * `AadhaarHash`, `PanHash`, `PoliceVerificationStatus`, `BankAccountMasked`, etc.
  * `VehicleType`, `Languages`, `ServiceRadiusKm`, etc. 

* `user.DPCManagers`

  * `DpcmId (PK, BIGINT, FK -> Users)`
  * `OrgName`, `PanHash`, `CommissionRate`, `ServiceRegionsJson`

* `user.BusinessConsumers`

  * `DbcId (PK, BIGINT, FK -> Users)`
  * `BusinessName`, `Gstin`, `ApiKey`, `PlanId`, etc.

* `user.EndConsumers`

  * `EcId (PK, BIGINT, FK -> Users)`
  * `DefaultAddressId (FK)`

* `user.Addresses`

  * `AddressId`, `UserId`, `Line1`, `City`, `State`, `PinCode`, `Lat`, `Lng`

* `kyc.KycRequests`

  * `KycRequestId`, `UserId`, `Type` (Aadhaar, PAN, Police, Bank)
  * `Status` (Pending, InProgress, Approved, Rejected)
  * `ExternalRefId`, `RequestedAt`, `CompletedAt`, `Remarks`

### 4.3 Service Areas & Geospatial

**Schema:** `geo`

* `geo.ServiceAreas`

  * `ServiceAreaId`, `OwnerUserId (DP or DPCM)`, `CenterLat`, `CenterLng`, `RadiusKm`, `SpatialGeography` (for SQL Server geography). 

### 4.4 Deliveries & Matching

**Schema:** `delivery`

* `delivery.Deliveries`

  * `DeliveryId (PK)`
  * `RequesterUserId` (DBC or EC)
  * `PickupAddressId`, `DropAddressId`
  * `WeightKg`, `PackageType`, `ScheduledAt`, `Priority`
  * `Status` (Created, Assigned, Accepted, PickedUp, InTransit, Delivered, Closed)
  * `AssignedDpId`, `AssignedDpcmId`, `MatchedAt`
  * `BasePrice`, `Surcharges`, `TaxAmount`, `TotalAmount`

* `delivery.DeliveryEvents`

  * `EventId`, `DeliveryId`, `EventType`, `EventAt`, `MetaJson`

* `delivery.ProofsOfDelivery`

  * `PodId`, `DeliveryId`, `RecipientName`, `OtpCodeHash`, `PodPhotoUrl`, `CapturedAt`

* `delivery.DeliveryRatings`

  * `RatingId`, `DeliveryId`, `RatedByUserId`, `TargetUserId`, `Rating (1–5)`, `Tags`, `Comments` 

### 4.5 Complaints & Inspection

**Schema:** `complaint`

* `complaint.Complaints`

  * `ComplaintId`, `DeliveryId`, `RaisedByUserId`, `AgainstUserId`
  * `ReasonCode`, `Description`, `Status` (Open, UnderReview, Resolved, Rejected)
  * `CreatedAt`, `ResolvedAt`

* `complaint.ComplaintAttachments`

  * `AttachmentId`, `ComplaintId`, `FileUrl`, `Type`

* `complaint.Inspections`

  * `InspectionId`, `ComplaintId`, `InspectorUserId`
  * `Findings`, `Verdict` (DP_Fault, Consumer_Fault, NoFault, Shared)
  * `PenaltyAmountDp`, `PenaltyAmountConsumer`
  * `CompletedAt`

### 4.6 Wallet, Billing, Subscriptions

**Schema:** `billing`

* `billing.Wallets`

  * `WalletId`, `UserId`, `Balance`, `Currency`, `LastUpdatedAt`

* `billing.WalletTransactions`

  * `WalletTxnId`, `WalletId`, `TxnType` (Credit/Debit)
  * `Amount`, `ReferenceType` (Delivery, Settlement, Refund, Penalty, Topup)
  * `ReferenceId`, `TxnAt`

* `billing.Subscriptions`

  * `SubscriptionId`, `UserId (DBC)`, `PlanId`, `StartDate`, `EndDate`, `IsAutoRenew`

* `billing.Plans`

  * `PlanId`, `Name`, `BillingPeriod`, `Price`, `CommissionDiscount`, etc.

* `billing.Invoices`

  * `InvoiceId`, `UserId`, `PeriodFrom`, `PeriodTo`, `Amount`, `Status`

### 4.7 Referrals & Donations

**Schema:** `growth`

* `growth.Referrals`

  * `ReferralId`, `ReferrerUserId`, `ReferredUserId`, `Status`, `RewardAmount`

* `growth.Donations`

  * `DonationId`, `DeliveryId`, `PayerUserId`, `BeneficiaryUserId`, `Amount`, `PlatformFee`

### 4.8 Audit & Admin

**Schema:** `admin`

* `admin.AuditLogs`

  * `AuditId`, `ActorUserId`, `Action`, `EntityName`, `EntityId`, `PayloadJson`, `CreatedAt`

* `admin.SystemSettings`

  * `Key`, `Value`, `Description`

---

## 5. Stored Procedures (Design & Naming)

### 5.1 Naming Convention

* Pattern: `<Schema>.<Entity>_<Action>`

  * Example: `delivery.Delivery_Create`, `billing.Wallet_ApplyTransaction`
* For complex flows: `<Schema>.sp_<Verb>_<Domain>`

  * Example: `delivery.sp_Match_CandidatesForDelivery`

### 5.2 IAM & KYC SPs (Examples)

* `iam.User_Create`

* `iam.User_GetById`

* `iam.User_GetByPhoneOrEmail`

* `iam.User_UpdateLastLogin`

* `kyc.KycRequest_Create`

* `kyc.KycRequest_UpdateStatus`

* `kyc.KycRequest_GetPendingByType`

* `user.DeliveryPartner_CreateOrUpdate`

* `user.DeliveryPartner_GetById`

* `user.DeliveryPartner_GetByDpcm`

### 5.3 Service Area & Geo SPs

* `geo.ServiceArea_CreateOrUpdate`
* `geo.ServiceArea_GetByOwner`
* `geo.ServiceArea_FindEligibleForPoint`

  * Input: `@Lat`, `@Lng`
  * Uses `geography::Point` and `STDistance` to filter.

### 5.4 Delivery & Matching SPs

* `delivery.Delivery_Create`
* `delivery.Delivery_GetById`
* `delivery.Delivery_UpdateStatus`
* `delivery.Delivery_AssignToDp`
* `delivery.Delivery_LogEvent`

**Matching Engine SP:**

* `delivery.sp_Match_CandidatesForDelivery`

  * Inputs: `@DeliveryId`, `@MaxCandidates`, maybe `@MinRating`
  * Logic:

    * Join `Delivery` → `ServiceAreas` / `DeliveryPartners`
    * Filter by availability, service radius, geospatial inclusion, KYC status. 
    * Compute price: `perKm * distance + perKg * weight + minCharge + surcharges`.
    * Order by price asc, rating desc, proximity asc.

### 5.5 Wallet & Billing SPs

* `billing.Wallet_GetByUserId`

* `billing.Wallet_ApplyTransaction`

  * Inputs: `@UserId`, `@Amount`, `@TxnType`, `@ReferenceType`, `@ReferenceId`
  * Must handle **balance update + insert transaction in a single transaction**.

* `billing.Subscription_Upsert`

* `billing.Invoice_GenerateForPeriod`

* `billing.Invoice_GetById`

### 5.6 Complaints & Inspector SPs

* `complaint.Complaint_Create`
* `complaint.Complaint_GetById`
* `complaint.Complaint_GetByUser`
* `complaint.Inspection_Create`
* `complaint.Inspection_ApplyVerdict`

  * This triggers wallet penalties (calls `billing.Wallet_ApplyTransaction` within transaction).

### 5.7 Reporting / Dashboard SPs

* `admin.sp_Dashboard_AdminSummary`
* `admin.sp_Dashboard_DpcmSummary`
* `admin.sp_Report_DeliveriesByDateRange`
* `admin.sp_Report_WalletReconciliation`

---

## 6. Functions (UDFs)

### 6.1 Scalar Functions

* `delivery.fn_CalcDeliveryPrice`

  * Inputs: `@DistanceKm`, `@WeightKg`, `@PerKm`, `@PerKg`, `@MinCharge`, `@SurchargeJson`
  * Returns `DECIMAL(18,2)`

* `delivery.fn_GetBehaviorIndex(@DeliveryPartnerId)`

  * Based on ratings, punctuality, complaint ratio. 

* `delivery.fn_GetConsumerReliabilityIndex(@UserId)`

  * Based on complaint validity, cancellations, etc.

### 6.2 Table-Valued Functions

* `delivery.fn_GetDeliveryHistory(@UserId, @FromDate, @ToDate)`
* `billing.fn_GetWalletSummary(@UserId, @FromDate, @ToDate)`

---

## 7. Views

* `admin.vw_DeliverySummary`

  * Aggregated metrics per day/region/status.

* `admin.vw_WalletReconciliation`

  * Joins `Wallets` + `WalletTransactions` for reporting.

* `admin.vw_KycQueue`

  * Pending KYC per type.

* `admin.vw_ComplaintOverview`

  * Complaint + inspection + verdict flags.

Views will be **read-only** and consumed via Dapper for dashboards.

---

## 8. Table Types (TVPs)

For bulk operations / filtering via Dapper:

* `dbo.Type_IdList`

  * Single column: `Id BIGINT`

* `delivery.Type_DeliveryFilter`

  * Columns: `Status`, `FromDate`, `ToDate`, `DpcmId`, `DpId` (nullable)

These are used to pass complex filter sets to reporting SPs.

---

## 9. Jobs / Scheduled Tasks

Use **SQL Agent** or Application Worker (DeliverX.Workers) with scheduled cron:

1. `Job_Settlements_Daily`

   * Runs daily to compute DP & DPCM payouts:
   * Queries completed deliveries, applies commissions, writes wallet transactions. 

2. `Job_SubscriptionBilling_Monthly`

   * Generates invoices, flags due payments.

3. `Job_KycReminder`

   * Sends reminder for pending KYC/Police verification.

4. `Job_DataRetention_Cleanup`

   * Anonymizes or archives old data as per retention policy.

Each job should map to one main stored procedure, e.g. `billing.sp_Run_DailySettlements`.

---

## 10. Dapper + Repository Pattern PRD

### 10.1 Infrastructure Abstractions

* `IDbConnectionFactory`

  * `CreateConnection()` returns `SqlConnection`

* `IUnitOfWork`

  * `BeginTransaction()`, `Commit()`, `Rollback()`

### 10.2 Repository Interfaces (Core/Application)

Examples:

```csharp
public interface IDeliveryRepository
{
    Task<long> CreateAsync(DeliveryEntity entity, IDbTransaction? tx = null);
    Task<DeliveryEntity?> GetByIdAsync(long deliveryId);
    Task UpdateStatusAsync(long deliveryId, DeliveryStatus status, IDbTransaction? tx = null);
    Task<IEnumerable<DeliveryCandidateDto>> MatchCandidatesAsync(long deliveryId, int maxCandidates);
}
```

Implementation uses **only stored procedures** and Dapper:

```csharp
public class DeliveryRepository : IDeliveryRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public async Task<long> CreateAsync(DeliveryEntity entity, IDbTransaction? tx = null)
    {
        var conn = tx?.Connection ?? _connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<long>(
            "delivery.Delivery_Create",
            new {
                entity.RequesterUserId,
                entity.PickupAddressId,
                entity.DropAddressId,
                entity.WeightKg,
                entity.PackageType,
                entity.ScheduledAt,
                entity.Priority
            },
            commandType: CommandType.StoredProcedure,
            transaction: tx
        );
    }
}
```

---

## 11. Development Plan & Tracker

### 11.1 Phased Plan

**Phase 1 – Foundation (Schema + Core SPs)**

* Create schemas, core tables (IAM, Users, Addresses, KYC, Delivery, Wallet).
* Implement base SPs for CRUD.
* Wire Dapper infrastructure + connection factory.

**Phase 2 – IAM & KYC**

* IAM SPs, repositories, services, controllers.
* KYC flow SPs + basic queues.

**Phase 3 – Service Area & Matching**

* Geo tables & SPs.
* Matching SP + repository + application service.

**Phase 4 – Delivery Lifecycle & POD**

* Status change SPs, POD tables, event logging.
* End-to-end flow: create → assign → accept → deliver.

**Phase 5 – Wallet & Billing**

* Wallet, transactions, subscription & invoices.
* Settlement stored procedures and jobs.

**Phase 6 – Complaints & Inspector**

* Complaint, inspection SPs.
* Penalty logic integrated with wallet.

**Phase 7 – Dashboards & Reporting**

* Views, dashboard SPs, repositories, controllers.

**Phase 8 – Hardening & Optimization**

* Indexing, query tuning, load tests, security review.

---

### 11.2 Tracker (You Can Maintain This in Cursor / Markdown)

```markdown
| ID  | Area        | Task                                      | Layer         | Status  | Notes |
|-----|-------------|-------------------------------------------|---------------|---------|-------|
| DB-01 | Foundation | Create schemas (iam, user, kyc, geo, delivery, billing, complaint, admin, growth) | DB          | TODO    |       |
| DB-02 | Foundation | Create core user/role tables             | DB            | TODO    |       |
| DB-03 | Foundation | Create delivery, delivery_events, pod    | DB            | TODO    |       |
| DB-04 | Foundation | Create wallet & wallet_transactions      | DB            | TODO    |       |
| DB-05 | IAM       | SPs: iam.User_Create / Get / UpdateLastLogin | DB/SP      | TODO    |       |
| DB-06 | IAM       | Repo: UserRepository (Dapper)            | Repo          | TODO    |       |
| DB-07 | KYC       | Tables: kyc.KycRequests                  | DB            | TODO    |       |
| DB-08 | KYC       | SPs: KycRequest_Create / UpdateStatus    | DB/SP         | TODO    |       |
| DB-09 | KYC       | Repo: KycRepository                      | Repo          | TODO    |       |
| DB-10 | ServiceArea | Tables: geo.ServiceAreas                | DB            | TODO    |       |
| DB-11 | ServiceArea | SPs: ServiceArea_CreateOrUpdate / GetByOwner / FindEligibleForPoint | DB/SP | TODO | |
| DB-12 | ServiceArea | Repo: ServiceAreaRepository (Dapper)    | Repo          | TODO    |       |
| DB-13 | Delivery  | SP: delivery.Delivery_Create             | DB/SP         | TODO    |       |
| DB-14 | Delivery  | SP: delivery.Delivery_UpdateStatus       | DB/SP         | TODO    |       |
| DB-15 | Delivery  | SP: delivery.Delivery_LogEvent           | DB/SP         | TODO    |       |
| DB-16 | Delivery  | SP: delivery.sp_Match_CandidatesForDelivery | DB/SP      | TODO    |       |
| DB-17 | Delivery  | Repo: DeliveryRepository                 | Repo          | TODO    |       |
| DB-18 | Wallet    | SP: billing.Wallet_GetByUserId           | DB/SP         | TODO    |       |
| DB-19 | Wallet    | SP: billing.Wallet_ApplyTransaction      | DB/SP         | TODO    |       |
| DB-20 | Wallet    | Repo: WalletRepository                   | Repo          | TODO    |       |
| DB-21 | Complaints| Tables: complaint.Complaints, Inspections| DB            | TODO    |       |
| DB-22 | Complaints| SPs: Complaint_Create / Get / Inspection_ApplyVerdict | DB/SP | TODO | |
| DB-23 | Complaints| Repo: ComplaintRepository, InspectionRepository | Repo   | TODO    |       |
| DB-24 | Reports   | Views: vw_DeliverySummary, vw_WalletReconciliation | DB/View | TODO | |
| DB-25 | Reports   | SPs: sp_Dashboard_AdminSummary, sp_Report_DeliveriesByDateRange | DB/SP | TODO | |
| DB-26 | Jobs      | SP: billing.sp_Run_DailySettlements      | DB/SP         | TODO    |       |
| DB-27 | Jobs      | SQL Agent Job: Job_Settlements_Daily     | DB/Job        | TODO    |       |
| DB-28 | Optimization | Create indexes for high-traffic queries | DB          | TODO    |       |
```

You can extend this tracker per sprint or per feature and mark **Status = TODO / IN_PROGRESS / DONE** as you go.

---

If you want, next step I can:

* Draft **actual SQL scripts + one full repository implementation**, or
* Give you a **Cursor-ready skeleton for Infrastructure (Dapper + Repos + DI)** that matches this PRD.
