# Feature PRD: Delivery Creation & Matching

**Feature ID:** F-05
**Version:** 1.0
**Priority:** P0 (Critical - MVP Core)
**Estimated Effort:** 3-4 weeks
**Dependencies:** F-01 (IAM), F-03 (Service Area), F-04 (Pricing)

---

## 1. Feature Overview

### Purpose
Enable DBCs and ECs to create delivery requests, and automatically match them with the best-suited Delivery Partners based on service area coverage, pricing, ratings, and availability.

### Success Criteria
- Delivery creation success rate > 99%
- Matching algorithm returns results in < 1 second
- DP acceptance rate > 75%
- Auto-assignment success rate > 90%

---

## 2. User Stories

### US-DEL-001: DBC Creates Delivery Order
**As a** Business Consumer
**I want to** create a delivery order with pickup and drop locations
**So that** my goods can be delivered to customers

**Acceptance Criteria:**
- Enter pickup/drop addresses or select from saved locations
- Specify package details (weight, type, dimensions)
- Choose priority (ASAP vs Scheduled)
- Get instant price estimate
- Receive delivery ID and tracking link

### US-DEL-002: Matching Algorithm Finds DPs
**As a** System
**I want to** match delivery requests to qualified DPs
**So that** deliveries are assigned to the most suitable partners

**Acceptance Criteria:**
- Filter by service area coverage (pickup + drop)
- Filter by DP availability status
- Rank by: price (asc), rating (desc), proximity
- Return top 5 candidates
- Notify all candidates simultaneously

### US-DEL-003: DP Receives Delivery Notification
**As a** Delivery Partner
**I want to** receive push notifications for new delivery requests
**So that** I can accept or reject them quickly

**Acceptance Criteria:**
- Push notification with delivery summary
- Show pickup/drop addresses, distance, earning
- Accept/Reject within 60 seconds (configurable timeout)
- Auto-reject if no response within timeout

### US-DEL-004: Auto-Assignment on First Acceptance
**As a** System
**I want to** assign delivery to the first DP who accepts
**So that** deliveries are fulfilled quickly

**Acceptance Criteria:**
- First DP to accept gets the delivery
- Other notified DPs receive cancellation notification
- If all reject, re-match with wider criteria
- Max 3 matching attempts before marking as "Unassignable"

---

## 3. Matching Algorithm

### Algorithm Flow
```
1. Validate delivery request (addresses, weight, etc.)
2. Calculate distance between pickup and drop
3. Find eligible DPs:
   a. Service area covers both pickup AND drop
   b. DP status = AVAILABLE or IDLE
   c. DP KYC status = VERIFIED
   d. DP IsActive = true
4. Rank eligible DPs:
   Primary: Price (ascending)
   Secondary: Rating (descending)
   Tertiary: Distance from pickup (ascending)
5. Select top N (default 5)
6. Notify all selected DPs simultaneously
7. Wait for acceptance (timeout: 60 seconds)
8. Assign to first acceptor
9. If all reject/timeout:
   - Increase search radius by 20%
   - Re-match (max 3 attempts)
   - If still no match, mark as "Unassignable"
```

### Matching Criteria (Configurable)

| Criteria | Weight | Description |
|----------|--------|-------------|
| Service Area Coverage | Required | Must cover pickup AND drop |
| DP Availability | Required | Must be AVAILABLE or IDLE |
| Price | 40% | Lower price preferred |
| Rating | 30% | Higher rating preferred |
| Proximity to Pickup | 20% | Closer to pickup preferred |
| Completion Rate | 10% | Higher completion rate preferred |

---

## 4. API Specifications

