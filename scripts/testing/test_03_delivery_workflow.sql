-- =====================================================
-- DELIVERYDOST TEST SCRIPT - DELIVERY WORKFLOW
-- =====================================================
-- Tests complete delivery lifecycle:
-- PENDING -> MATCHING -> ASSIGNED -> ACCEPTED ->
-- REACHED_PICKUP -> PICKED_UP -> IN_TRANSIT ->
-- REACHED_DROP -> DELIVERED
-- =====================================================

USE DeliveryDost_Dev;
GO

SET NOCOUNT ON;
PRINT '====================================================';
PRINT 'TEST: DELIVERY LIFECYCLE WORKFLOW';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETUTCDATE(), 121);
PRINT '====================================================';
PRINT '';

-- Test variables
DECLARE @DeliveryId UNIQUEIDENTIFIER = NEWID();
DECLARE @RequesterId UNIQUEIDENTIFIER;
DECLARE @DPId UNIQUEIDENTIFIER;
DECLARE @DPProfileId UNIQUEIDENTIFIER;
DECLARE @PackageTypeId UNIQUEIDENTIFIER;
DECLARE @CurrentStatus NVARCHAR(20);
DECLARE @EventCount INT;
DECLARE @TestsPassed INT = 0;
DECLARE @TestsFailed INT = 0;

-- =====================================================
-- SETUP: Get prerequisite data
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'SETUP: Getting prerequisite data';
PRINT '----------------------------------------------------';

-- Get a BC or EC user as requester
SELECT TOP 1 @RequesterId = Id FROM Users WHERE Role IN ('BC', 'EC') AND IsActive = 1;
IF @RequesterId IS NULL
BEGIN
    PRINT '  [ERROR] No active BC/EC user found. Run seed scripts first.';
    RETURN;
END
PRINT '  Requester ID: ' + CAST(@RequesterId AS VARCHAR(50));

-- Get an available DP
SELECT TOP 1 @DPId = dp.UserId, @DPProfileId = dp.Id
FROM DeliveryPartnerProfiles dp
WHERE dp.IsVerified = 1 AND dp.Status = 'ACTIVE';

IF @DPId IS NULL
BEGIN
    PRINT '  [ERROR] No verified DP found. Run seed scripts first.';
    RETURN;
END
PRINT '  DP ID: ' + CAST(@DPId AS VARCHAR(50));

-- Get a package type
SELECT TOP 1 @PackageTypeId = Id FROM MasterPackageTypes WHERE IsActive = 1;
IF @PackageTypeId IS NULL
BEGIN
    PRINT '  [ERROR] No package type found. Run seed scripts first.';
    RETURN;
END

PRINT '';

-- =====================================================
-- STEP 1: CREATE DELIVERY (PENDING)
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'STEP 1: CREATE DELIVERY (Status: PENDING)';
PRINT '----------------------------------------------------';

BEGIN TRY
    INSERT INTO Deliveries (
        Id, DeliveryNumber, RequesterId, RequesterType, PackageTypeId,
        PackageDescription, PackageWeight, PackageValue,
        PickupAddress, PickupCity, PickupState, PickupPincode,
        PickupLatitude, PickupLongitude, PickupContactName, PickupContactPhone,
        DropAddress, DropCity, DropState, DropPincode,
        DropLatitude, DropLongitude, DropContactName, DropContactPhone,
        DistanceKm, EstimatedDurationMinutes, BasePrice, SurgePriceMultiplier,
        EstimatedPrice, Status, Priority, IsFragile, RequiresOTP,
        CreatedAt, UpdatedAt
    )
    VALUES (
        @DeliveryId,
        'DEL-TEST-' + FORMAT(GETUTCDATE(), 'yyyyMMdd-HHmmss'),
        @RequesterId,
        'BC',
        @PackageTypeId,
        'Test Package for Workflow',
        2.5,
        500,
        '123 Test Pickup Street, Malviya Nagar', 'Jaipur', 'Rajasthan', '302017',
        26.8650, 75.8120, 'Pickup Contact', '9876543210',
        '456 Test Drop Street, C-Scheme', 'Jaipur', 'Rajasthan', '302001',
        26.9124, 75.7873, 'Drop Contact', '9876543211',
        8.5,
        25,
        150.00,
        1.0,
        150.00,
        'PENDING',
        'NORMAL',
        0,
        1,
        GETUTCDATE(),
        GETUTCDATE()
    );

    -- Create initial event
    INSERT INTO DeliveryEvents (Id, DeliveryId, Status, EventTime, Description, CreatedAt)
    VALUES (NEWID(), @DeliveryId, 'PENDING', GETUTCDATE(), 'Delivery request created', GETUTCDATE());

    SELECT @CurrentStatus = Status FROM Deliveries WHERE Id = @DeliveryId;

    IF @CurrentStatus = 'PENDING'
    BEGIN
        PRINT '  [PASS] Delivery created with PENDING status';
        PRINT '  -> Delivery ID: ' + CAST(@DeliveryId AS VARCHAR(50));
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Unexpected status: ' + @CurrentStatus;
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] Delivery creation failed: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
    RETURN;
