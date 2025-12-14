-- =====================================================
-- SCHEMA FIX SCRIPT
-- Fixes column name mismatches between SP scripts and EF schema
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- =====================================================
-- FIX: ufn_DeliveryPartner_GetCandidates
-- Ratings: TargetId instead of RatedUserId, Score instead of Rating
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'ufn_DeliveryPartner_GetCandidates' AND type = 'IF')
    DROP FUNCTION ufn_DeliveryPartner_GetCandidates;
GO

CREATE FUNCTION ufn_DeliveryPartner_GetCandidates (
    @DeliveryId UNIQUEIDENTIFIER
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        dp.Id AS DeliveryPartnerId,
        dp.UserId,
        dp.FullName,
        dp.VehicleType,
        dp.PerKmRate,
        dp.PerKgRate,
        dp.MinCharge,
        dp.CurrentActiveDeliveries,
        dp.MaxConcurrentDeliveries,
        dbo.ufn_CalculateDistance(
            dp.ServiceAreaCenterLat,
            dp.ServiceAreaCenterLng,
            d.PickupLat,
            d.PickupLng
        ) AS DistanceToPickup,
        dbo.ufn_CalculateDeliveryPrice(
            d.DistanceKm,
            d.WeightKg,
            dp.PerKmRate,
            dp.PerKgRate,
            dp.MinCharge
        ) AS EstimatedPrice,
        COALESCE(
            (SELECT AVG(CAST(r.Score AS DECIMAL(3,2)))
             FROM Ratings r
             WHERE r.TargetId = dp.UserId AND r.TargetType = 'DP'),
            0
        ) AS AverageRating,
        u.Phone,
        u.Email
    FROM Deliveries d
    CROSS JOIN DeliveryPartnerProfiles dp
    INNER JOIN Users u ON u.Id = dp.UserId
    WHERE d.Id = @DeliveryId
      AND dp.IsActive = 1
      AND dp.IsOnline = 1
      AND dp.CurrentActiveDeliveries < dp.MaxConcurrentDeliveries
      AND dp.ServiceAreaCenterLat IS NOT NULL
      AND (
          dbo.ufn_CalculateDistance(
              dp.ServiceAreaCenterLat,
              dp.ServiceAreaCenterLng,
              d.PickupLat,
              d.PickupLng
          ) <= ISNULL(dp.ServiceAreaRadiusKm, 50)
          AND d.DistanceKm <= ISNULL(dp.MaxDistanceKm, 100)
      )
      AND (d.AssignedDPId IS NULL OR dp.UserId <> d.AssignedDPId)
);
GO

PRINT 'Fixed ufn_DeliveryPartner_GetCandidates';

-- =====================================================
-- FIX: usp_User_Deactivate
-- AdminAuditLogs: UserId instead of AdminId, NewValue instead of NewValues
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_User_Deactivate')
    DROP PROCEDURE usp_User_Deactivate;
GO

CREATE PROCEDURE usp_User_Deactivate
    @UserId UNIQUEIDENTIFIER,
    @DeactivatedBy UNIQUEIDENTIFIER = NULL,
    @Reason NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE Users
        SET IsActive = 0, UpdatedAt = GETUTCDATE()
        WHERE Id = @UserId;

        INSERT INTO AdminAuditLogs (
            Id, UserId, Action, EntityType, EntityId,
            NewValue, IpAddress, CreatedAt
        )
        VALUES (
            NEWID(), @DeactivatedBy, 'USER_DEACTIVATED', 'User', CAST(@UserId AS NVARCHAR(50)),
            '{"Reason": "' + ISNULL(@Reason, 'No reason provided') + '"}', NULL, GETUTCDATE()
        );

        COMMIT;

        SELECT 1 AS Success, 'User deactivated successfully' AS Message;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        EXEC usp_LogError @ErrorContext = 'usp_User_Deactivate', @UserId = @UserId;
        THROW;
    END CATCH
END
GO

PRINT 'Fixed usp_User_Deactivate';

-- =====================================================
-- FIX: usp_User_Activate
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_User_Activate')
    DROP PROCEDURE usp_User_Activate;
GO

CREATE PROCEDURE usp_User_Activate
    @UserId UNIQUEIDENTIFIER,
    @ActivatedBy UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE Users
        SET IsActive = 1, UpdatedAt = GETUTCDATE()
        WHERE Id = @UserId;

        INSERT INTO AdminAuditLogs (
            Id, UserId, Action, EntityType, EntityId,
            IpAddress, CreatedAt
        )
        VALUES (
            NEWID(), @ActivatedBy, 'USER_ACTIVATED', 'User', CAST(@UserId AS NVARCHAR(50)),
            NULL, GETUTCDATE()
        );

        COMMIT;

        SELECT 1 AS Success, 'User activated successfully' AS Message;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        EXEC usp_LogError @ErrorContext = 'usp_User_Activate', @UserId = @UserId;
        THROW;
    END CATCH
