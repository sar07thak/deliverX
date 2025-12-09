-- =====================================================
-- PHASE 2: DELIVERIES - NORMALIZED TABLES
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- DeliveryAddresses - Separated pickup and drop addresses
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DeliveryAddresses')
BEGIN
    CREATE TABLE DeliveryAddresses (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DeliveryId UNIQUEIDENTIFIER NOT NULL,
        AddressType NVARCHAR(10) NOT NULL, -- PICKUP, DROP

        -- Geolocation
        Latitude DECIMAL(10,8) NOT NULL,
        Longitude DECIMAL(11,8) NOT NULL,
        GeoLocation GEOGRAPHY NULL,

        -- Address text
        AddressName NVARCHAR(100) NULL,
        AddressLine NVARCHAR(500) NOT NULL,

        -- Contact details
        ContactName NVARCHAR(255) NULL,
        ContactPhone NVARCHAR(15) NOT NULL,
        AlternatePhone NVARCHAR(15) NULL,
        WhatsAppNumber NVARCHAR(15) NULL,
        ContactEmail NVARCHAR(255) NULL,

        -- Instructions
        Instructions NVARCHAR(500) NULL,

        -- Reference to saved address (if used)
        SavedAddressId UNIQUEIDENTIFIER NULL,

        -- Timestamps
        EstimatedArrival DATETIME2 NULL,
        ActualArrival DATETIME2 NULL,

        -- Audit
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_DeliveryAddresses_Deliveries FOREIGN KEY (DeliveryId) REFERENCES Deliveries(Id) ON DELETE CASCADE,
        CONSTRAINT FK_DeliveryAddresses_SavedAddress FOREIGN KEY (SavedAddressId) REFERENCES SavedAddresses(Id),
        CONSTRAINT CK_DeliveryAddresses_Type CHECK (AddressType IN ('PICKUP', 'DROP')),
        CONSTRAINT UQ_DeliveryAddresses UNIQUE (DeliveryId, AddressType)
    );
    PRINT 'Created DeliveryAddresses table';
END
GO

-- DeliveryPackages - Package details (supports multiple packages per delivery)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DeliveryPackages')
BEGIN
    CREATE TABLE DeliveryPackages (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DeliveryId UNIQUEIDENTIFIER NOT NULL,

        -- Package identification
        PackageNumber INT NOT NULL DEFAULT 1,
        TrackingCode NVARCHAR(50) NULL,

        -- Package type (reference to master)
        PackageTypeId INT NULL,
        PackageType NVARCHAR(50) NULL, -- Denormalized for display

        -- Dimensions
        WeightKg DECIMAL(8,3) NOT NULL DEFAULT 0,
        LengthCm DECIMAL(8,2) NULL,
        WidthCm DECIMAL(8,2) NULL,
        HeightCm DECIMAL(8,2) NULL,
        VolumetricWeight DECIMAL(8,3) NULL,

        -- Value
        DeclaredValue DECIMAL(12,2) NULL,
        Currency NVARCHAR(3) NOT NULL DEFAULT 'INR',

        -- Description
        Description NVARCHAR(500) NULL,
        Contents NVARCHAR(500) NULL,

        -- Handling
        CautionTypeId INT NULL,
        CautionType NVARCHAR(50) NULL, -- Denormalized
        CautionNotes NVARCHAR(500) NULL,
        RequiresSpecialHandling BIT NOT NULL DEFAULT 0,
        IsHazardous BIT NOT NULL DEFAULT 0,
        IsFragile BIT NOT NULL DEFAULT 0,

        -- Package photos
        PhotoUrls NVARCHAR(MAX) NULL, -- JSON array of URLs

        -- Audit
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_DeliveryPackages_Deliveries FOREIGN KEY (DeliveryId) REFERENCES Deliveries(Id) ON DELETE CASCADE,
        CONSTRAINT FK_DeliveryPackages_PackageType FOREIGN KEY (PackageTypeId) REFERENCES MasterPackageTypes(Id),
        CONSTRAINT FK_DeliveryPackages_CautionType FOREIGN KEY (CautionTypeId) REFERENCES MasterCautionTypes(Id)
    );
    PRINT 'Created DeliveryPackages table';
END
GO

