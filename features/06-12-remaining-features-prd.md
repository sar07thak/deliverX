# DeliverX Network - Remaining Features PRD (F-06 through F-12)

**Version:** 1.0
**Date:** 2025-11-14

---

## F-06: Delivery State Machine & Proof of Delivery (POD)

### Overview
Implement a robust state machine to track delivery lifecycle from creation to completion, with comprehensive POD capture to ensure delivery verification.

### States
```
CREATED → MATCHING → ASSIGNED → ACCEPTED → PICKED_UP → IN_TRANSIT → DELIVERED → CLOSED
                                     ↓
                                 CANCELLED
```

### POD Requirements
- **Photo:** Capture package/recipient photo
- **Recipient Name:** Text input
- **OTP:** 4-digit code shared with recipient for verification
- **Signature:** Digital signature (optional)
- **GPS Coordinates:** Auto-captured at delivery time

### Key APIs
```http
POST /api/v1/deliveries/{id}/pickup  # Mark as picked up
POST /api/v1/deliveries/{id}/transit # Mark in transit
POST /api/v1/deliveries/{id}/deliver # Complete with POD
  Body: {
    "recipientName": "John Doe",
    "otp": "1234",
    "podPhotoUrl": "https://...",
    "deliveredLat": 26.9050,
    "deliveredLng": 75.7840,
    "notes": "Delivered to security"
  }
GET /api/v1/deliveries/{id}/pod      # Retrieve POD
```

### Database Schema
```sql
CREATE TABLE ProofOfDeliveries (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    DeliveryId UNIQUEIDENTIFIER NOT NULL UNIQUE,
    RecipientName NVARCHAR(255),
    RecipientOTP NVARCHAR(4),
    PODPhotoUrl NVARCHAR(500),
    SignatureUrl NVARCHAR(500),
    DeliveredLat DECIMAL(10, 8),
    DeliveredLng DECIMAL(11, 8),
    DeliveredAt DATETIME2,
    Notes NVARCHAR(500),
    VerifiedBy UNIQUEIDENTIFIER, -- EC userId if verified
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);
```

### Business Rules
- OTP sent to EC when delivery is PICKED_UP
- DP must upload POD photo to mark DELIVERED
- OTP verification optional but recommended
- Auto-close delivery 24 hours after DELIVERED if no complaint

---

## F-07: Ratings & Behavior Index

### Overview
Multi-directional rating system allowing DPs, DBCs, and ECs to rate each other, with a calculated behavior index to promote good conduct.

### Rating Types
1. **DP → Rated by DBC/EC** (after delivery)
2. **DBC/EC → Rated by DP** (customer behavior)
3. **DPCM → Rated by DPs** (management quality)

### Rating Attributes
- **Score:** 1-5 stars (required)
- **Tags:** Punctual, Polite, Careful, Fast, etc. (multi-select)
- **Comments:** Text feedback (optional, max 500 chars)
- **Anonymous:** Option to hide name

### Behavior Index Calculation
```
Behavior Index (0-100) =
  (Average Rating × 20) × 0.6 +
  (Completion Rate × 100) × 0.2 +
  (Punctuality Rate × 100) × 0.1 +
  (Complaint-Free Rate × 100) × 0.1
```

### Key APIs
```http
POST /api/v1/ratings
  Body: {
    "deliveryId": "delivery-uuid",
    "targetId": "dp-uuid",
    "targetType": "DP",
    "score": 5,
    "tags": ["Punctual", "Polite"],
    "comment": "Great service!",
    "isAnonymous": false
  }

GET /api/v1/ratings/{targetId}/summary
  Response: {
    "targetId": "dp-uuid",
    "averageRating": 4.7,
    "totalRatings": 342,
    "distribution": {"5": 250, "4": 60, "3": 20, "2": 10, "1": 2},
    "behaviorIndex": 92,
    "topTags": ["Punctual", "Polite", "Careful"]
  }
```

### Database Schema
```sql
CREATE TABLE Ratings (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    DeliveryId UNIQUEIDENTIFIER NOT NULL,
    RaterId UNIQUEIDENTIFIER NOT NULL, -- Who is rating
    RaterType NVARCHAR(20),
    TargetId UNIQUEIDENTIFIER NOT NULL, -- Who is being rated
    TargetType NVARCHAR(20),
    Score INT NOT NULL CHECK (Score >= 1 AND Score <= 5),
    Tags NVARCHAR(MAX), -- JSON array
    Comment NVARCHAR(500),
    IsAnonymous BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UNIQUE(DeliveryId, RaterId, TargetId)
);

CREATE TABLE BehaviorIndexes (
    UserId UNIQUEIDENTIFIER PRIMARY KEY,
    AverageRating DECIMAL(3, 2),
    CompletionRate DECIMAL(5, 2),
    PunctualityRate DECIMAL(5, 2),
    ComplaintFreeRate DECIMAL(5, 2),
    BehaviorIndex DECIMAL(5, 2), -- Computed
    LastCalculatedAt DATETIME2,
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
);
```