END CATCH

PRINT '';

-- =====================================================
-- STEP 2: START MATCHING
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'STEP 2: START MATCHING (Status: MATCHING)';
PRINT '----------------------------------------------------';

BEGIN TRY
    UPDATE Deliveries
    SET Status = 'MATCHING', UpdatedAt = GETUTCDATE()
    WHERE Id = @DeliveryId;

    INSERT INTO DeliveryEvents (Id, DeliveryId, Status, EventTime, Description, CreatedAt)
    VALUES (NEWID(), @DeliveryId, 'MATCHING', GETUTCDATE(), 'Finding available delivery partners', GETUTCDATE());

    SELECT @CurrentStatus = Status FROM Deliveries WHERE Id = @DeliveryId;

    IF @CurrentStatus = 'MATCHING'
    BEGIN
        PRINT '  [PASS] Status changed to MATCHING';
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Status not updated correctly';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- STEP 3: ASSIGN DP
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'STEP 3: ASSIGN DP (Status: ASSIGNED)';
PRINT '----------------------------------------------------';

BEGIN TRY
    UPDATE Deliveries
    SET
        Status = 'ASSIGNED',
        AssignedDPId = @DPId,
        AssignedAt = GETUTCDATE(),
        UpdatedAt = GETUTCDATE()
    WHERE Id = @DeliveryId;

    INSERT INTO DeliveryEvents (Id, DeliveryId, Status, EventTime, Description, ActorId, CreatedAt)
    VALUES (NEWID(), @DeliveryId, 'ASSIGNED', GETUTCDATE(), 'Delivery partner assigned', @DPId, GETUTCDATE());

    SELECT @CurrentStatus = Status FROM Deliveries WHERE Id = @DeliveryId;
    DECLARE @AssignedDP UNIQUEIDENTIFIER;
    SELECT @AssignedDP = AssignedDPId FROM Deliveries WHERE Id = @DeliveryId;

    IF @CurrentStatus = 'ASSIGNED' AND @AssignedDP = @DPId
    BEGIN
        PRINT '  [PASS] DP assigned successfully';
        PRINT '  -> Assigned DP: ' + CAST(@DPId AS VARCHAR(50));
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Assignment failed';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- STEP 4: DP ACCEPTS DELIVERY
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'STEP 4: DP ACCEPTS DELIVERY (Status: ACCEPTED)';
PRINT '----------------------------------------------------';

BEGIN TRY
    UPDATE Deliveries
    SET
        Status = 'ACCEPTED',
        AcceptedAt = GETUTCDATE(),
        UpdatedAt = GETUTCDATE()
    WHERE Id = @DeliveryId;

    INSERT INTO DeliveryEvents (Id, DeliveryId, Status, EventTime, Description, ActorId, CreatedAt)
    VALUES (NEWID(), @DeliveryId, 'ACCEPTED', GETUTCDATE(), 'Delivery partner accepted the order', @DPId, GETUTCDATE());

    SELECT @CurrentStatus = Status FROM Deliveries WHERE Id = @DeliveryId;

    IF @CurrentStatus = 'ACCEPTED'
    BEGIN
        PRINT '  [PASS] DP accepted delivery';
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Accept failed';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- STEP 5: DP REACHES PICKUP
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'STEP 5: DP REACHES PICKUP (Status: REACHED_PICKUP)';
PRINT '----------------------------------------------------';

BEGIN TRY
    UPDATE Deliveries
    SET
        Status = 'REACHED_PICKUP',
        ReachedPickupAt = GETUTCDATE(),
        UpdatedAt = GETUTCDATE()
    WHERE Id = @DeliveryId;

    INSERT INTO DeliveryEvents (Id, DeliveryId, Status, EventTime, Description, ActorId, Latitude, Longitude, CreatedAt)
    VALUES (NEWID(), @DeliveryId, 'REACHED_PICKUP', GETUTCDATE(), 'Delivery partner reached pickup location',
            @DPId, 26.8650, 75.8120, GETUTCDATE());

    SELECT @CurrentStatus = Status FROM Deliveries WHERE Id = @DeliveryId;

    IF @CurrentStatus = 'REACHED_PICKUP'
    BEGIN
        PRINT '  [PASS] DP reached pickup location';
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Status update failed';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- STEP 6: PACKAGE PICKED UP
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'STEP 6: PACKAGE PICKED UP (Status: PICKED_UP)';
PRINT '----------------------------------------------------';

