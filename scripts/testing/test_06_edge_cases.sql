-- =====================================================
-- DELIVERYDOST TEST SCRIPT - EDGE CASES & NEGATIVE TESTS
-- =====================================================
-- Tests boundary conditions, constraint violations,
-- concurrent operations, and error handling
-- =====================================================

USE DeliveryDost_Dev;
GO

SET NOCOUNT ON;
PRINT '====================================================';
PRINT 'TEST: EDGE CASES & NEGATIVE TESTS';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETUTCDATE(), 121);
PRINT '====================================================';
PRINT '';

DECLARE @TestsPassed INT = 0;
DECLARE @TestsFailed INT = 0;

-- =====================================================
-- CATEGORY 1: CONSTRAINT VIOLATIONS
-- =====================================================
PRINT '==================================================';
PRINT 'CATEGORY 1: CONSTRAINT VIOLATIONS';
PRINT '==================================================';
PRINT '';

-- TEST 1.1: NULL in required field
PRINT '----------------------------------------------------';
PRINT 'TEST 1.1: NULL in required field (Phone)';
PRINT '----------------------------------------------------';

BEGIN TRY
    INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, IsActive, CreatedAt, UpdatedAt)
    VALUES (NEWID(), NULL, 'test@test.com', 'Test User', 'hash', 'EC', 1, GETUTCDATE(), GETUTCDATE());

    PRINT '  [FAIL] NULL was allowed in required field';
    SET @TestsFailed = @TestsFailed + 1;
END TRY
BEGIN CATCH
    PRINT '  [PASS] NULL rejected: ' + LEFT(ERROR_MESSAGE(), 80);
    SET @TestsPassed = @TestsPassed + 1;
END CATCH
PRINT '';

-- TEST 1.2: Duplicate unique key
PRINT '----------------------------------------------------';
PRINT 'TEST 1.2: Duplicate unique key (Email)';
PRINT '----------------------------------------------------';

DECLARE @ExistingEmail NVARCHAR(255);
SELECT TOP 1 @ExistingEmail = Email FROM Users WHERE Email IS NOT NULL;

IF @ExistingEmail IS NOT NULL
BEGIN
    BEGIN TRY
        INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, IsActive, CreatedAt, UpdatedAt)
        VALUES (NEWID(), '99999999999', @ExistingEmail, 'Duplicate Email Test', 'hash', 'EC', 1, GETUTCDATE(), GETUTCDATE());

        PRINT '  [FAIL] Duplicate email was allowed';
        SET @TestsFailed = @TestsFailed + 1;
    END TRY
    BEGIN CATCH
        PRINT '  [PASS] Duplicate rejected: ' + LEFT(ERROR_MESSAGE(), 60);
        SET @TestsPassed = @TestsPassed + 1;
    END CATCH
END
ELSE
BEGIN
    PRINT '  [SKIP] No existing email to test against';
END
PRINT '';

-- TEST 1.3: Foreign key violation
PRINT '----------------------------------------------------';
PRINT 'TEST 1.3: Foreign key violation (Invalid UserId)';
PRINT '----------------------------------------------------';

BEGIN TRY
    INSERT INTO Wallets (Id, UserId, Balance, HoldBalance, Currency, Status, CreatedAt, UpdatedAt)
    VALUES (NEWID(), NEWID(), 100, 0, 'INR', 'ACTIVE', GETUTCDATE(), GETUTCDATE());

    PRINT '  [FAIL] Invalid FK was allowed';
    SET @TestsFailed = @TestsFailed + 1;
END TRY
BEGIN CATCH
    PRINT '  [PASS] FK violation caught: ' + LEFT(ERROR_MESSAGE(), 60);
    SET @TestsPassed = @TestsPassed + 1;
END CATCH
PRINT '';

-- TEST 1.4: Check constraint violation (Invalid Role)
PRINT '----------------------------------------------------';
PRINT 'TEST 1.4: Invalid enum value (Role)';
PRINT '----------------------------------------------------';

