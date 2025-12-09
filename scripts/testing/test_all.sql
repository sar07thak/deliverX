-- =====================================================
-- DELIVERYDOST - MASTER TEST RUNNER
-- =====================================================
-- Runs all test scripts in sequence
-- Reports overall pass/fail status
-- =====================================================

USE DeliveryDost_Dev;
GO

SET NOCOUNT ON;

PRINT '########################################################';
PRINT '#                                                      #';
PRINT '#     DELIVERYDOST - AUTOMATED TEST SUITE              #';
PRINT '#                                                      #';
PRINT '########################################################';
PRINT '';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETUTCDATE(), 121);
PRINT '';

-- =====================================================
-- PRE-TEST: VERIFY DATABASE CONNECTION
-- =====================================================
IF DB_NAME() != 'DeliveryDost_Dev'
BEGIN
    RAISERROR('ERROR: Must be connected to DeliveryDost_Dev database!', 16, 1);
    RETURN;
END

PRINT '====================================================';
PRINT 'PRE-TEST: DATABASE STATE';
PRINT '====================================================';

-- Show current data counts
SELECT
    'Users' AS Entity, COUNT(*) AS Count FROM Users
UNION ALL SELECT 'DPCManagers', COUNT(*) FROM DPCManagers
UNION ALL SELECT 'DeliveryPartnerProfiles', COUNT(*) FROM DeliveryPartnerProfiles
UNION ALL SELECT 'Deliveries', COUNT(*) FROM Deliveries
UNION ALL SELECT 'Wallets', COUNT(*) FROM Wallets
UNION ALL SELECT 'Complaints', COUNT(*) FROM Complaints
ORDER BY Entity;

PRINT '';

-- =====================================================
-- TEST EXECUTION INSTRUCTIONS
-- =====================================================
PRINT '====================================================';
PRINT 'TEST EXECUTION INSTRUCTIONS';
PRINT '====================================================';
PRINT '';
PRINT 'This master runner provides the structure for running tests.';
PRINT 'Execute each test file in sequence:';
PRINT '';
PRINT '  1. test_01_user_crud.sql        - User CRUD operations';
PRINT '  2. test_02_dp_crud.sql          - Delivery Partner CRUD';
PRINT '  3. test_03_delivery_workflow.sql - Delivery lifecycle';
PRINT '  4. test_04_wallet_workflow.sql  - Wallet operations';
PRINT '  5. test_05_complaint_workflow.sql - Complaint resolution';
PRINT '  6. test_06_edge_cases.sql       - Edge cases & validation';
PRINT '';
PRINT 'Or use SQLCMD mode with :r command:';
PRINT '  :r test_01_user_crud.sql';
PRINT '  :r test_02_dp_crud.sql';
PRINT '  etc.';
PRINT '';

-- =====================================================
-- QUICK SMOKE TESTS
-- =====================================================
PRINT '====================================================';
PRINT 'RUNNING QUICK SMOKE TESTS';
PRINT '====================================================';
PRINT '';

DECLARE @SmokeTestsPassed INT = 0;
DECLARE @SmokeTestsFailed INT = 0;

-- Smoke Test 1: Can create user
PRINT 'Smoke Test 1: Create User';
BEGIN TRY
    DECLARE @TestUserId UNIQUEIDENTIFIER = NEWID();
    DECLARE @TestPhone VARCHAR(15) = '99999' + RIGHT(CAST(ABS(CHECKSUM(NEWID())) AS VARCHAR), 5);

    INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, IsActive, IsPhoneVerified, CreatedAt, UpdatedAt)
    VALUES (@TestUserId, @TestPhone, 'smoke_test@test.com', 'Smoke Test User', 'hash', 'EC', 1, 1, GETUTCDATE(), GETUTCDATE());

    DELETE FROM Users WHERE Id = @TestUserId;
    PRINT '  [PASS]';
    SET @SmokeTestsPassed = @SmokeTestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @SmokeTestsFailed = @SmokeTestsFailed + 1;
END CATCH

-- Smoke Test 2: Can read users
PRINT 'Smoke Test 2: Read Users';
BEGIN TRY
    DECLARE @UserCount INT;
    SELECT @UserCount = COUNT(*) FROM Users;
    PRINT '  [PASS] Found ' + CAST(@UserCount AS VARCHAR) + ' users';
    SET @SmokeTestsPassed = @SmokeTestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @SmokeTestsFailed = @SmokeTestsFailed + 1;
END CATCH

-- Smoke Test 3: Can query deliveries with joins
PRINT 'Smoke Test 3: Complex Query (Deliveries with Joins)';
BEGIN TRY
    SELECT TOP 1
        d.Id, d.DeliveryNumber, d.Status,
        u.FullName AS RequesterName,
        dp.VehicleType
    FROM Deliveries d
    INNER JOIN Users u ON u.Id = d.RequesterId
    LEFT JOIN DeliveryPartnerProfiles dp ON dp.UserId = d.AssignedDPId;

    PRINT '  [PASS]';
    SET @SmokeTestsPassed = @SmokeTestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @SmokeTestsFailed = @SmokeTestsFailed + 1;
END CATCH

