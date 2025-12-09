-- =====================================================
-- PHASE 2: BACKWARD COMPATIBILITY VIEWS
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- =====================================================
-- v_BusinessConsumerProfiles - Reconstructs Old Table Shape
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'v_BusinessConsumerProfiles')
    DROP VIEW v_BusinessConsumerProfiles;
GO

CREATE VIEW v_BusinessConsumerProfiles AS
SELECT
    bc.Id,
    bc.UserId,
    bc.BusinessName,
    bc.ContactPersonName,
    bc.GSTIN,
    bc.PAN,
    bc.BusinessCategory,
    -- Reconstruct BusinessAddress from BCAddresses
    (
        SELECT TOP 1 CONCAT_WS(', ', bca.AddressLine1, bca.AddressLine2, bca.City, bca.State, bca.Pincode)
        FROM BCAddresses bca
        WHERE bca.BCId = bc.Id AND bca.IsPrimary = 1 AND bca.IsActive = 1
    ) AS BusinessAddress,
    NULL AS BankAccountEncrypted, -- Reference BankVerifications instead
    bsh.SubscriptionPlanId,
    bc.IsActive,
    bc.ActivatedAt,
    bc.CreatedAt,
    bc.UpdatedAt,
    bc.BusinessConstitution,
    bc.GSTRegistrationType,
    -- Reconstruct PickupLocationsJson from BCPickupLocations
    (
        SELECT pl.LocationName, pl.AddressLine1, pl.City, pl.Pincode, pl.Latitude, pl.Longitude
        FROM BCPickupLocations pl
        WHERE pl.BusinessConsumerProfileId = bc.Id AND pl.IsActive = 1
        FOR JSON PATH
    ) AS PickupLocationsJson,
    bsh.StartDate AS SubscriptionStartDate
FROM BusinessConsumers bc
LEFT JOIN (
    SELECT BCId, SubscriptionPlanId, StartDate,
           ROW_NUMBER() OVER (PARTITION BY BCId ORDER BY StartDate DESC) AS rn
    FROM BCSubscriptionHistory
    WHERE Status = 'ACTIVE'
) bsh ON bsh.BCId = bc.Id AND bsh.rn = 1;
GO

PRINT 'Created v_BusinessConsumerProfiles view';

-- =====================================================
-- v_DeliveriesExtended - Delivery with addresses, packages, routes
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'v_DeliveriesExtended')
    DROP VIEW v_DeliveriesExtended;
GO

CREATE VIEW v_DeliveriesExtended AS
SELECT
    d.Id,
    d.RequesterId,
    d.RequesterType,
    d.AssignedDPId,
    d.AssignedAt,
    d.Status,
    d.Priority,
    d.ScheduledAt,
    d.SpecialInstructions,
    d.PreferredDPId,
    d.MatchingAttempts,
    d.EstimatedPrice,
    d.FinalPrice,
    d.CreatedAt,
    d.UpdatedAt,
    d.CancelledAt,
    d.CancellationReason,

    -- Pickup address from DeliveryAddresses
    pickup.Latitude AS PickupLat,
    pickup.Longitude AS PickupLng,
    pickup.AddressLine AS PickupAddress,
    pickup.AddressName AS PickupAddressName,
    pickup.ContactName AS PickupContactName,
    pickup.ContactPhone AS PickupContactPhone,
    pickup.AlternatePhone AS PickupAlternatePhone,
    pickup.WhatsAppNumber AS PickupWhatsAppNumber,
    pickup.ContactEmail AS PickupContactEmail,
    pickup.Instructions AS PickupInstructions,
    pickup.SavedAddressId AS PickupSavedAddressId,

    -- Drop address from DeliveryAddresses
    dropoff.Latitude AS DropLat,
    dropoff.Longitude AS DropLng,
    dropoff.AddressLine AS DropAddress,
    dropoff.AddressName AS DropAddressName,
    dropoff.ContactName AS DropContactName,
    dropoff.ContactPhone AS DropContactPhone,
    dropoff.AlternatePhone AS DropAlternatePhone,
    dropoff.WhatsAppNumber AS DropWhatsAppNumber,
    dropoff.ContactEmail AS DropContactEmail,
    dropoff.Instructions AS DropInstructions,
    dropoff.SavedAddressId AS DropSavedAddressId,

    -- Package from DeliveryPackages (first/primary package)
    pkg.WeightKg,
    pkg.PackageType,
    CONCAT(pkg.LengthCm, 'x', pkg.WidthCm, 'x', pkg.HeightCm, ' cm') AS PackageDimensions,
    pkg.DeclaredValue AS PackageValue,
    pkg.Description AS PackageDescription,
    pkg.CautionType,
    pkg.CautionNotes,
    pkg.RequiresSpecialHandling,
    pkg.IsHazardous,

    -- Route from DeliveryRoutes
    route.DistanceKm,
    route.DistanceSource,
    route.EstimatedDurationMinutes,
    route.RoutePolyline

