-- =====================================================
-- DELIVERYDOST STORED PROCEDURES - FIXES
-- Corrects column name mismatches with actual schema
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- =====================================================
-- FIX: ufn_DeliveryPartner_GetCandidates
-- Ratings table: Score, TargetId, TargetType (not Rating, RatedUserId, RatingType)
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
-- AdminAuditLogs: UserId (not AdminId), OldValue/NewValue (not OldValues/NewValues)
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
-- AuthAuditLogs: EventType (not Action), Details (not IsSuccess/FailureReason)
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

        DECLARE @Phone NVARCHAR(20);
        DECLARE @Email NVARCHAR(255);

        SELECT @Phone = Phone, @Email = Email FROM Users WHERE Id = @UserId;

        IF @IsSuccess = 1
        BEGIN
            UPDATE Users
            SET
                LastLoginAt = GETUTCDATE(),
                FailedLoginAttempts = 0,
                LockedUntil = NULL,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @UserId;

            INSERT INTO AuthAuditLogs (Id, UserId, EventType, Phone, Email, IpAddress, UserAgent, Details, CreatedAt)
            VALUES (NEWID(), @UserId, 'LOGIN_SUCCESS', @Phone, @Email, @IpAddress, @UserAgent, '{"success": true}', GETUTCDATE());
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

            INSERT INTO AuthAuditLogs (Id, UserId, EventType, Phone, Email, IpAddress, UserAgent, Details, CreatedAt)
            VALUES (NEWID(), @UserId, 'LOGIN_FAILED', @Phone, @Email, @IpAddress, @UserAgent,
                    '{"success": false, "reason": "Invalid credentials", "attempts": ' + CAST(@FailedAttempts AS NVARCHAR) + '}',
                    GETUTCDATE());
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
-- Ratings: Score, TargetId
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DeliveryPartner_GetById')
    DROP PROCEDURE usp_DeliveryPartner_GetById;
GO

CREATE PROCEDURE usp_DeliveryPartner_GetById
    @DeliveryPartnerId UNIQUEIDENTIFIER
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
        dp.PerKmRate,
        dp.PerKgRate,
        dp.MinCharge,
        dp.MaxDistanceKm,
        dp.MaxConcurrentDeliveries,
        dp.CurrentActiveDeliveries,
        dp.IsActive,
        dp.IsOnline,
        dp.LastOnlineAt,
        dp.ActivatedAt,
        dp.CreatedAt,
        dp.UpdatedAt,
        u.Phone,
        u.Email,
        u.IsPhoneVerified,
        u.IsEmailVerified,
        (SELECT COUNT(*) FROM Deliveries WHERE AssignedDPId = dp.UserId AND Status = 'DELIVERED') AS CompletedDeliveries,
        (SELECT AVG(CAST(r.Score AS DECIMAL(3,2))) FROM Ratings r WHERE r.TargetId = dp.UserId AND r.TargetType = 'DP') AS AverageRating,
        (SELECT COUNT(*) FROM Ratings WHERE TargetId = dp.UserId AND TargetType = 'DP') AS TotalRatings,
        dpcm.OrganizationName AS DPCMName
    FROM DeliveryPartnerProfiles dp
    INNER JOIN Users u ON u.Id = dp.UserId
    LEFT JOIN DPCManagers dpcm ON dpcm.Id = dp.DPCMId
    WHERE dp.Id = @DeliveryPartnerId;
END
GO

PRINT 'Fixed usp_DeliveryPartner_GetById';

-- =====================================================
-- FIX: usp_DeliveryPartner_List
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DeliveryPartner_List')
    DROP PROCEDURE usp_DeliveryPartner_List;
GO

