# DeliveryDost Database Schema Refactoring Plan

## Executive Summary

This document provides a comprehensive analysis of the existing 58-table database schema and proposes a normalized, production-ready refactoring plan following Clean Architecture, SOLID principles, and 3NF+ normalization.

---

## 1. SCHEMA ANALYSIS SUMMARY

### 1.1 Current State Overview

| Metric | Value |
|--------|-------|
| Total Tables | 58 |
| Foreign Key Relationships | 75 |
| Primary "God Tables" | 4 (DeliveryPartnerProfiles, BusinessConsumerProfiles, DPCManagers, Deliveries) |
| JSON Blob Columns | 8+ |
| Mixed Static/Dynamic Tables | 6 |

### 1.2 Critical Issues Identified

#### A. DeliveryPartnerProfiles (32 columns) - WORST OFFENDER
**Problems:**
- Mixes static identity data with dynamic operational state
- Contains pricing config that should be versioned
- Service area stored as JSON blobs (ServiceAreaPolygonJson, ServiceAreaPincodesJson)
- Languages stored as JSON
- Real-time operational data (IsOnline, CurrentActiveDeliveries) mixed with profile

**Static Data (rarely changes):**
- Id, UserId, DPCMId, FullName, DOB, Gender, Address, ProfilePhotoUrl

**Dynamic/Operational Data (frequently changes):**
- IsOnline, LastOnlineAt, CurrentActiveDeliveries, IsActive, ActivatedAt

**Configuration Data (versioned changes):**
- PerKmRate, PerKgRate, MinCharge, MaxDistanceKm, MaxBidRate, MaxConcurrentDeliveries

**Service Area Data (separate entity):**
- ServiceAreaCenterLat/Lng, ServiceAreaRadiusKm, ServiceAreaPolygonJson, ServiceAreaPincodesJson, PreferredDirection, OneDirectionOnly, DirectionAngleDegrees

#### B. BusinessConsumerProfiles (16 columns)
**Problems:**
- PickupLocationsJson is a JSON blob (should be normalized to BCPickupLocations - already exists!)
- BankAccountEncrypted is duplicated (should reference BankVerifications)
- Mixes business identity with subscription state

#### C. DPCManagers (22 columns)
**Problems:**
- ServiceRegions is JSON (should be normalized)
- BankAccountEncrypted duplicated
- Commission config embedded (already has DPCMCommissionConfigs table)
- Security deposit info mixed with identity

#### D. Deliveries (50+ columns)
**Problems:**
- Extremely wide table
- Pickup and Drop details should be separate address records
- Caution/handling info could be separate
- Route info (RoutePolyline) should be separate

#### E. JSON Blob Anti-Patterns Found
| Table | Column | Should Be |
|-------|--------|-----------|
| DeliveryPartnerProfiles | Languages | MasterLanguages + DPLanguages junction |
| DeliveryPartnerProfiles | ServiceAreaPolygonJson | DPServiceAreaPolygons |
| DeliveryPartnerProfiles | ServiceAreaPincodesJson | DPServiceAreaPincodes |
| BusinessConsumerProfiles | PickupLocationsJson | BCPickupLocations (exists!) |
| BusinessConsumerProfiles | BusinessAddress | Normalized address fields |
| DPCManagers | ServiceRegions | DPCMServiceRegions |
| DPCManagers | BankAccountEncrypted | Reference BankVerifications |

### 1.3 What's Already Good

1. **Master Data Tables** - StateMasters, DistrictMasters, PincodeMasters are well-structured
2. **KYC Tables** - Separate tables for each verification type (Aadhaar, PAN, Bank, Police, VehicleLicense)
3. **Configuration Tables** - DPPricingConfigs, DPCMCommissionConfigs have effective date ranges
4. **Audit Logging** - AuthAuditLogs, AdminAuditLogs are properly separated
5. **Wallet System** - Wallets and WalletTransactions are well-normalized

---

## 2. PROPOSED TARGET SCHEMA

### 2.1 Entity Groupings (Domain Aggregates)

```
┌─────────────────────────────────────────────────────────────────┐
│                     IDENTITY & PROFILE                          │
├─────────────────────────────────────────────────────────────────┤
│ Users (master)                                                  │
│ ├── DeliveryPartners (master profile)                          │
│ │   ├── DPOperationalStatus (real-time state)                  │
│ │   ├── DPPricingConfigs (versioned pricing)                   │
│ │   ├── DPServiceAreas (service area definitions)              │
│ │   ├── DPAvailabilitySchedules (weekly schedules)             │
│ │   └── DPLanguages (junction to MasterLanguages)              │
│ ├── DPCManagers (master profile)                               │
│ │   ├── DPCMServiceRegions (service regions)                   │
│ │   └── DPCMCommissionConfigs (versioned)                      │
│ ├── BusinessConsumers (master profile)                         │
│ │   └── BCPickupLocations (existing, keep)                     │
│ └── Inspectors (existing, keep)                                │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                     KYC & VERIFICATION                          │
├─────────────────────────────────────────────────────────────────┤
│ KYCRequests (master tracking)                                   │
│ ├── AadhaarVerifications                                        │
│ ├── PANVerifications                                            │
│ ├── BankVerifications                                           │
│ ├── VehicleLicenseVerifications                                │
│ └── PoliceVerifications                                         │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                     DELIVERY OPERATIONS                         │
├─────────────────────────────────────────────────────────────────┤
│ Deliveries (core - slimmed down)                               │
│ ├── DeliveryAddresses (pickup/drop as separate records)        │
│ ├── DeliveryPackageDetails (package info)                      │
│ ├── DeliveryPricings (existing, keep)                          │
│ ├── DeliveryEvents (existing, keep)                            │
│ ├── DeliveryBids (existing, keep)                              │
│ ├── DeliveryMatchingHistories (existing, keep)                 │
│ └── ProofOfDeliveries (existing, keep)                         │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                     MASTER/LOOKUP DATA                          │
├─────────────────────────────────────────────────────────────────┤
│ MasterVehicleTypes (new)                                        │
│ MasterLanguages (new)                                           │
│ MasterPackageTypes (new)                                        │
│ MasterCautionTypes (new)                                        │
│ StateMasters (existing, keep)                                   │
│ DistrictMasters (existing, keep)                               │
│ PincodeMasters (existing, keep)                                │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 New Tables to Create

#### A. For Delivery Partners

| New Table | Purpose | Source Columns |
|-----------|---------|----------------|
| DeliveryPartners | Core identity/profile | From DeliveryPartnerProfiles (static only) |
| DPOperationalStatus | Real-time state | IsOnline, LastOnlineAt, CurrentActiveDeliveries |
| DPServiceAreas | Service area config | ServiceAreaCenter*, Radius, Direction* |
| DPServiceAreaPincodes | Normalized pincodes | From ServiceAreaPincodesJson |
| DPLanguages | Languages junction | From Languages JSON |
| DPAvailabilitySchedules | Weekly schedules | New structure |

#### B. For DPCM

| New Table | Purpose | Source Columns |
|-----------|---------|----------------|
| DPCMServiceRegions | Normalized regions | From ServiceRegions JSON |
| DPCMSecurityDeposits | Deposit history | Security deposit fields |

#### C. Master/Lookup Tables

| New Table | Purpose |
|-----------|---------|
| MasterVehicleTypes | Vehicle type lookup |
| MasterLanguages | Language lookup |
| MasterPackageTypes | Package type lookup |
| MasterCautionTypes | Caution type lookup |
| MasterAvailabilityTypes | Availability type lookup |

---

## 3. SQL DDL (CREATE / ALTER STATEMENTS)

### 3.1 Master/Lookup Tables

```sql
-- =====================================================
-- MASTER/LOOKUP TABLES
-- =====================================================

