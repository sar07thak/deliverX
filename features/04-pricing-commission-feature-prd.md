# Feature PRD: Pricing & Commission Model

**Feature ID:** F-04
**Version:** 1.0
**Priority:** P0 (Critical - MVP Core)
**Estimated Effort:** 2-3 weeks
**Dependencies:** F-02 (Registration), F-03 (Service Area)

---

## 1. Feature Overview

### Purpose
Implement a transparent, configurable pricing engine that calculates delivery costs based on distance, weight, and other factors, while managing multi-tier commission distribution between DPs, DPCMs, and platform.

### Success Criteria
- Accurate pricing calculation (100% accuracy)
- Commission split transparency
- Support for dynamic pricing rules
- Real-time price preview for customers
- Automated settlement calculations

---

## 2. Pricing Formula

### Base Formula
```
Total Price = (perKM × distance) + (perKG × weight) + minCharge + surcharges + taxes

Where:
- distance: Calculated using Haversine or Google Distance Matrix API
- weight: Package weight in KG
- perKM: Rate per kilometer (DP-configured or DPCM-mandated)
- perKG: Rate per kilogram (DP-configured)
- minCharge: Minimum charge even if distance is 0
- surcharges: Dynamic (peak hours, weather, priority delivery)
- taxes: GST (configurable %, default 18%)
```

### Commission Distribution
```
Customer Pays: ₹100

Distribution:
├─ DP Earning: ₹65 (65%)
├─ DPCM Commission: ₹10 (10%)
├─ Platform Fee: ₹15 (15%)
├─ Taxes (GST): ₹10 (10% of base)

DP receives: ₹65
DPCM receives: ₹10
Platform receives: ₹15
Tax authority: ₹10
```

---

## 3. API Specifications

### 3.1 Calculate Delivery Price (Preview)
```http
POST /api/v1/pricing/calculate
Content-Type: application/json

Request:
{
  "pickupLat": 26.9124,
  "pickupLng": 75.7873,
  "dropLat": 26.9050,
  "dropLng": 75.7840,
  "weightKg": 2.5,
  "packageType": "parcel",
  "priority": "ASAP", // or "SCHEDULED"
  "scheduledAt": null,
  "preferredDPId": null // Optional: get specific DP pricing
}

Response (200):
{
  "pricingBreakdown": {
    "distanceKm": 1.2,
    "distanceCost": 12.0,
    "weightCost": 12.5,
    "minCharge": 30.0,
    "surcharges": [
      {
        "type": "PEAK_HOUR",
        "amount": 5.0,
        "reason": "Peak hour (6-9 PM)"
      }
    ],
    "subtotal": 59.5,
    "gst": 10.71,
    "total": 70.21
  },
  "availableDPs": [
    {
      "dpId": "dp-uuid",
      "dpName": "Ravi Kumar",
      "rating": 4.8,
      "estimatedPrice": 70.21,
      "eta": "15-20 mins"
    }
  ],
  "cheapestPrice": 70.21,
  "estimatedDeliveryTime": "15-20 mins"
}
```

### 3.2 Get DP Pricing Configuration
```http
GET /api/v1/pricing/dp/{dpId}
Authorization: Bearer <access-token>

Response (200):
{
  "dpId": "dp-uuid",
  "pricingConfig": {
    "perKmRate": 10.0,
    "perKgRate": 5.0,
    "minCharge": 30.0,
    "maxDistanceKm": 20,
    "acceptsPriorityDelivery": true,
    "prioritySurcharge": 10.0,
    "peakHourSurcharge": 5.0,
    "currency": "INR"
  },
  "dpcmCommission": {
    "type": "PERCENTAGE",
    "value": 10.0
  },
  "platformFee": {
    "type": "PERCENTAGE",
    "value": 15.0
  }
}
```

### 3.3 Update DP Pricing
```http
PATCH /api/v1/pricing/dp/{dpId}
Authorization: Bearer <access-token>
Content-Type: application/json

Request:
{
  "perKmRate": 12.0,
  "perKgRate": 6.0,
  "minCharge": 35.0
}

Response (200):
{
  "message": "Pricing updated successfully",
  "effectiveFrom": "2025-11-14T10:00:00Z"
}
```

