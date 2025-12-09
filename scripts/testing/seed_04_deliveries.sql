-- =====================================================
-- DELIVERYDOST SEED DATA - STEP 4: DELIVERIES
-- =====================================================
-- Run AFTER seed_03_profiles.sql
-- Creates: Deliveries, DeliveryEvents, ProofOfDeliveries, Ratings
-- =====================================================

USE DeliveryDost_Dev;
GO

SET NOCOUNT ON;
PRINT '====================================';
PRINT 'STEP 4: SEEDING DELIVERIES & EVENTS';
PRINT '====================================';

-- =====================================================
-- Location data for realistic addresses
-- =====================================================
DECLARE @Locations TABLE (
    City VARCHAR(20),
    BaseLat DECIMAL(10,8),
    BaseLng DECIMAL(11,8),
    Areas NVARCHAR(500)
);
INSERT INTO @Locations VALUES
('Jaipur', 26.9124, 75.7873, 'MI Road,Bani Park,Johri Bazaar,Raja Park,Malviya Nagar,Vaishali Nagar'),
('Delhi', 28.6139, 77.2090, 'Connaught Place,Hauz Khas,Lajpat Nagar,Saket,Dwarka,Rohini'),
('Mumbai', 19.0760, 72.8777, 'Andheri,Bandra,Powai,Colaba,Dadar,Goregaon');

-- =====================================================
-- 4.1 CREATE DELIVERIES (300)
-- Distribution: 50 Pending, 30 Assigned, 40 In Progress, 150 Delivered, 20 Cancelled, 10 Failed
-- =====================================================
PRINT 'Creating Deliveries...';

DECLARE @DeliveryCount INT = 0;
DECLARE @i INT = 1;
DECLARE @MaxDeliveries INT = 300;
DECLARE @DeliveryId UNIQUEIDENTIFIER;
DECLARE @RequesterId UNIQUEIDENTIFIER;
DECLARE @RequesterType NVARCHAR(10);
DECLARE @DPId UNIQUEIDENTIFIER;
DECLARE @Status NVARCHAR(50);
DECLARE @City VARCHAR(20);
DECLARE @PickupLat DECIMAL(10,8), @PickupLng DECIMAL(11,8);
DECLARE @DropLat DECIMAL(10,8), @DropLng DECIMAL(11,8);
DECLARE @DistanceKm DECIMAL(10,3);
DECLARE @CreatedAt DATETIME2;
DECLARE @PackageType NVARCHAR(50);
DECLARE @WeightKg DECIMAL(8,3);
DECLARE @Priority NVARCHAR(20);