---

## F-08: Complaints & Inspector Flow

### Overview
Structured complaint management system with inspector-based investigation for complex disputes.

### Complaint Categories
- **DP Issues:** Late, rude, damaged goods, wrong POD
- **DBC/EC Issues:** Wrong address, unreachable, harassment
- **Platform Issues:** Pricing error, payment issue

### Complaint Workflow
```
1. User lodges complaint with evidence (photos, details)
2. System auto-triages:
   - Low severity → Auto-resolved with refund/penalty
   - High severity → Assign to Inspector
3. Inspector reviews evidence, contacts parties, visits location (if needed)
4. Inspector submits verdict with evidence
5. System applies outcome:
   - Refund customer
   - Penalty to DP/DBC
   - Suspend user (if fraud)
6. Close complaint with resolution notes
```

### Key APIs
```http
POST /api/v1/complaints
  Body: {
    "deliveryId": "delivery-uuid",
    "complainantId": "user-uuid",
    "defendantId": "user-uuid",
    "category": "LATE_DELIVERY",
    "description": "DP arrived 2 hours late",
    "evidenceUrls": ["https://..."],
    "severity": "MEDIUM"
  }

POST /api/v1/complaints/{id}/assign-inspector
  Body: {"inspectorId": "inspector-uuid"}

POST /api/v1/inspections/{id}/resolve
  Body: {
    "verdict": "UPHELD", // or REJECTED, PARTIAL
    "findings": "Evidence supports late delivery claim",
    "evidenceUrls": ["https://..."],
    "recommendedAction": "REFUND_50_PERCENT",
    "penaltyAmount": 25
  }
```

### Database Schema
```sql
CREATE TABLE Complaints (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    DeliveryId UNIQUEIDENTIFIER,
    ComplainantId UNIQUEIDENTIFIER NOT NULL,
    DefendantId UNIQUEIDENTIFIER,
    Category NVARCHAR(50),
    Severity NVARCHAR(20), -- LOW, MEDIUM, HIGH, CRITICAL
    Status NVARCHAR(50) DEFAULT 'PENDING',
    Description NVARCHAR(MAX),
    EvidenceUrls NVARCHAR(MAX), -- JSON
    AssignedInspectorId UNIQUEIDENTIFIER NULL,
    AssignedAt DATETIME2,
    ResolvedAt DATETIME2,
    Resolution NVARCHAR(MAX),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE Inspections (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    ComplaintId UNIQUEIDENTIFIER NOT NULL UNIQUE,
    InspectorId UNIQUEIDENTIFIER NOT NULL,
    Verdict NVARCHAR(50), -- UPHELD, REJECTED, PARTIAL
    Findings NVARCHAR(MAX),
    EvidenceUrls NVARCHAR(MAX),
    RecommendedAction NVARCHAR(100),
    PenaltyAmount DECIMAL(10, 2),
    RefundAmount DECIMAL(10, 2),
    SubmittedAt DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);
```

---

## F-09: Wallet, Payments & Settlements

### Overview
Comprehensive wallet system for all users with ledger-based transactions, automated settlements, and multi-payment gateway support.

### Wallet Features
- **DP Wallet:** Receive earnings, withdraw to bank
- **DBC Wallet:** Pre-load balance for deliveries, refunds
- **EC Wallet:** Optional for frequent users
- **DPCM Wallet:** Receive commissions

### Transaction Types
- **CREDIT:** Delivery earning, refund, top-up, referral reward
- **DEBIT:** Delivery payment, withdrawal, penalty

### Settlement Rules
- **DP:** Daily at midnight (min balance ₹100)
- **DPCM:** Weekly on Monday
- **DBC:** Instant refund to wallet or source

