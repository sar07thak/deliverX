# Feature PRD: Service Area & Geofencing

**Feature ID:** F-03
**Version:** 1.0
**Priority:** P0 (Critical - MVP Core)
**Estimated Effort:** 2-3 weeks
**Dependencies:** F-02 (Registration & KYC)

---

## 1. Feature Overview

### Purpose
Enable Delivery Partners and DPCMs to define their service coverage areas using geographical boundaries (circles/polygons), and match delivery requests to DPs whose service areas cover both pickup and drop locations.

### Success Criteria
- Service area configuration success rate > 99%
- Geospatial matching query performance < 500ms
- Accurate matching (0% false positives for coverage)
- Support for 100,000+ service areas without performance degradation

---

## 2. User Stories

### US-GEO-001: DP Service Area Setup
**As a** Delivery Partner
**I want to** define my service area on a map
**So that** I only receive delivery requests I can fulfill

**Acceptance Criteria:**
- Interactive map interface (Google Maps/Mapbox)
- Set center point by dragging pin or entering lat/lng
- Adjust radius using slider (1-50 km)
- Visual preview of coverage area
- Save and update anytime
- Support multiple service areas (future)

### US-GEO-002: DPCM Territory Management
**As a** DPCM
**I want to** define service territories for my DP network
**So that** I can manage deliveries within my operational region

**Acceptance Criteria:**
- Define city/region-level territories
- Assign DPs to territories
- Polygon-based areas (future)
- Territory overlap detection and warnings

### US-GEO-003: Delivery Matching by Location
**As a** System
**I want to** match deliveries only to DPs whose service area covers both pickup and drop
**So that** delivery assignments are geographically feasible

**Acceptance Criteria:**
- Check pickup point within DP service area
- Check drop point within DP service area
- Query returns eligible DPs within < 500ms
- Support for proximity-based ranking

---

## 3. Detailed Requirements

### 3.1 Service Area Models

#### Circle-based (MVP)
```json
{
  "centerLat": 26.9124,
  "centerLng": 75.7873,
  "radiusKm": 5
}
```

**Advantages:**
- Simple to configure
- Fast query performance
- Intuitive for DPs

**Limitations:**
- Not precise for irregular coverage (rivers, restricted zones)

#### Polygon-based (Phase 2)
```json
{
  "type": "Polygon",
  "coordinates": [
    [75.7873, 26.9124],
    [75.8001, 26.9200],
    [75.8100, 26.9050],
    [75.7873, 26.9124]
  ]
}
```

**Advantages:**
- Precise coverage
- Avoid restricted areas

**Challenges:**
- Complex UI for DP to draw
- Slower query performance

### 3.2 Geospatial Query Types

#### Type 1: Point-in-Circle (MVP)
```
Check if pickup/drop point is within distance from center:
distance = HAVERSINE(centerLat, centerLng, pointLat, pointLng)
if distance <= radiusKm: MATCH
```

#### Type 2: Point-in-Polygon (Phase 2)
```sql
-- Using SQL Server geography type
SELECT * FROM ServiceAreas
WHERE ServiceAreaGeography.STContains(geography::Point(@lat, @lng, 4326)) = 1
```

### 3.3 Matching Rules

**Rule 1: Both Ends Coverage (Default)**
- Pickup point WITHIN DP service area
- Drop point WITHIN DP service area

**Rule 2: Pickup Only Coverage (Optional)**
- Only pickup within service area
- DP willing to drop anywhere (configurable)

**Rule 3: Proximity Preference**
- Among eligible DPs, prefer closest to pickup
- Secondary: closest to drop

**Rule 4: Cross-Territory Matching**
- Allow DPs from different DPCMs if areas overlap

---

## 4. API Specifications

### 4.1 Set Service Area
```http
POST /api/v1/service-area
Authorization: Bearer <access-token>
Content-Type: application/json

Request:
{
  "userId": "dp-uuid",
  "areaType": "CIRCLE", // or POLYGON
  "centerLat": 26.9124,
  "centerLng": 75.7873,
  "radiusKm": 5,
  "isActive": true
}

Response (200):
{
  "serviceAreaId": "area-uuid",
  "coverage": {
    "type": "CIRCLE",
    "centerLat": 26.9124,
    "centerLng": 75.7873,
    "radiusKm": 5
  },
  "estimatedCoverage": "78.5 sq km",
  "message": "Service area updated successfully"
}
```