FROM Deliveries d
LEFT JOIN DeliveryAddresses pickup ON pickup.DeliveryId = d.Id AND pickup.AddressType = 'PICKUP'
LEFT JOIN DeliveryAddresses dropoff ON dropoff.DeliveryId = d.Id AND dropoff.AddressType = 'DROP'
LEFT JOIN DeliveryPackages pkg ON pkg.DeliveryId = d.Id AND pkg.PackageNumber = 1
LEFT JOIN DeliveryRoutes route ON route.DeliveryId = d.Id;
GO

PRINT 'Created v_DeliveriesExtended view';

-- =====================================================
-- v_ActiveDeliveries - Current active deliveries
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'v_ActiveDeliveries')
    DROP VIEW v_ActiveDeliveries;
GO

CREATE VIEW v_ActiveDeliveries AS
SELECT
    d.Id,
    d.RequesterId,
    d.RequesterType,
    d.AssignedDPId,
    d.AssignedAt,
    d.Status,
    d.Priority,
    d.ScheduledAt,
    d.EstimatedPrice,
    d.CreatedAt,

    -- Requester info
    u.FullName AS RequesterName,
    u.Phone AS RequesterPhone,

    -- DP info
    dpu.FullName AS DPName,
    dpu.Phone AS DPPhone,

    -- Addresses
    pickup.AddressLine AS PickupAddress,
    pickup.ContactPhone AS PickupPhone,
    dropoff.AddressLine AS DropAddress,
    dropoff.ContactPhone AS DropPhone,

    -- Route
    route.DistanceKm,
    route.EstimatedDurationMinutes

FROM Deliveries d
INNER JOIN Users u ON u.Id = d.RequesterId
LEFT JOIN Users dpu ON dpu.Id = d.AssignedDPId
LEFT JOIN DeliveryAddresses pickup ON pickup.DeliveryId = d.Id AND pickup.AddressType = 'PICKUP'
LEFT JOIN DeliveryAddresses dropoff ON dropoff.DeliveryId = d.Id AND dropoff.AddressType = 'DROP'
LEFT JOIN DeliveryRoutes route ON route.DeliveryId = d.Id

WHERE d.Status NOT IN ('DELIVERED', 'CANCELLED', 'RETURNED');
GO

PRINT 'Created v_ActiveDeliveries view';

-- =====================================================
-- v_DeliveryTimeline - Full status timeline for a delivery
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'v_DeliveryTimeline')
    DROP VIEW v_DeliveryTimeline;
GO

CREATE VIEW v_DeliveryTimeline AS
SELECT
    dsh.Id,
    dsh.DeliveryId,
    dsh.PreviousStatus,
    dsh.NewStatus,
    mds.Name AS StatusName,
    mds.StatusGroup,
    mds.DisplayColor,
    dsh.ChangedBy,
    u.FullName AS ChangedByName,
    dsh.ChangedByRole,
    dsh.ChangeReason,
    dsh.Latitude,
    dsh.Longitude,
    dsh.ChangedAt,
    dsh.MetadataJson
FROM DeliveryStatusHistory dsh
LEFT JOIN MasterDeliveryStatuses mds ON mds.Code = dsh.NewStatus
LEFT JOIN Users u ON u.Id = dsh.ChangedBy;
GO

PRINT 'Created v_DeliveryTimeline view';

PRINT 'Phase 2 - Views: COMPLETE';
