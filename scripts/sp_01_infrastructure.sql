-- =====================================================
-- DELIVERYDOST STORED PROCEDURES - INFRASTRUCTURE
-- ErrorLog table, helper procedures, base functions
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- =====================================================
-- ERROR LOG TABLE
-- =====================================================

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ErrorLog')
BEGIN
    CREATE TABLE ErrorLog (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        ErrorNumber INT NULL,
        ErrorSeverity INT NULL,
        ErrorState INT NULL,
        ErrorProcedure NVARCHAR(128) NULL,
        ErrorLine INT NULL,
        ErrorMessage NVARCHAR(4000) NULL,
        ErrorContext NVARCHAR(MAX) NULL,
        UserId UNIQUEIDENTIFIER NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE NONCLUSTERED INDEX IX_ErrorLog_CreatedAt ON ErrorLog(CreatedAt DESC);
    CREATE NONCLUSTERED INDEX IX_ErrorLog_Procedure ON ErrorLog(ErrorProcedure, CreatedAt DESC);

    PRINT 'Created ErrorLog table';
END
GO

-- =====================================================
-- PROCEDURE: Log Error (called from CATCH blocks)
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_LogError')
    DROP PROCEDURE usp_LogError;
GO

CREATE PROCEDURE usp_LogError
    @ErrorContext NVARCHAR(MAX) = NULL,
    @UserId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO ErrorLog (
        ErrorNumber,
        ErrorSeverity,
        ErrorState,
        ErrorProcedure,
        ErrorLine,
        ErrorMessage,
        ErrorContext,
        UserId
    )
    VALUES (
        ERROR_NUMBER(),
        ERROR_SEVERITY(),
        ERROR_STATE(),
        ERROR_PROCEDURE(),
        ERROR_LINE(),
        ERROR_MESSAGE(),
        @ErrorContext,
        @UserId
    );
END
GO

PRINT 'Created usp_LogError';

-- =====================================================
-- SEQUENCE: Delivery Tracking Codes
-- =====================================================

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'seq_DeliveryCode')
BEGIN
    CREATE SEQUENCE seq_DeliveryCode
        AS INT
        START WITH 100001
        INCREMENT BY 1
        NO CACHE;
    PRINT 'Created seq_DeliveryCode';
END
GO

-- =====================================================
-- SEQUENCE: Complaint Numbers
-- =====================================================

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'seq_ComplaintNumber')
BEGIN
    CREATE SEQUENCE seq_ComplaintNumber
        AS INT
        START WITH 10001
        INCREMENT BY 1
        NO CACHE;
    PRINT 'Created seq_ComplaintNumber';
END
GO

-- =====================================================
-- FUNCTION: Calculate Distance (Haversine formula)
-- Returns distance in kilometers between two lat/lng points
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'ufn_CalculateDistance' AND type = 'FN')
    DROP FUNCTION ufn_CalculateDistance;
GO

CREATE FUNCTION ufn_CalculateDistance (
    @Lat1 DECIMAL(10,8),
    @Lng1 DECIMAL(11,8),
    @Lat2 DECIMAL(10,8),
    @Lng2 DECIMAL(11,8)
)
RETURNS DECIMAL(10,3)
WITH SCHEMABINDING
AS
BEGIN
    DECLARE @R DECIMAL(10,2) = 6371; -- Earth's radius in km
    DECLARE @dLat DECIMAL(20,15) = RADIANS(@Lat2 - @Lat1);
    DECLARE @dLng DECIMAL(20,15) = RADIANS(@Lng2 - @Lng1);

    DECLARE @a DECIMAL(20,15) =
        SIN(@dLat/2) * SIN(@dLat/2) +
        COS(RADIANS(@Lat1)) * COS(RADIANS(@Lat2)) *
        SIN(@dLng/2) * SIN(@dLng/2);

    DECLARE @c DECIMAL(20,15) = 2 * ATN2(SQRT(@a), SQRT(1-@a));

    RETURN @R * @c;
END
GO

PRINT 'Created ufn_CalculateDistance';

-- =====================================================
-- FUNCTION: Calculate Delivery Price
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'ufn_CalculateDeliveryPrice' AND type = 'FN')
    DROP FUNCTION ufn_CalculateDeliveryPrice;
GO

CREATE FUNCTION ufn_CalculateDeliveryPrice (
    @DistanceKm DECIMAL(10,3),
    @WeightKg DECIMAL(8,3),
    @PerKmRate DECIMAL(10,2),
    @PerKgRate DECIMAL(10,2),
    @MinCharge DECIMAL(10,2)
)
RETURNS DECIMAL(10,2)
WITH SCHEMABINDING
AS
BEGIN
    DECLARE @DistanceCharge DECIMAL(10,2) = @DistanceKm * @PerKmRate;
    DECLARE @WeightCharge DECIMAL(10,2) = @WeightKg * @PerKgRate;
    DECLARE @TotalCharge DECIMAL(10,2) = @DistanceCharge + @WeightCharge;

    RETURN CASE
        WHEN @TotalCharge < @MinCharge THEN @MinCharge
        ELSE @TotalCharge
    END;
END
GO

PRINT 'Created ufn_CalculateDeliveryPrice';