### Key APIs
```http
GET /api/v1/wallets/{userId}
  Response: {
    "userId": "user-uuid",
    "balance": 1250.50,
    "currency": "INR",
    "status": "ACTIVE",
    "pendingBalance": 150.00 // Not yet settled
  }

POST /api/v1/wallets/{userId}/topup
  Body: {
    "amount": 500,
    "paymentMethod": "UPI",
    "returnUrl": "https://app.deliverx.com/wallet"
  }
  Response: {
    "transactionId": "txn-uuid",
    "paymentUrl": "https://razorpay.com/...",
    "status": "INITIATED"
  }

POST /api/v1/wallets/{userId}/withdraw
  Body: {
    "amount": 1000,
    "bankAccountId": "bank-uuid"
  }
  Response: {
    "transactionId": "txn-uuid",
    "status": "PROCESSING",
    "estimatedCompletion": "2025-11-15T10:00:00Z"
  }

GET /api/v1/wallets/{userId}/transactions?page=1&limit=20
  Response: {
    "transactions": [
      {
        "id": "txn-uuid",
        "type": "CREDIT",
        "amount": 65.00,
        "description": "Delivery #DEL123 earning",
        "balanceBefore": 1185.50,
        "balanceAfter": 1250.50,
        "status": "COMPLETED",
        "createdAt": "2025-11-14T14:30:00Z"
      }
    ],
    "pagination": {...}
  }
```

### Database Schema
```sql
CREATE TABLE Wallets (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL UNIQUE,
    Balance DECIMAL(12, 2) NOT NULL DEFAULT 0,
    PendingBalance DECIMAL(12, 2) DEFAULT 0,
    Currency NVARCHAR(3) DEFAULT 'INR',
    Status NVARCHAR(20) DEFAULT 'ACTIVE',
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE WalletTransactions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    WalletId UNIQUEIDENTIFIER NOT NULL,
    Type NVARCHAR(20) NOT NULL, -- CREDIT, DEBIT
    Category NVARCHAR(50), -- DELIVERY_EARNING, TOPUP, WITHDRAWAL, REFUND
    Amount DECIMAL(12, 2) NOT NULL,
    BalanceBefore DECIMAL(12, 2),
    BalanceAfter DECIMAL(12, 2),
    Description NVARCHAR(500),
    ReferenceType NVARCHAR(50), -- DELIVERY, SETTLEMENT, PAYMENT
    ReferenceId UNIQUEIDENTIFIER,
    Status NVARCHAR(20) DEFAULT 'COMPLETED',
    PaymentGatewayTxnId NVARCHAR(255),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),

    INDEX IX_WalletTxn_WalletId (WalletId),
    INDEX IX_WalletTxn_CreatedAt (CreatedAt)
);

CREATE TABLE Settlements (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    UserType NVARCHAR(20),
    SettlementType NVARCHAR(50), -- DAILY_DP, WEEKLY_DPCM
    Amount DECIMAL(12, 2) NOT NULL,
    TransactionIds NVARCHAR(MAX), -- JSON array of wallet txn IDs
    Status NVARCHAR(20) DEFAULT 'PENDING',
    InitiatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CompletedAt DATETIME2,
    BankAccountId UNIQUEIDENTIFIER,
    PayoutReferenceId NVARCHAR(255),
    FailureReason NVARCHAR(500),

    INDEX IX_Settlements_UserId (UserId),
    INDEX IX_Settlements_Status (Status)
);
```

### Payment Gateway Integration
```csharp
public class RazorpayService : IPaymentGatewayService
{
    public async Task<PaymentInitiationResult> InitiateTopupAsync(decimal amount, string userId)
    {
        var order = await _razorpayClient.Order.CreateAsync(new Dictionary<string, object>
        {
            {"amount", amount * 100}, // Razorpay uses paise
            {"currency", "INR"},
            {"receipt", $"topup_{userId}_{Guid.NewGuid()}"},
            {"payment_capture", 1}
        });

        return new PaymentInitiationResult
        {
            OrderId = order["id"].ToString(),
            PaymentUrl = $"https://checkout.razorpay.com/{order["id"]}",
            Amount = amount
        };
    }

    public async Task<PayoutResult> InitiatePayoutAsync(string bankAccountId, decimal amount)
    {
        var payout = await _razorpayClient.Payout.CreateAsync(new Dictionary<string, object>
        {
            {"account_number", "1234567890"},
            {"fund_account_id", bankAccountId},
            {"amount", amount * 100},
            {"currency", "INR"},
            {"mode", "IMPS"},
            {"purpose", "payout"},
            {"queue_if_low_balance", true}
        });

        return new PayoutResult
        {
            PayoutId = payout["id"].ToString(),
            Status = payout["status"].ToString(),
            Utr = payout["utr"]?.ToString()
        };
    }
}
```

---

## F-10: Subscription & Billing Engine

### Overview
Flexible subscription plans for DBCs with automated billing, invoicing, and GST compliance.

