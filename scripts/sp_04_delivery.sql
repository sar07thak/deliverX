-- =====================================================
-- DELIVERYDOST STORED PROCEDURES - DELIVERY MODULE
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- =====================================================
-- PROCEDURE: Create Delivery
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Delivery_Create')
    DROP PROCEDURE usp_Delivery_Create;
GO

CREATE PROCEDURE usp_Delivery_Create
    @RequesterId UNIQUEIDENTIFIER,
    @RequesterType NVARCHAR(20), -- 'BC' or 'EC'
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

        -- Validate requester exists
        IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = @RequesterId AND IsActive = 1)
        BEGIN
            RAISERROR('Requester not found or not active', 16, 1);
            RETURN;
        END

        -- Calculate distance
        DECLARE @DistanceKm DECIMAL(10,3) = dbo.ufn_CalculateDistance(@PickupLat, @PickupLng, @DropLat, @DropLng);

        -- Estimate duration (avg 30 km/h)
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

        -- Record creation event
        INSERT INTO DeliveryEvents (Id, DeliveryId, EventType, EventData, CreatedAt)
        VALUES (NEWID(), @NewDeliveryId, 'CREATED',
                '{"RequesterType": "' + @RequesterType + '", "Priority": "' + @Priority + '"}',
                GETUTCDATE());

        -- Insert into normalized DeliveryAddresses if table exists
        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DeliveryAddresses')
        BEGIN
            -- Pickup address
            INSERT INTO DeliveryAddresses (Id, DeliveryId, AddressType, Latitude, Longitude, AddressLine,
                                           ContactName, ContactPhone, Instructions, CreatedAt)
            VALUES (NEWID(), @NewDeliveryId, 'PICKUP', @PickupLat, @PickupLng, @PickupAddress,
                    @PickupContactName, @PickupContactPhone, @PickupInstructions, GETUTCDATE());

            -- Drop address
            INSERT INTO DeliveryAddresses (Id, DeliveryId, AddressType, Latitude, Longitude, AddressLine,
                                           ContactName, ContactPhone, Instructions, CreatedAt)
            VALUES (NEWID(), @NewDeliveryId, 'DROP', @DropLat, @DropLng, @DropAddress,
                    @DropContactName, @DropContactPhone, @DropInstructions, GETUTCDATE());
        END

        -- Insert into DeliveryPackages if table exists
        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DeliveryPackages')
        BEGIN
            INSERT INTO DeliveryPackages (Id, DeliveryId, PackageNumber, PackageType, WeightKg,
                                          DeclaredValue, Description, CautionType, CautionNotes,
                                          RequiresSpecialHandling, IsHazardous, CreatedAt, UpdatedAt)
            VALUES (NEWID(), @NewDeliveryId, 1, @PackageType, @WeightKg,
                    @PackageValue, @PackageDescription, @CautionType, @CautionNotes,
                    CASE WHEN @CautionType IS NOT NULL THEN 1 ELSE 0 END,
                    CASE WHEN @CautionType = 'HAZARDOUS' THEN 1 ELSE 0 END,
                    GETUTCDATE(), GETUTCDATE());
        END

        -- Insert into DeliveryRoutes if table exists
        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DeliveryRoutes')
        BEGIN
            INSERT INTO DeliveryRoutes (Id, DeliveryId, DistanceKm, DistanceSource,
                                        EstimatedDurationMinutes, RouteCalculatedAt, CreatedAt, UpdatedAt)
            VALUES (NEWID(), @NewDeliveryId, @DistanceKm, 'HAVERSINE',
                    @EstimatedDuration, GETUTCDATE(), GETUTCDATE(), GETUTCDATE());
        END

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

PRINT 'Created usp_Delivery_Create';

-- =====================================================
-- PROCEDURE: Get Delivery By ID
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Delivery_GetById')
    DROP PROCEDURE usp_Delivery_GetById;
GO