-- =====================================================
-- FUNCTION: Check if point is within radius
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'ufn_IsWithinRadius' AND type = 'FN')
    DROP FUNCTION ufn_IsWithinRadius;
GO

CREATE FUNCTION ufn_IsWithinRadius (
    @CenterLat DECIMAL(10,8),
    @CenterLng DECIMAL(11,8),
    @PointLat DECIMAL(10,8),
    @PointLng DECIMAL(11,8),
    @RadiusKm DECIMAL(10,3)
)
RETURNS BIT
WITH SCHEMABINDING
AS
BEGIN
    DECLARE @Distance DECIMAL(10,3) = dbo.ufn_CalculateDistance(@CenterLat, @CenterLng, @PointLat, @PointLng);
    RETURN CASE WHEN @Distance <= @RadiusKm THEN 1 ELSE 0 END;
END
GO

PRINT 'Created ufn_IsWithinRadius';

-- =====================================================
-- FUNCTION: Generate Delivery Tracking Code
-- Format: DD-YYYYMMDD-XXXXXX
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'ufn_GenerateDeliveryCode' AND type = 'FN')
    DROP FUNCTION ufn_GenerateDeliveryCode;
GO

CREATE FUNCTION ufn_GenerateDeliveryCode ()
RETURNS NVARCHAR(20)
AS
BEGIN
    DECLARE @SeqNum INT = NEXT VALUE FOR seq_DeliveryCode;
    RETURN 'DD-' + FORMAT(GETUTCDATE(), 'yyyyMMdd') + '-' + RIGHT('000000' + CAST(@SeqNum AS NVARCHAR), 6);
END
GO

PRINT 'Created ufn_GenerateDeliveryCode';

-- =====================================================
-- FUNCTION: Generate Complaint Number
-- Format: CMP-YYYYMMDD-XXXXX
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'ufn_GenerateComplaintNumber' AND type = 'FN')
    DROP FUNCTION ufn_GenerateComplaintNumber;
GO

CREATE FUNCTION ufn_GenerateComplaintNumber ()
RETURNS NVARCHAR(20)
AS
BEGIN
    DECLARE @SeqNum INT = NEXT VALUE FOR seq_ComplaintNumber;
    RETURN 'CMP-' + FORMAT(GETUTCDATE(), 'yyyyMMdd') + '-' + RIGHT('00000' + CAST(@SeqNum AS NVARCHAR), 5);
END
GO

PRINT 'Created ufn_GenerateComplaintNumber';

-- =====================================================
-- TABLE-VALUED FUNCTION: Find Delivery Partners in Radius
-- Returns DPs within radius who are online and available
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'ufn_DeliveryPartner_FindInRadius' AND type = 'IF')
    DROP FUNCTION ufn_DeliveryPartner_FindInRadius;
GO

CREATE FUNCTION ufn_DeliveryPartner_FindInRadius (
    @Latitude DECIMAL(10,8),
    @Longitude DECIMAL(11,8),
    @RadiusKm DECIMAL(10,3)
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
        ) AS DistanceFromCenter,
        u.Phone,
        u.Email
    FROM DeliveryPartnerProfiles dp
    INNER JOIN Users u ON u.Id = dp.UserId
    WHERE dp.IsActive = 1
      AND dp.IsOnline = 1
      AND dp.CurrentActiveDeliveries < dp.MaxConcurrentDeliveries
      AND dp.ServiceAreaCenterLat IS NOT NULL
      AND dp.ServiceAreaCenterLng IS NOT NULL
      AND dbo.ufn_CalculateDistance(
            dp.ServiceAreaCenterLat,
            dp.ServiceAreaCenterLng,
            @Latitude,
            @Longitude
          ) <= ISNULL(dp.ServiceAreaRadiusKm, @RadiusKm)
);
GO

PRINT 'Created ufn_DeliveryPartner_FindInRadius';

-- =====================================================
-- TABLE-VALUED FUNCTION: Get Candidates for Delivery
-- Returns ranked DP candidates for a specific delivery
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
            (SELECT AVG(CAST(r.Rating AS DECIMAL(3,2)))
             FROM Ratings r
             WHERE r.RatedUserId = dp.UserId AND r.RatingType = 'DELIVERY'),
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
          -- Check if pickup is within DP's service area
          dbo.ufn_CalculateDistance(
              dp.ServiceAreaCenterLat,
              dp.ServiceAreaCenterLng,
              d.PickupLat,
              d.PickupLng
          ) <= ISNULL(dp.ServiceAreaRadiusKm, 50)
          -- Or DP can handle the delivery distance
          AND d.DistanceKm <= ISNULL(dp.MaxDistanceKm, 100)
      )
      -- Exclude already assigned DP
      AND (d.AssignedDPId IS NULL OR dp.UserId <> d.AssignedDPId)
      -- Exclude if DP is preferred and we want others
      AND (d.PreferredDPId IS NULL OR dp.UserId = d.PreferredDPId OR d.PreferredDPId = dp.UserId)
);
GO

PRINT 'Created ufn_DeliveryPartner_GetCandidates';

PRINT 'Infrastructure setup: COMPLETE';