-- DeliveryRoutes - Route and distance information
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DeliveryRoutes')
BEGIN
    CREATE TABLE DeliveryRoutes (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DeliveryId UNIQUEIDENTIFIER NOT NULL,

        -- Route info
        DistanceKm DECIMAL(10,3) NOT NULL,
        DistanceSource NVARCHAR(50) NULL, -- GOOGLE, OSM, STRAIGHT_LINE, MANUAL

        -- Duration
        EstimatedDurationMinutes INT NULL,
        ActualDurationMinutes INT NULL,

        -- Route path
        RoutePolyline NVARCHAR(MAX) NULL, -- Encoded polyline
        RouteGeography GEOGRAPHY NULL, -- Full route as geography

        -- Via points (if any)
        ViaPointsJson NVARCHAR(MAX) NULL, -- JSON array of waypoints

        -- Navigation
        TurnByTurnJson NVARCHAR(MAX) NULL, -- JSON navigation instructions

        -- Timestamps
        RouteCalculatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        RouteStartedAt DATETIME2 NULL,
        RouteCompletedAt DATETIME2 NULL,

        -- Audit
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_DeliveryRoutes_Deliveries FOREIGN KEY (DeliveryId) REFERENCES Deliveries(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_DeliveryRoutes_Delivery UNIQUE (DeliveryId)
    );
    PRINT 'Created DeliveryRoutes table';
END
GO

-- DeliveryStatusHistory - Full status history with timestamps
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DeliveryStatusHistory')
BEGIN
    CREATE TABLE DeliveryStatusHistory (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DeliveryId UNIQUEIDENTIFIER NOT NULL,

        -- Status change
        PreviousStatus NVARCHAR(50) NULL,
        NewStatus NVARCHAR(50) NOT NULL,

        -- Change details
        ChangedBy UNIQUEIDENTIFIER NULL, -- User who made the change
        ChangedByRole NVARCHAR(20) NULL,
        ChangeReason NVARCHAR(500) NULL,

        -- Location at status change
        Latitude DECIMAL(10,8) NULL,
        Longitude DECIMAL(11,8) NULL,

        -- Metadata
        MetadataJson NVARCHAR(MAX) NULL, -- Additional context as JSON

        -- Timestamp
        ChangedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_DeliveryStatusHistory_Deliveries FOREIGN KEY (DeliveryId) REFERENCES Deliveries(Id) ON DELETE CASCADE,
        CONSTRAINT FK_DeliveryStatusHistory_ChangedBy FOREIGN KEY (ChangedBy) REFERENCES Users(Id)
    );
    PRINT 'Created DeliveryStatusHistory table';
END
GO

-- Master Delivery Statuses
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MasterDeliveryStatuses')
BEGIN
    CREATE TABLE MasterDeliveryStatuses (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Code NVARCHAR(50) NOT NULL UNIQUE,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        StatusGroup NVARCHAR(50) NOT NULL, -- PENDING, IN_PROGRESS, COMPLETED, CANCELLED
        DisplayColor NVARCHAR(7) NULL, -- Hex color
        IconClass NVARCHAR(50) NULL,
        SortOrder INT NOT NULL DEFAULT 0,
        AllowedNextStatuses NVARCHAR(500) NULL, -- Comma-separated list
        IsTerminal BIT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1
    );
    PRINT 'Created MasterDeliveryStatuses table';
END
GO

-- Seed delivery statuses
IF NOT EXISTS (SELECT 1 FROM MasterDeliveryStatuses)
BEGIN
    INSERT INTO MasterDeliveryStatuses (Code, Name, StatusGroup, SortOrder, AllowedNextStatuses, IsTerminal) VALUES
    ('PENDING', 'Pending', 'PENDING', 1, 'MATCHING,CANCELLED', 0),
    ('MATCHING', 'Finding Delivery Partner', 'PENDING', 2, 'ASSIGNED,CANCELLED,PENDING', 0),
    ('ASSIGNED', 'Delivery Partner Assigned', 'IN_PROGRESS', 3, 'PICKUP_STARTED,CANCELLED', 0),
    ('PICKUP_STARTED', 'On Way to Pickup', 'IN_PROGRESS', 4, 'PICKED_UP,CANCELLED', 0),
    ('PICKED_UP', 'Package Picked Up', 'IN_PROGRESS', 5, 'IN_TRANSIT,CANCELLED', 0),
    ('IN_TRANSIT', 'In Transit', 'IN_PROGRESS', 6, 'OUT_FOR_DELIVERY,CANCELLED', 0),
    ('OUT_FOR_DELIVERY', 'Out for Delivery', 'IN_PROGRESS', 7, 'DELIVERED,FAILED_DELIVERY,CANCELLED', 0),
    ('DELIVERED', 'Delivered', 'COMPLETED', 8, '', 1),
    ('FAILED_DELIVERY', 'Delivery Failed', 'COMPLETED', 9, 'PENDING,CANCELLED', 0),
    ('CANCELLED', 'Cancelled', 'CANCELLED', 10, '', 1),
    ('RETURNED', 'Returned to Sender', 'COMPLETED', 11, '', 1);
    PRINT 'Seeded MasterDeliveryStatuses data';
END
GO

PRINT 'Phase 2 - Delivery Tables: COMPLETE';
