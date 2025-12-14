# DeliveryDost Implementation Plan

## Current Status Summary

The codebase already has **most core features implemented**:
- 34 Domain Entities
- 95+ Database Tables
- 25+ Service Implementations
- DP, BC, DPCM, EC, Admin registration flows
- Delivery creation, tracking, bidding
- Wallet, Subscription, Rating, Complaint systems
- KYC verification flows (Aadhaar, PAN, Bank)
- Service Area management
- Saved Addresses (Group 3)
- Bidding Platform (Group 4)

---

## Gap Analysis: PRD/SRS vs Current Implementation

### 1. Traditional Courier API Module (HIGH PRIORITY)
**Status:** NOT IMPLEMENTED
**PRD Requirement:** For >15km or cross-pincode deliveries, fallback to Traditional Courier APIs (Delhivery, XpressBees, BlueDart)

**Implementation Needed:**
- [ ] Create `CourierPartner` entity
- [ ] Create `CourierShipment` entity for tracking
- [ ] Create `ICourierService` interface
- [ ] Implement courier rate comparison
- [ ] Implement shipment creation/tracking/cancellation
- [ ] Add courier fallback logic in DeliveryService

---

### 2. Pool Routes & Fleet Management (MEDIUM PRIORITY)
**Status:** NOT IMPLEMENTED
**PRD Requirement:** Admin creates predefined routes for pooling deliveries

**Implementation Needed:**
- [ ] Create `PoolRoute` entity (route waypoints, schedule)
- [ ] Create `PoolFleet` entity (vehicles assigned to routes)
- [ ] Create `PoolDelivery` entity (deliveries in a pool)
- [ ] Admin UI for route management
- [ ] Pool matching algorithm
- [ ] Pool delivery pricing

---

### 3. Inspector Workflow Enhancement (MEDIUM PRIORITY)
**Status:** PARTIALLY IMPLEMENTED (Inspector entity exists, workflow incomplete)
**PRD Requirement:** Complaints assigned to inspectors, verification, penalty system

**Implementation Needed:**
- [ ] Inspector assignment logic
- [ ] Inspector verification workflow
- [ ] Penalty transaction creation
- [ ] Admin UI for inspector management
- [ ] Inspector mobile interface/API

---

### 4. Auto-Selection Timer for Bidding (HIGH PRIORITY)
**Status:** NOT IMPLEMENTED (BiddingConfig exists but no timer logic)
**PRD Requirement:** After configured time (1,2,5,10,60 mins), auto-select lowest bid

**Implementation Needed:**
- [ ] Background job for bid expiry
- [ ] Auto-selection algorithm (L1 logic)
- [ ] Notification to DP on auto-selection
- [ ] Timer display in UI

---

### 5. News & Notification Module (MEDIUM PRIORITY)
**Status:** NOT IMPLEMENTED
**PRD Requirement:** Admin issues news & notifications to users

**Implementation Needed:**
- [ ] Create `News` entity
- [ ] Create `Notification` entity
- [ ] Create `UserNotification` entity
- [ ] Push notification service
- [ ] Admin UI for news management
- [ ] User notification center

---

### 6. Settlement Automation (HIGH PRIORITY)
**Status:** PARTIALLY IMPLEMENTED (Settlement entity exists, no automation)
**PRD Requirement:** Automated wallet settlement for DPs/DPCMs

**Implementation Needed:**
- [ ] Settlement scheduling job
- [ ] Commission calculation per delivery
- [ ] Payout batch processing
- [ ] Settlement reports
- [ ] Admin manual settlement trigger

---

### 7. Invoice Generation for BC (MEDIUM PRIORITY)
**Status:** PARTIALLY IMPLEMENTED (SubscriptionInvoice exists, delivery invoices missing)
**PRD Requirement:** Invoice generation for Business Consumers

**Implementation Needed:**
- [ ] Delivery invoice generation
- [ ] PDF invoice template
- [ ] Invoice download API
- [ ] Monthly billing summary

---

### 8. Admin Dashboard Drill-Down Analytics (LOW PRIORITY)
**Status:** BASIC IMPLEMENTATION
**PRD Requirement:** Comprehensive dashboards with drill-down

**Implementation Needed:**
- [ ] Delivery analytics charts
- [ ] Revenue analytics
- [ ] DP performance metrics
- [ ] Geographic heatmaps
- [ ] Trend analysis

---

### 9. BC API Enhancement (MEDIUM PRIORITY)
**Status:** PARTIALLY IMPLEMENTED
**PRD Requirement:** OAuth 2.0 Client Credentials, API Keys

**Implementation Needed:**
- [ ] API Key generation and management
- [ ] OAuth 2.0 client credentials flow
- [ ] Rate limiting per API key
- [ ] API usage dashboard for BC

---

## Execution Priority Order

### Phase 1: Critical Features (Immediate)
1. Auto-Selection Timer for Bidding
2. Traditional Courier API Module
3. Settlement Automation