### 3.4 Admin - Configure Platform Fees
```http
PATCH /api/v1/pricing/platform/fees
Authorization: Bearer <admin-access-token>
Content-Type: application/json

Request:
{
  "platformFeePercentage": 15.0,
  "gstPercentage": 18.0,
  "peakHourSurcharge": 5.0,
  "peakHours": ["08:00-10:00", "18:00-21:00"]
}

Response (200):
{
  "message": "Platform fees updated",
  "effectiveFrom": "2025-11-15T00:00:00Z"
}
```

---

## 4. Database Schema

```sql
-- DP Pricing Configuration
CREATE TABLE DPPricingConfigs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DPId UNIQUEIDENTIFIER NOT NULL UNIQUE FOREIGN KEY REFERENCES DeliveryPartnerProfiles(Id),
    PerKmRate DECIMAL(10, 2) NOT NULL,
    PerKgRate DECIMAL(10, 2) NOT NULL,
    MinCharge DECIMAL(10, 2) NOT NULL,
    MaxDistanceKm DECIMAL(5, 2) NOT NULL DEFAULT 20,
    AcceptsPriorityDelivery BIT DEFAULT 1,
    PrioritySurcharge DECIMAL(10, 2) DEFAULT 0,
    PeakHourSurcharge DECIMAL(10, 2) DEFAULT 0,
    Currency NVARCHAR(3) DEFAULT 'INR',
    EffectiveFrom DATETIME2 DEFAULT GETUTCDATE(),
    EffectiveTo DATETIME2 NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),

    INDEX IX_DPPricing_DPId (DPId),
    INDEX IX_DPPricing_Effective (EffectiveFrom, EffectiveTo)
);

-- DPCM Commission Configuration
CREATE TABLE DPCMCommissionConfigs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DPCMId UNIQUEIDENTIFIER NOT NULL UNIQUE FOREIGN KEY REFERENCES DPCManagers(Id),
    CommissionType NVARCHAR(20) NOT NULL, -- PERCENTAGE, FLAT_PER_DELIVERY
    CommissionValue DECIMAL(10, 2) NOT NULL,
    MinCommissionAmount DECIMAL(10, 2) DEFAULT 0,
    MaxCommissionAmount DECIMAL(10, 2) NULL,
    EffectiveFrom DATETIME2 DEFAULT GETUTCDATE(),
    EffectiveTo DATETIME2 NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),

    INDEX IX_DPCMCommission_DPCMId (DPCMId)
);

-- Platform Fee Configuration
CREATE TABLE PlatformFeeConfigs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FeeType NVARCHAR(50) NOT NULL, -- PLATFORM_COMMISSION, GST, PEAK_SURCHARGE
    FeeCalculationType NVARCHAR(20) NOT NULL, -- PERCENTAGE, FLAT
    FeeValue DECIMAL(10, 2) NOT NULL,
    ApplicableRoles NVARCHAR(MAX), -- JSON: ["DP", "DPCM", "DBC"]
    Conditions NVARCHAR(MAX), -- JSON: {"timeRange": "18:00-21:00"}
    EffectiveFrom DATETIME2 DEFAULT GETUTCDATE(),
    EffectiveTo DATETIME2 NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),

    INDEX IX_PlatformFee_Effective (EffectiveFrom, EffectiveTo)
);

-- Delivery Pricing Records (stored after delivery creation)
CREATE TABLE DeliveryPricings (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeliveryId UNIQUEIDENTIFIER NOT NULL UNIQUE FOREIGN KEY REFERENCES Deliveries(Id),
    DistanceKm DECIMAL(10, 2) NOT NULL,
    WeightKg DECIMAL(10, 2) NOT NULL,
    PerKmRate DECIMAL(10, 2) NOT NULL,
    PerKgRate DECIMAL(10, 2) NOT NULL,
    MinCharge DECIMAL(10, 2) NOT NULL,
    Surcharges NVARCHAR(MAX), -- JSON array: [{"type": "PEAK_HOUR", "amount": 5.0}]
    Subtotal DECIMAL(10, 2) NOT NULL,
    GSTPercentage DECIMAL(5, 2) NOT NULL,
    GSTAmount DECIMAL(10, 2) NOT NULL,
    TotalAmount DECIMAL(10, 2) NOT NULL,
    DPEarning DECIMAL(10, 2) NOT NULL,
    DPCMCommission DECIMAL(10, 2) NOT NULL,
    PlatformFee DECIMAL(10, 2) NOT NULL,
    Currency NVARCHAR(3) DEFAULT 'INR',
    CalculatedAt DATETIME2 DEFAULT GETUTCDATE(),

    INDEX IX_DeliveryPricing_DeliveryId (DeliveryId)
);
```