CREATE PROCEDURE usp_Delivery_GetById
    @DeliveryId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.Id,
        d.RequesterId,
        d.RequesterType,
        d.AssignedDPId,
        d.AssignedAt,
        d.PickupLat,
        d.PickupLng,
        d.PickupAddress,
        d.PickupAddressName,
        d.PickupContactName,
        d.PickupContactPhone,
        d.PickupInstructions,
        d.DropLat,
        d.DropLng,
        d.DropAddress,
        d.DropAddressName,
        d.DropContactName,
        d.DropContactPhone,
        d.DropInstructions,
        d.WeightKg,
        d.PackageType,
        d.PackageDimensions,
        d.PackageValue,
        d.PackageDescription,
        d.Priority,
        d.ScheduledAt,
        d.Status,
        d.EstimatedPrice,
        d.FinalPrice,
        d.SpecialInstructions,
        d.CautionType,
        d.CautionNotes,
        d.DistanceKm,
        d.EstimatedDurationMinutes,
        d.MatchingAttempts,
        d.CreatedAt,
        d.UpdatedAt,
        d.CancelledAt,
        d.CancellationReason,
        -- Requester info
        req.FullName AS RequesterName,
        req.Phone AS RequesterPhone,
        req.Email AS RequesterEmail,
        -- DP info
        dp.FullName AS DPName,
        dpUser.Phone AS DPPhone,
        dp.VehicleType AS DPVehicleType,
        dp.ProfilePhotoUrl AS DPPhotoUrl
    FROM Deliveries d
    INNER JOIN Users req ON req.Id = d.RequesterId
    LEFT JOIN Users dpUser ON dpUser.Id = d.AssignedDPId
    LEFT JOIN DeliveryPartnerProfiles dp ON dp.UserId = d.AssignedDPId
    WHERE d.Id = @DeliveryId;
END
GO

PRINT 'Created usp_Delivery_GetById';

-- =====================================================
-- PROCEDURE: List Deliveries
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Delivery_List')
    DROP PROCEDURE usp_Delivery_List;
GO

CREATE PROCEDURE usp_Delivery_List
    @PageNumber INT = 1,
    @PageSize INT = 20,
    @RequesterId UNIQUEIDENTIFIER = NULL,
    @AssignedDPId UNIQUEIDENTIFIER = NULL,
    @Status NVARCHAR(50) = NULL,
    @Priority NVARCHAR(20) = NULL,
    @FromDate DATETIME2 = NULL,
    @ToDate DATETIME2 = NULL,
    @SearchTerm NVARCHAR(100) = NULL,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    SELECT @TotalCount = COUNT(*)
    FROM Deliveries d
    WHERE (@RequesterId IS NULL OR d.RequesterId = @RequesterId)
      AND (@AssignedDPId IS NULL OR d.AssignedDPId = @AssignedDPId)
      AND (@Status IS NULL OR d.Status = @Status)
      AND (@Priority IS NULL OR d.Priority = @Priority)
      AND (@FromDate IS NULL OR d.CreatedAt >= @FromDate)
      AND (@ToDate IS NULL OR d.CreatedAt <= @ToDate)
      AND (@SearchTerm IS NULL OR
           d.PickupAddress LIKE '%' + @SearchTerm + '%' OR
           d.DropAddress LIKE '%' + @SearchTerm + '%' OR
           d.PickupContactName LIKE '%' + @SearchTerm + '%' OR
           d.DropContactName LIKE '%' + @SearchTerm + '%');

    SELECT
        d.Id,
        d.RequesterId,
        d.RequesterType,
        d.AssignedDPId,
        d.PickupAddress,
        d.DropAddress,
        d.WeightKg,
        d.PackageType,
        d.Priority,
        d.Status,
        d.EstimatedPrice,
        d.FinalPrice,
        d.DistanceKm,
        d.CreatedAt,
        d.UpdatedAt,
        req.FullName AS RequesterName,
        req.Phone AS RequesterPhone,
        dp.FullName AS DPName
    FROM Deliveries d
    INNER JOIN Users req ON req.Id = d.RequesterId
    LEFT JOIN DeliveryPartnerProfiles dp ON dp.UserId = d.AssignedDPId
    WHERE (@RequesterId IS NULL OR d.RequesterId = @RequesterId)
      AND (@AssignedDPId IS NULL OR d.AssignedDPId = @AssignedDPId)
      AND (@Status IS NULL OR d.Status = @Status)
      AND (@Priority IS NULL OR d.Priority = @Priority)
      AND (@FromDate IS NULL OR d.CreatedAt >= @FromDate)
      AND (@ToDate IS NULL OR d.CreatedAt <= @ToDate)
      AND (@SearchTerm IS NULL OR
           d.PickupAddress LIKE '%' + @SearchTerm + '%' OR
           d.DropAddress LIKE '%' + @SearchTerm + '%')
    ORDER BY d.CreatedAt DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    SELECT @TotalCount AS TotalCount;