-- Master Vehicle Types
CREATE TABLE MasterVehicleTypes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(20) NOT NULL UNIQUE,
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(200) NULL,
    MaxWeightKg DECIMAL(5,2) NULL,
    MaxVolumeL DECIMAL(8,2) NULL,
    RequiresLicense BIT NOT NULL DEFAULT 1,
    IconUrl NVARCHAR(500) NULL,
    SortOrder INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Seed data for vehicle types
INSERT INTO MasterVehicleTypes (Code, Name, RequiresLicense, SortOrder) VALUES
('BICYCLE', 'Bicycle', 0, 1),
('TWO_WHEELER', 'Two Wheeler (Bike/Scooter)', 1, 2),
('THREE_WHEELER', 'Three Wheeler (Auto)', 1, 3),
('FOUR_WHEELER', 'Four Wheeler (Car/Van)', 1, 4),
('MINI_TRUCK', 'Mini Truck', 1, 5);

-- Master Languages
CREATE TABLE MasterLanguages (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(10) NOT NULL UNIQUE,
    Name NVARCHAR(50) NOT NULL,
    NativeName NVARCHAR(50) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

-- Seed Indian languages
INSERT INTO MasterLanguages (Code, Name, NativeName) VALUES
('hi', 'Hindi', N'हिन्दी'),
('en', 'English', 'English'),
('bn', 'Bengali', N'বাংলা'),
('te', 'Telugu', N'తెలుగు'),
('mr', 'Marathi', N'मराठी'),
('ta', 'Tamil', N'தமிழ்'),
('gu', 'Gujarati', N'ગુજરાતી'),
('kn', 'Kannada', N'ಕನ್ನಡ'),
('ml', 'Malayalam', N'മലയാളം'),
('pa', 'Punjabi', N'ਪੰਜਾਬੀ'),
('or', 'Odia', N'ଓଡ଼ିଆ');

-- Master Package Types
CREATE TABLE MasterPackageTypes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(20) NOT NULL UNIQUE,
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(200) NULL,
    RequiresSpecialHandling BIT NOT NULL DEFAULT 0,
    SortOrder INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1
);

INSERT INTO MasterPackageTypes (Code, Name, SortOrder) VALUES
('DOCUMENT', 'Document/Envelope', 1),
('PARCEL', 'Parcel/Box', 2),
('FOOD', 'Food Items', 3),
('FRAGILE', 'Fragile Items', 4),
('ELECTRONICS', 'Electronics', 5),
('MEDICINE', 'Medicine/Pharma', 6),
('FLOWERS', 'Flowers/Perishables', 7),
('OTHER', 'Other', 99);

-- Master Caution Types
CREATE TABLE MasterCautionTypes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(20) NOT NULL UNIQUE,
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(200) NULL,
    Severity INT NOT NULL DEFAULT 1, -- 1=Low, 2=Medium, 3=High
    HandlingInstructions NVARCHAR(500) NULL,
    IconClass NVARCHAR(50) NULL,
    SortOrder INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1
);

INSERT INTO MasterCautionTypes (Code, Name, Severity, SortOrder) VALUES
('NONE', 'No Special Handling', 0, 0),
('FRAGILE', 'Fragile - Handle with Care', 2, 1),
('LIQUID', 'Contains Liquid', 2, 2),
('PERISHABLE', 'Perishable - Time Sensitive', 3, 3),
('ELECTRONICS', 'Electronic Items', 2, 4),
('HAZARDOUS', 'Hazardous Material', 3, 5),
('VALUABLE', 'High Value Item', 2, 6),
('KEEP_UPRIGHT', 'Keep Upright', 1, 7);

-- Master Availability Types
CREATE TABLE MasterAvailabilityTypes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(20) NOT NULL UNIQUE,
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(200) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

INSERT INTO MasterAvailabilityTypes (Code, Name, Description) VALUES
('FULL_TIME', 'Full Time', 'Available all days, all hours'),
('PART_TIME', 'Part Time', 'Available specific hours'),
('WEEKENDS', 'Weekends Only', 'Available only on weekends'),
('ON_DEMAND', 'On Demand', 'Available when manually turned on');
```

### 3.2 Delivery Partner Normalized Tables

```sql
-- =====================================================
-- DELIVERY PARTNER - NORMALIZED TABLES
-- =====================================================

-- DeliveryPartners - Core Identity (MASTER)
-- This replaces the static portion of DeliveryPartnerProfiles
CREATE TABLE DeliveryPartners (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    DPCMId UNIQUEIDENTIFIER NULL,

    -- Identity (rarely changes)
    FullName NVARCHAR(255) NOT NULL,
    ProfilePhotoUrl NVARCHAR(500) NULL,
    DOB DATETIME2 NOT NULL,
    Gender NVARCHAR(10) NULL,

    -- Address (normalized)
    AddressLine1 NVARCHAR(255) NULL,
    AddressLine2 NVARCHAR(255) NULL,
    City NVARCHAR(100) NULL,
    State NVARCHAR(100) NULL,
    Pincode NVARCHAR(6) NULL,

    -- Vehicle (reference to master)
    VehicleTypeId INT NULL,

    -- Activation
    IsActive BIT NOT NULL DEFAULT 0,
    ActivatedAt DATETIME2 NULL,
    DeactivatedAt DATETIME2 NULL,
    DeactivationReason NVARCHAR(500) NULL,

    -- Audit
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    -- Constraints
    CONSTRAINT FK_DeliveryPartners_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_DeliveryPartners_DPCManagers FOREIGN KEY (DPCMId) REFERENCES DPCManagers(Id),
    CONSTRAINT FK_DeliveryPartners_VehicleType FOREIGN KEY (VehicleTypeId) REFERENCES MasterVehicleTypes(Id),
    CONSTRAINT UQ_DeliveryPartners_UserId UNIQUE (UserId)
);