-- Smoke Test 4: Foreign keys are valid
PRINT 'Smoke Test 4: FK Integrity (Wallets -> Users)';
BEGIN TRY
    DECLARE @OrphanCount INT;
    SELECT @OrphanCount = COUNT(*) FROM Wallets w
    WHERE NOT EXISTS (SELECT 1 FROM Users u WHERE u.Id = w.UserId);

    IF @OrphanCount = 0
    BEGIN
        PRINT '  [PASS] No orphaned wallets';
        SET @SmokeTestsPassed = @SmokeTestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Found ' + CAST(@OrphanCount AS VARCHAR) + ' orphaned wallets';
        SET @SmokeTestsFailed = @SmokeTestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @SmokeTestsFailed = @SmokeTestsFailed + 1;
END CATCH

-- Smoke Test 5: Transactions work
PRINT 'Smoke Test 5: Transaction Support';
BEGIN TRY
    BEGIN TRANSACTION;

    -- Insert
    SET @TestUserId = NEWID();
    SET @TestPhone = '98888' + RIGHT(CAST(ABS(CHECKSUM(NEWID())) AS VARCHAR), 5);
    INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, IsActive, IsPhoneVerified, CreatedAt, UpdatedAt)
    VALUES (@TestUserId, @TestPhone, 'txn_test@test.com', 'Txn Test', 'hash', 'EC', 1, 1, GETUTCDATE(), GETUTCDATE());

    -- Rollback
    ROLLBACK TRANSACTION;

    -- Verify rollback
    DECLARE @RolledBack INT;
    SELECT @RolledBack = COUNT(*) FROM Users WHERE Id = @TestUserId;

    IF @RolledBack = 0
    BEGIN
        PRINT '  [PASS] Transaction rollback works';
        SET @SmokeTestsPassed = @SmokeTestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Rollback did not work';
        SET @SmokeTestsFailed = @SmokeTestsFailed + 1;
    END
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @SmokeTestsFailed = @SmokeTestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- SMOKE TEST SUMMARY
-- =====================================================
PRINT '====================================================';
PRINT 'SMOKE TEST SUMMARY';
PRINT '====================================================';
PRINT 'Passed: ' + CAST(@SmokeTestsPassed AS VARCHAR);
PRINT 'Failed: ' + CAST(@SmokeTestsFailed AS VARCHAR);
PRINT '';

IF @SmokeTestsFailed = 0
BEGIN
    PRINT '[SUCCESS] All smoke tests passed!';
    PRINT '';
    PRINT 'The database is ready for comprehensive testing.';
    PRINT 'Proceed with individual test scripts for detailed testing.';
END
ELSE
BEGIN
    PRINT '[WARNING] Some smoke tests failed!';
    PRINT '';
    PRINT 'Fix the issues above before running comprehensive tests.';
END

PRINT '';

-- =====================================================
-- STORED PROCEDURE VERIFICATION
-- =====================================================
PRINT '====================================================';
PRINT 'STORED PROCEDURE COUNT';
PRINT '====================================================';

DECLARE @SPCount INT;
SELECT @SPCount = COUNT(*) FROM sys.objects WHERE type = 'P';
PRINT 'Total Stored Procedures: ' + CAST(@SPCount AS VARCHAR);

DECLARE @FnCount INT;
SELECT @FnCount = COUNT(*) FROM sys.objects WHERE type IN ('FN', 'IF', 'TF');
PRINT 'Total Functions: ' + CAST(@FnCount AS VARCHAR);

PRINT '';

-- =====================================================
-- DATA DISTRIBUTION
-- =====================================================
PRINT '====================================================';
PRINT 'DATA DISTRIBUTION';
PRINT '====================================================';

-- Users by role
PRINT 'Users by Role:';
SELECT Role, COUNT(*) AS Count,
       SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS Active
FROM Users
GROUP BY Role
ORDER BY Role;

-- Deliveries by status
PRINT '';
PRINT 'Deliveries by Status:';
SELECT Status, COUNT(*) AS Count
FROM Deliveries
GROUP BY Status
ORDER BY
    CASE Status
        WHEN 'PENDING' THEN 1 WHEN 'MATCHING' THEN 2 WHEN 'ASSIGNED' THEN 3
        WHEN 'ACCEPTED' THEN 4 WHEN 'PICKED_UP' THEN 5 WHEN 'IN_TRANSIT' THEN 6
        WHEN 'DELIVERED' THEN 7 WHEN 'CANCELLED' THEN 8 WHEN 'FAILED' THEN 9
        ELSE 10
    END;

-- Complaints by status
PRINT '';
PRINT 'Complaints by Status:';
SELECT Status, COUNT(*) AS Count
FROM Complaints
GROUP BY Status;

PRINT '';

-- =====================================================
-- FINAL SUMMARY
-- =====================================================
PRINT '########################################################';
PRINT '#                                                      #';
PRINT '#     TEST RUNNER COMPLETE                             #';
PRINT '#                                                      #';
PRINT '########################################################';
PRINT '';
PRINT 'Finished at: ' + CONVERT(VARCHAR, GETUTCDATE(), 121);
PRINT '';
PRINT 'NEXT STEPS:';
PRINT '  1. If smoke tests passed, run individual test scripts';
PRINT '  2. Review test_01 through test_06 for comprehensive testing';
PRINT '  3. Run C# integration tests from Visual Studio / dotnet test';
PRINT '';
GO
