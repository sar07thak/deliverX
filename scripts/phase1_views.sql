-- =====================================================
-- PHASE 1: BACKWARD COMPATIBILITY VIEWS
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- =====================================================
-- v_DeliveryPartnerProfiles - Reconstructs Old Table Shape
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'v_DeliveryPartnerProfiles')
    DROP VIEW v_DeliveryPartnerProfiles;
GO

CREATE VIEW v_DeliveryPartnerProfiles AS
SELECT
    dp.Id,
    dp.UserId,
    dp.DPCMId,
    dp.FullName,
    dp.ProfilePhotoUrl,
    dp.DOB,
    dp.Gender,
    CONCAT_WS(', ', dp.AddressLine1, dp.City, dp.State, dp.Pincode) AS Address,
    mvt.Code AS VehicleType,
    (
        SELECT ml.Name AS [value]
        FROM DPLanguages dl
        INNER JOIN MasterLanguages ml ON ml.Id = dl.LanguageId
        WHERE dl.DPId = dp.Id
        FOR JSON PATH
    ) AS Languages,
    'FULL_TIME' AS Availability,
    dsa.CenterLat AS ServiceAreaCenterLat,
    dsa.CenterLng AS ServiceAreaCenterLng,
    dsa.RadiusKm AS ServiceAreaRadiusKm,
    dsa.PreferredDirection,
    dsa.OneDirectionOnly,
    dsa.DirectionAngleDegrees,
    dsa.MaxDistanceKm,
    (
        SELECT dsap.Pincode AS [value]
        FROM DPServiceAreaPincodes dsap
        WHERE dsap.ServiceAreaId = dsa.Id AND dsap.IsActive = 1
        FOR JSON PATH
    ) AS ServiceAreaPincodesJson,
    dsa.PolygonWKT AS ServiceAreaPolygonJson,
    dpc.PerKmRate,
    dpc.PerKgRate,
    dpc.MinCharge,
    dos.IsOnline,
    dos.LastOnlineAt,
    dos.CurrentActiveDeliveries,
    dos.MaxConcurrentDeliveries,
    dpc.MaxDistanceKm AS MaxBidRate,
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

PRINT 'Created v_DeliveryPartnerProfiles view';

-- =====================================================
-- v_OnlineDeliveryPartners - For Matching Queries
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'v_OnlineDeliveryPartners')
    DROP VIEW v_OnlineDeliveryPartners;
GO

CREATE VIEW v_OnlineDeliveryPartners AS
SELECT
    dp.Id AS DPId,
    dp.UserId,
    dp.FullName,
    dp.VehicleTypeId,
    mvt.Code AS VehicleType,
    dos.IsOnline,
    dos.CurrentActiveDeliveries,
    dos.MaxConcurrentDeliveries,
    (dos.MaxConcurrentDeliveries - dos.CurrentActiveDeliveries) AS AvailableSlots,
    dos.CurrentLat,
    dos.CurrentLng,
    dos.LocationUpdatedAt,
    dsa.Id AS ServiceAreaId,
    dsa.AreaType,
    dsa.CenterLat,
    dsa.CenterLng,
    dsa.RadiusKm,
    dsa.PreferredDirection,
    dsa.OneDirectionOnly,
    dsa.AllowDropOutsideArea,
    dsa.PolygonGeography,
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

PRINT 'Created v_OnlineDeliveryPartners view';

-- =====================================================
-- v_DPCMWithRegions - DPCM managers with service regions
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'v_DPCMWithRegions')
    DROP VIEW v_DPCMWithRegions;
GO

CREATE VIEW v_DPCMWithRegions AS
SELECT
    dpcm.Id,
    dpcm.UserId,
    dpcm.OrganizationName,
    dpcm.ContactPersonName,
    dpcm.IsActive,
    (
        SELECT dsr.RegionType, dsr.StateName, dsr.DistrictName, dsr.Pincode, dsr.CustomRegionName
        FROM DPCMServiceRegions dsr
        WHERE dsr.DPCMId = dpcm.Id AND dsr.IsActive = 1
        FOR JSON PATH
    ) AS ServiceRegions,
    (
        SELECT SUM(dsd.Amount)
        FROM DPCMSecurityDeposits dsd
        WHERE dsd.DPCMId = dpcm.Id AND dsd.Status = 'RECEIVED'
    ) AS TotalSecurityDeposit,
    (
        SELECT COUNT(*)
        FROM DeliveryPartners dp
        WHERE dp.DPCMId = dpcm.Id AND dp.IsActive = 1
    ) AS ActiveDPCount,
    dpcm.CreatedAt,
    dpcm.UpdatedAt
FROM DPCManagers dpcm;
GO

PRINT 'Created v_DPCMWithRegions view';

PRINT 'Phase 1 - Views: COMPLETE';