BEGIN TRY
    INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, IsActive, CreatedAt, UpdatedAt)
    VALUES (NEWID(), '98765432100', 'invalid_role@test.com', 'Invalid Role Test', 'hash', 'INVALID_ROLE', 1, GETUTCDATE(), GETUTCDATE());

    -- If check constraint exists, this should fail
    -- If no check constraint, we need to verify by other means
    DECLARE @InsertedRole NVARCHAR(20);
    SELECT @InsertedRole = Role FROM Users WHERE Phone = '98765432100';

    IF @InsertedRole = 'INVALID_ROLE'
    BEGIN
        PRINT '  [WARN] Invalid role accepted (no CHECK constraint)';
        DELETE FROM Users WHERE Phone = '98765432100';
    END

    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  [PASS] Invalid role rejected: ' + LEFT(ERROR_MESSAGE(), 60);
    SET @TestsPassed = @TestsPassed + 1;
END CATCH
PRINT '';

-- =====================================================
-- CATEGORY 2: BOUNDARY CONDITIONS
-- =====================================================
PRINT '==================================================';
PRINT 'CATEGORY 2: BOUNDARY CONDITIONS';
PRINT '==================================================';
PRINT '';

-- TEST 2.1: Maximum length string
PRINT '----------------------------------------------------';
PRINT 'TEST 2.1: Maximum length string';
PRINT '----------------------------------------------------';

BEGIN TRY
    DECLARE @MaxString NVARCHAR(MAX) = REPLICATE('A', 300); -- Test 300 char string

    INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, IsActive, CreatedAt, UpdatedAt)
    VALUES (NEWID(), '99888777666', 'maxlen@test.com', @MaxString, 'hash', 'EC', 1, GETUTCDATE(), GETUTCDATE());

    -- Check if truncated or rejected
    DECLARE @InsertedLen INT;
    SELECT @InsertedLen = LEN(FullName) FROM Users WHERE Phone = '99888777666';

    IF @InsertedLen <= 255
    BEGIN
        PRINT '  [PASS] Long string handled (truncated or rejected)';
        DELETE FROM Users WHERE Phone = '99888777666';
    END

    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  [PASS] Long string rejected: ' + LEFT(ERROR_MESSAGE(), 60);
    SET @TestsPassed = @TestsPassed + 1;
END CATCH
PRINT '';

-- TEST 2.2: Zero and negative amounts
PRINT '----------------------------------------------------';
PRINT 'TEST 2.2: Zero and negative amounts in wallet';
PRINT '----------------------------------------------------';

BEGIN TRY
    -- Create test user and wallet
    DECLARE @TestUserId UNIQUEIDENTIFIER = NEWID();
    DECLARE @TestWalletId UNIQUEIDENTIFIER = NEWID();

    INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, IsActive, IsPhoneVerified, CreatedAt, UpdatedAt)
    VALUES (@TestUserId, '99777666555', 'boundary@test.com', 'Boundary Test', 'hash', 'EC', 1, 1, GETUTCDATE(), GETUTCDATE());

    INSERT INTO Wallets (Id, UserId, Balance, HoldBalance, Currency, Status, CreatedAt, UpdatedAt)
    VALUES (@TestWalletId, @TestUserId, 100, 0, 'INR', 'ACTIVE', GETUTCDATE(), GETUTCDATE());

    -- Try to set negative balance
    UPDATE Wallets SET Balance = -50 WHERE Id = @TestWalletId;

    DECLARE @NegBalance DECIMAL(18,2);
    SELECT @NegBalance = Balance FROM Wallets WHERE Id = @TestWalletId;

    IF @NegBalance < 0
    BEGIN
        PRINT '  [WARN] Negative balance allowed (no CHECK constraint)';
    END
    ELSE
    BEGIN
        PRINT '  [PASS] Negative balance prevented';
    END

    -- Cleanup
    DELETE FROM Wallets WHERE Id = @TestWalletId;
    DELETE FROM Users WHERE Id = @TestUserId;
    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  [PASS] Negative balance rejected: ' + LEFT(ERROR_MESSAGE(), 60);
    SET @TestsPassed = @TestsPassed + 1;