BEGIN TRY
    UPDATE Deliveries
    SET
        Status = 'PICKED_UP',
        PickedUpAt = GETUTCDATE(),
        UpdatedAt = GETUTCDATE()
    WHERE Id = @DeliveryId;

    INSERT INTO DeliveryEvents (Id, DeliveryId, Status, EventTime, Description, ActorId, CreatedAt)
    VALUES (NEWID(), @DeliveryId, 'PICKED_UP', GETUTCDATE(), 'Package collected from sender', @DPId, GETUTCDATE());

    SELECT @CurrentStatus = Status FROM Deliveries WHERE Id = @DeliveryId;

    IF @CurrentStatus = 'PICKED_UP'
    BEGIN
        PRINT '  [PASS] Package picked up';
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Pickup status failed';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- STEP 7: IN TRANSIT
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'STEP 7: IN TRANSIT (Status: IN_TRANSIT)';
PRINT '----------------------------------------------------';

BEGIN TRY
    UPDATE Deliveries
    SET
        Status = 'IN_TRANSIT',
        UpdatedAt = GETUTCDATE()
    WHERE Id = @DeliveryId;

    INSERT INTO DeliveryEvents (Id, DeliveryId, Status, EventTime, Description, ActorId, Latitude, Longitude, CreatedAt)
    VALUES (NEWID(), @DeliveryId, 'IN_TRANSIT', GETUTCDATE(), 'On the way to delivery location',
            @DPId, 26.8900, 75.8000, GETUTCDATE());

    SELECT @CurrentStatus = Status FROM Deliveries WHERE Id = @DeliveryId;

    IF @CurrentStatus = 'IN_TRANSIT'
    BEGIN
        PRINT '  [PASS] Package in transit';
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Transit status failed';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- STEP 8: REACHED DROP LOCATION
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'STEP 8: REACHED DROP (Status: REACHED_DROP)';
PRINT '----------------------------------------------------';

BEGIN TRY
    UPDATE Deliveries
    SET
        Status = 'REACHED_DROP',
        ReachedDropAt = GETUTCDATE(),
        UpdatedAt = GETUTCDATE()
    WHERE Id = @DeliveryId;

    INSERT INTO DeliveryEvents (Id, DeliveryId, Status, EventTime, Description, ActorId, Latitude, Longitude, CreatedAt)
    VALUES (NEWID(), @DeliveryId, 'REACHED_DROP', GETUTCDATE(), 'Delivery partner reached drop location',
            @DPId, 26.9124, 75.7873, GETUTCDATE());

    SELECT @CurrentStatus = Status FROM Deliveries WHERE Id = @DeliveryId;

    IF @CurrentStatus = 'REACHED_DROP'
    BEGIN
        PRINT '  [PASS] DP reached drop location';
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Drop arrival failed';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- STEP 9: DELIVER PACKAGE (OTP VERIFICATION)
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'STEP 9: DELIVERED (Status: DELIVERED)';
PRINT '----------------------------------------------------';

BEGIN TRY
    -- Generate OTP for verification
    DECLARE @OTP VARCHAR(6) = RIGHT('000000' + CAST(ABS(CHECKSUM(NEWID())) % 1000000 AS VARCHAR), 6);

    UPDATE Deliveries
    SET
        Status = 'DELIVERED',
        DeliveredAt = GETUTCDATE(),
        FinalPrice = EstimatedPrice,
        OTPVerified = 1,
        UpdatedAt = GETUTCDATE()
    WHERE Id = @DeliveryId;

    INSERT INTO DeliveryEvents (Id, DeliveryId, Status, EventTime, Description, ActorId, CreatedAt)
    VALUES (NEWID(), @DeliveryId, 'DELIVERED', GETUTCDATE(), 'Package delivered successfully. OTP verified.', @DPId, GETUTCDATE());

    -- Create Proof of Delivery
    INSERT INTO ProofOfDeliveries (Id, DeliveryId, PhotoUrl, ReceiverName, ReceiverRelation, OTPVerified, DeliveredAt, CreatedAt)
    VALUES (NEWID(), @DeliveryId, '/proofs/' + CAST(@DeliveryId AS NVARCHAR(50)) + '/delivery.jpg',
            'Test Receiver', 'Self', 1, GETUTCDATE(), GETUTCDATE());

    SELECT @CurrentStatus = Status FROM Deliveries WHERE Id = @DeliveryId;

    IF @CurrentStatus = 'DELIVERED'
    BEGIN
        PRINT '  [PASS] Package delivered successfully';
        PRINT '  -> OTP Verified: Yes';
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Delivery completion failed';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- STEP 10: ADD RATING
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'STEP 10: ADD RATING';
PRINT '----------------------------------------------------';