### 4.1 Create Delivery
```http
POST /api/v1/deliveries
Authorization: Bearer <access-token>
Content-Type: application/json

Request:
{
  "requesterId": "dbc-uuid",
  "requesterType": "DBC", // or "EC"
  "pickup": {
    "lat": 26.9124,
    "lng": 75.7873,
    "address": "123 Main St, Jaipur",
    "contactName": "Store Manager",
    "contactPhone": "9876543210",
    "instructions": "Call before arrival"
  },
  "drop": {
    "lat": 26.9050,
    "lng": 75.7840,
    "address": "456 Park Ave, Jaipur",
    "contactName": "Customer Name",
    "contactPhone": "9876543211",
    "instructions": "Ring doorbell"
  },
  "package": {
    "weightKg": 2.5,
    "type": "parcel", // parcel, food, document, fragile
    "dimensions": {
      "lengthCm": 30,
      "widthCm": 20,
      "heightCm": 10
    },
    "value": 500, // Optional: for insurance
    "description": "Electronics"
  },
  "priority": "ASAP", // or "SCHEDULED"
  "scheduledAt": null, // Required if priority = SCHEDULED
  "specialInstructions": "Handle with care",
  "preferredDPId": null // Optional: request specific DP
}

Response (200):
{
  "deliveryId": "delivery-uuid",
  "status": "CREATED",
  "estimatedPrice": 70.21,
  "estimatedDistance": 1.2,
  "estimatedTime": "15-20 mins",
  "trackingUrl": "https://deliverx.com/track/delivery-uuid",
  "message": "Delivery created. Finding delivery partners..."
}
```

### 4.2 Trigger Matching
```http
POST /api/v1/deliveries/{deliveryId}/match
Authorization: Bearer <admin-token>

Response (200):
{
  "deliveryId": "delivery-uuid",
  "matchedDPs": [
    {
      "dpId": "dp-uuid-1",
      "dpName": "Ravi Kumar",
      "rating": 4.8,
      "estimatedPrice": 70.21,
      "distanceFromPickupKm": 0.5
    },
    // ... top 5
  ],
  "totalMatches": 5,
  "notificationsSent": 5,
  "status": "MATCHING_IN_PROGRESS"
}
```

### 4.3 Get Delivery Details
```http
GET /api/v1/deliveries/{deliveryId}
Authorization: Bearer <access-token>

Response (200):
{
  "id": "delivery-uuid",
  "status": "ASSIGNED",
  "createdAt": "2025-11-14T10:00:00Z",
  "assignedDP": {
    "dpId": "dp-uuid",
    "dpName": "Ravi Kumar",
    "dpPhone": "9876543210",
    "dpPhoto": "https://..."
  },
  "pickup": { /* same as request */ },
  "drop": { /* same as request */ },
  "package": { /* same as request */ },
  "pricing": {
    "estimatedPrice": 70.21,
    "finalPrice": 70.21,
    "breakdown": { /* pricing breakdown */ }
  },
  "timeline": [
    {
      "status": "CREATED",
      "timestamp": "2025-11-14T10:00:00Z"
    },
    {
      "status": "ASSIGNED",
      "timestamp": "2025-11-14T10:00:45Z",
      "dpId": "dp-uuid"
    }
  ],
  "trackingUrl": "https://deliverx.com/track/delivery-uuid"
}
```

### 4.4 DP Accept Delivery
```http
POST /api/v1/deliveries/{deliveryId}/accept
Authorization: Bearer <dp-access-token>

Response (200):
{
  "deliveryId": "delivery-uuid",
  "status": "ACCEPTED",
  "message": "Delivery accepted. Proceed to pickup location.",
  "pickup": { /* pickup details */ },
  "navigationUrl": "https://maps.google.com/...",
  "estimatedEarning": 65.00
}

Response (409 - Already Assigned):
{
  "code": "ALREADY_ASSIGNED",
  "message": "This delivery has already been accepted by another partner"
}
```

### 4.5 DP Reject Delivery
```http
POST /api/v1/deliveries/{deliveryId}/reject
Authorization: Bearer <dp-access-token>
Content-Type: application/json

Request:
{
  "reason": "TOO_FAR" // or "BUSY", "UNFAMILIAR_AREA", "OTHER"
}

Response (200):
{
  "message": "Delivery rejected. You will not receive further notifications for this delivery."
}
```

---

## 5. Database Schema