-- DPOperationalStatus - Real-time state (FREQUENTLY UPDATED)
-- Separated to avoid lock contention on the profile table
CREATE TABLE DPOperationalStatus (
    DPId UNIQUEIDENTIFIER PRIMARY KEY,

    -- Online status
    IsOnline BIT NOT NULL DEFAULT 0,
    LastOnlineAt DATETIME2 NULL,
    LastOfflineAt DATETIME2 NULL,

    -- Current workload
    CurrentActiveDeliveries INT NOT NULL DEFAULT 0,
    MaxConcurrentDeliveries INT NOT NULL DEFAULT 1,

    -- Current location (for matching)
    CurrentLat DECIMAL(10,8) NULL,
    CurrentLng DECIMAL(11,8) NULL,
    LocationUpdatedAt DATETIME2 NULL,

    -- Battery/connectivity (for mobile app)
    BatteryLevel INT NULL,
    NetworkType NVARCHAR(20) NULL,

    -- Audit
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_DPOperationalStatus_DeliveryPartners FOREIGN KEY (DPId) REFERENCES DeliveryPartners(Id)
);

-- DPServiceAreas - Service area configuration
CREATE TABLE DPServiceAreas (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DPId UNIQUEIDENTIFIER NOT NULL,

    -- Area definition
    AreaType NVARCHAR(20) NOT NULL DEFAULT 'RADIUS', -- RADIUS, POLYGON, PINCODE
    AreaName NVARCHAR(100) NULL,

    -- For RADIUS type
    CenterLat DECIMAL(10,8) NULL,
    CenterLng DECIMAL(11,8) NULL,
    RadiusKm DECIMAL(5,2) NULL,

    -- For POLYGON type (using geography for spatial queries)
    PolygonWKT NVARCHAR(MAX) NULL, -- Well-Known Text format
    PolygonGeography GEOGRAPHY NULL, -- Computed from WKT

    -- Direction preferences
    PreferredDirection NVARCHAR(10) NULL, -- NORTH, SOUTH, EAST, WEST, ANY
    OneDirectionOnly BIT NOT NULL DEFAULT 0,
    DirectionAngleDegrees DECIMAL(5,2) NULL,

    -- Settings
    AllowDropOutsideArea BIT NOT NULL DEFAULT 0,
    MaxDistanceKm DECIMAL(5,2) NULL,

    -- Status
    IsActive BIT NOT NULL DEFAULT 1,
    IsPrimary BIT NOT NULL DEFAULT 0,

    -- Audit
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_DPServiceAreas_DeliveryPartners FOREIGN KEY (DPId) REFERENCES DeliveryPartners(Id)
);

-- DPServiceAreaPincodes - For PINCODE-based service areas
CREATE TABLE DPServiceAreaPincodes (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ServiceAreaId UNIQUEIDENTIFIER NOT NULL,
    Pincode NVARCHAR(6) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_DPServiceAreaPincodes_ServiceAreas FOREIGN KEY (ServiceAreaId) REFERENCES DPServiceAreas(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_DPServiceAreaPincodes UNIQUE (ServiceAreaId, Pincode)
);

-- DPLanguages - Junction table for languages
CREATE TABLE DPLanguages (
    DPId UNIQUEIDENTIFIER NOT NULL,
    LanguageId INT NOT NULL,
    ProficiencyLevel NVARCHAR(20) NULL, -- NATIVE, FLUENT, BASIC

    CONSTRAINT PK_DPLanguages PRIMARY KEY (DPId, LanguageId),
    CONSTRAINT FK_DPLanguages_DeliveryPartners FOREIGN KEY (DPId) REFERENCES DeliveryPartners(Id) ON DELETE CASCADE,
    CONSTRAINT FK_DPLanguages_MasterLanguages FOREIGN KEY (LanguageId) REFERENCES MasterLanguages(Id)
);

-- DPAvailabilitySchedules - Weekly availability
CREATE TABLE DPAvailabilitySchedules (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DPId UNIQUEIDENTIFIER NOT NULL,

    DayOfWeek INT NOT NULL, -- 0=Sunday, 1=Monday, etc.
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_DPAvailabilitySchedules_DeliveryPartners FOREIGN KEY (DPId) REFERENCES DeliveryPartners(Id) ON DELETE CASCADE,
    CONSTRAINT CK_DPAvailabilitySchedules_DayOfWeek CHECK (DayOfWeek >= 0 AND DayOfWeek <= 6),
    CONSTRAINT CK_DPAvailabilitySchedules_Time CHECK (StartTime < EndTime)
);
```

### 3.3 DPCM Normalized Tables

```sql
-- =====================================================
-- DPCM - NORMALIZED TABLES
-- =====================================================

-- DPCMServiceRegions - Normalized from ServiceRegions JSON
CREATE TABLE DPCMServiceRegions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DPCMId UNIQUEIDENTIFIER NOT NULL,

    RegionType NVARCHAR(20) NOT NULL, -- STATE, DISTRICT, PINCODE, CUSTOM
    StateName NVARCHAR(100) NULL,
    DistrictName NVARCHAR(100) NULL,
    Pincode NVARCHAR(6) NULL,
    CustomRegionName NVARCHAR(100) NULL,

    IsActive BIT NOT NULL DEFAULT 1,
    AssignedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_DPCMServiceRegions_DPCManagers FOREIGN KEY (DPCMId) REFERENCES DPCManagers(Id) ON DELETE CASCADE
);

-- DPCMSecurityDeposits - Security deposit history
CREATE TABLE DPCMSecurityDeposits (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DPCMId UNIQUEIDENTIFIER NOT NULL,

    Amount DECIMAL(10,2) NOT NULL,
    TransactionType NVARCHAR(20) NOT NULL, -- DEPOSIT, PARTIAL_REFUND, FULL_REFUND, FORFEITURE
    TransactionRef NVARCHAR(100) NULL,
    Status NVARCHAR(20) NOT NULL, -- PENDING, RECEIVED, REFUNDED, FORFEITED

    ReceivedAt DATETIME2 NULL,
    RefundedAt DATETIME2 NULL,
    Remarks NVARCHAR(500) NULL,

    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_DPCMSecurityDeposits_DPCManagers FOREIGN KEY (DPCMId) REFERENCES DPCManagers(Id)
);
```

### 3.4 Indexes for New Tables

```sql
-- =====================================================
-- INDEXES FOR NEW TABLES
-- =====================================================

