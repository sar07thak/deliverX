-- =====================================================
-- DELIVERYDOST STORED PROCEDURES - DELIVERY PARTNER MODULE
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- =====================================================
-- PROCEDURE: Insert Delivery Partner Profile
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DeliveryPartner_Insert')
    DROP PROCEDURE usp_DeliveryPartner_Insert;
GO

CREATE PROCEDURE usp_DeliveryPartner_Insert
    @UserId UNIQUEIDENTIFIER,
    @DPCMId UNIQUEIDENTIFIER = NULL,
    @FullName NVARCHAR(255),
    @DOB DATETIME2,
    @Gender NVARCHAR(10) = NULL,
    @Address NVARCHAR(500) = NULL,
    @ProfilePhotoUrl NVARCHAR(500) = NULL,
    @VehicleType NVARCHAR(50) = NULL,
    @Languages NVARCHAR(MAX) = NULL,
    @Availability NVARCHAR(20) = 'FULL_TIME',
    @NewProfileId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        -- Validate user exists and has DP role
        IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = @UserId AND Role = 'DP')
        BEGIN
            RAISERROR('User not found or not a Delivery Partner', 16, 1);
            RETURN;
        END

        -- Check if profile already exists
        IF EXISTS (SELECT 1 FROM DeliveryPartnerProfiles WHERE UserId = @UserId)
        BEGIN
            RAISERROR('Delivery Partner profile already exists for this user', 16, 1);
            RETURN;
        END

        -- Validate DPCM if provided
        IF @DPCMId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM DPCManagers WHERE Id = @DPCMId AND IsActive = 1)
        BEGIN
            RAISERROR('Invalid or inactive DPCM', 16, 1);
            RETURN;
        END

        SET @NewProfileId = NEWID();

        INSERT INTO DeliveryPartnerProfiles (
            Id, UserId, DPCMId, FullName, ProfilePhotoUrl, DOB, Gender,
            Address, VehicleType, Languages, Availability,
            IsActive, IsOnline, CurrentActiveDeliveries, MaxConcurrentDeliveries,
            CreatedAt, UpdatedAt
        )
        VALUES (
            @NewProfileId, @UserId, @DPCMId, @FullName, @ProfilePhotoUrl, @DOB, @Gender,
            @Address, @VehicleType, @Languages, @Availability,
            0, 0, 0, 3, -- Default max 3 concurrent deliveries
            GETUTCDATE(), GETUTCDATE()
        );

        -- Return the created profile
        SELECT
            dp.Id, dp.UserId, dp.DPCMId, dp.FullName, dp.ProfilePhotoUrl,
            dp.DOB, dp.Gender, dp.Address, dp.VehicleType, dp.Languages,
            dp.Availability, dp.IsActive, dp.IsOnline, dp.CreatedAt,
            u.Phone, u.Email
        FROM DeliveryPartnerProfiles dp
        INNER JOIN Users u ON u.Id = dp.UserId
        WHERE dp.Id = @NewProfileId;

    END TRY
    BEGIN CATCH
        EXEC usp_LogError @ErrorContext = 'usp_DeliveryPartner_Insert', @UserId = @UserId;
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_DeliveryPartner_Insert';

-- =====================================================
-- PROCEDURE: Update Delivery Partner Profile
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DeliveryPartner_Update')
    DROP PROCEDURE usp_DeliveryPartner_Update;
GO