```sql
-- Deliveries
CREATE TABLE Deliveries (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    RequesterId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Users(Id),
    RequesterType NVARCHAR(20) NOT NULL, -- DBC, EC

    -- Assigned DP
    AssignedDPId UNIQUEIDENTIFIER NULL FOREIGN KEY REFERENCES DeliveryPartnerProfiles(Id),
    AssignedAt DATETIME2 NULL,

    -- Pickup
    PickupLat DECIMAL(10, 8) NOT NULL,
    PickupLng DECIMAL(11, 8) NOT NULL,
    PickupAddress NVARCHAR(500) NOT NULL,
    PickupContactName NVARCHAR(255),
    PickupContactPhone NVARCHAR(15),
    PickupInstructions NVARCHAR(500),

    -- Drop
    DropLat DECIMAL(10, 8) NOT NULL,
    DropLng DECIMAL(11, 8) NOT NULL,
    DropAddress NVARCHAR(500) NOT NULL,
    DropContactName NVARCHAR(255),
    DropContactPhone NVARCHAR(15),
    DropInstructions NVARCHAR(500),

    -- Package
    WeightKg DECIMAL(10, 2) NOT NULL,
    PackageType NVARCHAR(50) NOT NULL,
    PackageDimensions NVARCHAR(MAX), -- JSON: {length, width, height}
    PackageValue DECIMAL(10, 2),
    PackageDescription NVARCHAR(500),

    -- Scheduling
    Priority NVARCHAR(20) NOT NULL DEFAULT 'ASAP', -- ASAP, SCHEDULED
    ScheduledAt DATETIME2 NULL,

    -- Status
    Status NVARCHAR(50) NOT NULL DEFAULT 'CREATED',
    -- CREATED, MATCHING, ASSIGNED, ACCEPTED, PICKED_UP, IN_TRANSIT, DELIVERED, CANCELLED, UNASSIGNABLE

    -- Pricing
    EstimatedPrice DECIMAL(10, 2),
    FinalPrice DECIMAL(10, 2),

    -- Special
    SpecialInstructions NVARCHAR(MAX),
    PreferredDPId UNIQUEIDENTIFIER NULL,

    -- Metadata
    DistanceKm DECIMAL(10, 2),
    EstimatedDurationMinutes INT,
    MatchingAttempts INT DEFAULT 0,

    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CancelledAt DATETIME2 NULL,
    CancellationReason NVARCHAR(500),

    INDEX IX_Deliveries_RequesterId (RequesterId),
    INDEX IX_Deliveries_AssignedDPId (AssignedDPId),
    INDEX IX_Deliveries_Status (Status),
    INDEX IX_Deliveries_CreatedAt (CreatedAt),
    INDEX IX_Deliveries_Priority_Status (Priority, Status)
);

-- Delivery State History (audit trail)
CREATE TABLE DeliveryEvents (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeliveryId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Deliveries(Id),
    EventType NVARCHAR(50) NOT NULL,
    -- CREATED, MATCHED, ASSIGNED, ACCEPTED, REJECTED, PICKED_UP, IN_TRANSIT, DELIVERED, CANCELLED
    FromStatus NVARCHAR(50),
    ToStatus NVARCHAR(50),
    ActorId UNIQUEIDENTIFIER NULL FOREIGN KEY REFERENCES Users(Id),
    ActorType NVARCHAR(20), -- SYSTEM, DP, DBC, EC, ADMIN
    Metadata NVARCHAR(MAX), -- JSON: event-specific data
    Timestamp DATETIME2 DEFAULT GETUTCDATE(),

    INDEX IX_DeliveryEvents_DeliveryId (DeliveryId),
    INDEX IX_DeliveryEvents_Timestamp (Timestamp)
);

-- Matching History (which DPs were notified)
CREATE TABLE DeliveryMatchingHistory (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeliveryId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Deliveries(Id),
    DPId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES DeliveryPartnerProfiles(Id),
    MatchingAttempt INT NOT NULL DEFAULT 1,
    NotifiedAt DATETIME2 DEFAULT GETUTCDATE(),
    ResponseType NVARCHAR(20), -- ACCEPTED, REJECTED, TIMEOUT, NULL (no response)
    RespondedAt DATETIME2 NULL,
    RejectionReason NVARCHAR(500),

    INDEX IX_MatchingHistory_DeliveryId (DeliveryId),
    INDEX IX_MatchingHistory_DPId (DPId)
);

-- DP Availability Status
CREATE TABLE DPAvailability (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DPId UNIQUEIDENTIFIER NOT NULL UNIQUE FOREIGN KEY REFERENCES DeliveryPartnerProfiles(Id),
    Status NVARCHAR(20) NOT NULL DEFAULT 'OFFLINE',
    -- OFFLINE, AVAILABLE, BUSY (on delivery), BREAK
    CurrentDeliveryId UNIQUEIDENTIFIER NULL FOREIGN KEY REFERENCES Deliveries(Id),
    LastLocationLat DECIMAL(10, 8),
    LastLocationLng DECIMAL(11, 8),
    LastLocationUpdatedAt DATETIME2,
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),

    INDEX IX_DPAvailability_Status (Status),
    INDEX IX_DPAvailability_DPId (DPId)
);
```