END
GO

PRINT 'Created usp_Delivery_List';

-- =====================================================
-- PROCEDURE: Match Candidates for Delivery
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

    -- Get candidates with ranking
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
        (SELECT AVG(CAST(r.Rating AS DECIMAL(3,2)))
         FROM Ratings r WHERE r.RatedUserId = dp.UserId) AS AverageRating,
        (SELECT COUNT(*) FROM Deliveries d2
         WHERE d2.AssignedDPId = dp.UserId AND d2.Status = 'DELIVERED') AS TotalDeliveries,
        CASE WHEN dp.UserId = @PreferredDPId THEN 1 ELSE 0 END AS IsPreferred,
        -- Ranking score (lower is better)
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
        (SELECT AVG(CAST(r.Rating AS DECIMAL(3,2))) FROM Ratings r WHERE r.RatedUserId = dp.UserId) DESC;

    -- Increment matching attempts
    UPDATE Deliveries
    SET MatchingAttempts = MatchingAttempts + 1, UpdatedAt = GETUTCDATE()
    WHERE Id = @DeliveryId;
END
GO

PRINT 'Created usp_Delivery_MatchCandidates';

-- =====================================================
-- PROCEDURE: Assign Delivery Partner
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

        -- Validate delivery exists and is in correct status
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

        -- Validate DP is available
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

        -- Assign the delivery
        UPDATE Deliveries
        SET
            AssignedDPId = @DPUserId,
            AssignedAt = GETUTCDATE(),
            Status = 'ASSIGNED',
            EstimatedPrice = ISNULL(@EstimatedPrice, EstimatedPrice),
            UpdatedAt = GETUTCDATE()
        WHERE Id = @DeliveryId;

        -- Increment DP's active deliveries
        EXEC usp_DeliveryPartner_IncrementActiveDeliveries @DPUserId, 1;

        -- Record event
        INSERT INTO DeliveryEvents (Id, DeliveryId, EventType, EventData, CreatedBy, CreatedAt)
        VALUES (NEWID(), @DeliveryId, 'ASSIGNED',
                '{"AssignedDPId": "' + CAST(@DPUserId AS NVARCHAR(50)) + '"}',
                @DPUserId, GETUTCDATE());

        -- Record in DeliveryMatchingHistories
        INSERT INTO DeliveryMatchingHistories (Id, DeliveryId, DPId, MatchType, MatchedAt, IsAccepted, CreatedAt)
        VALUES (NEWID(), @DeliveryId, @DPUserId, 'DIRECT_ASSIGN', GETUTCDATE(), 1, GETUTCDATE());

        COMMIT;

        -- Return updated delivery
        EXEC usp_Delivery_GetById @DeliveryId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        EXEC usp_LogError @ErrorContext = 'usp_Delivery_AssignPartner';
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_Delivery_AssignPartner';