CREATE PROCEDURE usp_DeliveryPartner_List
    @PageNumber INT = 1,
    @PageSize INT = 20,
    @IsActive BIT = NULL,
    @IsOnline BIT = NULL,
    @VehicleType NVARCHAR(20) = NULL,
    @DPCMId UNIQUEIDENTIFIER = NULL,
    @SearchTerm NVARCHAR(100) = NULL,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    SELECT @TotalCount = COUNT(*)
    FROM DeliveryPartnerProfiles dp
    INNER JOIN Users u ON u.Id = dp.UserId
    WHERE (@IsActive IS NULL OR dp.IsActive = @IsActive)
      AND (@IsOnline IS NULL OR dp.IsOnline = @IsOnline)
      AND (@VehicleType IS NULL OR dp.VehicleType = @VehicleType)
      AND (@DPCMId IS NULL OR dp.DPCMId = @DPCMId)
      AND (@SearchTerm IS NULL OR
           dp.FullName LIKE '%' + @SearchTerm + '%' OR
           u.Phone LIKE '%' + @SearchTerm + '%' OR
           u.Email LIKE '%' + @SearchTerm + '%');

    SELECT
        dp.Id,
        dp.UserId,
        dp.DPCMId,
        dp.FullName,
        dp.VehicleType,
        dp.PerKmRate,
        dp.PerKgRate,
        dp.MinCharge,
        dp.IsActive,
        dp.IsOnline,
        dp.LastOnlineAt,
        dp.CurrentActiveDeliveries,
        dp.CreatedAt,
        u.Phone,
        u.Email,
        (SELECT COUNT(*) FROM Deliveries WHERE AssignedDPId = dp.UserId AND Status = 'DELIVERED') AS CompletedDeliveries,
        (SELECT AVG(CAST(r.Score AS DECIMAL(3,2))) FROM Ratings r WHERE r.TargetId = dp.UserId AND r.TargetType = 'DP') AS AverageRating,
        (SELECT COUNT(*) FROM Ratings WHERE TargetId = dp.UserId AND TargetType = 'DP') AS TotalRatings
    FROM DeliveryPartnerProfiles dp
    INNER JOIN Users u ON u.Id = dp.UserId
    WHERE (@IsActive IS NULL OR dp.IsActive = @IsActive)
      AND (@IsOnline IS NULL OR dp.IsOnline = @IsOnline)
      AND (@VehicleType IS NULL OR dp.VehicleType = @VehicleType)
      AND (@DPCMId IS NULL OR dp.DPCMId = @DPCMId)
      AND (@SearchTerm IS NULL OR
           dp.FullName LIKE '%' + @SearchTerm + '%' OR
           u.Phone LIKE '%' + @SearchTerm + '%' OR
           u.Email LIKE '%' + @SearchTerm + '%')
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
    @Latitude DECIMAL(10,8),
    @Longitude DECIMAL(11,8),
    @RadiusKm DECIMAL(10,3) = 10,
    @VehicleType NVARCHAR(20) = NULL,
    @MaxResults INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@MaxResults)
        dp.Id AS DeliveryPartnerId,
        dp.UserId,
        dp.FullName,
        dp.VehicleType,
        dp.PerKmRate,
        dp.PerKgRate,
        dp.MinCharge,
        dp.MaxDistanceKm,
        dp.CurrentActiveDeliveries,
        dp.MaxConcurrentDeliveries,
        dp.ServiceAreaCenterLat,
        dp.ServiceAreaCenterLng,
        dp.ServiceAreaRadiusKm,
        dbo.ufn_CalculateDistance(
            dp.ServiceAreaCenterLat,
            dp.ServiceAreaCenterLng,
            @Latitude,
            @Longitude
        ) AS DistanceKm,
        (SELECT AVG(CAST(r.Score AS DECIMAL(3,2))) FROM Ratings r WHERE r.TargetId = dp.UserId AND r.TargetType = 'DP') AS AverageRating,
        u.Phone,
        u.Email
    FROM DeliveryPartnerProfiles dp
    INNER JOIN Users u ON u.Id = dp.UserId
    WHERE dp.IsActive = 1
      AND dp.IsOnline = 1
      AND dp.CurrentActiveDeliveries < dp.MaxConcurrentDeliveries
      AND dp.ServiceAreaCenterLat IS NOT NULL
      AND (@VehicleType IS NULL OR dp.VehicleType = @VehicleType)
      AND dbo.ufn_CalculateDistance(
            dp.ServiceAreaCenterLat,
            dp.ServiceAreaCenterLng,
            @Latitude,
            @Longitude
          ) <= @RadiusKm
    ORDER BY
        dbo.ufn_CalculateDistance(dp.ServiceAreaCenterLat, dp.ServiceAreaCenterLng, @Latitude, @Longitude) ASC,
        dp.CurrentActiveDeliveries ASC;
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
    @DeliveryPartnerId UNIQUEIDENTIFIER,
    @ActivatedBy UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE DeliveryPartnerProfiles
        SET IsActive = 1, ActivatedAt = GETUTCDATE(), UpdatedAt = GETUTCDATE()
        WHERE Id = @DeliveryPartnerId;

        INSERT INTO AdminAuditLogs (
            Id, UserId, Action, EntityType, EntityId, IpAddress, CreatedAt
        )
        VALUES (
            NEWID(), @ActivatedBy, 'DP_ACTIVATED', 'DeliveryPartner', CAST(@DeliveryPartnerId AS NVARCHAR(50)),
            NULL, GETUTCDATE()
        );

        COMMIT;

        SELECT 1 AS Success, 'Delivery Partner activated' AS Message;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        EXEC usp_LogError @ErrorContext = 'usp_DeliveryPartner_Activate';
        THROW;
    END CATCH
END
GO

PRINT 'Fixed usp_DeliveryPartner_Activate';

-- =====================================================
-- FIX: usp_Delivery_Create
-- DeliveryEvents: Metadata (not EventData), Timestamp (not CreatedAt), ActorId (not CreatedBy)
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Delivery_Create')
    DROP PROCEDURE usp_Delivery_Create;
GO