-- DeliveryPartners indexes
CREATE NONCLUSTERED INDEX IX_DeliveryPartners_UserId ON DeliveryPartners(UserId);
CREATE NONCLUSTERED INDEX IX_DeliveryPartners_DPCMId ON DeliveryPartners(DPCMId) WHERE DPCMId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_DeliveryPartners_IsActive ON DeliveryPartners(IsActive) INCLUDE (UserId, FullName);
CREATE NONCLUSTERED INDEX IX_DeliveryPartners_VehicleTypeId ON DeliveryPartners(VehicleTypeId) WHERE VehicleTypeId IS NOT NULL;

-- DPOperationalStatus indexes (critical for matching)
CREATE NONCLUSTERED INDEX IX_DPOperationalStatus_IsOnline ON DPOperationalStatus(IsOnline)
    WHERE IsOnline = 1;
CREATE NONCLUSTERED INDEX IX_DPOperationalStatus_Location ON DPOperationalStatus(CurrentLat, CurrentLng)
    WHERE IsOnline = 1 AND CurrentLat IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_DPOperationalStatus_Availability ON DPOperationalStatus(IsOnline, CurrentActiveDeliveries, MaxConcurrentDeliveries)
    WHERE IsOnline = 1;

-- DPServiceAreas indexes
CREATE NONCLUSTERED INDEX IX_DPServiceAreas_DPId ON DPServiceAreas(DPId);
CREATE NONCLUSTERED INDEX IX_DPServiceAreas_Active ON DPServiceAreas(DPId, IsActive) WHERE IsActive = 1;
CREATE NONCLUSTERED INDEX IX_DPServiceAreas_Location ON DPServiceAreas(CenterLat, CenterLng, RadiusKm)
    WHERE IsActive = 1 AND AreaType = 'RADIUS';

-- Spatial index for polygon-based areas
CREATE SPATIAL INDEX IX_DPServiceAreas_Polygon ON DPServiceAreas(PolygonGeography)
    WHERE PolygonGeography IS NOT NULL;

-- DPServiceAreaPincodes indexes
CREATE NONCLUSTERED INDEX IX_DPServiceAreaPincodes_Pincode ON DPServiceAreaPincodes(Pincode)
    WHERE IsActive = 1;

-- DPLanguages indexes
CREATE NONCLUSTERED INDEX IX_DPLanguages_LanguageId ON DPLanguages(LanguageId);

-- DPAvailabilitySchedules indexes
CREATE NONCLUSTERED INDEX IX_DPAvailabilitySchedules_DPId ON DPAvailabilitySchedules(DPId, DayOfWeek)
    WHERE IsActive = 1;

-- DPCMServiceRegions indexes
CREATE NONCLUSTERED INDEX IX_DPCMServiceRegions_DPCMId ON DPCMServiceRegions(DPCMId) WHERE IsActive = 1;
CREATE NONCLUSTERED INDEX IX_DPCMServiceRegions_Pincode ON DPCMServiceRegions(Pincode) WHERE Pincode IS NOT NULL AND IsActive = 1;
CREATE NONCLUSTERED INDEX IX_DPCMServiceRegions_State ON DPCMServiceRegions(StateName) WHERE StateName IS NOT NULL AND IsActive = 1;
```

---

## 4. DATA MIGRATION SCRIPTS

### 4.1 Migrate DeliveryPartnerProfiles to Normalized Structure

```sql
-- =====================================================
-- DATA MIGRATION: DeliveryPartnerProfiles -> New Tables
-- Run in a transaction, idempotent
-- =====================================================

