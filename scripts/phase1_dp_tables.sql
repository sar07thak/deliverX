-- =====================================================
-- PHASE 1: DELIVERY PARTNER - NORMALIZED TABLES
-- =====================================================

USE DeliveryDost_Dev;
GO

-- DeliveryPartners - Core Identity (MASTER)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DeliveryPartners')
BEGIN
    CREATE TABLE DeliveryPartners (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        UserId UNIQUEIDENTIFIER NOT NULL,
        DPCMId UNIQUEIDENTIFIER NULL,

        -- Identity (rarely changes)
        FullName NVARCHAR(255) NOT NULL,
        ProfilePhotoUrl NVARCHAR(500) NULL,
        DOB DATETIME2 NULL,
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
    PRINT 'Created DeliveryPartners table';
END
GO

-- DPOperationalStatus - Real-time state (FREQUENTLY UPDATED)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DPOperationalStatus')
BEGIN
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
    PRINT 'Created DPOperationalStatus table';
END
GO

-- DPServiceAreas - Service area configuration
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DPServiceAreas')
BEGIN
    CREATE TABLE DPServiceAreas (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DPId UNIQUEIDENTIFIER NOT NULL,

        -- Area definition
        AreaType NVARCHAR(20) NOT NULL DEFAULT 'RADIUS',
        AreaName NVARCHAR(100) NULL,

        -- For RADIUS type
        CenterLat DECIMAL(10,8) NULL,
        CenterLng DECIMAL(11,8) NULL,
        RadiusKm DECIMAL(5,2) NULL,

        -- For POLYGON type (using geography for spatial queries)
        PolygonWKT NVARCHAR(MAX) NULL,
        PolygonGeography GEOGRAPHY NULL,

        -- Direction preferences
        PreferredDirection NVARCHAR(10) NULL,
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
    PRINT 'Created DPServiceAreas table';
END
GO

-- DPServiceAreaPincodes - For PINCODE-based service areas
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DPServiceAreaPincodes')
BEGIN
    CREATE TABLE DPServiceAreaPincodes (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        ServiceAreaId UNIQUEIDENTIFIER NOT NULL,
        Pincode NVARCHAR(6) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,

        CONSTRAINT FK_DPServiceAreaPincodes_ServiceAreas FOREIGN KEY (ServiceAreaId) REFERENCES DPServiceAreas(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_DPServiceAreaPincodes UNIQUE (ServiceAreaId, Pincode)
    );
    PRINT 'Created DPServiceAreaPincodes table';
END
GO

-- DPLanguages - Junction table for languages
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DPLanguages')
BEGIN
    CREATE TABLE DPLanguages (
        DPId UNIQUEIDENTIFIER NOT NULL,
        LanguageId INT NOT NULL,
        ProficiencyLevel NVARCHAR(20) NULL,

        CONSTRAINT PK_DPLanguages PRIMARY KEY (DPId, LanguageId),
        CONSTRAINT FK_DPLanguages_DeliveryPartners FOREIGN KEY (DPId) REFERENCES DeliveryPartners(Id) ON DELETE CASCADE,
        CONSTRAINT FK_DPLanguages_MasterLanguages FOREIGN KEY (LanguageId) REFERENCES MasterLanguages(Id)
    );
    PRINT 'Created DPLanguages table';
END
GO

-- DPAvailabilitySchedules - Weekly availability
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DPAvailabilitySchedules')
BEGIN
    CREATE TABLE DPAvailabilitySchedules (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DPId UNIQUEIDENTIFIER NOT NULL,

        DayOfWeek INT NOT NULL,
        StartTime TIME NOT NULL,
        EndTime TIME NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,

        CONSTRAINT FK_DPAvailabilitySchedules_DeliveryPartners FOREIGN KEY (DPId) REFERENCES DeliveryPartners(Id) ON DELETE CASCADE,
        CONSTRAINT CK_DPAvailabilitySchedules_DayOfWeek CHECK (DayOfWeek >= 0 AND DayOfWeek <= 6),
        CONSTRAINT CK_DPAvailabilitySchedules_Time CHECK (StartTime < EndTime)
    );
    PRINT 'Created DPAvailabilitySchedules table';
END
GO

PRINT 'Phase 1 - DP Tables: COMPLETE';