END
GO

PRINT 'Fixed usp_User_Activate';

-- =====================================================
-- FIX: usp_User_UpdateLoginStatus
-- AuthAuditLogs: EventType instead of Action, Details instead of IsSuccess/FailureReason
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_User_UpdateLoginStatus')
    DROP PROCEDURE usp_User_UpdateLoginStatus;
GO

CREATE PROCEDURE usp_User_UpdateLoginStatus
    @UserId UNIQUEIDENTIFIER,
    @IsSuccess BIT,
    @IpAddress NVARCHAR(50) = NULL,
    @UserAgent NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @IsSuccess = 1
        BEGIN
            UPDATE Users
            SET
                LastLoginAt = GETUTCDATE(),
                FailedLoginAttempts = 0,
                LockedUntil = NULL,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @UserId;

            INSERT INTO AuthAuditLogs (Id, UserId, EventType, IpAddress, UserAgent, Details, CreatedAt)
            VALUES (NEWID(), @UserId, 'LOGIN_SUCCESS', @IpAddress, @UserAgent, '{"success": true}', GETUTCDATE());
        END
        ELSE
        BEGIN
            DECLARE @FailedAttempts INT;
            DECLARE @MaxAttempts INT = 5;
            DECLARE @LockoutMinutes INT = 30;

            SELECT @FailedAttempts = FailedLoginAttempts + 1
            FROM Users
            WHERE Id = @UserId;

            UPDATE Users
            SET
                FailedLoginAttempts = @FailedAttempts,
                LockedUntil = CASE
                    WHEN @FailedAttempts >= @MaxAttempts
                    THEN DATEADD(MINUTE, @LockoutMinutes, GETUTCDATE())
                    ELSE LockedUntil
                END,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @UserId;

            INSERT INTO AuthAuditLogs (Id, UserId, EventType, IpAddress, UserAgent, Details, CreatedAt)
            VALUES (NEWID(), @UserId, 'LOGIN_FAILED', @IpAddress, @UserAgent, '{"reason": "Invalid credentials"}', GETUTCDATE());
        END

        COMMIT;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        EXEC usp_LogError @ErrorContext = 'usp_User_UpdateLoginStatus', @UserId = @UserId;
        THROW;
    END CATCH
END
GO

PRINT 'Fixed usp_User_UpdateLoginStatus';

-- =====================================================
-- FIX: usp_DeliveryPartner_GetById
-- Ratings: TargetId, Score
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DeliveryPartner_GetById')
    DROP PROCEDURE usp_DeliveryPartner_GetById;
GO

CREATE PROCEDURE usp_DeliveryPartner_GetById
    @ProfileId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        dp.Id,
        dp.UserId,
        dp.DPCMId,
        dp.FullName,
        dp.ProfilePhotoUrl,
        dp.DOB,
        dp.Gender,
        dp.Address,
        dp.VehicleType,
        dp.Languages,
        dp.Availability,
        dp.ServiceAreaCenterLat,
        dp.ServiceAreaCenterLng,
        dp.ServiceAreaRadiusKm,
        dp.ServiceAreaPincodesJson,
        dp.PreferredDirection,
        dp.OneDirectionOnly,
        dp.PerKmRate,
        dp.PerKgRate,
        dp.MinCharge,
        dp.MaxDistanceKm,
        dp.MaxBidRate,
        dp.IsActive,
        dp.ActivatedAt,
        dp.IsOnline,
        dp.LastOnlineAt,
        dp.CurrentActiveDeliveries,
        dp.MaxConcurrentDeliveries,
        dp.CreatedAt,
        dp.UpdatedAt,
        u.Phone,
        u.Email,
        u.IsPhoneVerified,
        u.IsEmailVerified,
        dpcm.OrganizationName AS DPCMOrganization,
        dpcm.ContactPersonName AS DPCMContactPerson,
        w.Balance AS WalletBalance,
        (SELECT AVG(CAST(r.Score AS DECIMAL(3,2)))
         FROM Ratings r WHERE r.TargetId = dp.UserId AND r.TargetType = 'DP') AS AverageRating,
        (SELECT COUNT(*) FROM Deliveries d
         WHERE d.AssignedDPId = dp.UserId AND d.Status = 'DELIVERED') AS TotalDeliveries
    FROM DeliveryPartnerProfiles dp
    INNER JOIN Users u ON u.Id = dp.UserId
    LEFT JOIN DPCManagers dpcm ON dpcm.Id = dp.DPCMId
    LEFT JOIN Wallets w ON w.UserId = dp.UserId AND w.IsActive = 1
    WHERE dp.Id = @ProfileId;