### 4.2 Find Eligible DPs for Delivery
```http
POST /api/v1/service-area/match
Authorization: Bearer <access-token>
Content-Type: application/json

Request:
{
  "pickupLat": 26.9150,
  "pickupLng": 75.7900,
  "dropLat": 26.9050,
  "dropLng": 75.7850
}

Response (200):
{
  "matchedDPs": [
    {
      "dpId": "dp-uuid-1",
      "dpName": "Ravi Kumar",
      "serviceAreaId": "area-uuid",
      "distanceFromPickupKm": 0.5,
      "distanceFromDropKm": 1.2,
      "coverageType": "BOTH_ENDS",
      "pricing": {
        "perKmRate": 10,
        "perKgRate": 5,
        "minCharge": 30
      }
    }
  ],
  "totalMatches": 1,
  "queryTimeMs": 45
}
```

### 4.3 Get Service Area Details
```http
GET /api/v1/service-area/{dpId}
Authorization: Bearer <access-token>

Response (200):
{
  "serviceAreas": [
    {
      "id": "area-uuid",
      "type": "CIRCLE",
      "centerLat": 26.9124,
      "centerLng": 75.7873,
      "radiusKm": 5,
      "isActive": true,
      "createdAt": "2025-11-10T10:00:00Z",
      "updatedAt": "2025-11-14T08:30:00Z"
    }
  ]
}
```

### 4.4 Check Point Coverage
```http
POST /api/v1/service-area/check-coverage
Content-Type: application/json

Request:
{
  "dpId": "dp-uuid",
  "pointLat": 26.9100,
  "pointLng": 75.7850
}

Response (200):
{
  "isCovered": true,
  "serviceAreaId": "area-uuid",
  "distanceFromCenterKm": 1.2
}
```

---

## 5. Database Schema

```sql
-- Service Areas
CREATE TABLE ServiceAreas (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Users(Id),
    UserRole NVARCHAR(50) NOT NULL, -- DP, DPCM
    AreaType NVARCHAR(20) NOT NULL DEFAULT 'CIRCLE', -- CIRCLE, POLYGON

    -- Circle-based fields
    CenterLat DECIMAL(10, 8) NULL,
    CenterLng DECIMAL(11, 8) NULL,
    RadiusKm DECIMAL(5, 2) NULL,

    -- Polygon-based fields (Phase 2)
    PolygonGeography GEOGRAPHY NULL,

    -- Computed geography for indexing (works for both circle and polygon)
    ServiceAreaGeography AS (
        CASE
            WHEN AreaType = 'CIRCLE' THEN
                geography::Point(CenterLat, CenterLng, 4326).STBuffer(RadiusKm * 1000)
            ELSE PolygonGeography
        END
    ) PERSISTED,

    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),

    INDEX IX_ServiceAreas_UserId (UserId),
    INDEX IX_ServiceAreas_IsActive (IsActive),
    CONSTRAINT CK_ServiceAreas_CircleData CHECK (
        (AreaType = 'CIRCLE' AND CenterLat IS NOT NULL AND CenterLng IS NOT NULL AND RadiusKm IS NOT NULL)
        OR (AreaType = 'POLYGON' AND PolygonGeography IS NOT NULL)
    )
);

-- Spatial index for fast geospatial queries
CREATE SPATIAL INDEX SIX_ServiceAreas_Geography
ON ServiceAreas(ServiceAreaGeography)
USING GEOGRAPHY_GRID
WITH (
    GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM),
    CELLS_PER_OBJECT = 16
);

-- Delivery Geolocation Logs (for analytics)
CREATE TABLE DeliveryGeoLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeliveryId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Deliveries(Id),
    EventType NVARCHAR(50) NOT NULL, -- PICKUP_REACHED, DROP_REACHED, IN_TRANSIT
    Latitude DECIMAL(10, 8) NOT NULL,
    Longitude DECIMAL(11, 8) NOT NULL,
    Altitude DECIMAL(10, 2) NULL,
    Accuracy DECIMAL(10, 2) NULL, -- GPS accuracy in meters
    Timestamp DATETIME2 DEFAULT GETUTCDATE(),

    INDEX IX_DeliveryGeoLogs_DeliveryId (DeliveryId),
    INDEX IX_DeliveryGeoLogs_Timestamp (Timestamp)
);
```

---

## 6. Development Implementation

### 6.1 Technology Stack
- **Geospatial DB:** SQL Server 2022 (geography type) or PostgreSQL with PostGIS
- **Map UI:** Google Maps JavaScript API / Mapbox GL JS
- **Distance Calculation:** Haversine formula (custom) or SQL Server STDistance()
- **Spatial Indexing:** SQL Server Spatial Index / PostGIS GIST index