---

## 6. Development Implementation

### 6.1 MatchingService.cs
```csharp
public class MatchingService : IMatchingService
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceAreaService _serviceAreaService;
    private readonly IPricingService _pricingService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<MatchingService> _logger;

    public async Task<MatchingResult> MatchDeliveryAsync(
        Guid deliveryId,
        int attempt = 1,
        CancellationToken ct = default)
    {
        var delivery = await _context.Deliveries.FindAsync(deliveryId);

        // 1. Find eligible DPs by service area
        var eligibleDPIds = await _serviceAreaService.FindEligibleDPsAsync(
            delivery.PickupLat, delivery.PickupLng,
            delivery.DropLat, delivery.DropLng, ct);

        if (!eligibleDPIds.Any())
        {
            _logger.LogWarning("No DPs found for delivery {DeliveryId}", deliveryId);

            if (attempt < 3)
            {
                // Retry with wider radius (future enhancement)
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
                return await MatchDeliveryAsync(deliveryId, attempt + 1, ct);
            }

            delivery.Status = "UNASSIGNABLE";
            await _context.SaveChangesAsync(ct);
            return MatchingResult.Failure("No delivery partners available");
        }

        // 2. Filter by availability
        var availableDPs = await _context.DPAvailability
            .Where(av => eligibleDPIds.Contains(av.DPId) &&
                         (av.Status == "AVAILABLE" || av.Status == "OFFLINE"))
            .Select(av => av.DPId)
            .ToListAsync(ct);

        if (!availableDPs.Any())
        {
            _logger.LogWarning("No available DPs for delivery {DeliveryId}", deliveryId);
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
            return await MatchDeliveryAsync(deliveryId, attempt + 1, ct);
        }

        // 3. Get DP details with pricing and ratings
        var dpDetails = await _context.DeliveryPartnerProfiles
            .Where(dp => availableDPs.Contains(dp.UserId))
            .Select(dp => new
            {
                dp.UserId,
                dp.FullName,
                Pricing = _context.DPPricingConfigs.FirstOrDefault(p => p.DPId == dp.Id),
                Rating = _context.Ratings
                    .Where(r => r.TargetId == dp.UserId && r.TargetType == "DP")
                    .Average(r => (double?)r.Score) ?? 0
            })
            .ToListAsync(ct);

        // 4. Calculate price for each DP
        var dpPricings = new List<DPMatchCandidate>();
        foreach (var dp in dpDetails)
        {
            var pricing = await _pricingService.CalculatePriceAsync(new PricingRequest
            {
                PickupLat = delivery.PickupLat,
                PickupLng = delivery.PickupLng,
                DropLat = delivery.DropLat,
                DropLng = delivery.DropLng,
                WeightKg = delivery.WeightKg,
                Priority = delivery.Priority,
                DPId = dp.UserId
            }, ct);

            dpPricings.Add(new DPMatchCandidate
            {
                DPId = dp.UserId,
                DPName = dp.FullName,
                Price = pricing.Total,
                Rating = dp.Rating,
                DistanceFromPickup = CalculateDistance(
                    delivery.PickupLat, delivery.PickupLng,
                    /* DP last location - need to fetch */
                )
            });
        }

        // 5. Rank DPs: Price (asc) → Rating (desc) → Proximity (asc)
        var rankedDPs = dpPricings
            .OrderBy(dp => dp.Price)
            .ThenByDescending(dp => dp.Rating)
            .ThenBy(dp => dp.DistanceFromPickup)
            .Take(5)
            .ToList();

        // 6. Notify all top candidates
        foreach (var dp in rankedDPs)
        {
            await _notificationService.SendDeliveryNotificationAsync(dp.DPId, deliveryId);

            // Log matching history
            _context.DeliveryMatchingHistory.Add(new DeliveryMatchingHistory
            {
                DeliveryId = deliveryId,
                DPId = dp.DPId,
                MatchingAttempt = attempt,
                NotifiedAt = DateTime.UtcNow
            });
        }

        delivery.Status = "MATCHING";
        delivery.MatchingAttempts = attempt;
        await _context.SaveChangesAsync(ct);

        return new MatchingResult
        {
            IsSuccess = true,
            MatchedDPs = rankedDPs,
            TotalMatches = rankedDPs.Count
        };
    }

    public async Task<Result> AcceptDeliveryAsync(
        Guid deliveryId,
        Guid dpId,
        CancellationToken ct)
    {
        var delivery = await _context.Deliveries.FindAsync(deliveryId);

        // Check if already assigned
        if (delivery.AssignedDPId.HasValue)
        {
            return Result.Failure("ALREADY_ASSIGNED", "Delivery already accepted by another partner");
        }

        // Assign delivery
        delivery.AssignedDPId = dpId;
        delivery.AssignedAt = DateTime.UtcNow;
        delivery.Status = "ASSIGNED";

        // Update DP availability
        var dpAvailability = await _context.DPAvailability.FirstOrDefaultAsync(a => a.DPId == dpId, ct);
        if (dpAvailability != null)
        {
            dpAvailability.Status = "BUSY";
            dpAvailability.CurrentDeliveryId = deliveryId;
        }

        // Update matching history
        var matchHistory = await _context.DeliveryMatchingHistory
            .FirstOrDefaultAsync(m => m.DeliveryId == deliveryId && m.DPId == dpId, ct);
        if (matchHistory != null)
        {
            matchHistory.ResponseType = "ACCEPTED";
            matchHistory.RespondedAt = DateTime.UtcNow;
        }

        // Log event
        _context.DeliveryEvents.Add(new DeliveryEvent
        {
            DeliveryId = deliveryId,
            EventType = "ACCEPTED",
            FromStatus = "MATCHING",
            ToStatus = "ASSIGNED",
            ActorId = dpId,
            ActorType = "DP"
        });

        await _context.SaveChangesAsync(ct);

        // Notify other DPs that delivery is assigned
        await _notificationService.NotifyOtherDPsDeliveryAssignedAsync(deliveryId, dpId);

        // Notify requester
        await _notificationService.NotifyRequesterDeliveryAssignedAsync(deliveryId);

        return Result.Success();
    }

    private double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
    {
        // Haversine formula (same as in ServiceAreaService)
        const double R = 6371;
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private double ToRadians(double degrees) => degrees * Math.PI / 180;
}
```

