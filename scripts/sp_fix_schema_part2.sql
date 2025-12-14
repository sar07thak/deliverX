-- =====================================================
-- SCHEMA FIX SCRIPT - PART 2
-- Fixes: Delivery, DPCM, Complaint modules
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- =====================================================
-- FIX: usp_Delivery_Create
-- DeliveryEvents: Metadata instead of EventData, Timestamp instead of CreatedAt
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Delivery_Create')
    DROP PROCEDURE usp_Delivery_Create;
GO

CREATE PROCEDURE usp_Delivery_Create
    @RequesterId UNIQUEIDENTIFIER,
    @RequesterType NVARCHAR(20),
    @PickupLat DECIMAL(10,8),
    @PickupLng DECIMAL(11,8),
    @PickupAddress NVARCHAR(500),
    @PickupContactName NVARCHAR(255) = NULL,
    @PickupContactPhone NVARCHAR(15) = NULL,
    @PickupInstructions NVARCHAR(500) = NULL,
    @DropLat DECIMAL(10,8),
    @DropLng DECIMAL(11,8),
    @DropAddress NVARCHAR(500),
    @DropContactName NVARCHAR(255) = NULL,
    @DropContactPhone NVARCHAR(15) = NULL,
    @DropInstructions NVARCHAR(500) = NULL,
    @WeightKg DECIMAL(8,3),
    @PackageType NVARCHAR(50),
    @PackageDescription NVARCHAR(500) = NULL,
    @PackageValue DECIMAL(10,2) = NULL,
    @Priority NVARCHAR(20) = 'NORMAL',
    @ScheduledAt DATETIME2 = NULL,
    @SpecialInstructions NVARCHAR(1000) = NULL,
    @PreferredDPId UNIQUEIDENTIFIER = NULL,
    @CautionType NVARCHAR(50) = NULL,
    @CautionNotes NVARCHAR(500) = NULL,
    @NewDeliveryId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = @RequesterId AND IsActive = 1)
        BEGIN
            RAISERROR('Requester not found or not active', 16, 1);
            RETURN;
        END

        DECLARE @DistanceKm DECIMAL(10,3) = dbo.ufn_CalculateDistance(@PickupLat, @PickupLng, @DropLat, @DropLng);
        DECLARE @EstimatedDuration INT = CEILING((@DistanceKm / 30.0) * 60);

        SET @NewDeliveryId = NEWID();

        INSERT INTO Deliveries (
            Id, RequesterId, RequesterType,
            PickupLat, PickupLng, PickupAddress, PickupContactName, PickupContactPhone, PickupInstructions,
            DropLat, DropLng, DropAddress, DropContactName, DropContactPhone, DropInstructions,
            WeightKg, PackageType, PackageDescription, PackageValue,
            Priority, ScheduledAt, SpecialInstructions, PreferredDPId,
            CautionType, CautionNotes, IsHazardous, RequiresSpecialHandling,
            DistanceKm, DistanceSource, EstimatedDurationMinutes,
            Status, MatchingAttempts,
            CreatedAt, UpdatedAt
        )
        VALUES (
            @NewDeliveryId, @RequesterId, @RequesterType,
            @PickupLat, @PickupLng, @PickupAddress, @PickupContactName, @PickupContactPhone, @PickupInstructions,
            @DropLat, @DropLng, @DropAddress, @DropContactName, @DropContactPhone, @DropInstructions,
            @WeightKg, @PackageType, @PackageDescription, @PackageValue,
            @Priority, @ScheduledAt, @SpecialInstructions, @PreferredDPId,
            @CautionType, @CautionNotes,
            CASE WHEN @CautionType = 'HAZARDOUS' THEN 1 ELSE 0 END,
            CASE WHEN @CautionType IS NOT NULL AND @CautionType <> 'NONE' THEN 1 ELSE 0 END,
            @DistanceKm, 'HAVERSINE', @EstimatedDuration,
            'PENDING', 0,
            GETUTCDATE(), GETUTCDATE()
        );

        -- Record creation event using correct column names
        INSERT INTO DeliveryEvents (Id, DeliveryId, EventType, FromStatus, ToStatus, ActorId, ActorType, Metadata, Timestamp)
        VALUES (NEWID(), @NewDeliveryId, 'CREATED', NULL, 'PENDING', @RequesterId, @RequesterType,
                '{"RequesterType": "' + @RequesterType + '", "Priority": "' + @Priority + '"}',
                GETUTCDATE());

        COMMIT;

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
    @RadiusKm DECIMAL(10,2) = 20,
    @MaxCandidates INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PickupLat DECIMAL(10,8), @PickupLng DECIMAL(11,8);
    DECLARE @DropLat DECIMAL(10,8), @DropLng DECIMAL(11,8);
    DECLARE @WeightKg DECIMAL(8,3), @DistanceKm DECIMAL(10,3);
    DECLARE @PreferredDPId UNIQUEIDENTIFIER;

    SELECT
        @PickupLat = PickupLat,
        @PickupLng = PickupLng,
        @DropLat = DropLat,
        @DropLng = DropLng,
        @WeightKg = WeightKg,
        @DistanceKm = DistanceKm,
        @PreferredDPId = PreferredDPId
    FROM Deliveries
    WHERE Id = @DeliveryId;

    IF @PickupLat IS NULL
    BEGIN
        RAISERROR('Delivery not found', 16, 1);
        RETURN;
    END

    SELECT TOP (@MaxCandidates)
        dp.Id AS ProfileId,
        dp.UserId,
        dp.FullName,
        dp.VehicleType,
        dp.ProfilePhotoUrl,
        dp.PerKmRate,
        dp.PerKgRate,
        dp.MinCharge,
        dp.CurrentActiveDeliveries,
        dp.MaxConcurrentDeliveries,
        u.Phone,
        dbo.ufn_CalculateDistance(dp.ServiceAreaCenterLat, dp.ServiceAreaCenterLng, @PickupLat, @PickupLng) AS DistanceToPickup,
        dbo.ufn_CalculateDeliveryPrice(@DistanceKm, @WeightKg, dp.PerKmRate, dp.PerKgRate, dp.MinCharge) AS EstimatedPrice,
        (SELECT AVG(CAST(r.Score AS DECIMAL(3,2)))
         FROM Ratings r WHERE r.TargetId = dp.UserId AND r.TargetType = 'DP') AS AverageRating,
        (SELECT COUNT(*) FROM Deliveries d2
         WHERE d2.AssignedDPId = dp.UserId AND d2.Status = 'DELIVERED') AS TotalDeliveries,
        CASE WHEN dp.UserId = @PreferredDPId THEN 1 ELSE 0 END AS IsPreferred,
        ROW_NUMBER() OVER (ORDER BY
            CASE WHEN dp.UserId = @PreferredDPId THEN 0 ELSE 1 END,
            dbo.ufn_CalculateDeliveryPrice(@DistanceKm, @WeightKg, dp.PerKmRate, dp.PerKgRate, dp.MinCharge),
            dbo.ufn_CalculateDistance(dp.ServiceAreaCenterLat, dp.ServiceAreaCenterLng, @PickupLat, @PickupLng)
        ) AS [Rank]
    FROM DeliveryPartnerProfiles dp
    INNER JOIN Users u ON u.Id = dp.UserId
    WHERE dp.IsActive = 1
      AND dp.IsOnline = 1
      AND dp.CurrentActiveDeliveries < dp.MaxConcurrentDeliveries
      AND dp.ServiceAreaCenterLat IS NOT NULL
      AND dbo.ufn_CalculateDistance(dp.ServiceAreaCenterLat, dp.ServiceAreaCenterLng, @PickupLat, @PickupLng) <= @RadiusKm
      AND (@DistanceKm <= ISNULL(dp.MaxDistanceKm, 100))
    ORDER BY
        CASE WHEN dp.UserId = @PreferredDPId THEN 0 ELSE 1 END,
        dbo.ufn_CalculateDeliveryPrice(@DistanceKm, @WeightKg, dp.PerKmRate, dp.PerKgRate, dp.MinCharge),
        ISNULL((SELECT AVG(CAST(r.Score AS DECIMAL(3,2))) FROM Ratings r WHERE r.TargetId = dp.UserId AND r.TargetType = 'DP'), 0) DESC;

    UPDATE Deliveries
    SET MatchingAttempts = MatchingAttempts + 1, UpdatedAt = GETUTCDATE()
    WHERE Id = @DeliveryId;