### Subscription Plans
| Plan | Price/Month | Per-Delivery Fee | API Hits | Features |
|------|-------------|------------------|----------|----------|
| Free | ₹0 | ₹10 | 100/month | Basic |
| Starter | ₹999 | ₹7 | 1,000/month | Priority support |
| Business | ₹4,999 | ₹5 | 10,000/month | Dedicated account manager, Analytics |
| Enterprise | Custom | ₹3 | Unlimited | SLA, Custom integrations |

### Billing Features
- **Proration:** Charge only for unused days on plan change
- **Auto-renewal:** Charge card on file automatically
- **Invoice Generation:** GST-compliant invoices
- **Payment Methods:** Card, UPI, Net Banking, Wallet

### Key APIs
```http
GET /api/v1/subscriptions/plans
  Response: [
    {
      "id": "plan-starter",
      "name": "Starter",
      "priceMonthly": 999,
      "priceQuarterly": 2697, // 10% discount
      "priceYearly": 9591, // 20% discount
      "features": {...}
    }
  ]

POST /api/v1/subscriptions
  Body: {
    "userId": "dbc-uuid",
    "planId": "plan-starter",
    "billingCycle": "MONTHLY",
    "paymentMethodId": "pm-uuid"
  }

POST /api/v1/subscriptions/{id}/change-plan
  Body: {"newPlanId": "plan-business"}

GET /api/v1/invoices/{userId}?status=PAID
GET /api/v1/invoices/{id}/download (PDF)
```

### Database Schema
```sql
CREATE TABLE SubscriptionPlans (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Code NVARCHAR(50) UNIQUE,
    Name NVARCHAR(100),
    PriceMonthly DECIMAL(10, 2),
    PriceQuarterly DECIMAL(10, 2),
    PriceYearly DECIMAL(10, 2),
    PerDeliveryFee DECIMAL(10, 2),
    APIHitsPerMonth INT,
    Features NVARCHAR(MAX), -- JSON
    IsActive BIT DEFAULT 1
);

CREATE TABLE Subscriptions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    PlanId UNIQUEIDENTIFIER NOT NULL,
    BillingCycle NVARCHAR(20), -- MONTHLY, QUARTERLY, YEARLY
    Status NVARCHAR(20) DEFAULT 'ACTIVE',
    StartDate DATETIME2,
    EndDate DATETIME2,
    AutoRenew BIT DEFAULT 1,
    PaymentMethodId UNIQUEIDENTIFIER,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),

    INDEX IX_Subscriptions_UserId (UserId),
    INDEX IX_Subscriptions_Status (Status)
);

CREATE TABLE Invoices (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    SubscriptionId UNIQUEIDENTIFIER,
    InvoiceNumber NVARCHAR(50) UNIQUE,
    InvoiceDate DATETIME2,
    DueDate DATETIME2,
    Subtotal DECIMAL(10, 2),
    TaxAmount DECIMAL(10, 2),
    TotalAmount DECIMAL(10, 2),
    Status NVARCHAR(20), -- PENDING, PAID, OVERDUE, CANCELLED
    PaidAt DATETIME2,
    PDFUrl NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),

    INDEX IX_Invoices_UserId (UserId),
    INDEX IX_Invoices_Status (Status)
);
```

---

## F-11: Referral & Donation Modules

### Overview
Viral growth mechanism through referral rewards and optional donation/tipping for DPs.

### Referral System
- **DP Referrals:** Earn ₹100 when referred DP completes 10 deliveries
- **DBC Referrals:** Earn ₹500 when referred DBC spends ₹5,000
- **Multi-level:** Up to 2 levels (referrer + their referrer)

### Donation/Tip Flow
- EC can tip DP after delivery (₹10, ₹20, ₹50, Custom)
- 95% goes to DP, 5% platform fee
- Tax handling on tips

### Key APIs
```http
POST /api/v1/referrals/generate-code
  Response: {"referralCode": "RAVI123"}

POST /api/v1/referrals/apply
  Body: {"referralCode": "RAVI123"}

GET /api/v1/referrals/{userId}/stats
  Response: {
    "totalReferrals": 15,
    "successfulReferrals": 12,
    "pendingRewards": 200,
    "totalEarned": 1200
  }

POST /api/v1/donations
  Body: {
    "deliveryId": "delivery-uuid",
    "dpId": "dp-uuid",
    "amount": 50,
    "message": "Great service!"
  }
```