CREATE PROCEDURE usp_DeliveryPartner_Update
    @ProfileId UNIQUEIDENTIFIER,
    @FullName NVARCHAR(255) = NULL,
    @Address NVARCHAR(500) = NULL,
    @ProfilePhotoUrl NVARCHAR(500) = NULL,
    @VehicleType NVARCHAR(50) = NULL,
    @Languages NVARCHAR(MAX) = NULL,
    @Availability NVARCHAR(20) = NULL,
    @MaxConcurrentDeliveries INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM DeliveryPartnerProfiles WHERE Id = @ProfileId)
        BEGIN
            RAISERROR('Delivery Partner profile not found', 16, 1);
            RETURN;
        END

        UPDATE DeliveryPartnerProfiles
        SET
            FullName = ISNULL(@FullName, FullName),
            Address = ISNULL(@Address, Address),
            ProfilePhotoUrl = ISNULL(@ProfilePhotoUrl, ProfilePhotoUrl),
            VehicleType = ISNULL(@VehicleType, VehicleType),
            Languages = ISNULL(@Languages, Languages),
            Availability = ISNULL(@Availability, Availability),
            MaxConcurrentDeliveries = ISNULL(@MaxConcurrentDeliveries, MaxConcurrentDeliveries),
            UpdatedAt = GETUTCDATE()
        WHERE Id = @ProfileId;

        -- Return updated profile
        SELECT
            dp.*, u.Phone, u.Email
        FROM DeliveryPartnerProfiles dp
        INNER JOIN Users u ON u.Id = dp.UserId
        WHERE dp.Id = @ProfileId;

    END TRY
    BEGIN CATCH
        EXEC usp_LogError @ErrorContext = 'usp_DeliveryPartner_Update';
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_DeliveryPartner_Update';

-- =====================================================
-- PROCEDURE: Get Delivery Partner By ID
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
        -- DPCM info
        dpcm.OrganizationName AS DPCMOrganization,
        dpcm.ContactPersonName AS DPCMContactPerson,
        -- Wallet balance
        w.Balance AS WalletBalance,
        -- Average rating
        (SELECT AVG(CAST(r.Rating AS DECIMAL(3,2)))
         FROM Ratings r WHERE r.RatedUserId = dp.UserId) AS AverageRating,
        -- Total deliveries
        (SELECT COUNT(*) FROM Deliveries d
         WHERE d.AssignedDPId = dp.UserId AND d.Status = 'DELIVERED') AS TotalDeliveries
    FROM DeliveryPartnerProfiles dp
    INNER JOIN Users u ON u.Id = dp.UserId
    LEFT JOIN DPCManagers dpcm ON dpcm.Id = dp.DPCMId
    LEFT JOIN Wallets w ON w.UserId = dp.UserId AND w.IsActive = 1
    WHERE dp.Id = @ProfileId;
END
GO

PRINT 'Created usp_DeliveryPartner_GetById';

-- =====================================================
-- PROCEDURE: Get Delivery Partner By User ID
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
        SELECT NULL AS Id; -- Return empty result
END
GO

PRINT 'Created usp_DeliveryPartner_GetByUserId';

-- =====================================================
-- PROCEDURE: List Delivery Partners
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

    -- Get total count
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

    -- Get paged results
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
        (SELECT AVG(CAST(r.Rating AS DECIMAL(3,2)))
         FROM Ratings r WHERE r.RatedUserId = dp.UserId) AS AverageRating,
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

PRINT 'Created usp_DeliveryPartner_List';

-- =====================================================
-- PROCEDURE: Set Online Status
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DeliveryPartner_SetOnlineStatus')
    DROP PROCEDURE usp_DeliveryPartner_SetOnlineStatus;
GO