WHILE @i <= @MaxDeliveries
BEGIN
    -- Determine status distribution
    SET @Status = CASE
        WHEN @i <= 50 THEN 'PENDING'
        WHEN @i <= 80 THEN 'ASSIGNED'
        WHEN @i <= 90 THEN 'ACCEPTED'
        WHEN @i <= 100 THEN 'EN_ROUTE_PICKUP'
        WHEN @i <= 110 THEN 'PICKED_UP'
        WHEN @i <= 120 THEN 'EN_ROUTE_DROP'
        WHEN @i <= 270 THEN 'DELIVERED'
        WHEN @i <= 290 THEN 'CANCELLED'
        ELSE 'FAILED'
    END;

    -- Pick random requester (EC or BC)
    IF @i % 3 = 0
        SELECT TOP 1 @RequesterId = u.Id, @RequesterType = 'BC'
        FROM Users u WHERE u.Role = 'BC' AND u.IsActive = 1 ORDER BY NEWID();
    ELSE
        SELECT TOP 1 @RequesterId = u.Id, @RequesterType = 'EC'
        FROM Users u WHERE u.Role = 'EC' AND u.IsActive = 1 ORDER BY NEWID();

    -- Pick city and generate locations
    SET @City = CASE @i % 3 WHEN 0 THEN 'Jaipur' WHEN 1 THEN 'Delhi' ELSE 'Mumbai' END;

    SELECT
        @PickupLat = BaseLat + (RAND(CHECKSUM(NEWID())) - 0.5) * 0.08,
        @PickupLng = BaseLng + (RAND(CHECKSUM(NEWID())) - 0.5) * 0.08,
        @DropLat = BaseLat + (RAND(CHECKSUM(NEWID())) - 0.5) * 0.08,
        @DropLng = BaseLng + (RAND(CHECKSUM(NEWID())) - 0.5) * 0.08
    FROM @Locations WHERE City = @City;

    -- Calculate distance (simplified Haversine approximation)
    SET @DistanceKm = SQRT(POWER((@DropLat - @PickupLat) * 111, 2) + POWER((@DropLng - @PickupLng) * 85, 2));
    IF @DistanceKm < 1 SET @DistanceKm = RAND() * 10 + 2;

    -- Random package details
    SET @PackageType = CASE @i % 8
        WHEN 0 THEN 'Document' WHEN 1 THEN 'Parcel' WHEN 2 THEN 'Food'
        WHEN 3 THEN 'Medicine' WHEN 4 THEN 'Electronics' WHEN 5 THEN 'Fragile'
        WHEN 6 THEN 'Heavy Goods' ELSE 'Liquid'
    END;
    SET @WeightKg = CASE @PackageType
        WHEN 'Document' THEN RAND() * 2 + 0.1
        WHEN 'Heavy Goods' THEN RAND() * 30 + 20
        ELSE RAND() * 10 + 0.5
    END;
    SET @Priority = CASE @i % 10 WHEN 0 THEN 'EXPRESS' WHEN 1 THEN 'EXPRESS' WHEN 2 THEN 'ECONOMY' ELSE 'NORMAL' END;

    -- Created date (last 60 days, more recent for pending)
    SET @CreatedAt = CASE
        WHEN @Status = 'PENDING' THEN DATEADD(HOUR, -CAST(RAND() * 48 AS INT), GETUTCDATE())
        WHEN @Status IN ('ASSIGNED', 'ACCEPTED', 'EN_ROUTE_PICKUP', 'PICKED_UP', 'EN_ROUTE_DROP') THEN DATEADD(HOUR, -CAST(RAND() * 24 AS INT), GETUTCDATE())
        ELSE DATEADD(DAY, -CAST(RAND() * 60 AS INT), GETUTCDATE())
    END;

    -- Assign DP for non-pending statuses
    SET @DPId = NULL;
    IF @Status NOT IN ('PENDING', 'MATCHING')
    BEGIN
        SELECT TOP 1 @DPId = dp.UserId
        FROM DeliveryPartnerProfiles dp
        WHERE dp.IsActive = 1
        ORDER BY NEWID();
    END

    SET @DeliveryId = NEWID();

    -- Insert Delivery
    INSERT INTO Deliveries (
        Id, RequesterId, RequesterType, AssignedDPId, AssignedAt,
        PickupLat, PickupLng, PickupAddress, PickupContactName, PickupContactPhone, PickupInstructions,
        DropLat, DropLng, DropAddress, DropContactName, DropContactPhone, DropInstructions,
        WeightKg, PackageType, PackageDescription, PackageValue, Priority, ScheduledAt, Status,
        EstimatedPrice, FinalPrice, DistanceKm, EstimatedDurationMinutes, MatchingAttempts,
        IsHazardous, RequiresSpecialHandling, CreatedAt, UpdatedAt, CancelledAt, CancellationReason
    )
    VALUES (
        @DeliveryId, @RequesterId, @RequesterType,
        @DPId,
        CASE WHEN @DPId IS NOT NULL THEN DATEADD(MINUTE, 10, @CreatedAt) ELSE NULL END,
        @PickupLat, @PickupLng,
        CAST(@i AS NVARCHAR) + ', ' + @City + ' Main Road, ' + @City,
        'Pickup Contact ' + CAST(@i AS NVARCHAR),
        '98765' + RIGHT('00000' + CAST(@i AS VARCHAR), 5),
        CASE @i % 5 WHEN 0 THEN 'Call before arrival' ELSE NULL END,
        @DropLat, @DropLng,
        CAST(@i + 100 AS NVARCHAR) + ', ' + @City + ' Market Area, ' + @City,
        'Drop Contact ' + CAST(@i AS NVARCHAR),
        '98764' + RIGHT('00000' + CAST(@i AS VARCHAR), 5),
        CASE @i % 4 WHEN 0 THEN 'Leave at door' ELSE NULL END,
        @WeightKg, @PackageType,
        'Package #' + CAST(@i AS NVARCHAR) + ' - ' + @PackageType,
        CASE @PackageType WHEN 'Electronics' THEN RAND() * 20000 + 5000 WHEN 'Document' THEN RAND() * 500 + 100 ELSE RAND() * 5000 + 500 END,
        @Priority,
        @CreatedAt,
        @Status,
        @DistanceKm * 12 + @WeightKg * 3 + 50, -- Estimated price
        CASE WHEN @Status = 'DELIVERED' THEN @DistanceKm * 12 + @WeightKg * 3 + 50 ELSE NULL END,
        @DistanceKm,
        CAST(@DistanceKm * 4 AS INT) + 15, -- Duration in minutes
        CASE WHEN @Status = 'PENDING' THEN 0 ELSE CAST(RAND() * 3 + 1 AS INT) END,
        CASE @PackageType WHEN 'Liquid' THEN 1 ELSE 0 END,
        CASE @PackageType WHEN 'Fragile' THEN 1 WHEN 'Electronics' THEN 1 ELSE 0 END,
        @CreatedAt,
        GETUTCDATE(),
        CASE WHEN @Status = 'CANCELLED' THEN DATEADD(MINUTE, CAST(RAND() * 60 AS INT), @CreatedAt) ELSE NULL END,
        CASE WHEN @Status = 'CANCELLED' THEN 'Customer requested cancellation' ELSE NULL END
    );

    SET @DeliveryCount = @DeliveryCount + 1;
    SET @i = @i + 1;