### Phase 2: Important Features
4. Inspector Workflow Enhancement
5. BC API Enhancement
6. Invoice Generation

### Phase 3: Nice-to-Have Features
7. Pool Routes & Fleet Management
8. News & Notification Module
9. Admin Dashboard Analytics

---

## Detailed Implementation Steps

### Step 1: Auto-Selection Timer for Bidding

**1.1 Add background service for bid processing:**
```csharp
// Services/BidExpiryService.cs
public class BidExpiryService : BackgroundService
{
    // Check expired deliveries
    // Auto-select lowest bid
    // Notify DP
}
```

**1.2 Update BiddingService:**
- Add auto-selection logic
- Integrate with notification system

**1.3 Add timer display in delivery bidding UI**

---

### Step 2: Traditional Courier API Module

**2.1 Create Domain Entities:**
```csharp
// Entities/CourierPartner.cs
public class CourierPartner
{
    public int Id { get; set; }
    public string Name { get; set; }  // Delhivery, XpressBees, etc.
    public string ApiBaseUrl { get; set; }
    public string ApiKey { get; set; }
    public bool IsActive { get; set; }
}

// Entities/CourierShipment.cs
public class CourierShipment
{
    public int Id { get; set; }
    public int DeliveryId { get; set; }
    public int CourierPartnerId { get; set; }
    public string AWBNumber { get; set; }
    public string Status { get; set; }
    public decimal CourierCharge { get; set; }
}
```

**2.2 Create Courier Service:**
```csharp
public interface ICourierService
{
    Task<List<CourierRateDto>> GetRatesAsync(CourierRateRequest request);
    Task<CourierShipmentDto> CreateShipmentAsync(CreateCourierShipmentRequest request);
    Task<CourierTrackingDto> TrackShipmentAsync(string awbNumber);
    Task<bool> CancelShipmentAsync(string awbNumber);
}
```

**2.3 Modify DeliveryService to check distance and fallback**

---

### Step 3: Settlement Automation

**3.1 Create Settlement Background Job:**
```csharp
public class SettlementJob : BackgroundService
{
    // Run daily at configured time
    // Calculate pending settlements
    // Process payouts
}
```

**3.2 Settlement calculation logic:**
- Sum completed deliveries since last settlement
- Apply commission rates
- Deduct platform fees
- Create settlement records

---

## File Structure for New Features

```
src/
├── DeliveryDost.Domain/
│   └── Entities/
│       ├── CourierPartner.cs (NEW)
│       ├── CourierShipment.cs (NEW)
│       ├── PoolRoute.cs (NEW)
│       ├── PoolFleet.cs (NEW)
│       ├── News.cs (NEW)
│       └── UserNotification.cs (NEW)
│
├── DeliveryDost.Application/
│   ├── Services/
│   │   ├── ICourierService.cs (NEW)
│   │   ├── IPoolingService.cs (NEW)
│   │   └── INotificationService.cs (NEW)
│   └── DTOs/
│       ├── CourierDTOs.cs (NEW)
│       └── PoolingDTOs.cs (NEW)
│
├── DeliveryDost.Infrastructure/
│   ├── Services/
│   │   ├── CourierService.cs (NEW)
│   │   ├── PoolingService.cs (NEW)
│   │   ├── NotificationService.cs (NEW)
│   │   └── BidExpiryBackgroundService.cs (NEW)
│   └── Data/
│       └── Configurations/
│           ├── CourierPartnerConfiguration.cs (NEW)
│           └── PoolRouteConfiguration.cs (NEW)
│
└── DeliveryDost.Web/
    ├── Controllers/
    │   ├── CourierController.cs (NEW)
    │   └── PoolingController.cs (NEW)
    └── Views/
        ├── Admin/
        │   ├── PoolRoutes.cshtml (NEW)
        │   └── News.cshtml (NEW)
        └── Courier/
            └── Rates.cshtml (NEW)
```

---

## Database Migration Plan

### Migration 1: Courier API Support
- CourierPartners table
- CourierShipments table
- Update Deliveries table (add CourierShipmentId)

### Migration 2: Pool Routes
- PoolRoutes table
- PoolFleet table
- PoolDeliveryAssignments table

### Migration 3: Notifications
- News table
- UserNotifications table

---

## Estimated Effort

| Feature | Complexity | Files to Create/Modify |
|---------|------------|------------------------|
| Auto-Selection Timer | Medium | 5-7 files |
| Courier API Module | High | 15-20 files |
| Settlement Automation | Medium | 8-10 files |
| Inspector Workflow | Medium | 6-8 files |
| News & Notifications | Medium | 10-12 files |
| Pool Routes | High | 15-18 files |
| BC API Enhancement | Medium | 5-7 files |
| Invoice Generation | Low | 4-5 files |
| Dashboard Analytics | Medium | 8-10 files |

---

## Ready to Execute

Confirm which features to implement first, and I will begin execution.