---

## 5. Development Implementation

### 5.1 PricingService.cs
```csharp
public class PricingService : IPricingService
{
    private readonly ApplicationDbContext _context;
    private readonly IDistanceCalculator _distanceCalculator;
    private readonly ILogger<PricingService> _logger;

    public async Task<PricingCalculationResult> CalculatePriceAsync(
        PricingRequest request,
        CancellationToken ct)
    {
        // 1. Calculate distance
        var distance = await _distanceCalculator.CalculateAsync(
            request.PickupLat, request.PickupLng,
            request.DropLat, request.DropLng);

        // 2. Get eligible DPs
        var eligibleDPIds = await _serviceAreaService.FindEligibleDPsAsync(
            request.PickupLat, request.PickupLng,
            request.DropLat, request.DropLng, ct);

        if (!eligibleDPIds.Any())
            return PricingCalculationResult.Failure("No delivery partners available for this route");

        // 3. Get pricing for each DP
        var dpPricings = new List<DPPricingEstimate>();

        foreach (var dpId in eligibleDPIds)
        {
            var dpConfig = await GetDPPricingConfigAsync(dpId, ct);
            var breakdown = CalculatePriceBreakdown(dpConfig, distance, request.WeightKg, request.Priority);

            dpPricings.Add(new DPPricingEstimate
            {
                DPId = dpId,
                Breakdown = breakdown,
                TotalPrice = breakdown.Total
            });
        }

        // 4. Sort by price (cheapest first)
        var sortedPricings = dpPricings.OrderBy(p => p.TotalPrice).ToList();

        return new PricingCalculationResult
        {
            IsSuccess = true,
            DistanceKm = distance,
            AvailableDPs = sortedPricings,
            CheapestPrice = sortedPricings.First().TotalPrice
        };
    }

    private PriceBreakdown CalculatePriceBreakdown(
        DPPricingConfig config,
        double distanceKm,
        double weightKg,
        string priority)
    {
        // Distance cost
        var distanceCost = config.PerKmRate * (decimal)distanceKm;

        // Weight cost
        var weightCost = config.PerKgRate * (decimal)weightKg;

        // Base cost (max of calculated or min charge)
        var baseCost = Math.Max(distanceCost + weightCost, config.MinCharge);

        // Surcharges
        var surcharges = new List<Surcharge>();

        // Priority surcharge
        if (priority == "ASAP" && config.AcceptsPriorityDelivery)
        {
            surcharges.Add(new Surcharge
            {
                Type = "PRIORITY",
                Amount = config.PrioritySurcharge,
                Reason = "ASAP delivery requested"
            });
        }

        // Peak hour surcharge
        if (IsPeakHour(DateTime.UtcNow))
        {
            surcharges.Add(new Surcharge
            {
                Type = "PEAK_HOUR",
                Amount = config.PeakHourSurcharge,
                Reason = $"Peak hour ({DateTime.UtcNow:HH:mm})"
            });
        }

        var totalSurcharges = surcharges.Sum(s => s.Amount);
        var subtotal = baseCost + totalSurcharges;

        // GST
        var gstPercentage = GetCurrentGSTPercentage();
        var gstAmount = subtotal * gstPercentage / 100;

        var total = subtotal + gstAmount;

        return new PriceBreakdown
        {
            DistanceKm = distanceKm,
            DistanceCost = distanceCost,
            WeightCost = weightCost,
            MinCharge = config.MinCharge,
            Surcharges = surcharges,
            Subtotal = subtotal,
            GSTPercentage = gstPercentage,
            GSTAmount = gstAmount,
            Total = total
        };
    }

    public async Task<CommissionBreakdown> CalculateCommissionAsync(
        Guid deliveryId,
        decimal totalAmount,
        CancellationToken ct)
    {
        var delivery = await _context.Deliveries
            .Include(d => d.DeliveryPartner)
            .ThenInclude(dp => dp.DPCM)
            .FirstOrDefaultAsync(d => d.Id == deliveryId, ct);

        // Get commission configs
        var platformFeeConfig = await GetPlatformFeeConfigAsync(ct);
        var dpcmCommissionConfig = delivery.DeliveryPartner.DPCMId.HasValue
            ? await GetDPCMCommissionConfigAsync(delivery.DeliveryPartner.DPCMId.Value, ct)
            : null;

        // Calculate platform fee
        var platformFee = totalAmount * platformFeeConfig.FeeValue / 100;

        // Calculate DPCM commission
        var dpcmCommission = dpcmCommissionConfig != null
            ? CalculateDPCMCommission(totalAmount, dpcmCommissionConfig)
            : 0;

        // DP earning = total - platform fee - DPCM commission - GST
        var gstAmount = totalAmount * GetCurrentGSTPercentage() / 100;
        var dpEarning = totalAmount - platformFee - dpcmCommission - gstAmount;

        return new CommissionBreakdown
        {
            TotalAmount = totalAmount,
            DPEarning = dpEarning,
            DPCMCommission = dpcmCommission,
            PlatformFee = platformFee,
            GSTAmount = gstAmount
        };
    }

    private decimal CalculateDPCMCommission(decimal totalAmount, DPCMCommissionConfig config)
    {
        decimal commission;

        if (config.CommissionType == "PERCENTAGE")
        {
            commission = totalAmount * config.CommissionValue / 100;
        }
        else // FLAT_PER_DELIVERY
        {
            commission = config.CommissionValue;
        }

        // Apply min/max limits
        if (config.MinCommissionAmount > 0)
            commission = Math.Max(commission, config.MinCommissionAmount);

        if (config.MaxCommissionAmount.HasValue)
            commission = Math.Min(commission, config.MaxCommissionAmount.Value);

        return commission;
    }

    private bool IsPeakHour(DateTime time)
    {
        var hour = time.Hour;
        return (hour >= 8 && hour < 10) || (hour >= 18 && hour < 21);
    }

    private decimal GetCurrentGSTPercentage()
    {
        // Fetch from config or default to 18%
        return 18.0m;
    }
}
```

