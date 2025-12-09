-- =====================================================
-- PHASE 2: DATA MIGRATION SCRIPTS (Idempotent)
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

BEGIN TRANSACTION;
BEGIN TRY

    PRINT 'Starting Phase 2 data migration...';

    -- =====================================================
    -- MIGRATE BusinessConsumerProfiles -> BusinessConsumers
    -- =====================================================

    INSERT INTO BusinessConsumers (
        Id, UserId, BusinessName, ContactPersonName, BusinessCategory,
        BusinessConstitution, GSTIN, GSTRegistrationType, PAN,
        IsActive, ActivatedAt, CreatedAt, UpdatedAt
    )
    SELECT
        bcp.Id,
        bcp.UserId,
        bcp.BusinessName,
        bcp.ContactPersonName,
        bcp.BusinessCategory,
        bcp.BusinessConstitution,
        bcp.GSTIN,
        bcp.GSTRegistrationType,
        bcp.PAN,
        bcp.IsActive,
        bcp.ActivatedAt,
        bcp.CreatedAt,
        bcp.UpdatedAt
    FROM BusinessConsumerProfiles bcp
    WHERE NOT EXISTS (SELECT 1 FROM BusinessConsumers bc WHERE bc.Id = bcp.Id);

    PRINT 'Migrated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records to BusinessConsumers';

    -- Migrate BusinessAddress JSON to BCAddresses
    INSERT INTO BCAddresses (
        Id, BCId, AddressType, AddressLine1, City, State, Pincode, IsPrimary, IsActive, CreatedAt
    )
    SELECT
        NEWID(),
        bcp.Id,
        'REGISTERED',
        CASE
            WHEN ISJSON(bcp.BusinessAddress) = 1 THEN JSON_VALUE(bcp.BusinessAddress, '$.addressLine1')
            ELSE LEFT(bcp.BusinessAddress, 255)
        END,
        CASE
            WHEN ISJSON(bcp.BusinessAddress) = 1 THEN ISNULL(JSON_VALUE(bcp.BusinessAddress, '$.city'), 'Unknown')
            ELSE 'Unknown'
        END,
        CASE
            WHEN ISJSON(bcp.BusinessAddress) = 1 THEN ISNULL(JSON_VALUE(bcp.BusinessAddress, '$.state'), 'Unknown')
            ELSE 'Unknown'
        END,
        CASE
            WHEN ISJSON(bcp.BusinessAddress) = 1 THEN ISNULL(JSON_VALUE(bcp.BusinessAddress, '$.pincode'), '000000')
            ELSE '000000'
        END,
        1, -- IsPrimary
        1, -- IsActive
        GETUTCDATE()
    FROM BusinessConsumerProfiles bcp
    WHERE bcp.BusinessAddress IS NOT NULL
      AND LEN(bcp.BusinessAddress) > 0
      AND NOT EXISTS (
          SELECT 1 FROM BCAddresses bca
          WHERE bca.BCId = bcp.Id AND bca.AddressType = 'REGISTERED'
      );

    PRINT 'Migrated BC addresses';

    -- Migrate subscription history
    INSERT INTO BCSubscriptionHistory (
        Id, BCId, SubscriptionPlanId, StartDate, Status, CreatedAt
    )
    SELECT
        NEWID(),
        bcp.Id,
        bcp.SubscriptionPlanId,
        ISNULL(bcp.SubscriptionStartDate, bcp.CreatedAt),
        'ACTIVE',
        GETUTCDATE()
    FROM BusinessConsumerProfiles bcp
    WHERE bcp.SubscriptionPlanId IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM BCSubscriptionHistory bsh
          WHERE bsh.BCId = bcp.Id AND bsh.SubscriptionPlanId = bcp.SubscriptionPlanId
      );

    PRINT 'Migrated BC subscription history';

    -- =====================================================
    -- MIGRATE Deliveries -> DeliveryAddresses
    -- =====================================================

    -- Migrate Pickup Addresses
    INSERT INTO DeliveryAddresses (
        Id, DeliveryId, AddressType,
        Latitude, Longitude, AddressName, AddressLine,
        ContactName, ContactPhone, AlternatePhone, WhatsAppNumber, ContactEmail,
        Instructions, SavedAddressId, CreatedAt
    )
    SELECT
        NEWID(),
        d.Id,
        'PICKUP',
        d.PickupLat,
        d.PickupLng,
        d.PickupAddressName,
        d.PickupAddress,
        d.PickupContactName,
        d.PickupContactPhone,
        d.PickupAlternatePhone,
        d.PickupWhatsAppNumber,
        d.PickupContactEmail,
        d.PickupInstructions,
        d.PickupSavedAddressId,
        d.CreatedAt
    FROM Deliveries d
    WHERE d.PickupLat IS NOT NULL
      AND d.PickupLng IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM DeliveryAddresses da
          WHERE da.DeliveryId = d.Id AND da.AddressType = 'PICKUP'
      );

    PRINT 'Migrated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' pickup addresses';

    -- Migrate Drop Addresses
    INSERT INTO DeliveryAddresses (
        Id, DeliveryId, AddressType,
        Latitude, Longitude, AddressName, AddressLine,
        ContactName, ContactPhone, AlternatePhone, WhatsAppNumber, ContactEmail,
        Instructions, SavedAddressId, CreatedAt
    )
    SELECT
        NEWID(),
        d.Id,
        'DROP',
        d.DropLat,
        d.DropLng,
        d.DropAddressName,
        d.DropAddress,
        d.DropContactName,
        d.DropContactPhone,
        d.DropAlternatePhone,
        d.DropWhatsAppNumber,
        d.DropContactEmail,
        d.DropInstructions,
        d.DropSavedAddressId,
        d.CreatedAt
    FROM Deliveries d
    WHERE d.DropLat IS NOT NULL
      AND d.DropLng IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM DeliveryAddresses da
          WHERE da.DeliveryId = d.Id AND da.AddressType = 'DROP'
      );

    PRINT 'Migrated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' drop addresses';

    -- =====================================================
    -- MIGRATE Deliveries -> DeliveryPackages
    -- =====================================================

    INSERT INTO DeliveryPackages (
        Id, DeliveryId, PackageNumber, PackageType, WeightKg, DeclaredValue,
        Description, CautionType, CautionNotes, RequiresSpecialHandling, IsHazardous,
        CreatedAt, UpdatedAt
    )
    SELECT
        NEWID(),
        d.Id,
        1, -- First package
        d.PackageType,
        ISNULL(d.WeightKg, 0),
        d.PackageValue,
        d.PackageDescription,
        d.CautionType,
        d.CautionNotes,
        ISNULL(d.RequiresSpecialHandling, 0),
        ISNULL(d.IsHazardous, 0),
        d.CreatedAt,
        d.UpdatedAt
    FROM Deliveries d
    WHERE NOT EXISTS (
        SELECT 1 FROM DeliveryPackages dp WHERE dp.DeliveryId = d.Id
    );

    PRINT 'Migrated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' packages';

    -- =====================================================
    -- MIGRATE Deliveries -> DeliveryRoutes
    -- =====================================================

    INSERT INTO DeliveryRoutes (
        Id, DeliveryId, DistanceKm, DistanceSource,
        EstimatedDurationMinutes, RoutePolyline, RouteCalculatedAt, CreatedAt, UpdatedAt
    )
    SELECT
        NEWID(),
        d.Id,
        ISNULL(d.DistanceKm, 0),
        d.DistanceSource,
        d.EstimatedDurationMinutes,
        d.RoutePolyline,
        d.CreatedAt,
        d.CreatedAt,
        d.UpdatedAt
    FROM Deliveries d
    WHERE d.DistanceKm IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM DeliveryRoutes dr WHERE dr.DeliveryId = d.Id
      );

    PRINT 'Migrated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' routes';

    COMMIT TRANSACTION;
    PRINT 'Phase 2 data migration completed successfully!';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Phase 2 migration failed: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
GO