END
GO

PRINT 'Fixed usp_DeliveryPartner_GetById';

-- =====================================================
-- FIX: usp_DeliveryPartner_GetByUserId
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DeliveryPartner_GetByUserId')
    DROP PROCEDURE usp_DeliveryPartner_GetByUserId;
GO

CREATE PROCEDURE usp_DeliveryPartner_GetByUserId
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProfileId UNIQUEIDENTIFIER;
    SELECT @ProfileId = Id FROM DeliveryPartnerProfiles WHERE UserId = @UserId;

    IF @ProfileId IS NOT NULL
        EXEC usp_DeliveryPartner_GetById @ProfileId;
    ELSE
        SELECT NULL AS Id;
END
GO

PRINT 'Fixed usp_DeliveryPartner_GetByUserId';

-- =====================================================
-- FIX: usp_DeliveryPartner_List
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DeliveryPartner_List')
    DROP PROCEDURE usp_DeliveryPartner_List;
GO

CREATE PROCEDURE usp_DeliveryPartner_List
    @PageNumber INT = 1,
    @PageSize INT = 20,
    @DPCMId UNIQUEIDENTIFIER = NULL,
    @IsActive BIT = NULL,
    @IsOnline BIT = NULL,
    @VehicleType NVARCHAR(50) = NULL,
    @SearchTerm NVARCHAR(100) = NULL,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    SELECT @TotalCount = COUNT(*)
    FROM DeliveryPartnerProfiles dp
    INNER JOIN Users u ON u.Id = dp.UserId
    WHERE (@DPCMId IS NULL OR dp.DPCMId = @DPCMId)
      AND (@IsActive IS NULL OR dp.IsActive = @IsActive)
      AND (@IsOnline IS NULL OR dp.IsOnline = @IsOnline)
      AND (@VehicleType IS NULL OR dp.VehicleType = @VehicleType)
      AND (@SearchTerm IS NULL OR
           dp.FullName LIKE '%' + @SearchTerm + '%' OR
           u.Phone LIKE '%' + @SearchTerm + '%');

    SELECT
        dp.Id,
        dp.UserId,
        dp.DPCMId,
        dp.FullName,
        dp.ProfilePhotoUrl,
        dp.VehicleType,
        dp.IsActive,
        dp.ActivatedAt,
        dp.IsOnline,
        dp.LastOnlineAt,
        dp.CurrentActiveDeliveries,
        dp.MaxConcurrentDeliveries,
        dp.PerKmRate,
        dp.MinCharge,
        dp.CreatedAt,
        u.Phone,
        u.Email,
        (SELECT AVG(CAST(r.Score AS DECIMAL(3,2)))
         FROM Ratings r WHERE r.TargetId = dp.UserId AND r.TargetType = 'DP') AS AverageRating,
        (SELECT COUNT(*) FROM Deliveries d
         WHERE d.AssignedDPId = dp.UserId AND d.Status = 'DELIVERED') AS TotalDeliveries
    FROM DeliveryPartnerProfiles dp
    INNER JOIN Users u ON u.Id = dp.UserId
    WHERE (@DPCMId IS NULL OR dp.DPCMId = @DPCMId)
      AND (@IsActive IS NULL OR dp.IsActive = @IsActive)
      AND (@IsOnline IS NULL OR dp.IsOnline = @IsOnline)
      AND (@VehicleType IS NULL OR dp.VehicleType = @VehicleType)
      AND (@SearchTerm IS NULL OR
           dp.FullName LIKE '%' + @SearchTerm + '%' OR
           u.Phone LIKE '%' + @SearchTerm + '%')
    ORDER BY dp.CreatedAt DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    SELECT @TotalCount AS TotalCount;
END
GO

PRINT 'Fixed usp_DeliveryPartner_List';

-- =====================================================
-- FIX: usp_DeliveryPartner_GetAvailable
-- Remove NULLS LAST (not supported in SQL Server)
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DeliveryPartner_GetAvailable')
    DROP PROCEDURE usp_DeliveryPartner_GetAvailable;
GO