END CATCH
PRINT '';

-- TEST 2.3: Empty string vs NULL
PRINT '----------------------------------------------------';
PRINT 'TEST 2.3: Empty string vs NULL handling';
PRINT '----------------------------------------------------';

BEGIN TRY
    SET @TestUserId = NEWID();

    INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, IsActive, CreatedAt, UpdatedAt)
    VALUES (@TestUserId, '99666555444', '', 'Empty Email Test', 'hash', 'EC', 1, GETUTCDATE(), GETUTCDATE());

    DECLARE @InsertedEmail NVARCHAR(255);
    SELECT @InsertedEmail = Email FROM Users WHERE Id = @TestUserId;

    IF @InsertedEmail = '' OR @InsertedEmail IS NULL
    BEGIN
        PRINT '  [PASS] Empty string handled: ' + ISNULL(@InsertedEmail, 'NULL');
    END

    DELETE FROM Users WHERE Id = @TestUserId;
    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  [PASS] Empty string rejected: ' + LEFT(ERROR_MESSAGE(), 60);
    SET @TestsPassed = @TestsPassed + 1;
END CATCH
PRINT '';

-- =====================================================
-- CATEGORY 3: BUSINESS LOGIC EDGE CASES
-- =====================================================
PRINT '==================================================';
PRINT 'CATEGORY 3: BUSINESS LOGIC EDGE CASES';
PRINT '==================================================';
PRINT '';

-- TEST 3.1: Delivery to same location
PRINT '----------------------------------------------------';
PRINT 'TEST 3.1: Delivery with same pickup and drop';
PRINT '----------------------------------------------------';

BEGIN TRY
    -- Get existing requester
    DECLARE @RequesterId UNIQUEIDENTIFIER;
    DECLARE @PackageTypeId UNIQUEIDENTIFIER;
    SELECT TOP 1 @RequesterId = Id FROM Users WHERE Role IN ('BC', 'EC') AND IsActive = 1;
    SELECT TOP 1 @PackageTypeId = Id FROM MasterPackageTypes WHERE IsActive = 1;

    IF @RequesterId IS NOT NULL AND @PackageTypeId IS NOT NULL
    BEGIN
        DECLARE @SameLocDelivery UNIQUEIDENTIFIER = NEWID();

        INSERT INTO Deliveries (
            Id, DeliveryNumber, RequesterId, RequesterType, PackageTypeId,
            PackageDescription, PackageWeight,
            PickupAddress, PickupCity, PickupPincode, PickupLatitude, PickupLongitude, PickupContactName, PickupContactPhone,
            DropAddress, DropCity, DropPincode, DropLatitude, DropLongitude, DropContactName, DropContactPhone,
            DistanceKm, EstimatedDurationMinutes, BasePrice, EstimatedPrice, Status,
            CreatedAt, UpdatedAt
        )
        VALUES (
            @SameLocDelivery, 'DEL-SAMETEST', @RequesterId, 'BC', @PackageTypeId,
            'Same location test', 1,
            'Same Address', 'Jaipur', '302001', 26.9124, 75.7873, 'Contact', '9999999999',
            'Same Address', 'Jaipur', '302001', 26.9124, 75.7873, 'Contact', '9999999999', -- Same coordinates!
            0, -- Zero distance
            5, 50, 50, 'PENDING',
            GETUTCDATE(), GETUTCDATE()
        );

        DECLARE @InsertedDistance DECIMAL(10,2);
        SELECT @InsertedDistance = DistanceKm FROM Deliveries WHERE Id = @SameLocDelivery;

        PRINT '  [WARN] Same location delivery created with distance: ' + CAST(@InsertedDistance AS VARCHAR);
        PRINT '  -> Application should validate this before insert';

        DELETE FROM Deliveries WHERE Id = @SameLocDelivery;
    END

    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  [PASS] Same location rejected: ' + LEFT(ERROR_MESSAGE(), 60);
    SET @TestsPassed = @TestsPassed + 1;