BEGIN TRANSACTION;
BEGIN TRY

    -- Step 1: Migrate to DeliveryPartners (core identity)
    INSERT INTO DeliveryPartners (
        Id, UserId, DPCMId, FullName, ProfilePhotoUrl, DOB, Gender,
        AddressLine1, City, State, Pincode,
        VehicleTypeId, IsActive, ActivatedAt, CreatedAt, UpdatedAt
    )
    SELECT
        dpp.Id,
        dpp.UserId,
        dpp.DPCMId,
        dpp.FullName,
        dpp.ProfilePhotoUrl,
        dpp.DOB,
        dpp.Gender,
        -- Parse address if stored as single field
        CASE WHEN dpp.Address IS NOT NULL THEN LEFT(dpp.Address, 255) ELSE NULL END,
        NULL, -- City - needs parsing or manual update
        NULL, -- State - needs parsing or manual update
        NULL, -- Pincode - needs parsing or manual update
        -- Map vehicle type to master
        (SELECT TOP 1 Id FROM MasterVehicleTypes WHERE Code = UPPER(REPLACE(dpp.VehicleType, ' ', '_'))),
        dpp.IsActive,
        dpp.ActivatedAt,
        dpp.CreatedAt,
        dpp.UpdatedAt
    FROM DeliveryPartnerProfiles dpp
    WHERE NOT EXISTS (SELECT 1 FROM DeliveryPartners dp WHERE dp.Id = dpp.Id);

    PRINT 'Migrated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records to DeliveryPartners';

    -- Step 2: Migrate to DPOperationalStatus
    INSERT INTO DPOperationalStatus (
        DPId, IsOnline, LastOnlineAt, CurrentActiveDeliveries,
        MaxConcurrentDeliveries, UpdatedAt
    )
    SELECT
        dpp.Id,
        dpp.IsOnline,
        dpp.LastOnlineAt,
        dpp.CurrentActiveDeliveries,
        CASE WHEN dpp.MaxConcurrentDeliveries > 0 THEN dpp.MaxConcurrentDeliveries ELSE 1 END,
        GETUTCDATE()
    FROM DeliveryPartnerProfiles dpp
    WHERE NOT EXISTS (SELECT 1 FROM DPOperationalStatus dos WHERE dos.DPId = dpp.Id);

    PRINT 'Migrated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records to DPOperationalStatus';

    -- Step 3: Migrate to DPServiceAreas (radius-based)
    INSERT INTO DPServiceAreas (
        Id, DPId, AreaType, CenterLat, CenterLng, RadiusKm,
        PreferredDirection, OneDirectionOnly, DirectionAngleDegrees,
        MaxDistanceKm, IsActive, IsPrimary, CreatedAt, UpdatedAt
    )
    SELECT
        NEWID(),
        dpp.Id,
        'RADIUS',
        dpp.ServiceAreaCenterLat,
        dpp.ServiceAreaCenterLng,
        dpp.ServiceAreaRadiusKm,
        dpp.PreferredDirection,
        dpp.OneDirectionOnly,
        dpp.DirectionAngleDegrees,
        dpp.MaxDistanceKm,
        1, -- IsActive
        1, -- IsPrimary
        GETUTCDATE(),
        GETUTCDATE()
    FROM DeliveryPartnerProfiles dpp
    WHERE dpp.ServiceAreaCenterLat IS NOT NULL
      AND dpp.ServiceAreaCenterLng IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM DPServiceAreas dsa
          WHERE dsa.DPId = dpp.Id AND dsa.AreaType = 'RADIUS'
      );

    PRINT 'Migrated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records to DPServiceAreas';

    -- Step 4: Migrate ServiceAreaPincodesJson to DPServiceAreaPincodes
    -- This requires parsing JSON - SQL Server 2016+
    INSERT INTO DPServiceAreaPincodes (Id, ServiceAreaId, Pincode, IsActive)
    SELECT
        NEWID(),
        dsa.Id,
        TRIM(j.value),
        1
    FROM DeliveryPartnerProfiles dpp
    CROSS APPLY OPENJSON(dpp.ServiceAreaPincodesJson) j
    INNER JOIN DPServiceAreas dsa ON dsa.DPId = dpp.Id AND dsa.IsPrimary = 1
    WHERE dpp.ServiceAreaPincodesJson IS NOT NULL
      AND ISJSON(dpp.ServiceAreaPincodesJson) = 1
      AND NOT EXISTS (
          SELECT 1 FROM DPServiceAreaPincodes dsap
          WHERE dsap.ServiceAreaId = dsa.Id AND dsap.Pincode = TRIM(j.value)
      );

    PRINT 'Migrated pincodes to DPServiceAreaPincodes';

    -- Step 5: Migrate Languages JSON to DPLanguages
    -- Assuming Languages is stored as JSON array like ["Hindi", "English"]
    INSERT INTO DPLanguages (DPId, LanguageId, ProficiencyLevel)
    SELECT
        dp.Id,
        ml.Id,
        'FLUENT'
    FROM DeliveryPartnerProfiles dpp
    INNER JOIN DeliveryPartners dp ON dp.Id = dpp.Id
    CROSS APPLY OPENJSON(dpp.Languages) j
    INNER JOIN MasterLanguages ml ON ml.Name = TRIM(j.value) OR ml.Code = LOWER(TRIM(j.value))
    WHERE dpp.Languages IS NOT NULL
      AND ISJSON(dpp.Languages) = 1
      AND NOT EXISTS (
          SELECT 1 FROM DPLanguages dl WHERE dl.DPId = dp.Id AND dl.LanguageId = ml.Id
      );

    PRINT 'Migrated languages to DPLanguages';

    -- Step 6: Migrate pricing to DPPricingConfigs if not already there
    INSERT INTO DPPricingConfigs (
        Id, DPId, PerKmRate, PerKgRate, MinCharge, MaxDistanceKm,
        AcceptsPriorityDelivery, PrioritySurcharge, PeakHourSurcharge,
        Currency, EffectiveFrom, CreatedAt, UpdatedAt
    )
    SELECT
        NEWID(),
        dpp.UserId, -- Note: DPPricingConfigs uses UserId, not DeliveryPartnerId
        dpp.PerKmRate,
        dpp.PerKgRate,
        dpp.MinCharge,
        dpp.MaxDistanceKm,
        0, -- AcceptsPriorityDelivery
        0, -- PrioritySurcharge
        0, -- PeakHourSurcharge
        'INR',
        GETUTCDATE(),
        GETUTCDATE(),
        GETUTCDATE()
    FROM DeliveryPartnerProfiles dpp
    WHERE dpp.PerKmRate IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM DPPricingConfigs dpc
          WHERE dpc.DPId = dpp.UserId
            AND dpc.EffectiveTo IS NULL -- Current active config
      );

    PRINT 'Migrated pricing to DPPricingConfigs';

    COMMIT TRANSACTION;
    PRINT 'Migration completed successfully!';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Migration failed: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
```

### 4.2 Migrate DPCManagers Service Regions

```sql
-- =====================================================
-- DATA MIGRATION: DPCManagers ServiceRegions JSON
-- =====================================================

BEGIN TRANSACTION;
BEGIN TRY

    -- Parse ServiceRegions JSON and insert into DPCMServiceRegions
    -- Assuming format like ["Delhi", "Noida", "Gurgaon"] or more complex
    INSERT INTO DPCMServiceRegions (Id, DPCMId, RegionType, CustomRegionName, IsActive, AssignedAt)
    SELECT
        NEWID(),
        dpcm.Id,
        'CUSTOM',
        TRIM(j.value),
        1,
        GETUTCDATE()
    FROM DPCManagers dpcm
    CROSS APPLY OPENJSON(dpcm.ServiceRegions) j
    WHERE dpcm.ServiceRegions IS NOT NULL
      AND ISJSON(dpcm.ServiceRegions) = 1
      AND NOT EXISTS (
          SELECT 1 FROM DPCMServiceRegions dsr
          WHERE dsr.DPCMId = dpcm.Id AND dsr.CustomRegionName = TRIM(j.value)
      );

    PRINT 'Migrated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' service regions';

    -- Migrate security deposit info
    INSERT INTO DPCMSecurityDeposits (
        Id, DPCMId, Amount, TransactionType, TransactionRef, Status,
        ReceivedAt, CreatedAt
    )
    SELECT
        NEWID(),
        dpcm.Id,
        dpcm.SecurityDeposit,
        'DEPOSIT',
        dpcm.SecurityDepositTransactionRef,
        ISNULL(dpcm.SecurityDepositStatus, 'PENDING'),
        dpcm.SecurityDepositReceivedAt,
        GETUTCDATE()
    FROM DPCManagers dpcm
    WHERE dpcm.SecurityDeposit > 0
      AND NOT EXISTS (
          SELECT 1 FROM DPCMSecurityDeposits dsd WHERE dsd.DPCMId = dpcm.Id
      );

    PRINT 'Migrated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' security deposits';

    COMMIT TRANSACTION;
    PRINT 'DPCM migration completed!';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'DPCM migration failed: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
```

---

## 5. BACKWARD COMPATIBILITY VIEWS

### 5.1 v_DeliveryPartnerProfiles - Reconstructs Old Table Shape

```sql
-- =====================================================
-- COMPATIBILITY VIEW: v_DeliveryPartnerProfiles
-- Provides same columns as original DeliveryPartnerProfiles
-- =====================================================