CREATE PROCEDURE usp_Delivery_Create
    @RequesterId UNIQUEIDENTIFIER,
    @RequesterType NVARCHAR(10), -- EC, BC
    @PickupLat DECIMAL(10,8),
    @PickupLng DECIMAL(11,8),
    @PickupAddress NVARCHAR(500),
    @PickupContactName NVARCHAR(100),
    @PickupContactPhone NVARCHAR(20),
    @PickupInstructions NVARCHAR(500) = NULL,
    @DropLat DECIMAL(10,8),
    @DropLng DECIMAL(11,8),
    @DropAddress NVARCHAR(500),
    @DropContactName NVARCHAR(100),
    @DropContactPhone NVARCHAR(20),
    @DropInstructions NVARCHAR(500) = NULL,
    @WeightKg DECIMAL(8,3),
    @PackageType NVARCHAR(50),
    @PackageDescription NVARCHAR(500) = NULL,
    @PackageValue DECIMAL(18,2) = NULL,
    @Priority NVARCHAR(20) = 'NORMAL', -- EXPRESS, NORMAL, ECONOMY
    @ScheduledAt DATETIME2 = NULL,
    @PreferredDPId UNIQUEIDENTIFIER = NULL,
    @SpecialInstructions NVARCHAR(1000) = NULL,
    @NewDeliveryId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        SET @NewDeliveryId = NEWID();

        -- Calculate distance using Haversine
        DECLARE @DistanceKm DECIMAL(10,3) = dbo.ufn_CalculateDistance(@PickupLat, @PickupLng, @DropLat, @DropLng);
        DECLARE @EstimatedDuration INT = CAST(@DistanceKm * 3 AS INT); -- ~20 km/h average

        INSERT INTO Deliveries (
            Id, RequesterId, RequesterType,
            PickupLat, PickupLng, PickupAddress, PickupContactName, PickupContactPhone, PickupInstructions,
            DropLat, DropLng, DropAddress, DropContactName, DropContactPhone, DropInstructions,
            WeightKg, PackageType, PackageDescription, PackageValue,
            Priority, ScheduledAt, PreferredDPId, SpecialInstructions,
            DistanceKm, EstimatedDurationMinutes,
            Status, MatchingAttempts,
            CreatedAt, UpdatedAt
        )
        VALUES (
            @NewDeliveryId, @RequesterId, @RequesterType,
            @PickupLat, @PickupLng, @PickupAddress, @PickupContactName, @PickupContactPhone, @PickupInstructions,
            @DropLat, @DropLng, @DropAddress, @DropContactName, @DropContactPhone, @DropInstructions,
            @WeightKg, @PackageType, @PackageDescription, @PackageValue,
            @Priority, ISNULL(@ScheduledAt, GETUTCDATE()), @PreferredDPId, @SpecialInstructions,
            @DistanceKm, @EstimatedDuration,
            'PENDING', 0,
            GETUTCDATE(), GETUTCDATE()
        );

        -- Create initial event
        INSERT INTO DeliveryEvents (
            Id, DeliveryId, EventType, FromStatus, ToStatus,
            ActorId, ActorType, Metadata, Timestamp
        )
        VALUES (
            NEWID(), @NewDeliveryId, 'CREATED', NULL, 'PENDING',
            @RequesterId, @RequesterType, '{"source": "API"}', GETUTCDATE()
        );

        COMMIT;

        -- Return created delivery
        SELECT
            d.*,
            u.FullName AS RequesterName,
            u.Phone AS RequesterPhone
        FROM Deliveries d
        INNER JOIN Users u ON u.Id = d.RequesterId
        WHERE d.Id = @NewDeliveryId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        EXEC usp_LogError @ErrorContext = 'usp_Delivery_Create', @UserId = @RequesterId;
        THROW;
    END CATCH
END
GO

PRINT 'Fixed usp_Delivery_Create';

-- =====================================================
-- FIX: usp_Delivery_MatchCandidates
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Delivery_MatchCandidates')
    DROP PROCEDURE usp_Delivery_MatchCandidates;
GO