---

## 6. Testing Strategy

### 6.1 Unit Tests
```csharp
[Theory]
[InlineData(5, 2, 10, 5, 30, 60)] // distance, weight, perKm, perKg, minCharge, expected
[InlineData(1, 1, 10, 5, 30, 30)] // Below min charge, should return minCharge
[InlineData(10, 5, 10, 5, 30, 125)] // distance*perKm + weight*perKg
public void CalculatePrice_VariousInputs_ReturnsCorrectPrice(
    double distance, double weight, decimal perKm, decimal perKg, decimal minCharge, decimal expected)
{
    var config = new DPPricingConfig
    {
        PerKmRate = perKm,
        PerKgRate = perKg,
        MinCharge = minCharge
    };

    var result = _service.CalculatePriceBreakdown(config, distance, weight, "SCHEDULED");

    Assert.Equal(expected, result.Subtotal);
}

[Fact]
public void CalculateCommission_100Total_SplitsCorrectly()
{
    // Arrange: Total ₹100, Platform 15%, DPCM 10%
    var delivery = CreateTestDelivery();

    // Act
    var breakdown = await _service.CalculateCommissionAsync(delivery.Id, 100, CancellationToken.None);

    // Assert
    Assert.Equal(15, breakdown.PlatformFee);
    Assert.Equal(10, breakdown.DPCMCommission);
    Assert.InRange(breakdown.DPEarning, 60, 70); // Approx 65 after GST
}
```

---

## 7. Monitoring & Metrics

### Key Metrics:
- Average delivery price
- Price range (P50, P95)
- Commission distribution (DP vs DPCM vs Platform)
- Pricing calculation latency
- Price change impact on acceptance rate

### Alerts:
- Pricing calculation failures
- Negative DP earnings (config error)
- Unusual surcharge spikes

---

## 8. Future Enhancements

- **Dynamic surge pricing:** Based on demand/supply ratio
- **Promotional discounts:** Coupon codes
- **Subscription-based pricing:** Monthly plans with reduced rates
- **Distance API integration:** Google Distance Matrix for accurate road distance
- **Weather-based surcharges:** Rain, extreme heat
- **Multi-currency support:** International expansion

---

**Status:** Ready for Development
**Next Steps:** Sprint 2 (2 weeks) - Implement pricing engine + commission calculator