CREATE OR ALTER VIEW v_DeliveryPartnerProfiles AS
SELECT
    -- From DeliveryPartners
    dp.Id,
    dp.UserId,
    dp.DPCMId,
    dp.FullName,
    dp.ProfilePhotoUrl,
    dp.DOB,
    dp.Gender,
    -- Reconstruct Address field
    CONCAT_WS(', ', dp.AddressLine1, dp.City, dp.State, dp.Pincode) AS Address,
    -- Vehicle type from master
    mvt.Code AS VehicleType,
    -- Languages as JSON (reconstructed)
    (
        SELECT ml.Name AS [value]
        FROM DPLanguages dl
        INNER JOIN MasterLanguages ml ON ml.Id = dl.LanguageId
        WHERE dl.DPId = dp.Id
        FOR JSON PATH
    ) AS Languages,
    -- Availability (would need mapping or default)
    'FULL_TIME' AS Availability,

    -- From DPServiceAreas (primary area)
    dsa.CenterLat AS ServiceAreaCenterLat,
    dsa.CenterLng AS ServiceAreaCenterLng,
    dsa.RadiusKm AS ServiceAreaRadiusKm,
    dsa.PreferredDirection,
    dsa.OneDirectionOnly,
    dsa.DirectionAngleDegrees,
    dsa.MaxDistanceKm,
    -- Pincodes as JSON
    (
        SELECT dsap.Pincode AS [value]
        FROM DPServiceAreaPincodes dsap
        WHERE dsap.ServiceAreaId = dsa.Id AND dsap.IsActive = 1
        FOR JSON PATH
    ) AS ServiceAreaPincodesJson,
    dsa.PolygonWKT AS ServiceAreaPolygonJson,

    -- From DPPricingConfigs (current active)
    dpc.PerKmRate,
    dpc.PerKgRate,
    dpc.MinCharge,

    -- From DPOperationalStatus
    dos.IsOnline,
    dos.LastOnlineAt,
    dos.CurrentActiveDeliveries,
    dos.MaxConcurrentDeliveries,
    dpc.MaxDistanceKm AS MaxBidRate, -- Note: This mapping may need adjustment

    -- Activation status
    dp.IsActive,
    dp.ActivatedAt,
    dp.CreatedAt,
    dp.UpdatedAt

FROM DeliveryPartners dp
LEFT JOIN DPOperationalStatus dos ON dos.DPId = dp.Id
LEFT JOIN DPServiceAreas dsa ON dsa.DPId = dp.Id AND dsa.IsPrimary = 1
LEFT JOIN DPPricingConfigs dpc ON dpc.DPId = dp.UserId AND dpc.EffectiveTo IS NULL
LEFT JOIN MasterVehicleTypes mvt ON mvt.Id = dp.VehicleTypeId;
GO
```

### 5.2 v_OnlineDeliveryPartners - For Matching Queries

```sql
-- =====================================================
-- OPTIMIZED VIEW: v_OnlineDeliveryPartners
-- For delivery matching - only online, available DPs
-- =====================================================

CREATE OR ALTER VIEW v_OnlineDeliveryPartners AS
SELECT
    dp.Id AS DPId,
    dp.UserId,
    dp.FullName,
    dp.VehicleTypeId,
    mvt.Code AS VehicleType,

    -- Operational status
    dos.IsOnline,
    dos.CurrentActiveDeliveries,
    dos.MaxConcurrentDeliveries,
    (dos.MaxConcurrentDeliveries - dos.CurrentActiveDeliveries) AS AvailableSlots,
    dos.CurrentLat,
    dos.CurrentLng,
    dos.LocationUpdatedAt,

    -- Primary service area
    dsa.Id AS ServiceAreaId,
    dsa.AreaType,
    dsa.CenterLat,
    dsa.CenterLng,
    dsa.RadiusKm,
    dsa.PreferredDirection,
    dsa.OneDirectionOnly,
    dsa.AllowDropOutsideArea,
    dsa.PolygonGeography,

    -- Current pricing
    dpc.PerKmRate,
    dpc.PerKgRate,
    dpc.MinCharge

FROM DeliveryPartners dp
INNER JOIN DPOperationalStatus dos ON dos.DPId = dp.Id
LEFT JOIN DPServiceAreas dsa ON dsa.DPId = dp.Id AND dsa.IsPrimary = 1 AND dsa.IsActive = 1
LEFT JOIN DPPricingConfigs dpc ON dpc.DPId = dp.UserId AND dpc.EffectiveTo IS NULL
LEFT JOIN MasterVehicleTypes mvt ON mvt.Id = dp.VehicleTypeId

WHERE dp.IsActive = 1
  AND dos.IsOnline = 1
  AND dos.CurrentActiveDeliveries < dos.MaxConcurrentDeliveries;
GO
```

---

## 6. CLEANUP SCRIPTS (COMMENTED - EXECUTE MANUALLY AFTER VERIFICATION)

```sql
-- =====================================================
-- CLEANUP: Archive and Drop Old Tables
-- EXECUTE ONLY AFTER FULL VERIFICATION
-- =====================================================

/*
-- Step 1: Rename old table to archived
EXEC sp_rename 'DeliveryPartnerProfiles', 'DeliveryPartnerProfiles_Archived_20250108';

-- Step 2: Create synonym for compatibility (optional)
CREATE SYNONYM DeliveryPartnerProfiles FOR v_DeliveryPartnerProfiles;

-- Step 3: After 30-day observation, drop archived table
-- DROP TABLE DeliveryPartnerProfiles_Archived_20250108;
*/

-- Validation query before cleanup:
SELECT
    'DeliveryPartnerProfiles' AS TableName,
    (SELECT COUNT(*) FROM DeliveryPartnerProfiles) AS OldCount,
    (SELECT COUNT(*) FROM DeliveryPartners) AS NewCount,
    CASE
        WHEN (SELECT COUNT(*) FROM DeliveryPartnerProfiles) = (SELECT COUNT(*) FROM DeliveryPartners)
        THEN 'MATCH - Safe to archive'
        ELSE 'MISMATCH - Do not archive!'
    END AS Status;
```

---

## 7. INDEX & PERFORMANCE RECOMMENDATIONS

### 7.1 Key Query Patterns & Indexes

#### Pattern 1: Find Online DPs in Radius
```sql
-- Sample query
SELECT dp.*, dos.*, dsa.*
FROM DeliveryPartners dp
INNER JOIN DPOperationalStatus dos ON dos.DPId = dp.Id
INNER JOIN DPServiceAreas dsa ON dsa.DPId = dp.Id AND dsa.IsPrimary = 1
WHERE dp.IsActive = 1
  AND dos.IsOnline = 1
  AND dos.CurrentActiveDeliveries < dos.MaxConcurrentDeliveries
  AND dsa.AreaType = 'RADIUS'
  AND (
      -- Haversine approximation for quick filter
      ABS(dsa.CenterLat - @PickupLat) <= (dsa.RadiusKm / 111.0)
      AND ABS(dsa.CenterLng - @PickupLng) <= (dsa.RadiusKm / (111.0 * COS(RADIANS(dsa.CenterLat))))
  );