CREATE PROCEDURE usp_Delivery_MatchCandidates
    @DeliveryId UNIQUEIDENTIFIER,
    @MaxCandidates INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PickupLat DECIMAL(10,8);
    DECLARE @PickupLng DECIMAL(11,8);
    DECLARE @DistanceKm DECIMAL(10,3);
    DECLARE @WeightKg DECIMAL(8,3);
    DECLARE @PreferredDPId UNIQUEIDENTIFIER;

    SELECT
        @PickupLat = PickupLat,
        @PickupLng = PickupLng,
        @DistanceKm = DistanceKm,
        @WeightKg = WeightKg,
        @PreferredDPId = PreferredDPId
    FROM Deliveries
    WHERE Id = @DeliveryId;

    SELECT TOP (@MaxCandidates)
        dp.Id AS DeliveryPartnerId,
        dp.UserId,
        dp.FullName,
        dp.VehicleType,
        dp.PerKmRate,
        dp.PerKgRate,
        dp.MinCharge,
        dp.CurrentActiveDeliveries,
        dp.MaxConcurrentDeliveries,
        dbo.ufn_CalculateDistance(dp.ServiceAreaCenterLat, dp.ServiceAreaCenterLng, @PickupLat, @PickupLng) AS DistanceToPickup,
        dbo.ufn_CalculateDeliveryPrice(@DistanceKm, @WeightKg, dp.PerKmRate, dp.PerKgRate, dp.MinCharge) AS EstimatedPrice,
        COALESCE(
            (SELECT AVG(CAST(r.Score AS DECIMAL(3,2))) FROM Ratings r WHERE r.TargetId = dp.UserId AND r.TargetType = 'DP'),
            0
        ) AS AverageRating,
        CASE WHEN dp.UserId = @PreferredDPId THEN 1 ELSE 0 END AS IsPreferred,
        u.Phone,
        u.Email
    FROM DeliveryPartnerProfiles dp
    INNER JOIN Users u ON u.Id = dp.UserId
    WHERE dp.IsActive = 1
      AND dp.IsOnline = 1
      AND dp.CurrentActiveDeliveries < dp.MaxConcurrentDeliveries
      AND dp.ServiceAreaCenterLat IS NOT NULL
      AND dbo.ufn_CalculateDistance(dp.ServiceAreaCenterLat, dp.ServiceAreaCenterLng, @PickupLat, @PickupLng)
          <= ISNULL(dp.ServiceAreaRadiusKm, 50)
      AND @DistanceKm <= ISNULL(dp.MaxDistanceKm, 100)
    ORDER BY
        CASE WHEN dp.UserId = @PreferredDPId THEN 0 ELSE 1 END,
        dbo.ufn_CalculateDistance(dp.ServiceAreaCenterLat, dp.ServiceAreaCenterLng, @PickupLat, @PickupLng),
        dp.CurrentActiveDeliveries;
END
GO

PRINT 'Fixed usp_Delivery_MatchCandidates';

-- =====================================================
-- FIX: usp_Delivery_AssignPartner
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Delivery_AssignPartner')
    DROP PROCEDURE usp_Delivery_AssignPartner;
GO

CREATE PROCEDURE usp_Delivery_AssignPartner
    @DeliveryId UNIQUEIDENTIFIER,
    @DeliveryPartnerId UNIQUEIDENTIFIER,
    @EstimatedPrice DECIMAL(18,2) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CurrentStatus NVARCHAR(20);
        DECLARE @DPUserId UNIQUEIDENTIFIER;

        SELECT @CurrentStatus = Status FROM Deliveries WHERE Id = @DeliveryId;

        IF @CurrentStatus NOT IN ('PENDING', 'MATCHING')
        BEGIN
            RAISERROR('Delivery cannot be assigned in current status', 16, 1);
            RETURN;
        END

        SELECT @DPUserId = UserId FROM DeliveryPartnerProfiles WHERE Id = @DeliveryPartnerId;

        IF @DPUserId IS NULL
        BEGIN
            RAISERROR('Delivery Partner not found', 16, 1);
            RETURN;
        END

        -- Verify DP is available
        DECLARE @IsOnline BIT;
        DECLARE @CurrentDeliveries INT;
        DECLARE @MaxDeliveries INT;

        SELECT
            @IsOnline = IsOnline,
            @CurrentDeliveries = CurrentActiveDeliveries,
            @MaxDeliveries = MaxConcurrentDeliveries
        FROM DeliveryPartnerProfiles
        WHERE Id = @DeliveryPartnerId;

        IF @IsOnline = 0 OR @CurrentDeliveries >= @MaxDeliveries
        BEGIN
            RAISERROR('Delivery Partner is not available', 16, 1);
            RETURN;
        END

        -- Assign delivery
        UPDATE Deliveries
        SET
            AssignedDPId = @DPUserId,
            AssignedAt = GETUTCDATE(),
            Status = 'ASSIGNED',
            EstimatedPrice = ISNULL(@EstimatedPrice, EstimatedPrice),
            UpdatedAt = GETUTCDATE()
        WHERE Id = @DeliveryId;

        -- Increment DP active deliveries
        UPDATE DeliveryPartnerProfiles
        SET CurrentActiveDeliveries = CurrentActiveDeliveries + 1
        WHERE Id = @DeliveryPartnerId;

        -- Add event
        INSERT INTO DeliveryEvents (
            Id, DeliveryId, EventType, FromStatus, ToStatus,
            ActorId, ActorType, Metadata, Timestamp
        )
        VALUES (
            NEWID(), @DeliveryId, 'ASSIGNED', @CurrentStatus, 'ASSIGNED',
            @DPUserId, 'DP', '{"dpId": "' + CAST(@DeliveryPartnerId AS NVARCHAR(50)) + '"}', GETUTCDATE()
        );

        COMMIT;

        -- Return updated delivery
        SELECT d.*, dp.FullName AS DPName, u.Phone AS DPPhone
        FROM Deliveries d
        INNER JOIN DeliveryPartnerProfiles dp ON dp.UserId = d.AssignedDPId
        INNER JOIN Users u ON u.Id = dp.UserId
        WHERE d.Id = @DeliveryId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        EXEC usp_LogError @ErrorContext = 'usp_Delivery_AssignPartner';
        THROW;
    END CATCH
END
GO

PRINT 'Fixed usp_Delivery_AssignPartner';

-- =====================================================
-- FIX: usp_Delivery_UpdateStatus
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Delivery_UpdateStatus')
    DROP PROCEDURE usp_Delivery_UpdateStatus;