### 6.2 Core Service Implementation

#### ServiceAreaService.cs
```csharp
public class ServiceAreaService : IServiceAreaService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ServiceAreaService> _logger;

    public async Task<Result<ServiceArea>> SetServiceAreaAsync(
        Guid userId,
        ServiceAreaRequest request,
        CancellationToken ct)
    {
        // 1. Validate input
        if (request.AreaType == "CIRCLE")
        {
            if (request.RadiusKm < 1 || request.RadiusKm > 50)
                return Result<ServiceArea>.Failure("Radius must be between 1 and 50 km");
        }

        // 2. Check if service area exists
        var existingArea = await _context.ServiceAreas
            .FirstOrDefaultAsync(sa => sa.UserId == userId && sa.IsActive, ct);

        if (existingArea != null)
        {
            // Update existing
            existingArea.CenterLat = request.CenterLat;
            existingArea.CenterLng = request.CenterLng;
            existingArea.RadiusKm = request.RadiusKm;
            existingArea.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new
            var newArea = new ServiceArea
            {
                UserId = userId,
                UserRole = "DP",
                AreaType = "CIRCLE",
                CenterLat = request.CenterLat,
                CenterLng = request.CenterLng,
                RadiusKm = request.RadiusKm,
                IsActive = true
            };
            _context.ServiceAreas.Add(newArea);
        }

        await _context.SaveChangesAsync(ct);

        return Result<ServiceArea>.Success(existingArea ?? newArea);
    }

    public async Task<List<Guid>> FindEligibleDPsAsync(
        double pickupLat,
        double pickupLng,
        double dropLat,
        double dropLng,
        CancellationToken ct)
    {
        // Create geography points for pickup and drop
        var pickupPoint = $"POINT({pickupLng} {pickupLat})";
        var dropPoint = $"POINT({dropLng} {dropLat})";

        // SQL query using spatial functions
        var sql = @"
            SELECT sa.UserId
            FROM ServiceAreas sa
            INNER JOIN DeliveryPartnerProfiles dp ON sa.UserId = dp.UserId
            WHERE sa.IsActive = 1
              AND dp.IsActive = 1
              AND sa.ServiceAreaGeography.STContains(geography::STGeomFromText(@pickupPoint, 4326)) = 1
              AND sa.ServiceAreaGeography.STContains(geography::STGeomFromText(@dropPoint, 4326)) = 1
        ";

        var eligibleDPs = await _context.Database
            .SqlQueryRaw<Guid>(sql,
                new SqlParameter("@pickupPoint", pickupPoint),
                new SqlParameter("@dropPoint", dropPoint))
            .ToListAsync(ct);

        return eligibleDPs;
    }

    // Alternative: Haversine-based for circle matching (faster for simple cases)
    public async Task<List<Guid>> FindEligibleDPsCircleAsync(
        double pickupLat,
        double pickupLng,
        double dropLat,
        double dropLng,
        CancellationToken ct)
    {
        var allAreas = await _context.ServiceAreas
            .Where(sa => sa.IsActive && sa.AreaType == "CIRCLE")
            .Include(sa => sa.User)
            .ToListAsync(ct);

        var eligibleDPs = allAreas
            .Where(sa =>
            {
                var pickupDistance = CalculateDistance(
                    sa.CenterLat.Value, sa.CenterLng.Value,
                    pickupLat, pickupLng);
                var dropDistance = CalculateDistance(
                    sa.CenterLat.Value, sa.CenterLng.Value,
                    dropLat, dropLng);

                return pickupDistance <= sa.RadiusKm && dropDistance <= sa.RadiusKm;
            })
            .Select(sa => sa.UserId)
            .ToList();

        return eligibleDPs;
    }

    private double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
    {
        // Haversine formula
        const double R = 6371; // Earth radius in km
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

### 6.3 Frontend Implementation (React Example)

```javascript
import { GoogleMap, Circle, Marker } from '@react-google-maps/api';
import { useState } from 'react';