### Database Schema
```sql
CREATE TABLE Referrals (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    ReferrerId UNIQUEIDENTIFIER NOT NULL,
    RefereeId UNIQUEIDENTIFIER NOT NULL,
    ReferralCode NVARCHAR(20),
    RefereeType NVARCHAR(20), -- DP, DBC
    Status NVARCHAR(20), -- PENDING, QUALIFIED, REWARDED
    RewardAmount DECIMAL(10, 2),
    QualificationCriteria NVARCHAR(MAX), -- JSON
    QualifiedAt DATETIME2,
    RewardedAt DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),

    INDEX IX_Referrals_ReferrerId (ReferrerId),
    INDEX IX_Referrals_Code (ReferralCode)
);

CREATE TABLE Donations (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    DeliveryId UNIQUEIDENTIFIER,
    DonorId UNIQUEIDENTIFIER NOT NULL,
    RecipientId UNIQUEIDENTIFIER NOT NULL,
    Amount DECIMAL(10, 2) NOT NULL,
    PlatformFee DECIMAL(10, 2),
    NetAmount DECIMAL(10, 2),
    Message NVARCHAR(500),
    Status NVARCHAR(20) DEFAULT 'COMPLETED',
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);
```

---

## F-12: Admin & DPCM Dashboards

### Overview
Comprehensive dashboards for platform management and DP network oversight.

### Admin Dashboard Features
1. **KPI Overview:**
   - Active DPs, DBCs, ECs
   - Deliveries today/week/month
   - Revenue (GMV, commission)
   - Platform health (uptime, errors)

2. **User Management:**
   - Search/filter users
   - View profiles, KYC status
   - Activate/deactivate accounts
   - Manually verify KYC

3. **KYC Queue:**
   - Pending verifications
   - Approve/reject with reasons
   - Bulk actions

4. **Complaint Management:**
   - All complaints list
   - Assign inspectors
   - View resolutions
   - Trending issues

5. **Financial Reports:**
   - Settlement reports
   - Commission breakdown
   - Invoice generation
   - Tax reports (GST)

6. **System Configuration:**
   - Platform fees
   - Surge pricing rules
   - Feature flags
   - Email/SMS templates

### DPCM Dashboard Features
1. **DP Network Overview:**
   - Total DPs, active today
   - Top performers
   - Pending KYC

2. **DP Management:**
   - Add DP (single/bulk)
   - View DP profiles
   - Set commission rates
   - Assign territories

3. **Performance Analytics:**
   - Deliveries by DP
   - Earnings by DP
   - Acceptance rates
   - Ratings distribution

4. **Settlement Reports:**
   - Pending payouts
   - Commission earned
   - Download reports

5. **Complaint View:**
   - Complaints involving my DPs
   - Resolution status

### Key Dashboard APIs
```http
GET /api/v1/admin/kpis
  Response: {
    "users": {
      "activeDPs": 1250,
      "activeDBCs": 450,
      "newRegistrationsToday": 23
    },
    "deliveries": {
      "today": 456,
      "thisWeek": 2890,
      "completionRate": 96.5
    },
    "revenue": {
      "gmvToday": 45600,
      "commissionToday": 6840,
      "gmvThisMonth": 1250000
    },
    "platformHealth": {
      "uptime": 99.95,
      "errorRate": 0.08,
      "avgLatency": 245
    }
  }

GET /api/v1/admin/kyc/pending?page=1&limit=20
POST /api/v1/admin/kyc/{id}/approve
POST /api/v1/admin/kyc/{id}/reject

GET /api/v1/dpcm/{dpcmId}/dps?status=ACTIVE
GET /api/v1/dpcm/{dpcmId}/analytics?period=MONTH
```

### UI Components
- **Charts:** Line, bar, pie (Chart.js / Recharts)
- **Tables:** Sortable, filterable, paginated (React Table)
- **Export:** CSV, PDF downloads
- **Real-time updates:** SignalR for live metrics

---

## Cross-Cutting Concerns

### Notifications
All features use centralized notification service:
- **Channels:** Push (FCM), SMS, Email, In-app
- **Templates:** Configurable per event type
- **Preferences:** Users can opt-out

### Audit Logging
All state changes logged to `AuditLogs` table:
- Who did what, when
- Before/after values for updates
- IP address, user agent

### Feature Flags
Control feature rollout:
- Enable/disable features per environment
- A/B testing capability
- Gradual rollout (10% → 50% → 100%)

### Data Retention
- **Deliveries:** Retain forever (business records)
- **PODs:** 7 years (compliance)
- **Audit Logs:** 5 years
- **Analytics Data:** Aggregated after 1 year

---

**End of Features F-06 through F-12**

For detailed implementation of F-01 through F-05, refer to individual feature PRD files.
