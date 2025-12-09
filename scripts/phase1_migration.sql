-- =====================================================
-- PHASE 1: DATA MIGRATION SCRIPTS (Idempotent)
-- Migrates data from DeliveryPartnerProfiles to new tables
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

BEGIN TRANSACTION;
BEGIN TRY

    PRINT 'Starting data migration...';

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
        CASE WHEN dpp.Address IS NOT NULL THEN LEFT(dpp.Address, 255) ELSE NULL END,
        NULL,
        NULL,
        NULL,
        (SELECT TOP 1 Id FROM MasterVehicleTypes WHERE Code = UPPER(REPLACE(ISNULL(dpp.VehicleType, ''), ' ', '_'))),
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
        1,
        1,
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

    -- Step 6: Migrate DPCM ServiceRegions JSON
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

    PRINT 'Migrated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' DPCM service regions';

    -- Step 7: Migrate DPCM Security Deposits
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
    PRINT 'Data migration completed successfully!';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Migration failed: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
GO