GO

CREATE PROCEDURE usp_Delivery_UpdateStatus
    @DeliveryId UNIQUEIDENTIFIER,
    @NewStatus NVARCHAR(20),
    @ActorId UNIQUEIDENTIFIER,
    @ActorType NVARCHAR(10),
    @Metadata NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CurrentStatus NVARCHAR(20);
        SELECT @CurrentStatus = Status FROM Deliveries WHERE Id = @DeliveryId;

        IF @CurrentStatus IS NULL
        BEGIN
            RAISERROR('Delivery not found', 16, 1);
            RETURN;
        END

        UPDATE Deliveries
        SET
            Status = @NewStatus,
            UpdatedAt = GETUTCDATE()
        WHERE Id = @DeliveryId;

        INSERT INTO DeliveryEvents (
            Id, DeliveryId, EventType, FromStatus, ToStatus,
            ActorId, ActorType, Metadata, Timestamp
        )
        VALUES (
            NEWID(), @DeliveryId, 'STATUS_CHANGED', @CurrentStatus, @NewStatus,
            @ActorId, @ActorType, @Metadata, GETUTCDATE()
        );

        COMMIT;

        SELECT * FROM Deliveries WHERE Id = @DeliveryId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        EXEC usp_LogError @ErrorContext = 'usp_Delivery_UpdateStatus';
        THROW;
    END CATCH
END
GO

PRINT 'Fixed usp_Delivery_UpdateStatus';

-- =====================================================
-- FIX: usp_Delivery_GetTimeline
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Delivery_GetTimeline')
    DROP PROCEDURE usp_Delivery_GetTimeline;
GO

CREATE PROCEDURE usp_Delivery_GetTimeline
    @DeliveryId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        e.Id,
        e.DeliveryId,
        e.EventType,
        e.FromStatus,
        e.ToStatus,
        e.ActorId,
        e.ActorType,
        e.Metadata,
        e.Timestamp,
        u.FullName AS ActorName
    FROM DeliveryEvents e
    LEFT JOIN Users u ON u.Id = e.ActorId
    WHERE e.DeliveryId = @DeliveryId
    ORDER BY e.Timestamp ASC;
END
GO

PRINT 'Fixed usp_Delivery_GetTimeline';

-- =====================================================
-- FIX: usp_DPCM_Activate
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DPCM_Activate')
    DROP PROCEDURE usp_DPCM_Activate;
GO

CREATE PROCEDURE usp_DPCM_Activate
    @DPCMId UNIQUEIDENTIFIER,
    @ActivatedBy UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM DPCManagers WHERE Id = @DPCMId)
        BEGIN
            RAISERROR('DPCM not found', 16, 1);
            RETURN;
        END

        DECLARE @DepositStatus NVARCHAR(20);
        DECLARE @DepositAmount DECIMAL(18,2);

        SELECT @DepositStatus = SecurityDepositStatus, @DepositAmount = SecurityDeposit
        FROM DPCManagers WHERE Id = @DPCMId;

        IF @DepositAmount > 0 AND @DepositStatus <> 'RECEIVED'
        BEGIN
            RAISERROR('Security deposit not received', 16, 1);
            RETURN;
        END

        UPDATE DPCManagers
        SET IsActive = 1, ActivatedAt = GETUTCDATE(), UpdatedAt = GETUTCDATE()
        WHERE Id = @DPCMId;

        INSERT INTO AdminAuditLogs (
            Id, UserId, Action, EntityType, EntityId, IpAddress, CreatedAt
        )
        VALUES (
            NEWID(), @ActivatedBy, 'DPCM_ACTIVATED', 'DPCManager', CAST(@DPCMId AS NVARCHAR(50)),
            NULL, GETUTCDATE()
        );

        COMMIT;

        SELECT 1 AS Success, 'DPCM activated successfully' AS Message;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        EXEC usp_LogError @ErrorContext = 'usp_DPCM_Activate';
        THROW;
    END CATCH
END
GO

PRINT 'Fixed usp_DPCM_Activate';

-- =====================================================
-- FIX: usp_DPCM_UpdateSecurityDeposit
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DPCM_UpdateSecurityDeposit')
    DROP PROCEDURE usp_DPCM_UpdateSecurityDeposit;
GO