---

## 7. Testing Strategy

### Unit Tests
```csharp
[Fact]
public async Task MatchDelivery_EligibleDPsAvailable_ReturnsMatches()
{
    // Arrange
    var delivery = await CreateTestDelivery();
    await SeedAvailableDPs(count: 10);

    // Act
    var result = await _matchingService.MatchDeliveryAsync(delivery.Id);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotEmpty(result.MatchedDPs);
    Assert.InRange(result.MatchedDPs.Count, 1, 5);
}

[Fact]
public async Task AcceptDelivery_FirstDP_AssignsSuccessfully()
{
    var delivery = await CreateTestDelivery();
    var dpId = Guid.NewGuid();

    var result = await _matchingService.AcceptDeliveryAsync(delivery.Id, dpId, CancellationToken.None);

    Assert.True(result.IsSuccess);
    var updated = await _context.Deliveries.FindAsync(delivery.Id);
    Assert.Equal(dpId, updated.AssignedDPId);
    Assert.Equal("ASSIGNED", updated.Status);
}

[Fact]
public async Task AcceptDelivery_AlreadyAssigned_ReturnsError()
{
    var delivery = await CreateTestDelivery();
    delivery.AssignedDPId = Guid.NewGuid();
    await _context.SaveChangesAsync();

    var result = await _matchingService.AcceptDeliveryAsync(delivery.Id, Guid.NewGuid(), CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.Equal("ALREADY_ASSIGNED", result.ErrorCode);
}
```

---

## 8. Monitoring & Metrics

### Key Metrics:
- Deliveries created per day
- Matching success rate (% deliveries matched within 1st attempt)
- Average time to match (seconds)
- DP acceptance rate
- Unassignable delivery rate
- Notification-to-acceptance time

### Alerts:
- Matching failures > 10% in 1 hour
- Unassignable deliveries spike
- Notification delivery failures

---

**Status:** Ready for Development
**Next Steps:** Sprint 3-4 (3 weeks) - Delivery creation + matching algorithm + notifications