-- Supporting index (already created above)
-- IX_DPServiceAreas_Location covers this pattern
```

#### Pattern 2: Find DPs by Pincode
```sql
SELECT DISTINCT dp.*
FROM DeliveryPartners dp
INNER JOIN DPOperationalStatus dos ON dos.DPId = dp.Id
INNER JOIN DPServiceAreas dsa ON dsa.DPId = dp.Id
INNER JOIN DPServiceAreaPincodes dsap ON dsap.ServiceAreaId = dsa.Id
WHERE dp.IsActive = 1
  AND dos.IsOnline = 1
  AND dsap.Pincode = @PickupPincode
  AND dsap.IsActive = 1;

-- Supporting index (already created above)
-- IX_DPServiceAreaPincodes_Pincode covers this pattern
```

#### Pattern 3: Spatial Query with Geography
```sql
-- For polygon-based service areas
DECLARE @PickupPoint GEOGRAPHY = geography::Point(@PickupLat, @PickupLng, 4326);

SELECT dp.*, dsa.*
FROM DeliveryPartners dp
INNER JOIN DPOperationalStatus dos ON dos.DPId = dp.Id
INNER JOIN DPServiceAreas dsa ON dsa.DPId = dp.Id
WHERE dp.IsActive = 1
  AND dos.IsOnline = 1
  AND dsa.PolygonGeography IS NOT NULL
  AND dsa.PolygonGeography.STContains(@PickupPoint) = 1;

-- Supporting spatial index
-- IX_DPServiceAreas_Polygon covers this pattern
```

### 7.2 Statistics & Maintenance

```sql
-- Update statistics after migration
UPDATE STATISTICS DeliveryPartners;
UPDATE STATISTICS DPOperationalStatus;
UPDATE STATISTICS DPServiceAreas;
UPDATE STATISTICS DPServiceAreaPincodes;
UPDATE STATISTICS DPLanguages;

-- Rebuild indexes if fragmentation > 30%
ALTER INDEX ALL ON DeliveryPartners REBUILD;
ALTER INDEX ALL ON DPOperationalStatus REBUILD;
ALTER INDEX ALL ON DPServiceAreas REBUILD;
```

---

## 8. TESTING CHECKLIST

### 8.1 Data Integrity Validation

- [ ] **Row Count Match**
  ```sql
  SELECT 'DeliveryPartnerProfiles' AS Source, COUNT(*) FROM DeliveryPartnerProfiles
  UNION ALL
  SELECT 'DeliveryPartners' AS Source, COUNT(*) FROM DeliveryPartners;
  -- Counts should match
  ```

- [ ] **Key Fields Match** (sample 100 records)
  ```sql
  SELECT TOP 100
      dpp.Id,
      dpp.FullName AS Old_FullName,
      dp.FullName AS New_FullName,
      CASE WHEN dpp.FullName = dp.FullName THEN 'MATCH' ELSE 'MISMATCH' END AS Status
  FROM DeliveryPartnerProfiles dpp
  INNER JOIN DeliveryPartners dp ON dp.Id = dpp.Id
  ORDER BY dpp.CreatedAt DESC;
  ```

- [ ] **Operational Status Migrated**
  ```sql
  SELECT
      COUNT(*) AS TotalDPs,
      SUM(CASE WHEN dos.DPId IS NOT NULL THEN 1 ELSE 0 END) AS WithOperationalStatus
  FROM DeliveryPartners dp
  LEFT JOIN DPOperationalStatus dos ON dos.DPId = dp.Id;
  -- All DPs should have operational status
  ```

- [ ] **Service Areas Migrated**
  ```sql
  SELECT
      COUNT(*) AS TotalWithServiceArea_Old,
      (SELECT COUNT(DISTINCT DPId) FROM DPServiceAreas) AS TotalWithServiceArea_New
  FROM DeliveryPartnerProfiles
  WHERE ServiceAreaCenterLat IS NOT NULL;
  -- Counts should match
  ```

### 8.2 Compatibility View Validation

- [ ] **View Returns Same Columns**
  ```sql
  -- Compare column lists
  SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DeliveryPartnerProfiles'
  EXCEPT
  SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'v_DeliveryPartnerProfiles';
  -- Should return empty or only computed columns
  ```

- [ ] **View Returns Same Data**
  ```sql
  SELECT TOP 10 Id, FullName, IsOnline, ServiceAreaRadiusKm
  FROM DeliveryPartnerProfiles
  EXCEPT
  SELECT TOP 10 Id, FullName, IsOnline, ServiceAreaRadiusKm
  FROM v_DeliveryPartnerProfiles;
  -- Should return empty
  ```

### 8.3 Performance Validation

- [ ] **Online DP Query < 100ms**
  ```sql
  SET STATISTICS TIME ON;
  SELECT * FROM v_OnlineDeliveryPartners WHERE CurrentLat IS NOT NULL;
  SET STATISTICS TIME OFF;
  -- CPU time should be < 100ms for reasonable dataset
  ```

- [ ] **Spatial Query Performance**
  ```sql
  DECLARE @Point GEOGRAPHY = geography::Point(28.6139, 77.2090, 4326);
  SET STATISTICS TIME ON;
  SELECT * FROM DPServiceAreas WHERE PolygonGeography.STContains(@Point) = 1;
  SET STATISTICS TIME OFF;
  -- Should use spatial index
  ```

### 8.4 Application Compatibility

- [ ] DP Registration flow works
- [ ] DP can go online/offline
- [ ] DP service area update works
- [ ] Delivery matching finds correct DPs
- [ ] Pricing calculation uses new tables
- [ ] DPCM can view their DPs

---

## 9. ROLLBACK PLAN

If issues are found after migration:

```sql
-- 1. Drop the synonym if created
DROP SYNONYM IF EXISTS DeliveryPartnerProfiles;

-- 2. Rename archived table back
EXEC sp_rename 'DeliveryPartnerProfiles_Archived_20250108', 'DeliveryPartnerProfiles';

-- 3. Drop new tables (in reverse dependency order)
DROP TABLE IF EXISTS DPServiceAreaPincodes;
DROP TABLE IF EXISTS DPServiceAreas;
DROP TABLE IF EXISTS DPLanguages;
DROP TABLE IF EXISTS DPAvailabilitySchedules;
DROP TABLE IF EXISTS DPOperationalStatus;
DROP TABLE IF EXISTS DeliveryPartners;
DROP TABLE IF EXISTS DPCMServiceRegions;
DROP TABLE IF EXISTS DPCMSecurityDeposits;
DROP TABLE IF EXISTS MasterVehicleTypes;
DROP TABLE IF EXISTS MasterLanguages;
DROP TABLE IF EXISTS MasterPackageTypes;
DROP TABLE IF EXISTS MasterCautionTypes;
DROP TABLE IF EXISTS MasterAvailabilityTypes;