BEGIN TRY
    INSERT INTO Ratings (Id, DeliveryId, RaterId, RateeId, RaterType, RateeType, Rating, Review, CreatedAt)
    VALUES (NEWID(), @DeliveryId, @RequesterId, @DPId, 'BC', 'DP', 5, 'Excellent service! Very prompt delivery.', GETUTCDATE());

    DECLARE @RatingCount INT;
    SELECT @RatingCount = COUNT(*) FROM Ratings WHERE DeliveryId = @DeliveryId;

    IF @RatingCount = 1
    BEGIN
        PRINT '  [PASS] Rating added successfully';
        PRINT '  -> Rating: 5 stars';
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Rating not added';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- VERIFY: Check Event Trail
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'VERIFY: DELIVERY EVENT TRAIL';
PRINT '----------------------------------------------------';

SELECT @EventCount = COUNT(*) FROM DeliveryEvents WHERE DeliveryId = @DeliveryId;

PRINT '  Total Events: ' + CAST(@EventCount AS VARCHAR);
PRINT '';
PRINT '  Event Timeline:';

SELECT
    ROW_NUMBER() OVER (ORDER BY EventTime) AS Step,
    Status,
    FORMAT(EventTime, 'HH:mm:ss') AS Time,
    Description
FROM DeliveryEvents
WHERE DeliveryId = @DeliveryId
ORDER BY EventTime;

PRINT '';

-- =====================================================
-- VERIFY: Final Delivery State
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'VERIFY: FINAL DELIVERY STATE';
PRINT '----------------------------------------------------';

SELECT
    d.DeliveryNumber,
    d.Status,
    d.FinalPrice,
    d.OTPVerified,
    FORMAT(d.CreatedAt, 'HH:mm:ss') AS Created,
    FORMAT(d.AcceptedAt, 'HH:mm:ss') AS Accepted,
    FORMAT(d.PickedUpAt, 'HH:mm:ss') AS PickedUp,
    FORMAT(d.DeliveredAt, 'HH:mm:ss') AS Delivered,
    DATEDIFF(MINUTE, d.CreatedAt, d.DeliveredAt) AS TotalMinutes
FROM Deliveries d
WHERE d.Id = @DeliveryId;

PRINT '';

-- =====================================================
-- CLEANUP
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'CLEANUP: Removing test data';
PRINT '----------------------------------------------------';

BEGIN TRY
    DELETE FROM Ratings WHERE DeliveryId = @DeliveryId;
    DELETE FROM ProofOfDeliveries WHERE DeliveryId = @DeliveryId;
    DELETE FROM DeliveryEvents WHERE DeliveryId = @DeliveryId;
    DELETE FROM Deliveries WHERE Id = @DeliveryId;
    PRINT '  Test data cleaned up successfully';
END TRY
BEGIN CATCH
    PRINT '  [WARN] Cleanup failed: ' + ERROR_MESSAGE();
END CATCH

PRINT '';

-- =====================================================
-- TEST SUMMARY
-- =====================================================
PRINT '====================================================';
PRINT 'TEST SUMMARY: DELIVERY WORKFLOW';
PRINT '====================================================';
PRINT 'Tests Passed: ' + CAST(@TestsPassed AS VARCHAR);
PRINT 'Tests Failed: ' + CAST(@TestsFailed AS VARCHAR);
PRINT 'Total Tests:  ' + CAST(@TestsPassed + @TestsFailed AS VARCHAR);
PRINT '';

IF @TestsFailed = 0
    PRINT 'RESULT: ALL TESTS PASSED - Full delivery lifecycle verified';
ELSE
    PRINT 'RESULT: SOME TESTS FAILED - Review above for details';

PRINT '';
PRINT 'Finished at: ' + CONVERT(VARCHAR, GETUTCDATE(), 121);
PRINT '====================================================';
GO