function ServiceAreaMap({ initialCenter, initialRadius, onSave }) {
  const [center, setCenter] = useState(initialCenter);
  const [radius, setRadius] = useState(initialRadius * 1000); // Convert km to meters

  const handleMarkerDrag = (e) => {
    setCenter({
      lat: e.latLng.lat(),
      lng: e.latLng.lng()
    });
  };

  const handleSave = () => {
    onSave({
      centerLat: center.lat,
      centerLng: center.lng,
      radiusKm: radius / 1000
    });
  };

  return (
    <div>
      <GoogleMap
        center={center}
        zoom={12}
        mapContainerStyle={{ width: '100%', height: '500px' }}
      >
        <Marker
          position={center}
          draggable={true}
          onDragEnd={handleMarkerDrag}
        />
        <Circle
          center={center}
          radius={radius}
          options={{
            fillColor: '#4285F4',
            fillOpacity: 0.2,
            strokeColor: '#4285F4',
            strokeOpacity: 0.8,
            strokeWeight: 2
          }}
        />
      </GoogleMap>

      <div className="controls">
        <label>
          Radius: {(radius / 1000).toFixed(1)} km
          <input
            type="range"
            min="1000"
            max="50000"
            step="500"
            value={radius}
            onChange={(e) => setRadius(Number(e.target.value))}
          />
        </label>
        <button onClick={handleSave}>Save Service Area</button>
      </div>
    </div>
  );
}
```

---

## 7. Testing Strategy

### 7.1 Unit Tests
```csharp
[Fact]
public void CalculateDistance_SamePoint_ReturnsZero()
{
    var service = new ServiceAreaService(...);
    var distance = service.CalculateDistance(26.9124, 75.7873, 26.9124, 75.7873);
    Assert.Equal(0, distance, 2);
}

[Fact]
public async Task FindEligibleDPs_PointOutsideAllAreas_ReturnsEmpty()
{
    // Arrange
    await SeedServiceAreas();
    var service = CreateServiceAreaService();

    // Act
    var result = await service.FindEligibleDPsCircleAsync(
        0, 0, 0, 0, CancellationToken.None); // Middle of ocean

    // Assert
    Assert.Empty(result);
}

[Fact]
public async Task FindEligibleDPs_BothPointsInArea_ReturnsDP()
{
    // Test implementation
}
```

### 7.2 Integration Tests
- End-to-end service area creation
- Geospatial query accuracy test (known points)
- Performance test with 100,000 service areas

### 7.3 Load Tests
- 10,000 concurrent geospatial queries
- Query response time < 500ms at P95

---

## 8. Performance Optimization

### 8.1 Indexing Strategy
- Spatial index on ServiceAreaGeography column
- Composite index on (UserId, IsActive)
- Separate index for CenterLat, CenterLng for Haversine queries

### 8.2 Caching
```csharp
// Cache active service areas in Redis
public async Task<List<ServiceArea>> GetActiveServiceAreasAsync(CancellationToken ct)
{
    var cacheKey = "active_service_areas";
    var cached = await _cache.GetAsync<List<ServiceArea>>(cacheKey);

    if (cached != null)
        return cached;

    var areas = await _context.ServiceAreas
        .Where(sa => sa.IsActive)
        .ToListAsync(ct);

    await _cache.SetAsync(cacheKey, areas, TimeSpan.FromMinutes(15));
    return areas;
}
```

### 8.3 Query Optimization
- Use SQL Server spatial index (faster than in-memory Haversine for large datasets)
- For < 1000 service areas: Haversine in-memory
- For > 1000 service areas: SQL spatial queries

---

## 9. Monitoring & Metrics

### Key Metrics:
- Geospatial query latency (P50, P95, P99)
- Service area updates per day
- Average service area radius
- Delivery match rate (% of deliveries finding eligible DPs)
- Geographic coverage density (DPs per sq km)

### Alerts:
- Query latency > 1 second
- Zero matches for delivery (coverage gap)
- Spatial index fragmentation

---

## 10. Future Enhancements

- **Polygon-based service areas:** For precise coverage
- **Multi-area support:** DP can define multiple non-contiguous areas
- **Time-based areas:** Different coverage during peak/off-peak hours
- **Exclusion zones:** Define areas to avoid (gated communities, restricted zones)
- **Heatmap analytics:** Show delivery demand vs DP coverage
- **Auto-suggest service area:** Based on historical deliveries

---

## 11. Deployment Checklist

- [ ] SQL Server spatial features enabled
- [ ] Spatial index created on ServiceAreaGeography
- [ ] Google Maps API key configured (with restrictions)
- [ ] Redis cache for service areas
- [ ] Background job to rebuild spatial index weekly
- [ ] Monitoring dashboards for query performance
- [ ] Load testing completed (100k areas, 10k concurrent queries)

---

**Status:** Ready for Development
**Next Steps:** Sprint 2 (2 weeks) - Service area UI + geospatial matching