END

PRINT '  -> Created ' + CAST(@DeliveryCount AS VARCHAR) + ' Deliveries';
GO

-- =====================================================
-- 4.2 CREATE DELIVERY EVENTS
-- =====================================================
PRINT 'Creating DeliveryEvents...';

DECLARE @EventCount INT = 0;

-- Create events based on delivery status
INSERT INTO DeliveryEvents (Id, DeliveryId, EventType, FromStatus, ToStatus, ActorId, ActorType, Metadata, Timestamp)
SELECT
    NEWID(),
    d.Id,
    'CREATED',
    NULL,
    'PENDING',
    d.RequesterId,
    d.RequesterType,
    '{"source": "WEB"}',
    d.CreatedAt
FROM Deliveries d
WHERE NOT EXISTS (SELECT 1 FROM DeliveryEvents e WHERE e.DeliveryId = d.Id AND e.EventType = 'CREATED');

SET @EventCount = @EventCount + @@ROWCOUNT;

-- Add ASSIGNED events
INSERT INTO DeliveryEvents (Id, DeliveryId, EventType, FromStatus, ToStatus, ActorId, ActorType, Metadata, Timestamp)
SELECT
    NEWID(),
    d.Id,
    'ASSIGNED',
    'PENDING',
    'ASSIGNED',
    d.AssignedDPId,
    'DP',
    '{"matchType": "AUTO"}',
    DATEADD(MINUTE, 5, d.CreatedAt)
FROM Deliveries d
WHERE d.AssignedDPId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM DeliveryEvents e WHERE e.DeliveryId = d.Id AND e.EventType = 'ASSIGNED');

SET @EventCount = @EventCount + @@ROWCOUNT;

-- Add ACCEPTED events for in-progress and completed deliveries
INSERT INTO DeliveryEvents (Id, DeliveryId, EventType, FromStatus, ToStatus, ActorId, ActorType, Metadata, Timestamp)
SELECT
    NEWID(),
    d.Id,
    'ACCEPTED',
    'ASSIGNED',
    'ACCEPTED',
    d.AssignedDPId,
    'DP',
    '{}',
    DATEADD(MINUTE, 8, d.CreatedAt)
FROM Deliveries d
WHERE d.Status IN ('ACCEPTED', 'EN_ROUTE_PICKUP', 'ARRIVED_PICKUP', 'PICKED_UP', 'EN_ROUTE_DROP', 'ARRIVED_DROP', 'DELIVERED', 'FAILED')
  AND d.AssignedDPId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM DeliveryEvents e WHERE e.DeliveryId = d.Id AND e.EventType = 'ACCEPTED');