END
GO

PRINT 'Fixed usp_Delivery_MatchCandidates';

-- =====================================================
-- FIX: usp_Delivery_AssignPartner
-- DeliveryEvents, DeliveryMatchingHistories columns
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Delivery_AssignPartner')
    DROP PROCEDURE usp_Delivery_AssignPartner;
GO

CREATE PROCEDURE usp_Delivery_AssignPartner
    @DeliveryId UNIQUEIDENTIFIER,
    @DPUserId UNIQUEIDENTIFIER,
    @EstimatedPrice DECIMAL(10,2) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CurrentStatus NVARCHAR(50), @CurrentDPId UNIQUEIDENTIFIER;
        SELECT @CurrentStatus = Status, @CurrentDPId = AssignedDPId
        FROM Deliveries
        WHERE Id = @DeliveryId;

        IF @CurrentStatus IS NULL
        BEGIN
            RAISERROR('Delivery not found', 16, 1);
            RETURN;
        END

        IF @CurrentStatus NOT IN ('PENDING', 'MATCHING')
        BEGIN
            RAISERROR('Delivery cannot be assigned in current status', 16, 1);
            RETURN;
        END

        IF @CurrentDPId IS NOT NULL
        BEGIN
            RAISERROR('Delivery already has an assigned partner', 16, 1);
            RETURN;
        END

        DECLARE @DPActiveDeliveries INT, @DPMaxDeliveries INT, @DPIsOnline BIT, @DPIsActive BIT;
        SELECT
            @DPActiveDeliveries = CurrentActiveDeliveries,
            @DPMaxDeliveries = MaxConcurrentDeliveries,
            @DPIsOnline = IsOnline,
            @DPIsActive = IsActive
        FROM DeliveryPartnerProfiles
        WHERE UserId = @DPUserId;

        IF @DPIsActive <> 1 OR @DPIsOnline <> 1
        BEGIN
            RAISERROR('Delivery Partner is not available', 16, 1);
            RETURN;
        END

        IF @DPActiveDeliveries >= @DPMaxDeliveries
        BEGIN
            RAISERROR('Delivery Partner has reached maximum concurrent deliveries', 16, 1);
            RETURN;
        END

        UPDATE Deliveries
        SET
            AssignedDPId = @DPUserId,
            AssignedAt = GETUTCDATE(),
            Status = 'ASSIGNED',
            EstimatedPrice = ISNULL(@EstimatedPrice, EstimatedPrice),
            UpdatedAt = GETUTCDATE()
        WHERE Id = @DeliveryId;

        EXEC usp_DeliveryPartner_IncrementActiveDeliveries @DPUserId, 1;

        -- Record event with correct columns
        INSERT INTO DeliveryEvents (Id, DeliveryId, EventType, FromStatus, ToStatus, ActorId, ActorType, Metadata, Timestamp)
        VALUES (NEWID(), @DeliveryId, 'ASSIGNED', @CurrentStatus, 'ASSIGNED', @DPUserId, 'DP',
                '{"AssignedDPId": "' + CAST(@DPUserId AS NVARCHAR(50)) + '"}',
                GETUTCDATE());

        -- Record in DeliveryMatchingHistories with correct columns
        INSERT INTO DeliveryMatchingHistories (Id, DeliveryId, DPId, MatchingAttempt, NotifiedAt, ResponseType, RespondedAt)
        VALUES (NEWID(), @DeliveryId, @DPUserId, 1, GETUTCDATE(), 'ACCEPTED', GETUTCDATE());

        COMMIT;

        EXEC usp_Delivery_GetById @DeliveryId;

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
    @NewStatus NVARCHAR(50),
    @UpdatedBy UNIQUEIDENTIFIER,
    @Notes NVARCHAR(500) = NULL,
    @Latitude DECIMAL(10,8) = NULL,
    @Longitude DECIMAL(11,8) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CurrentStatus NVARCHAR(50), @AssignedDPId UNIQUEIDENTIFIER;
        SELECT @CurrentStatus = Status, @AssignedDPId = AssignedDPId
        FROM Deliveries
        WHERE Id = @DeliveryId;

        IF @CurrentStatus IS NULL
        BEGIN
            RAISERROR('Delivery not found', 16, 1);
            RETURN;
        END

        DECLARE @ValidTransition BIT = 0;
        SELECT @ValidTransition = CASE
            WHEN @CurrentStatus = 'PENDING' AND @NewStatus IN ('MATCHING', 'ASSIGNED', 'CANCELLED') THEN 1
            WHEN @CurrentStatus = 'MATCHING' AND @NewStatus IN ('ASSIGNED', 'PENDING', 'CANCELLED') THEN 1
            WHEN @CurrentStatus = 'ASSIGNED' AND @NewStatus IN ('ACCEPTED', 'REJECTED', 'CANCELLED') THEN 1
            WHEN @CurrentStatus = 'ACCEPTED' AND @NewStatus IN ('PICKUP_STARTED', 'CANCELLED') THEN 1
            WHEN @CurrentStatus = 'PICKUP_STARTED' AND @NewStatus IN ('PICKED_UP', 'CANCELLED') THEN 1
            WHEN @CurrentStatus = 'PICKED_UP' AND @NewStatus IN ('IN_TRANSIT', 'CANCELLED') THEN 1
            WHEN @CurrentStatus = 'IN_TRANSIT' AND @NewStatus IN ('OUT_FOR_DELIVERY', 'CANCELLED') THEN 1
            WHEN @CurrentStatus = 'OUT_FOR_DELIVERY' AND @NewStatus IN ('DELIVERED', 'FAILED_DELIVERY', 'CANCELLED') THEN 1
            WHEN @CurrentStatus = 'FAILED_DELIVERY' AND @NewStatus IN ('PENDING', 'RETURNED', 'CANCELLED') THEN 1
            ELSE 0
        END;

        IF @ValidTransition = 0
        BEGIN
            RAISERROR('Invalid status transition from %s to %s', 16, 1, @CurrentStatus, @NewStatus);
            RETURN;
        END

        UPDATE Deliveries
        SET
            Status = @NewStatus,
            UpdatedAt = GETUTCDATE(),
            CancelledAt = CASE WHEN @NewStatus = 'CANCELLED' THEN GETUTCDATE() ELSE CancelledAt END,
            CancellationReason = CASE WHEN @NewStatus = 'CANCELLED' THEN @Notes ELSE CancellationReason END
        WHERE Id = @DeliveryId;

        -- Record event with correct columns (no Latitude/Longitude in this table)
        INSERT INTO DeliveryEvents (Id, DeliveryId, EventType, FromStatus, ToStatus, ActorId, ActorType, Metadata, Timestamp)
        VALUES (NEWID(), @DeliveryId, 'STATUS_CHANGE', @CurrentStatus, @NewStatus, @UpdatedBy, 'USER',
                '{"Notes": "' + ISNULL(@Notes, '') + '", "Lat": "' + ISNULL(CAST(@Latitude AS NVARCHAR), '') + '", "Lng": "' + ISNULL(CAST(@Longitude AS NVARCHAR), '') + '"}',
                GETUTCDATE());

        IF @NewStatus IN ('DELIVERED', 'CANCELLED', 'RETURNED') AND @AssignedDPId IS NOT NULL
        BEGIN
            EXEC usp_DeliveryPartner_IncrementActiveDeliveries @AssignedDPId, -1;
        END

        COMMIT;

        SELECT 1 AS Success, 'Status updated successfully' AS Message, @NewStatus AS NewStatus;

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
-- FIX: usp_Delivery_Cancel
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Delivery_Cancel')
    DROP PROCEDURE usp_Delivery_Cancel;