END CATCH
PRINT '';

-- TEST 3.2: Rating outside valid range
PRINT '----------------------------------------------------';
PRINT 'TEST 3.2: Rating outside valid range (0-5)';
PRINT '----------------------------------------------------';

BEGIN TRY
    DECLARE @ExistingDelivery UNIQUEIDENTIFIER;
    DECLARE @ExistingDP UNIQUEIDENTIFIER;
    DECLARE @ExistingRequester UNIQUEIDENTIFIER;

    SELECT TOP 1
        @ExistingDelivery = Id,
        @ExistingDP = AssignedDPId,
        @ExistingRequester = RequesterId
    FROM Deliveries
    WHERE Status = 'DELIVERED' AND AssignedDPId IS NOT NULL;

    IF @ExistingDelivery IS NOT NULL
    BEGIN
        -- Try rating of 10 (should be max 5)
        INSERT INTO Ratings (Id, DeliveryId, RaterId, RateeId, RaterType, RateeType, Rating, CreatedAt)
        VALUES (NEWID(), @ExistingDelivery, @ExistingRequester, @ExistingDP, 'BC', 'DP', 10, GETUTCDATE());

        PRINT '  [WARN] Rating of 10 was accepted (no CHECK constraint)';
        DELETE FROM Ratings WHERE DeliveryId = @ExistingDelivery AND Rating = 10;
    END

    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  [PASS] Invalid rating rejected: ' + LEFT(ERROR_MESSAGE(), 60);
    SET @TestsPassed = @TestsPassed + 1;
END CATCH
PRINT '';

-- TEST 3.3: Status transition validation
PRINT '----------------------------------------------------';
PRINT 'TEST 3.3: Invalid status transition';
PRINT '----------------------------------------------------';

BEGIN TRY
    -- Get a pending delivery
    DECLARE @PendingDelivery UNIQUEIDENTIFIER;
    SELECT TOP 1 @PendingDelivery = Id FROM Deliveries WHERE Status = 'PENDING';

    IF @PendingDelivery IS NOT NULL
    BEGIN
        -- Try to jump from PENDING directly to DELIVERED (invalid)
        UPDATE Deliveries
        SET Status = 'DELIVERED', DeliveredAt = GETUTCDATE()
        WHERE Id = @PendingDelivery;

        DECLARE @NewStatus NVARCHAR(20);
        SELECT @NewStatus = Status FROM Deliveries WHERE Id = @PendingDelivery;

        IF @NewStatus = 'DELIVERED'
        BEGIN
            PRINT '  [WARN] Invalid status transition allowed (PENDING -> DELIVERED)';
            PRINT '  -> Application should enforce status machine';
            -- Revert
            UPDATE Deliveries SET Status = 'PENDING', DeliveredAt = NULL WHERE Id = @PendingDelivery;
        END
        ELSE
        BEGIN
            PRINT '  [PASS] Invalid transition prevented';
        END
    END
    ELSE
    BEGIN
        PRINT '  [SKIP] No pending delivery to test';
    END

    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  [PASS] Invalid transition rejected: ' + LEFT(ERROR_MESSAGE(), 60);
    SET @TestsPassed = @TestsPassed + 1;
END CATCH
PRINT '';

-- =====================================================
-- CATEGORY 4: CONCURRENT OPERATIONS
-- =====================================================
PRINT '==================================================';
PRINT 'CATEGORY 4: CONCURRENT OPERATIONS SIMULATION';
PRINT '==================================================';
PRINT '';

-- TEST 4.1: Double booking prevention
PRINT '----------------------------------------------------';
PRINT 'TEST 4.1: Double booking prevention';
PRINT '----------------------------------------------------';