CREATE PROCEDURE usp_DPCM_UpdateSecurityDeposit
    @DPCMId UNIQUEIDENTIFIER,
    @Status NVARCHAR(20),
    @TransactionRef NVARCHAR(100) = NULL,
    @UpdatedBy UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @OldStatus NVARCHAR(20);
        SELECT @OldStatus = SecurityDepositStatus FROM DPCManagers WHERE Id = @DPCMId;

        UPDATE DPCManagers
        SET
            SecurityDepositStatus = @Status,
            SecurityDepositTransactionRef = ISNULL(@TransactionRef, SecurityDepositTransactionRef),
            SecurityDepositReceivedAt = CASE WHEN @Status = 'RECEIVED' THEN GETUTCDATE() ELSE SecurityDepositReceivedAt END,
            UpdatedAt = GETUTCDATE()
        WHERE Id = @DPCMId;

        INSERT INTO AdminAuditLogs (
            Id, UserId, Action, EntityType, EntityId,
            OldValue, NewValue, IpAddress, CreatedAt
        )
        VALUES (
            NEWID(), @UpdatedBy, 'DPCM_SECURITY_DEPOSIT_UPDATED', 'DPCManager', CAST(@DPCMId AS NVARCHAR(50)),
            '{"OldStatus": "' + ISNULL(@OldStatus, 'NULL') + '"}',
            '{"NewStatus": "' + @Status + '", "TransactionRef": "' + ISNULL(@TransactionRef, '') + '"}',
            NULL, GETUTCDATE()
        );

        COMMIT;

        SELECT * FROM DPCManagers WHERE Id = @DPCMId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        EXEC usp_LogError @ErrorContext = 'usp_DPCM_UpdateSecurityDeposit';
        THROW;
    END CATCH
END
GO

PRINT 'Fixed usp_DPCM_UpdateSecurityDeposit';

-- =====================================================
-- FIX: usp_DPCM_GetDeliveryPartners
-- No TotalDeliveries, TotalEarnings, AverageRating, TotalRatings columns
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DPCM_GetDeliveryPartners')
    DROP PROCEDURE usp_DPCM_GetDeliveryPartners;
GO

CREATE PROCEDURE usp_DPCM_GetDeliveryPartners
    @DPCMId UNIQUEIDENTIFIER,
    @PageNumber INT = 1,
    @PageSize INT = 20,
    @IsActive BIT = NULL,
    @IsOnline BIT = NULL,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    SELECT @TotalCount = COUNT(*)
    FROM DeliveryPartnerProfiles dp
    WHERE dp.DPCMId = @DPCMId
      AND (@IsActive IS NULL OR dp.IsActive = @IsActive)
      AND (@IsOnline IS NULL OR dp.IsOnline = @IsOnline);

    SELECT
        dp.Id, dp.UserId, dp.FullName, dp.VehicleType,
        dp.IsActive, dp.IsOnline, dp.CurrentActiveDeliveries,
        (SELECT COUNT(*) FROM Deliveries WHERE AssignedDPId = dp.UserId AND Status = 'DELIVERED') AS CompletedDeliveries,
        (SELECT AVG(CAST(r.Score AS DECIMAL(3,2))) FROM Ratings r WHERE r.TargetId = dp.UserId AND r.TargetType = 'DP') AS AverageRating,
        (SELECT COUNT(*) FROM Ratings WHERE TargetId = dp.UserId AND TargetType = 'DP') AS TotalRatings,
        dp.PerKmRate, dp.PerKgRate, dp.MinCharge,
        dp.ServiceAreaCenterLat, dp.ServiceAreaCenterLng, dp.ServiceAreaRadiusKm,
        u.Phone, u.Email
    FROM DeliveryPartnerProfiles dp
    INNER JOIN Users u ON u.Id = dp.UserId
    WHERE dp.DPCMId = @DPCMId
      AND (@IsActive IS NULL OR dp.IsActive = @IsActive)
      AND (@IsOnline IS NULL OR dp.IsOnline = @IsOnline)
    ORDER BY dp.CreatedAt DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    SELECT @TotalCount AS TotalCount;
END
GO

PRINT 'Fixed usp_DPCM_GetDeliveryPartners';

-- =====================================================
-- FIX: usp_Complaint_Create
-- Deliveries has no TrackingCode column - use Id
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Complaint_Create')
    DROP PROCEDURE usp_Complaint_Create;
GO

CREATE PROCEDURE usp_Complaint_Create
    @DeliveryId UNIQUEIDENTIFIER,
    @RaisedById UNIQUEIDENTIFIER,
    @RaisedByType NVARCHAR(10),
    @AgainstId UNIQUEIDENTIFIER = NULL,
    @AgainstType NVARCHAR(10) = NULL,
    @Category NVARCHAR(50),
    @Severity NVARCHAR(20) = 'MEDIUM',
    @Subject NVARCHAR(255),
    @Description NVARCHAR(MAX),
    @NewComplaintId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM Deliveries WHERE Id = @DeliveryId)
        BEGIN
            RAISERROR('Delivery not found', 16, 1);
            RETURN;
        END

        IF EXISTS (
            SELECT 1 FROM Complaints
            WHERE DeliveryId = @DeliveryId
              AND RaisedById = @RaisedById
              AND Category = @Category
              AND Status NOT IN ('CLOSED', 'REJECTED')
        )
        BEGIN
            RAISERROR('An active complaint already exists for this delivery', 16, 1);
            RETURN;
        END

        SET @NewComplaintId = NEWID();
        DECLARE @ComplaintNumber NVARCHAR(20) = 'CMP-' + FORMAT(GETUTCDATE(), 'yyyyMMdd') + '-' + RIGHT('00000' + CAST(ABS(CHECKSUM(NEWID())) % 100000 AS NVARCHAR), 5);

        INSERT INTO Complaints (
            Id, ComplaintNumber, DeliveryId, RaisedById, RaisedByType,
            AgainstId, AgainstType, Category, Severity,
            Subject, Description, Status,
            CreatedAt, UpdatedAt
        )
        VALUES (
            @NewComplaintId, @ComplaintNumber, @DeliveryId, @RaisedById, @RaisedByType,
            @AgainstId, @AgainstType, @Category, @Severity,
            @Subject, @Description, 'OPEN',
            GETUTCDATE(), GETUTCDATE()
        );

        SELECT
            c.Id, c.ComplaintNumber, c.DeliveryId, c.RaisedById, c.RaisedByType,
            c.AgainstId, c.AgainstType, c.Category, c.Severity,
            c.Subject, c.Description, c.Status,
            c.CreatedAt,
            CAST(d.Id AS NVARCHAR(50)) AS DeliveryReference,
            rb.FullName AS RaisedByName, rb.Phone AS RaisedByPhone
        FROM Complaints c
        INNER JOIN Deliveries d ON d.Id = c.DeliveryId
        INNER JOIN Users rb ON rb.Id = c.RaisedById
        WHERE c.Id = @NewComplaintId;

    END TRY
    BEGIN CATCH
        EXEC usp_LogError @ErrorContext = 'usp_Complaint_Create', @UserId = @RaisedById;
        THROW;
    END CATCH