CREATE PROCEDURE usp_DeliveryPartner_GetAvailable
    @PickupLat DECIMAL(10,8),
    @PickupLng DECIMAL(11,8),
    @DropLat DECIMAL(10,8),
    @DropLng DECIMAL(11,8),
    @WeightKg DECIMAL(8,3),
    @RadiusKm DECIMAL(10,2) = 20,
    @MaxResults INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DeliveryDistance DECIMAL(10,3) = dbo.ufn_CalculateDistance(@PickupLat, @PickupLng, @DropLat, @DropLng);

    SELECT TOP (@MaxResults)
        dp.Id AS ProfileId,
        dp.UserId,
        dp.FullName,
        dp.VehicleType,
        dp.PerKmRate,
        dp.PerKgRate,
        dp.MinCharge,
        dp.CurrentActiveDeliveries,
        dp.MaxConcurrentDeliveries,
        u.Phone,
        dbo.ufn_CalculateDistance(dp.ServiceAreaCenterLat, dp.ServiceAreaCenterLng, @PickupLat, @PickupLng) AS DistanceToPickup,
        @DeliveryDistance AS DeliveryDistance,
        dbo.ufn_CalculateDeliveryPrice(@DeliveryDistance, @WeightKg, dp.PerKmRate, dp.PerKgRate, dp.MinCharge) AS EstimatedPrice,
        (SELECT AVG(CAST(r.Score AS DECIMAL(3,2)))
         FROM Ratings r WHERE r.TargetId = dp.UserId AND r.TargetType = 'DP') AS AverageRating,
        (SELECT COUNT(*) FROM Deliveries d
         WHERE d.AssignedDPId = dp.UserId AND d.Status = 'DELIVERED') AS TotalDeliveries
    FROM DeliveryPartnerProfiles dp
    INNER JOIN Users u ON u.Id = dp.UserId
    WHERE dp.IsActive = 1
      AND dp.IsOnline = 1
      AND dp.CurrentActiveDeliveries < dp.MaxConcurrentDeliveries
      AND dp.ServiceAreaCenterLat IS NOT NULL
      AND dbo.ufn_CalculateDistance(dp.ServiceAreaCenterLat, dp.ServiceAreaCenterLng, @PickupLat, @PickupLng) <= @RadiusKm
      AND (@DeliveryDistance <= ISNULL(dp.MaxDistanceKm, 100))
    ORDER BY
        dbo.ufn_CalculateDeliveryPrice(@DeliveryDistance, @WeightKg, dp.PerKmRate, dp.PerKgRate, dp.MinCharge) ASC,
        ISNULL((SELECT AVG(CAST(r.Score AS DECIMAL(3,2))) FROM Ratings r WHERE r.TargetId = dp.UserId AND r.TargetType = 'DP'), 0) DESC,
        dbo.ufn_CalculateDistance(dp.ServiceAreaCenterLat, dp.ServiceAreaCenterLng, @PickupLat, @PickupLng) ASC;
END
GO

PRINT 'Fixed usp_DeliveryPartner_GetAvailable';

-- =====================================================
-- FIX: usp_DeliveryPartner_Activate
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DeliveryPartner_Activate')
    DROP PROCEDURE usp_DeliveryPartner_Activate;
GO

CREATE PROCEDURE usp_DeliveryPartner_Activate
    @ProfileId UNIQUEIDENTIFIER,
    @ActivatedBy UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE DeliveryPartnerProfiles
        SET IsActive = 1, ActivatedAt = GETUTCDATE(), UpdatedAt = GETUTCDATE()
        WHERE Id = @ProfileId;

        DECLARE @UserId UNIQUEIDENTIFIER;
        SELECT @UserId = UserId FROM DeliveryPartnerProfiles WHERE Id = @ProfileId;

        IF NOT EXISTS (SELECT 1 FROM Wallets WHERE UserId = @UserId)
        BEGIN
            INSERT INTO Wallets (Id, UserId, WalletType, Balance, HoldBalance, Currency, IsActive, CreatedAt, UpdatedAt)
            VALUES (NEWID(), @UserId, 'DP', 0, 0, 'INR', 1, GETUTCDATE(), GETUTCDATE());
        END

        INSERT INTO AdminAuditLogs (Id, UserId, Action, EntityType, EntityId, CreatedAt)
        VALUES (NEWID(), @ActivatedBy, 'DP_ACTIVATED', 'DeliveryPartner', CAST(@ProfileId AS NVARCHAR(50)), GETUTCDATE());

        COMMIT;

        SELECT 1 AS Success, 'Delivery Partner activated successfully' AS Message;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        EXEC usp_LogError @ErrorContext = 'usp_DeliveryPartner_Activate';
        THROW;
    END CATCH
END
GO

PRINT 'Fixed usp_DeliveryPartner_Activate';

PRINT 'Schema fixes Part 1: COMPLETE';