CREATE PROCEDURE usp_DeliveryPartner_SetOnlineStatus
    @UserId UNIQUEIDENTIFIER,
    @IsOnline BIT,
    @Latitude DECIMAL(10,8) = NULL,
    @Longitude DECIMAL(11,8) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Validate DP exists and is active
        IF NOT EXISTS (SELECT 1 FROM DeliveryPartnerProfiles WHERE UserId = @UserId AND IsActive = 1)
        BEGIN
            RAISERROR('Delivery Partner not found or not active', 16, 1);
            RETURN;
        END

        UPDATE DeliveryPartnerProfiles
        SET
            IsOnline = @IsOnline,
            LastOnlineAt = CASE WHEN @IsOnline = 1 THEN GETUTCDATE() ELSE LastOnlineAt END,
            UpdatedAt = GETUTCDATE()
        WHERE UserId = @UserId;

        -- Update operational status if exists
        IF EXISTS (SELECT 1 FROM DPOperationalStatus dos
                   INNER JOIN DeliveryPartners dp ON dp.Id = dos.DPId
                   WHERE dp.UserId = @UserId)
        BEGIN
            UPDATE dos
            SET
                IsOnline = @IsOnline,
                LastOnlineAt = CASE WHEN @IsOnline = 1 THEN GETUTCDATE() ELSE dos.LastOnlineAt END,
                LastOfflineAt = CASE WHEN @IsOnline = 0 THEN GETUTCDATE() ELSE dos.LastOfflineAt END,
                CurrentLat = ISNULL(@Latitude, dos.CurrentLat),
                CurrentLng = ISNULL(@Longitude, dos.CurrentLng),
                LocationUpdatedAt = CASE WHEN @Latitude IS NOT NULL THEN GETUTCDATE() ELSE dos.LocationUpdatedAt END,
                UpdatedAt = GETUTCDATE()
            FROM DPOperationalStatus dos
            INNER JOIN DeliveryPartners dp ON dp.Id = dos.DPId
            WHERE dp.UserId = @UserId;
        END

        SELECT
            dp.UserId,
            dp.IsOnline,
            dp.LastOnlineAt,
            dp.CurrentActiveDeliveries,
            dp.MaxConcurrentDeliveries
        FROM DeliveryPartnerProfiles dp
        WHERE dp.UserId = @UserId;

    END TRY
    BEGIN CATCH
        EXEC usp_LogError @ErrorContext = 'usp_DeliveryPartner_SetOnlineStatus', @UserId = @UserId;
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_DeliveryPartner_SetOnlineStatus';

-- =====================================================
-- PROCEDURE: Update Pricing
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DeliveryPartner_UpdatePricing')
    DROP PROCEDURE usp_DeliveryPartner_UpdatePricing;
GO

CREATE PROCEDURE usp_DeliveryPartner_UpdatePricing
    @UserId UNIQUEIDENTIFIER,
    @PerKmRate DECIMAL(10,2),
    @PerKgRate DECIMAL(10,2),
    @MinCharge DECIMAL(10,2),
    @MaxDistanceKm DECIMAL(10,2) = NULL,
    @MaxBidRate DECIMAL(10,2) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Update profile
        UPDATE DeliveryPartnerProfiles
        SET
            PerKmRate = @PerKmRate,
            PerKgRate = @PerKgRate,
            MinCharge = @MinCharge,
            MaxDistanceKm = ISNULL(@MaxDistanceKm, MaxDistanceKm),
            MaxBidRate = ISNULL(@MaxBidRate, MaxBidRate),
            UpdatedAt = GETUTCDATE()
        WHERE UserId = @UserId;

        -- Insert pricing history (if table exists)
        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DPPricingConfigs')
        BEGIN
            -- Close existing active config
            UPDATE DPPricingConfigs
            SET EffectiveTo = GETUTCDATE(), UpdatedAt = GETUTCDATE()
            WHERE DPId = @UserId AND EffectiveTo IS NULL;

            -- Insert new config
            INSERT INTO DPPricingConfigs (
                Id, DPId, PerKmRate, PerKgRate, MinCharge, MaxDistanceKm,
                Currency, EffectiveFrom, CreatedAt, UpdatedAt
            )
            VALUES (
                NEWID(), @UserId, @PerKmRate, @PerKgRate, @MinCharge, @MaxDistanceKm,
                'INR', GETUTCDATE(), GETUTCDATE(), GETUTCDATE()
            );
        END

        COMMIT;

        SELECT 1 AS Success, 'Pricing updated successfully' AS Message;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        EXEC usp_LogError @ErrorContext = 'usp_DeliveryPartner_UpdatePricing', @UserId = @UserId;
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_DeliveryPartner_UpdatePricing';

-- =====================================================
-- PROCEDURE: Update Service Area
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DeliveryPartner_UpdateServiceArea')
    DROP PROCEDURE usp_DeliveryPartner_UpdateServiceArea;
GO