SET @EventCount = @EventCount + @@ROWCOUNT;

-- Add PICKED_UP events
INSERT INTO DeliveryEvents (Id, DeliveryId, EventType, FromStatus, ToStatus, ActorId, ActorType, Metadata, Timestamp)
SELECT
    NEWID(),
    d.Id,
    'PICKED_UP',
    'EN_ROUTE_PICKUP',
    'PICKED_UP',
    d.AssignedDPId,
    'DP',
    '{}',
    DATEADD(MINUTE, 25, d.CreatedAt)
FROM Deliveries d
WHERE d.Status IN ('PICKED_UP', 'EN_ROUTE_DROP', 'ARRIVED_DROP', 'DELIVERED', 'FAILED')
  AND d.AssignedDPId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM DeliveryEvents e WHERE e.DeliveryId = d.Id AND e.EventType = 'PICKED_UP');

SET @EventCount = @EventCount + @@ROWCOUNT;

-- Add DELIVERED events
INSERT INTO DeliveryEvents (Id, DeliveryId, EventType, FromStatus, ToStatus, ActorId, ActorType, Metadata, Timestamp)
SELECT
    NEWID(),
    d.Id,
    'DELIVERED',
    'EN_ROUTE_DROP',
    'DELIVERED',
    d.AssignedDPId,
    'DP',
    '{"deliveredTo": "Recipient"}',
    DATEADD(MINUTE, CAST(d.EstimatedDurationMinutes AS INT) + 30, d.CreatedAt)
FROM Deliveries d
WHERE d.Status = 'DELIVERED'
  AND d.AssignedDPId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM DeliveryEvents e WHERE e.DeliveryId = d.Id AND e.EventType = 'DELIVERED');

SET @EventCount = @EventCount + @@ROWCOUNT;

-- Add CANCELLED events
INSERT INTO DeliveryEvents (Id, DeliveryId, EventType, FromStatus, ToStatus, ActorId, ActorType, Metadata, Timestamp)
SELECT
    NEWID(),
    d.Id,
    'CANCELLED',
    CASE WHEN d.AssignedDPId IS NOT NULL THEN 'ASSIGNED' ELSE 'PENDING' END,
    'CANCELLED',
    d.RequesterId,
    d.RequesterType,
    '{"reason": "' + ISNULL(d.CancellationReason, 'User cancelled') + '"}',
    ISNULL(d.CancelledAt, DATEADD(MINUTE, 15, d.CreatedAt))
FROM Deliveries d
WHERE d.Status = 'CANCELLED'
  AND NOT EXISTS (SELECT 1 FROM DeliveryEvents e WHERE e.DeliveryId = d.Id AND e.EventType = 'CANCELLED');

SET @EventCount = @EventCount + @@ROWCOUNT;

PRINT '  -> Created ' + CAST(@EventCount AS VARCHAR) + ' DeliveryEvents';
GO

-- =====================================================
-- 4.3 CREATE PROOF OF DELIVERIES
-- =====================================================
PRINT 'Creating ProofOfDeliveries...';

DECLARE @PODCount INT = 0;

INSERT INTO ProofOfDeliveries (Id, DeliveryId, SignatureUrl, PhotoUrl, ReceiverName, ReceiverRelation, Notes, CapturedAt, VerifiedBy, VerifiedAt, CreatedAt)
SELECT
    NEWID(),
    d.Id,
    '/signatures/delivery_' + CAST(ROW_NUMBER() OVER (ORDER BY d.Id) AS NVARCHAR) + '.png',
    '/photos/delivery_' + CAST(ROW_NUMBER() OVER (ORDER BY d.Id) AS NVARCHAR) + '.jpg',
    CASE ROW_NUMBER() OVER (ORDER BY d.Id) % 4
        WHEN 0 THEN 'Self' WHEN 1 THEN 'Family Member' WHEN 2 THEN 'Neighbor' ELSE 'Security Guard'
    END,
    CASE ROW_NUMBER() OVER (ORDER BY d.Id) % 4
        WHEN 0 THEN 'Self' WHEN 1 THEN 'Spouse' WHEN 2 THEN 'Neighbor' ELSE 'Building Security'
    END,
    CASE ROW_NUMBER() OVER (ORDER BY d.Id) % 5 WHEN 0 THEN 'Package in good condition' ELSE NULL END,
    DATEADD(MINUTE, CAST(d.EstimatedDurationMinutes AS INT) + 28, d.CreatedAt),
    NULL,
    NULL,
    DATEADD(MINUTE, CAST(d.EstimatedDurationMinutes AS INT) + 30, d.CreatedAt)
