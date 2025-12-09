-- =====================================================
-- PHASE 2: INDEXES FOR NEW TABLES
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- BusinessConsumers indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BusinessConsumers_UserId' AND object_id = OBJECT_ID('BusinessConsumers'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BusinessConsumers_UserId ON BusinessConsumers(UserId);
    PRINT 'Created IX_BusinessConsumers_UserId';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BusinessConsumers_IsActive' AND object_id = OBJECT_ID('BusinessConsumers'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BusinessConsumers_IsActive ON BusinessConsumers(IsActive) WHERE IsActive = 1;
    PRINT 'Created IX_BusinessConsumers_IsActive';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BusinessConsumers_GSTIN' AND object_id = OBJECT_ID('BusinessConsumers'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BusinessConsumers_GSTIN ON BusinessConsumers(GSTIN) WHERE GSTIN IS NOT NULL;
    PRINT 'Created IX_BusinessConsumers_GSTIN';
END
GO

-- BCAddresses indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BCAddresses_BCId' AND object_id = OBJECT_ID('BCAddresses'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BCAddresses_BCId ON BCAddresses(BCId);
    PRINT 'Created IX_BCAddresses_BCId';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BCAddresses_Pincode' AND object_id = OBJECT_ID('BCAddresses'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BCAddresses_Pincode ON BCAddresses(Pincode) WHERE IsActive = 1;
    PRINT 'Created IX_BCAddresses_Pincode';
END
GO

-- Spatial index for BC addresses
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BCAddresses_GeoLocation' AND object_id = OBJECT_ID('BCAddresses'))
BEGIN
    CREATE SPATIAL INDEX IX_BCAddresses_GeoLocation ON BCAddresses(GeoLocation);
    PRINT 'Created IX_BCAddresses_GeoLocation (Spatial)';
END
GO

-- BCSubscriptionHistory indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BCSubscriptionHistory_BCId' AND object_id = OBJECT_ID('BCSubscriptionHistory'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BCSubscriptionHistory_BCId ON BCSubscriptionHistory(BCId, Status);
    PRINT 'Created IX_BCSubscriptionHistory_BCId';
END
GO

-- DeliveryAddresses indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DeliveryAddresses_DeliveryId' AND object_id = OBJECT_ID('DeliveryAddresses'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DeliveryAddresses_DeliveryId ON DeliveryAddresses(DeliveryId);
    PRINT 'Created IX_DeliveryAddresses_DeliveryId';
END
GO

-- Spatial index for delivery addresses
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DeliveryAddresses_GeoLocation' AND object_id = OBJECT_ID('DeliveryAddresses'))
BEGIN
    CREATE SPATIAL INDEX IX_DeliveryAddresses_GeoLocation ON DeliveryAddresses(GeoLocation);
    PRINT 'Created IX_DeliveryAddresses_GeoLocation (Spatial)';
END
GO

-- DeliveryPackages indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DeliveryPackages_DeliveryId' AND object_id = OBJECT_ID('DeliveryPackages'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DeliveryPackages_DeliveryId ON DeliveryPackages(DeliveryId);
    PRINT 'Created IX_DeliveryPackages_DeliveryId';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DeliveryPackages_PackageTypeId' AND object_id = OBJECT_ID('DeliveryPackages'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DeliveryPackages_PackageTypeId ON DeliveryPackages(PackageTypeId) WHERE PackageTypeId IS NOT NULL;
    PRINT 'Created IX_DeliveryPackages_PackageTypeId';
END
GO

-- DeliveryRoutes indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DeliveryRoutes_DeliveryId' AND object_id = OBJECT_ID('DeliveryRoutes'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DeliveryRoutes_DeliveryId ON DeliveryRoutes(DeliveryId);
    PRINT 'Created IX_DeliveryRoutes_DeliveryId';
END
GO

-- Spatial index for routes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DeliveryRoutes_RouteGeography' AND object_id = OBJECT_ID('DeliveryRoutes'))
BEGIN
    CREATE SPATIAL INDEX IX_DeliveryRoutes_RouteGeography ON DeliveryRoutes(RouteGeography);
    PRINT 'Created IX_DeliveryRoutes_RouteGeography (Spatial)';
END
GO

-- DeliveryStatusHistory indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DeliveryStatusHistory_DeliveryId' AND object_id = OBJECT_ID('DeliveryStatusHistory'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DeliveryStatusHistory_DeliveryId ON DeliveryStatusHistory(DeliveryId, ChangedAt DESC);
    PRINT 'Created IX_DeliveryStatusHistory_DeliveryId';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DeliveryStatusHistory_Status' AND object_id = OBJECT_ID('DeliveryStatusHistory'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DeliveryStatusHistory_Status ON DeliveryStatusHistory(NewStatus, ChangedAt DESC);
    PRINT 'Created IX_DeliveryStatusHistory_Status';
END
GO

PRINT 'Phase 2 - Indexes: COMPLETE';