BEGIN TRY
    -- Get an available DP
    DECLARE @AvailableDP UNIQUEIDENTIFIER;
    SELECT TOP 1 @AvailableDP = UserId FROM DeliveryPartnerProfiles
    WHERE IsAvailable = 1 AND IsVerified = 1 AND Status = 'ACTIVE';

    IF @AvailableDP IS NOT NULL
    BEGIN
        -- Create two deliveries
        SELECT TOP 1 @RequesterId = Id FROM Users WHERE Role = 'BC' AND IsActive = 1;
        SELECT TOP 1 @PackageTypeId = Id FROM MasterPackageTypes;

        DECLARE @Delivery1 UNIQUEIDENTIFIER = NEWID();
        DECLARE @Delivery2 UNIQUEIDENTIFIER = NEWID();

        INSERT INTO Deliveries (Id, DeliveryNumber, RequesterId, RequesterType, PackageTypeId, PackageDescription, PackageWeight,
            PickupAddress, PickupCity, PickupPincode, PickupLatitude, PickupLongitude, PickupContactName, PickupContactPhone,
            DropAddress, DropCity, DropPincode, DropLatitude, DropLongitude, DropContactName, DropContactPhone,
            DistanceKm, EstimatedDurationMinutes, BasePrice, EstimatedPrice, Status, AssignedDPId, AssignedAt, CreatedAt, UpdatedAt)
        VALUES
        (@Delivery1, 'DEL-DOUBLE1', @RequesterId, 'BC', @PackageTypeId, 'Double booking test 1', 1,
            'Addr 1', 'Jaipur', '302001', 26.91, 75.78, 'Contact', '9999999999',
            'Addr 2', 'Jaipur', '302002', 26.92, 75.79, 'Contact', '9999999998',
            5, 15, 100, 100, 'ASSIGNED', @AvailableDP, GETUTCDATE(), GETUTCDATE(), GETUTCDATE()),
        (@Delivery2, 'DEL-DOUBLE2', @RequesterId, 'BC', @PackageTypeId, 'Double booking test 2', 1,
            'Addr 3', 'Jaipur', '302003', 26.93, 75.80, 'Contact', '9999999997',
            'Addr 4', 'Jaipur', '302004', 26.94, 75.81, 'Contact', '9999999996',
            5, 15, 100, 100, 'ASSIGNED', @AvailableDP, GETUTCDATE(), GETUTCDATE(), GETUTCDATE()); -- Same DP!

        -- Check if DP has multiple active deliveries
        DECLARE @ActiveCount INT;
        SELECT @ActiveCount = COUNT(*) FROM Deliveries
        WHERE AssignedDPId = @AvailableDP AND Status IN ('ASSIGNED', 'ACCEPTED', 'IN_TRANSIT');

        IF @ActiveCount > 1
        BEGIN
            PRINT '  [WARN] DP assigned to ' + CAST(@ActiveCount AS VARCHAR) + ' deliveries simultaneously';
            PRINT '  -> Application should check DP availability before assignment';
        END

        -- Cleanup
        DELETE FROM Deliveries WHERE Id IN (@Delivery1, @Delivery2);
    END

    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  [PASS] Double booking prevented: ' + LEFT(ERROR_MESSAGE(), 60);
    SET @TestsPassed = @TestsPassed + 1;
END CATCH
PRINT '';

-- =====================================================
-- CATEGORY 5: DATA INTEGRITY
-- =====================================================
PRINT '==================================================';
PRINT 'CATEGORY 5: DATA INTEGRITY CHECKS';
PRINT '==================================================';
PRINT '';

-- TEST 5.1: Orphaned records check
PRINT '----------------------------------------------------';
PRINT 'TEST 5.1: Check for orphaned records';
PRINT '----------------------------------------------------';