FROM Deliveries d
WHERE d.Status = 'DELIVERED'
  AND NOT EXISTS (SELECT 1 FROM ProofOfDeliveries p WHERE p.DeliveryId = d.Id);

SET @PODCount = @@ROWCOUNT;
PRINT '  -> Created ' + CAST(@PODCount AS VARCHAR) + ' ProofOfDeliveries';
GO

-- =====================================================
-- 4.4 CREATE RATINGS
-- =====================================================
PRINT 'Creating Ratings...';

DECLARE @RatingCount INT = 0;

-- Ratings from customers for DPs (for delivered orders)
INSERT INTO Ratings (Id, DeliveryId, RaterId, RaterType, TargetId, TargetType, Score, Tags, Comment, IsAnonymous, CreatedAt)
SELECT
    NEWID(),
    d.Id,
    d.RequesterId,
    d.RequesterType,
    d.AssignedDPId,
    'DP',
    CASE
        WHEN ROW_NUMBER() OVER (ORDER BY d.Id) % 10 = 0 THEN 2  -- 10% poor
        WHEN ROW_NUMBER() OVER (ORDER BY d.Id) % 5 = 0 THEN 3  -- 10% average
        WHEN ROW_NUMBER() OVER (ORDER BY d.Id) % 3 = 0 THEN 4  -- 20% good
        ELSE 5  -- 60% excellent
    END,
    CASE
        WHEN ROW_NUMBER() OVER (ORDER BY d.Id) % 3 = 0 THEN '["fast","polite"]'
        WHEN ROW_NUMBER() OVER (ORDER BY d.Id) % 3 = 1 THEN '["careful","professional"]'
        ELSE NULL
    END,
    CASE
        WHEN ROW_NUMBER() OVER (ORDER BY d.Id) % 4 = 0 THEN 'Great service!'
        WHEN ROW_NUMBER() OVER (ORDER BY d.Id) % 4 = 1 THEN 'Delivered on time'
        WHEN ROW_NUMBER() OVER (ORDER BY d.Id) % 4 = 2 THEN 'Good experience'
        ELSE NULL
    END,
    CASE ROW_NUMBER() OVER (ORDER BY d.Id) % 10 WHEN 0 THEN 1 ELSE 0 END,
    DATEADD(HOUR, 1, d.UpdatedAt)
FROM Deliveries d
WHERE d.Status = 'DELIVERED'
  AND d.AssignedDPId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM Ratings r WHERE r.DeliveryId = d.Id AND r.RaterId = d.RequesterId);

SET @RatingCount = @@ROWCOUNT;
PRINT '  -> Created ' + CAST(@RatingCount AS VARCHAR) + ' Ratings';
GO

-- =====================================================
-- SUMMARY
-- =====================================================
PRINT '';
PRINT '====================================';
PRINT 'STEP 4 COMPLETE: Deliveries Summary';
PRINT '====================================';

SELECT Status, COUNT(*) AS Count
FROM Deliveries
GROUP BY Status
ORDER BY
    CASE Status
        WHEN 'PENDING' THEN 1 WHEN 'MATCHING' THEN 2 WHEN 'ASSIGNED' THEN 3
        WHEN 'ACCEPTED' THEN 4 WHEN 'EN_ROUTE_PICKUP' THEN 5 WHEN 'PICKED_UP' THEN 6
        WHEN 'EN_ROUTE_DROP' THEN 7 WHEN 'DELIVERED' THEN 8 WHEN 'CANCELLED' THEN 9 ELSE 10
    END;

SELECT 'DeliveryEvents' AS Entity, COUNT(*) AS Count FROM DeliveryEvents
UNION ALL SELECT 'ProofOfDeliveries', COUNT(*) FROM ProofOfDeliveries
UNION ALL SELECT 'Ratings', COUNT(*) FROM Ratings;
GO
