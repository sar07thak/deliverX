-- =====================================================
-- PHASE 1: INDEXES FOR NEW TABLES
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- DeliveryPartners indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DeliveryPartners_UserId' AND object_id = OBJECT_ID('DeliveryPartners'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DeliveryPartners_UserId ON DeliveryPartners(UserId);
    PRINT 'Created IX_DeliveryPartners_UserId';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DeliveryPartners_DPCMId' AND object_id = OBJECT_ID('DeliveryPartners'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DeliveryPartners_DPCMId ON DeliveryPartners(DPCMId) WHERE DPCMId IS NOT NULL;
    PRINT 'Created IX_DeliveryPartners_DPCMId';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DeliveryPartners_IsActive' AND object_id = OBJECT_ID('DeliveryPartners'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DeliveryPartners_IsActive ON DeliveryPartners(IsActive) INCLUDE (UserId, FullName);
    PRINT 'Created IX_DeliveryPartners_IsActive';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DeliveryPartners_VehicleTypeId' AND object_id = OBJECT_ID('DeliveryPartners'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DeliveryPartners_VehicleTypeId ON DeliveryPartners(VehicleTypeId) WHERE VehicleTypeId IS NOT NULL;
    PRINT 'Created IX_DeliveryPartners_VehicleTypeId';
END
GO

-- DPOperationalStatus indexes (critical for matching)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DPOperationalStatus_IsOnline' AND object_id = OBJECT_ID('DPOperationalStatus'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DPOperationalStatus_IsOnline ON DPOperationalStatus(IsOnline) WHERE IsOnline = 1;
    PRINT 'Created IX_DPOperationalStatus_IsOnline';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DPOperationalStatus_Location' AND object_id = OBJECT_ID('DPOperationalStatus'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DPOperationalStatus_Location ON DPOperationalStatus(CurrentLat, CurrentLng) WHERE IsOnline = 1 AND CurrentLat IS NOT NULL;
    PRINT 'Created IX_DPOperationalStatus_Location';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DPOperationalStatus_Availability' AND object_id = OBJECT_ID('DPOperationalStatus'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DPOperationalStatus_Availability ON DPOperationalStatus(IsOnline, CurrentActiveDeliveries, MaxConcurrentDeliveries) WHERE IsOnline = 1;
    PRINT 'Created IX_DPOperationalStatus_Availability';
END
GO

-- DPServiceAreas indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DPServiceAreas_DPId' AND object_id = OBJECT_ID('DPServiceAreas'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DPServiceAreas_DPId ON DPServiceAreas(DPId);
    PRINT 'Created IX_DPServiceAreas_DPId';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DPServiceAreas_Active' AND object_id = OBJECT_ID('DPServiceAreas'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DPServiceAreas_Active ON DPServiceAreas(DPId, IsActive) WHERE IsActive = 1;
    PRINT 'Created IX_DPServiceAreas_Active';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DPServiceAreas_Location' AND object_id = OBJECT_ID('DPServiceAreas'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DPServiceAreas_Location ON DPServiceAreas(CenterLat, CenterLng, RadiusKm) WHERE IsActive = 1 AND AreaType = 'RADIUS';
    PRINT 'Created IX_DPServiceAreas_Location';
END
GO

-- Spatial index for polygon-based areas (cannot have WHERE clause)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DPServiceAreas_Polygon' AND object_id = OBJECT_ID('DPServiceAreas'))
BEGIN
    CREATE SPATIAL INDEX IX_DPServiceAreas_Polygon ON DPServiceAreas(PolygonGeography);
    PRINT 'Created IX_DPServiceAreas_Polygon (Spatial)';
END
GO

-- DPServiceAreaPincodes indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DPServiceAreaPincodes_Pincode' AND object_id = OBJECT_ID('DPServiceAreaPincodes'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DPServiceAreaPincodes_Pincode ON DPServiceAreaPincodes(Pincode) WHERE IsActive = 1;
    PRINT 'Created IX_DPServiceAreaPincodes_Pincode';
END
GO

-- DPLanguages indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DPLanguages_LanguageId' AND object_id = OBJECT_ID('DPLanguages'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DPLanguages_LanguageId ON DPLanguages(LanguageId);
    PRINT 'Created IX_DPLanguages_LanguageId';
END
GO

-- DPAvailabilitySchedules indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DPAvailabilitySchedules_DPId' AND object_id = OBJECT_ID('DPAvailabilitySchedules'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DPAvailabilitySchedules_DPId ON DPAvailabilitySchedules(DPId, DayOfWeek) WHERE IsActive = 1;
    PRINT 'Created IX_DPAvailabilitySchedules_DPId';
END
GO

-- DPCMServiceRegions indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DPCMServiceRegions_DPCMId' AND object_id = OBJECT_ID('DPCMServiceRegions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DPCMServiceRegions_DPCMId ON DPCMServiceRegions(DPCMId) WHERE IsActive = 1;
    PRINT 'Created IX_DPCMServiceRegions_DPCMId';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DPCMServiceRegions_Pincode' AND object_id = OBJECT_ID('DPCMServiceRegions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DPCMServiceRegions_Pincode ON DPCMServiceRegions(Pincode) WHERE Pincode IS NOT NULL AND IsActive = 1;
    PRINT 'Created IX_DPCMServiceRegions_Pincode';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DPCMServiceRegions_State' AND object_id = OBJECT_ID('DPCMServiceRegions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DPCMServiceRegions_State ON DPCMServiceRegions(StateName) WHERE StateName IS NOT NULL AND IsActive = 1;
    PRINT 'Created IX_DPCMServiceRegions_State';
END
GO

PRINT 'Phase 1 - Indexes: COMPLETE';