END
GO

PRINT 'Fixed usp_Complaint_Create';

-- =====================================================
-- FIX: usp_Complaint_GetById
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Complaint_GetById')
    DROP PROCEDURE usp_Complaint_GetById;
GO

CREATE PROCEDURE usp_Complaint_GetById
    @ComplaintId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.Id, c.ComplaintNumber, c.DeliveryId, c.RaisedById, c.RaisedByType,
        c.AgainstId, c.AgainstType, c.Category, c.Severity,
        c.Subject, c.Description, c.Status,
        c.Resolution, c.ResolutionNotes,
        c.AssignedToId, c.AssignedAt, c.ResolvedAt, c.ClosedAt,
        c.CreatedAt, c.UpdatedAt,
        CAST(d.Id AS NVARCHAR(50)) AS DeliveryReference,
        d.Status AS DeliveryStatus,
        rb.FullName AS RaisedByName, rb.Phone AS RaisedByPhone, rb.Email AS RaisedByEmail,
        ag.FullName AS AgainstName, ag.Phone AS AgainstPhone,
        ast.FullName AS AssignedToName
    FROM Complaints c
    INNER JOIN Deliveries d ON d.Id = c.DeliveryId
    INNER JOIN Users rb ON rb.Id = c.RaisedById
    LEFT JOIN Users ag ON ag.Id = c.AgainstId
    LEFT JOIN Users ast ON ast.Id = c.AssignedToId
    WHERE c.Id = @ComplaintId;

    SELECT
        e.Id, e.Type, e.FileName, e.FileUrl, e.Description,
        e.UploadedById, e.UploadedAt,
        u.FullName AS UploadedByName
    FROM ComplaintEvidences e
    INNER JOIN Users u ON u.Id = e.UploadedById
    WHERE e.ComplaintId = @ComplaintId
    ORDER BY e.UploadedAt DESC;

    SELECT
        cm.Id, cm.AuthorId, cm.Content, cm.IsInternal, cm.CreatedAt,
        u.FullName AS AuthorName, u.Role AS AuthorRole
    FROM ComplaintComments cm
    INNER JOIN Users u ON u.Id = cm.AuthorId
    WHERE cm.ComplaintId = @ComplaintId
    ORDER BY cm.CreatedAt ASC;
END
GO

PRINT 'Fixed usp_Complaint_GetById';

-- =====================================================
-- FIX: usp_Complaint_List
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Complaint_List')
    DROP PROCEDURE usp_Complaint_List;
GO