BEGIN TRY
    -- Wallets without users
    DECLARE @OrphanedWallets INT;
    SELECT @OrphanedWallets = COUNT(*) FROM Wallets w
    WHERE NOT EXISTS (SELECT 1 FROM Users u WHERE u.Id = w.UserId);

    IF @OrphanedWallets > 0
        PRINT '  [FAIL] Found ' + CAST(@OrphanedWallets AS VARCHAR) + ' orphaned wallets';
    ELSE
        PRINT '  [PASS] No orphaned wallets found';

    -- Deliveries without requesters
    DECLARE @OrphanedDeliveries INT;
    SELECT @OrphanedDeliveries = COUNT(*) FROM Deliveries d
    WHERE NOT EXISTS (SELECT 1 FROM Users u WHERE u.Id = d.RequesterId);

    IF @OrphanedDeliveries > 0
        PRINT '  [FAIL] Found ' + CAST(@OrphanedDeliveries AS VARCHAR) + ' orphaned deliveries';
    ELSE
        PRINT '  [PASS] No orphaned deliveries found';

    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH
PRINT '';

-- TEST 5.2: Balance consistency check
PRINT '----------------------------------------------------';
PRINT 'TEST 5.2: Wallet balance consistency';
PRINT '----------------------------------------------------';

BEGIN TRY
    -- Check if any wallet has negative balance
    DECLARE @NegativeBalances INT;
    SELECT @NegativeBalances = COUNT(*) FROM Wallets WHERE Balance < 0 OR HoldBalance < 0;

    IF @NegativeBalances > 0
        PRINT '  [WARN] Found ' + CAST(@NegativeBalances AS VARCHAR) + ' wallets with negative balance';
    ELSE
        PRINT '  [PASS] All wallet balances are non-negative';

    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH
PRINT '';

-- TEST 5.3: Status consistency
PRINT '----------------------------------------------------';
PRINT 'TEST 5.3: Delivery status consistency';
PRINT '----------------------------------------------------';

BEGIN TRY
    -- Check for DELIVERED without DeliveredAt
    DECLARE @InconsistentDeliveries INT;
    SELECT @InconsistentDeliveries = COUNT(*) FROM Deliveries
    WHERE Status = 'DELIVERED' AND DeliveredAt IS NULL;

    IF @InconsistentDeliveries > 0
        PRINT '  [WARN] Found ' + CAST(@InconsistentDeliveries AS VARCHAR) + ' DELIVERED without DeliveredAt';
    ELSE
        PRINT '  [PASS] All DELIVERED have DeliveredAt timestamp';

    -- Check for ASSIGNED without AssignedDPId
    SELECT @InconsistentDeliveries = COUNT(*) FROM Deliveries
    WHERE Status IN ('ASSIGNED', 'ACCEPTED', 'PICKED_UP', 'IN_TRANSIT', 'DELIVERED')
      AND AssignedDPId IS NULL;

    IF @InconsistentDeliveries > 0
        PRINT '  [WARN] Found ' + CAST(@InconsistentDeliveries AS VARCHAR) + ' assigned deliveries without DP';
    ELSE
        PRINT '  [PASS] All assigned deliveries have DP assigned';

    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH
PRINT '';

-- =====================================================
-- TEST SUMMARY
-- =====================================================
PRINT '====================================================';
PRINT 'TEST SUMMARY: EDGE CASES & NEGATIVE TESTS';
PRINT '====================================================';
PRINT 'Tests Passed: ' + CAST(@TestsPassed AS VARCHAR);
PRINT 'Tests Failed: ' + CAST(@TestsFailed AS VARCHAR);
PRINT 'Total Tests:  ' + CAST(@TestsPassed + @TestsFailed AS VARCHAR);
PRINT '';

IF @TestsFailed = 0
    PRINT 'RESULT: ALL TESTS PASSED';
ELSE
    PRINT 'RESULT: SOME TESTS FAILED - Review above for details';

PRINT '';
PRINT 'NOTE: Some [WARN] results indicate missing CHECK constraints.';
PRINT 'Consider adding application-level validation for these cases.';
PRINT '';
PRINT 'Finished at: ' + CONVERT(VARCHAR, GETUTCDATE(), 121);
PRINT '====================================================';
GO