GO

CREATE PROCEDURE usp_Delivery_Cancel
    @DeliveryId UNIQUEIDENTIFIER,
    @CancelledBy UNIQUEIDENTIFIER,
    @Reason NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    EXEC usp_Delivery_UpdateStatus @DeliveryId, 'CANCELLED', @CancelledBy, @Reason;
END
GO

PRINT 'Fixed usp_Delivery_Cancel';

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
        de.Id,
        de.DeliveryId,
        de.EventType,
        de.FromStatus,
        de.ToStatus,
        de.Metadata,
        de.ActorId,
        de.ActorType,
        u.FullName AS ActorName,
        de.Timestamp AS CreatedAt
    FROM DeliveryEvents de
    LEFT JOIN Users u ON u.Id = de.ActorId
    WHERE de.DeliveryId = @DeliveryId
    ORDER BY de.Timestamp ASC;
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
-- No TotalDeliveries, TotalEarnings, AverageRating, TotalRatings in DP table
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
        (SELECT COUNT(*) FROM Deliveries d WHERE d.AssignedDPId = dp.UserId AND d.Status = 'DELIVERED') AS TotalDeliveries,
        (SELECT ISNULL(SUM(FinalPrice), 0) FROM Deliveries d WHERE d.AssignedDPId = dp.UserId AND d.Status = 'DELIVERED') AS TotalEarnings,
        (SELECT AVG(CAST(r.Score AS DECIMAL(3,2))) FROM Ratings r WHERE r.TargetId = dp.UserId AND r.TargetType = 'DP') AS AverageRating,
        (SELECT COUNT(*) FROM Ratings r WHERE r.TargetId = dp.UserId AND r.TargetType = 'DP') AS TotalRatings,
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

PRINT 'Schema fixes Part 2: COMPLETE';