-- =====================================================
-- PROCEDURE: Update Delivery Status
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

        -- Validate status transition
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

        -- Update status
        UPDATE Deliveries
        SET
            Status = @NewStatus,
            UpdatedAt = GETUTCDATE(),
            CancelledAt = CASE WHEN @NewStatus = 'CANCELLED' THEN GETUTCDATE() ELSE CancelledAt END,
            CancellationReason = CASE WHEN @NewStatus = 'CANCELLED' THEN @Notes ELSE CancellationReason END
        WHERE Id = @DeliveryId;

        -- Record event
        INSERT INTO DeliveryEvents (Id, DeliveryId, EventType, EventData, Latitude, Longitude, CreatedBy, CreatedAt)
        VALUES (NEWID(), @DeliveryId, 'STATUS_CHANGE',
                '{"PreviousStatus": "' + @CurrentStatus + '", "NewStatus": "' + @NewStatus + '", "Notes": "' + ISNULL(@Notes, '') + '"}',
                @Latitude, @Longitude, @UpdatedBy, GETUTCDATE());

        -- Record in DeliveryStatusHistory if table exists
        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DeliveryStatusHistory')
        BEGIN
            INSERT INTO DeliveryStatusHistory (Id, DeliveryId, PreviousStatus, NewStatus, ChangedBy, ChangeReason, Latitude, Longitude, ChangedAt)
            VALUES (NEWID(), @DeliveryId, @CurrentStatus, @NewStatus, @UpdatedBy, @Notes, @Latitude, @Longitude, GETUTCDATE());
        END

        -- Handle completion - decrement DP active deliveries
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

PRINT 'Created usp_Delivery_UpdateStatus';

-- =====================================================
-- PROCEDURE: Cancel Delivery
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

PRINT 'Created usp_Delivery_Cancel';

-- =====================================================
-- PROCEDURE: Get Delivery Timeline
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
        de.EventData,
        de.Latitude,
        de.Longitude,
        de.CreatedBy,
        u.FullName AS CreatedByName,
        de.CreatedAt
    FROM DeliveryEvents de
    LEFT JOIN Users u ON u.Id = de.CreatedBy
    WHERE de.DeliveryId = @DeliveryId
    ORDER BY de.CreatedAt ASC;
END
GO

PRINT 'Created usp_Delivery_GetTimeline';

-- =====================================================
-- PROCEDURE: Set Final Price
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Delivery_SetFinalPrice')
    DROP PROCEDURE usp_Delivery_SetFinalPrice;
GO

CREATE PROCEDURE usp_Delivery_SetFinalPrice
    @DeliveryId UNIQUEIDENTIFIER,
    @FinalPrice DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Deliveries
    SET FinalPrice = @FinalPrice, UpdatedAt = GETUTCDATE()
    WHERE Id = @DeliveryId;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

PRINT 'Created usp_Delivery_SetFinalPrice';

-- =====================================================
-- PROCEDURE: Get Deliveries by DP
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Delivery_GetByDP')
    DROP PROCEDURE usp_Delivery_GetByDP;
GO

CREATE PROCEDURE usp_Delivery_GetByDP
    @DPUserId UNIQUEIDENTIFIER,
    @PageNumber INT = 1,
    @PageSize INT = 20,
    @Status NVARCHAR(50) = NULL,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    SELECT @TotalCount = COUNT(*)
    FROM Deliveries
    WHERE AssignedDPId = @DPUserId
      AND (@Status IS NULL OR Status = @Status);

    SELECT
        d.Id,
        d.RequesterId,
        d.RequesterType,
        d.PickupAddress,
        d.DropAddress,
        d.PickupContactPhone,
        d.DropContactPhone,
        d.WeightKg,
        d.PackageType,
        d.Priority,
        d.Status,
        d.EstimatedPrice,
        d.FinalPrice,
        d.DistanceKm,
        d.AssignedAt,
        d.CreatedAt,
        req.FullName AS RequesterName,
        req.Phone AS RequesterPhone
    FROM Deliveries d
    INNER JOIN Users req ON req.Id = d.RequesterId
    WHERE d.AssignedDPId = @DPUserId
      AND (@Status IS NULL OR d.Status = @Status)
    ORDER BY d.AssignedAt DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    SELECT @TotalCount AS TotalCount;
END
GO

PRINT 'Created usp_Delivery_GetByDP';

PRINT 'Delivery module: COMPLETE';