CREATE PROCEDURE usp_Complaint_List
    @PageNumber INT = 1,
    @PageSize INT = 20,
    @Status NVARCHAR(20) = NULL,
    @Category NVARCHAR(50) = NULL,
    @Severity NVARCHAR(20) = NULL,
    @RaisedById UNIQUEIDENTIFIER = NULL,
    @AgainstId UNIQUEIDENTIFIER = NULL,
    @AssignedToId UNIQUEIDENTIFIER = NULL,
    @SearchTerm NVARCHAR(100) = NULL,
    @FromDate DATETIME2 = NULL,
    @ToDate DATETIME2 = NULL,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    SELECT @TotalCount = COUNT(*)
    FROM Complaints c
    WHERE (@Status IS NULL OR c.Status = @Status)
      AND (@Category IS NULL OR c.Category = @Category)
      AND (@Severity IS NULL OR c.Severity = @Severity)
      AND (@RaisedById IS NULL OR c.RaisedById = @RaisedById)
      AND (@AgainstId IS NULL OR c.AgainstId = @AgainstId)
      AND (@AssignedToId IS NULL OR c.AssignedToId = @AssignedToId)
      AND (@SearchTerm IS NULL OR
           c.ComplaintNumber LIKE '%' + @SearchTerm + '%' OR
           c.Subject LIKE '%' + @SearchTerm + '%')
      AND (@FromDate IS NULL OR c.CreatedAt >= @FromDate)
      AND (@ToDate IS NULL OR c.CreatedAt <= @ToDate);

    SELECT
        c.Id, c.ComplaintNumber, c.DeliveryId, c.RaisedById, c.RaisedByType,
        c.AgainstId, c.AgainstType, c.Category, c.Severity,
        c.Subject, c.Status, c.AssignedToId,
        c.CreatedAt, c.UpdatedAt, c.ResolvedAt,
        rb.FullName AS RaisedByName,
        ag.FullName AS AgainstName,
        ast.FullName AS AssignedToName,
        CAST(d.Id AS NVARCHAR(50)) AS DeliveryReference,
        (SELECT COUNT(*) FROM ComplaintEvidences WHERE ComplaintId = c.Id) AS EvidenceCount,
        (SELECT COUNT(*) FROM ComplaintComments WHERE ComplaintId = c.Id) AS CommentCount
    FROM Complaints c
    INNER JOIN Users rb ON rb.Id = c.RaisedById
    INNER JOIN Deliveries d ON d.Id = c.DeliveryId
    LEFT JOIN Users ag ON ag.Id = c.AgainstId
    LEFT JOIN Users ast ON ast.Id = c.AssignedToId
    WHERE (@Status IS NULL OR c.Status = @Status)
      AND (@Category IS NULL OR c.Category = @Category)
      AND (@Severity IS NULL OR c.Severity = @Severity)
      AND (@RaisedById IS NULL OR c.RaisedById = @RaisedById)
      AND (@AgainstId IS NULL OR c.AgainstId = @AgainstId)
      AND (@AssignedToId IS NULL OR c.AssignedToId = @AssignedToId)
      AND (@SearchTerm IS NULL OR
           c.ComplaintNumber LIKE '%' + @SearchTerm + '%' OR
           c.Subject LIKE '%' + @SearchTerm + '%')
      AND (@FromDate IS NULL OR c.CreatedAt >= @FromDate)
      AND (@ToDate IS NULL OR c.CreatedAt <= @ToDate)
    ORDER BY
        CASE c.Severity WHEN 'CRITICAL' THEN 1 WHEN 'HIGH' THEN 2 WHEN 'MEDIUM' THEN 3 ELSE 4 END,
        c.CreatedAt DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    SELECT @TotalCount AS TotalCount;
END
GO

PRINT 'Fixed usp_Complaint_List';

-- =====================================================
-- Create stored procedures for code generation
-- (replacing functions that use NEXT VALUE FOR)
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_GenerateDeliveryCode')
    DROP PROCEDURE usp_GenerateDeliveryCode;
GO

CREATE PROCEDURE usp_GenerateDeliveryCode
    @DeliveryCode NVARCHAR(20) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @SeqNum INT = NEXT VALUE FOR seq_DeliveryCode;
    SET @DeliveryCode = 'DD-' + FORMAT(GETUTCDATE(), 'yyyyMMdd') + '-' + RIGHT('000000' + CAST(@SeqNum AS NVARCHAR), 6);
END
GO

PRINT 'Created usp_GenerateDeliveryCode';

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_GenerateComplaintNumber')
    DROP PROCEDURE usp_GenerateComplaintNumber;
GO

CREATE PROCEDURE usp_GenerateComplaintNumber
    @ComplaintNumber NVARCHAR(20) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @SeqNum INT = NEXT VALUE FOR seq_ComplaintNumber;
    SET @ComplaintNumber = 'CMP-' + FORMAT(GETUTCDATE(), 'yyyyMMdd') + '-' + RIGHT('00000' + CAST(@SeqNum AS NVARCHAR), 5);
END
GO

PRINT 'Created usp_GenerateComplaintNumber';

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_GeneratePaymentNumber')
    DROP PROCEDURE usp_GeneratePaymentNumber;
GO

CREATE PROCEDURE usp_GeneratePaymentNumber
    @PaymentNumber NVARCHAR(20) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @SeqNum INT = NEXT VALUE FOR seq_PaymentNumber;
    SET @PaymentNumber = 'PAY-' + FORMAT(GETUTCDATE(), 'yyyyMMdd') + '-' + RIGHT('00000' + CAST(@SeqNum AS NVARCHAR), 5);
END
GO

PRINT 'Created usp_GeneratePaymentNumber';

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_GenerateSettlementNumber')
    DROP PROCEDURE usp_GenerateSettlementNumber;
GO

CREATE PROCEDURE usp_GenerateSettlementNumber
    @SettlementNumber NVARCHAR(20) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @SeqNum INT = NEXT VALUE FOR seq_SettlementNumber;
    SET @SettlementNumber = 'STL-' + FORMAT(GETUTCDATE(), 'yyyyMMdd') + '-' + RIGHT('0000' + CAST(@SeqNum AS NVARCHAR), 4);
END
GO

PRINT 'Created usp_GenerateSettlementNumber';

PRINT 'All fixes applied successfully!';