-- 4. Application will now use original table
```

---

## 10. EXECUTION STATUS

### Phase 1 - COMPLETED ✅
- [x] Analysis of DeliveryPartnerProfiles
- [x] Master/Lookup tables (MasterVehicleTypes, MasterLanguages, MasterPackageTypes, MasterCautionTypes, MasterAvailabilityTypes)
- [x] DP normalized tables (DeliveryPartners, DPOperationalStatus, DPServiceAreas, DPServiceAreaPincodes, DPLanguages, DPAvailabilitySchedules)
- [x] DPCM normalized tables (DPCMServiceRegions, DPCMSecurityDeposits)
- [x] Indexes created (including spatial indexes)
- [x] Migration scripts executed
- [x] Compatibility views (v_DeliveryPartnerProfiles, v_OnlineDeliveryPartners, v_DPCMWithRegions)

### Phase 2 - COMPLETED ✅
- [x] Normalize BusinessConsumerProfiles (BusinessConsumers, BCAddresses, BCSubscriptionHistory)
- [x] Normalize Deliveries table (DeliveryAddresses, DeliveryPackages, DeliveryRoutes, DeliveryStatusHistory)
- [x] Master tables (MasterBusinessCategories, MasterDeliveryStatuses)
- [x] Geography columns added to all location tables
- [x] Compatibility views (v_BusinessConsumerProfiles, v_DeliveriesExtended, v_ActiveDeliveries, v_DeliveryTimeline)

### Phase 3 - COMPLETED ✅
- [x] Archive tables for partitioning (DeliveriesArchive, DeliveryEventsArchive, AuditLogsArchive)
- [x] Archive procedures (sp_ArchiveOldDeliveries, sp_ArchiveOldAuditLogs)
- [x] CDC via triggers (TR_Users_ChangeTracking, TR_Deliveries_ChangeTracking, TR_Wallets_ChangeTracking, TR_WalletTransactions_ChangeTracking)
- [x] ChangeTrackingLog table for audit trail
- [x] Row-level security functions and predicates
- [x] Security context management (sp_SetSecurityContext, sp_ClearSecurityContext)
- [x] Secure views (v_MyDeliveries, v_MyWallet)
- [x] Full-text search catalog and procedures (requires FTS installation)

---

## 11. DATABASE SUMMARY (Post-Refactoring)

| Metric | Before | After |
|--------|--------|-------|
| Total Tables | 58 | **86** |
| Master/Lookup Tables | 3 | **11** |
| Archive Tables | 0 | **3** |
| Views | 0 | **12** |
| Stored Procedures | 0 | **8** |
| Triggers | 0 | **4** |
| Functions | 0 | **6** |
| Spatial Indexes | 0 | **5** |

### New Tables Created

**Phase 1 - Master Tables (7):**
- MasterVehicleTypes, MasterLanguages, MasterPackageTypes
- MasterCautionTypes, MasterAvailabilityTypes

**Phase 1 - DP Normalized (6):**
- DeliveryPartners, DPOperationalStatus, DPServiceAreas
- DPServiceAreaPincodes, DPLanguages, DPAvailabilitySchedules

**Phase 1 - DPCM Normalized (2):**
- DPCMServiceRegions, DPCMSecurityDeposits

**Phase 2 - BC Normalized (3):**
- BusinessConsumers, BCAddresses, BCSubscriptionHistory

**Phase 2 - Delivery Normalized (4):**
- DeliveryAddresses, DeliveryPackages, DeliveryRoutes, DeliveryStatusHistory
- MasterBusinessCategories, MasterDeliveryStatuses

**Phase 3 - Archive/Audit (4):**
- DeliveriesArchive, DeliveryEventsArchive, AuditLogsArchive
- ChangeTrackingLog, SecurityContext

### Views Created (12)
- v_DeliveryPartnerProfiles (backward compatibility)
- v_OnlineDeliveryPartners (optimized for matching)
- v_DPCMWithRegions
- v_BusinessConsumerProfiles (backward compatibility)
- v_DeliveriesExtended
- v_ActiveDeliveries
- v_DeliveryTimeline
- v_RecentChanges (audit)
- v_MyDeliveries (RLS secure view)
- v_MyWallet (RLS secure view)
- v_FullTextIndexStatus

### Stored Procedures (8)
- sp_ArchiveOldDeliveries
- sp_ArchiveOldAuditLogs
- sp_GetEntityHistory
- sp_SetSecurityContext
- sp_ClearSecurityContext
- sp_SearchDeliveries (requires FTS)
- sp_SearchUsers (requires FTS)
- sp_SearchAddresses (requires FTS)

### Triggers (4)
- TR_Users_ChangeTracking
- TR_Deliveries_ChangeTracking
- TR_Wallets_ChangeTracking
- TR_WalletTransactions_ChangeTracking

### Security Functions (6)
- fn_GetCurrentUserId
- fn_GetCurrentUserRole
- fn_GetCurrentDPCMId
- fn_DeliveriesSecurityPredicate
- fn_DPProfilesSecurityPredicate
- fn_WalletsSecurityPredicate

---

## 12. SQL SCRIPTS REFERENCE

All scripts located in `scripts/` folder:

| Script | Purpose |
|--------|---------|
| phase1_master_tables.sql | Master/lookup tables |
| phase1_dp_tables.sql | DP normalized tables |
| phase1_dpcm_tables.sql | DPCM normalized tables |
| phase1_indexes.sql | Phase 1 indexes |
| phase1_migration.sql | Phase 1 data migration |
| phase1_views.sql | Phase 1 compatibility views |
| phase2_bc_tables.sql | BC normalized tables |
| phase2_delivery_tables.sql | Delivery normalized tables |
| phase2_indexes.sql | Phase 2 indexes |
| phase2_migration.sql | Phase 2 data migration |
| phase2_views.sql | Phase 2 views |
| phase3_partitioning.sql | Archive tables and procedures |
| phase3_cdc_audit.sql | Change tracking triggers |
| phase3_row_level_security.sql | RLS functions and policies |
| phase3_fulltext_search.sql | Full-text search (requires FTS) |

---

**Document Version:** 3.0
**Created:** 2025-01-08
**Updated:** 2025-12-08
**Author:** Schema Refactoring Analysis