CREATE PROCEDURE usp_DeliveryPartner_UpdateServiceArea
    @UserId UNIQUEIDENTIFIER,
    @CenterLat DECIMAL(10,8),
    @CenterLng DECIMAL(11,8),
    @RadiusKm DECIMAL(10,2),
    @PincodesJson NVARCHAR(MAX) = NULL,
    @PreferredDirection NVARCHAR(20) = NULL,
    @OneDirectionOnly BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        UPDATE DeliveryPartnerProfiles
        SET
            ServiceAreaCenterLat = @CenterLat,
            ServiceAreaCenterLng = @CenterLng,
            ServiceAreaRadiusKm = @RadiusKm,
            ServiceAreaPincodesJson = @PincodesJson,
            PreferredDirection = @PreferredDirection,
            OneDirectionOnly = @OneDirectionOnly,
            UpdatedAt = GETUTCDATE()
        WHERE UserId = @UserId;

        SELECT 1 AS Success, 'Service area updated successfully' AS Message;

    END TRY
    BEGIN CATCH
        EXEC usp_LogError @ErrorContext = 'usp_DeliveryPartner_UpdateServiceArea', @UserId = @UserId;
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_DeliveryPartner_UpdateServiceArea';

-- =====================================================
-- PROCEDURE: Get Available Partners for Matching
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
        (SELECT AVG(CAST(r.Rating AS DECIMAL(3,2)))
         FROM Ratings r WHERE r.RatedUserId = dp.UserId) AS AverageRating,
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
        (SELECT AVG(CAST(r.Rating AS DECIMAL(3,2))) FROM Ratings r WHERE r.RatedUserId = dp.UserId) DESC NULLS LAST,
        dbo.ufn_CalculateDistance(dp.ServiceAreaCenterLat, dp.ServiceAreaCenterLng, @PickupLat, @PickupLng) ASC;
END
GO

PRINT 'Created usp_DeliveryPartner_GetAvailable';

-- =====================================================
-- PROCEDURE: Activate Delivery Partner
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

        -- Create wallet if not exists
        DECLARE @UserId UNIQUEIDENTIFIER;
        SELECT @UserId = UserId FROM DeliveryPartnerProfiles WHERE Id = @ProfileId;

        IF NOT EXISTS (SELECT 1 FROM Wallets WHERE UserId = @UserId)
        BEGIN
            INSERT INTO Wallets (Id, UserId, WalletType, Balance, HoldBalance, Currency, IsActive, CreatedAt, UpdatedAt)
            VALUES (NEWID(), @UserId, 'DP', 0, 0, 'INR', 1, GETUTCDATE(), GETUTCDATE());
        END

        -- Log audit
        INSERT INTO AdminAuditLogs (Id, AdminId, Action, EntityType, EntityId, CreatedAt)
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

PRINT 'Created usp_DeliveryPartner_Activate';

-- =====================================================
-- PROCEDURE: Increment Active Deliveries Count
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DeliveryPartner_IncrementActiveDeliveries')
    DROP PROCEDURE usp_DeliveryPartner_IncrementActiveDeliveries;
GO

CREATE PROCEDURE usp_DeliveryPartner_IncrementActiveDeliveries
    @UserId UNIQUEIDENTIFIER,
    @Increment INT = 1 -- Positive to increment, negative to decrement
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE DeliveryPartnerProfiles
    SET
        CurrentActiveDeliveries = CurrentActiveDeliveries + @Increment,
        UpdatedAt = GETUTCDATE()
    WHERE UserId = @UserId
      AND (CurrentActiveDeliveries + @Increment) >= 0; -- Prevent negative

    -- Also update normalized table if exists
    UPDATE dos
    SET CurrentActiveDeliveries = dos.CurrentActiveDeliveries + @Increment, UpdatedAt = GETUTCDATE()
    FROM DPOperationalStatus dos
    INNER JOIN DeliveryPartners dp ON dp.Id = dos.DPId
    WHERE dp.UserId = @UserId
      AND (dos.CurrentActiveDeliveries + @Increment) >= 0;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

PRINT 'Created usp_DeliveryPartner_IncrementActiveDeliveries';

PRINT 'DeliveryPartner module: COMPLETE';
