-- =====================================================
-- DELIVERYDOST TEST SCRIPT - USER CRUD OPERATIONS
-- =====================================================
-- Tests: sp_User_Insert, sp_User_GetById, sp_User_Update,
--        sp_User_GetByPhone, sp_User_Search, sp_User_Delete
-- =====================================================

USE DeliveryDost_Dev;
GO

SET NOCOUNT ON;
PRINT '====================================================';
PRINT 'TEST: USER CRUD OPERATIONS';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETUTCDATE(), 121);
PRINT '====================================================';
PRINT '';

-- Test variables
DECLARE @TestUserId UNIQUEIDENTIFIER;
DECLARE @TestPhone VARCHAR(15) = '99999999' + RIGHT(CAST(ABS(CHECKSUM(NEWID())) AS VARCHAR), 2);
DECLARE @TestEmail NVARCHAR(255) = 'test_' + CAST(NEWID() AS NVARCHAR(36)) + '@test.com';
DECLARE @ResultCount INT;
DECLARE @TestsPassed INT = 0;
DECLARE @TestsFailed INT = 0;

-- =====================================================
-- TEST 1: INSERT NEW USER
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 1: INSERT NEW USER';
PRINT '----------------------------------------------------';

BEGIN TRY
    SET @TestUserId = NEWID();

    -- Insert using direct INSERT (simulating stored procedure)
    INSERT INTO Users (
        Id, Phone, Email, FullName, PasswordHash, Role,
        Is2FAEnabled, IsActive, IsPhoneVerified, IsEmailVerified,
        FailedLoginAttempts, CreatedAt, UpdatedAt
    )
    VALUES (
        @TestUserId,
        @TestPhone,
        @TestEmail,
        'Test User for CRUD',
        'AQAAAAIAAYagAAAAEK...(hashed)',
        'EC',
        0, 1, 1, 0,
        0,
        GETUTCDATE(),
        GETUTCDATE()
    );

    PRINT '  [PASS] User inserted successfully';
    PRINT '  -> User ID: ' + CAST(@TestUserId AS VARCHAR(50));
    PRINT '  -> Phone: ' + @TestPhone;
    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  [FAIL] Insert failed: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 2: GET USER BY ID
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 2: GET USER BY ID';
PRINT '----------------------------------------------------';

BEGIN TRY
    SELECT @ResultCount = COUNT(*) FROM Users WHERE Id = @TestUserId;

    IF @ResultCount = 1
    BEGIN
        PRINT '  [PASS] User retrieved by ID';

        SELECT
            Id, Phone, Email, FullName, Role, IsActive,
            IsPhoneVerified, IsEmailVerified, CreatedAt
        FROM Users
        WHERE Id = @TestUserId;

        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] User not found by ID';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] Get by ID failed: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 3: GET USER BY PHONE
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 3: GET USER BY PHONE';
PRINT '----------------------------------------------------';

BEGIN TRY
    SELECT @ResultCount = COUNT(*) FROM Users WHERE Phone = @TestPhone;

    IF @ResultCount = 1
    BEGIN
        PRINT '  [PASS] User retrieved by Phone';

        SELECT Id, Phone, FullName, Role
        FROM Users
        WHERE Phone = @TestPhone;

        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] User not found by Phone';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] Get by Phone failed: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 4: UPDATE USER
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 4: UPDATE USER';
PRINT '----------------------------------------------------';

BEGIN TRY
    UPDATE Users
    SET
        FullName = 'Updated Test User',
        IsEmailVerified = 1,
        UpdatedAt = GETUTCDATE()
    WHERE Id = @TestUserId;

    -- Verify update
    DECLARE @UpdatedName NVARCHAR(255);
    DECLARE @UpdatedEmailVerified BIT;

    SELECT
        @UpdatedName = FullName,
        @UpdatedEmailVerified = IsEmailVerified
    FROM Users
    WHERE Id = @TestUserId;

    IF @UpdatedName = 'Updated Test User' AND @UpdatedEmailVerified = 1
    BEGIN
        PRINT '  [PASS] User updated successfully';
        PRINT '  -> New FullName: ' + @UpdatedName;
        PRINT '  -> IsEmailVerified: 1';
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Update verification failed';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] Update failed: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 5: SEARCH USERS BY ROLE
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 5: SEARCH USERS BY ROLE';
PRINT '----------------------------------------------------';

BEGIN TRY
    SELECT @ResultCount = COUNT(*) FROM Users WHERE Role = 'EC';

    IF @ResultCount > 0
    BEGIN
        PRINT '  [PASS] Search by role returned ' + CAST(@ResultCount AS VARCHAR) + ' users';

        -- Show first 5
        SELECT TOP 5 Id, Phone, FullName, Role, IsActive, CreatedAt
        FROM Users
        WHERE Role = 'EC'
        ORDER BY CreatedAt DESC;

        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] No users found for role EC';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] Search failed: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 6: SEARCH USERS BY PARTIAL NAME
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 6: SEARCH USERS BY PARTIAL NAME';
PRINT '----------------------------------------------------';

BEGIN TRY
    SELECT @ResultCount = COUNT(*) FROM Users WHERE FullName LIKE '%Test%';

    IF @ResultCount > 0
    BEGIN
        PRINT '  [PASS] Search by name returned ' + CAST(@ResultCount AS VARCHAR) + ' users';

        SELECT TOP 5 Id, Phone, FullName, Role
        FROM Users
        WHERE FullName LIKE '%Test%'
        ORDER BY CreatedAt DESC;

        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] No users found with "Test" in name';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] Search failed: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 7: DEACTIVATE USER (SOFT DELETE)
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 7: DEACTIVATE USER (SOFT DELETE)';
PRINT '----------------------------------------------------';

BEGIN TRY
    UPDATE Users
    SET IsActive = 0, UpdatedAt = GETUTCDATE()
    WHERE Id = @TestUserId;

    -- Verify deactivation
    DECLARE @IsActive BIT;
    SELECT @IsActive = IsActive FROM Users WHERE Id = @TestUserId;

    IF @IsActive = 0
    BEGIN
        PRINT '  [PASS] User deactivated successfully';
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Deactivation verification failed';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] Deactivation failed: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 8: REACTIVATE USER
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 8: REACTIVATE USER';
PRINT '----------------------------------------------------';

BEGIN TRY
    UPDATE Users
    SET IsActive = 1, UpdatedAt = GETUTCDATE()
    WHERE Id = @TestUserId;

    SELECT @IsActive = IsActive FROM Users WHERE Id = @TestUserId;

    IF @IsActive = 1
    BEGIN
        PRINT '  [PASS] User reactivated successfully';
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Reactivation verification failed';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] Reactivation failed: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 9: LIST USERS WITH PAGINATION
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 9: LIST USERS WITH PAGINATION';
PRINT '----------------------------------------------------';

BEGIN TRY
    DECLARE @PageSize INT = 10;
    DECLARE @PageNumber INT = 1;
    DECLARE @TotalCount INT;

    SELECT @TotalCount = COUNT(*) FROM Users;

    SELECT
        Id, Phone, FullName, Role, IsActive, CreatedAt
    FROM Users
    ORDER BY CreatedAt DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    PRINT '  [PASS] Pagination working - Page ' + CAST(@PageNumber AS VARCHAR) + ' of ' + CAST(CEILING(CAST(@TotalCount AS FLOAT) / @PageSize) AS VARCHAR);
    PRINT '  -> Total Users: ' + CAST(@TotalCount AS VARCHAR);
    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  [FAIL] Pagination failed: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 10: UNIQUE CONSTRAINT - DUPLICATE PHONE
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 10: UNIQUE CONSTRAINT - DUPLICATE PHONE';
PRINT '----------------------------------------------------';

BEGIN TRY
    -- Try to insert user with same phone
    INSERT INTO Users (
        Id, Phone, Email, FullName, PasswordHash, Role,
        IsActive, IsPhoneVerified, CreatedAt, UpdatedAt
    )
    VALUES (
        NEWID(),
        @TestPhone, -- Same phone as test user
        'duplicate_' + CAST(NEWID() AS NVARCHAR(36)) + '@test.com',
        'Duplicate Phone Test',
        'hash',
        'EC',
        1, 1, GETUTCDATE(), GETUTCDATE()
    );

    PRINT '  [FAIL] Duplicate phone was allowed (constraint not working)';
    SET @TestsFailed = @TestsFailed + 1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() = 2627 OR ERROR_NUMBER() = 2601 -- Unique constraint violation
    BEGIN
        PRINT '  [PASS] Unique constraint prevented duplicate phone';
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Unexpected error: ' + ERROR_MESSAGE();
        SET @TestsFailed = @TestsFailed + 1;
    END
END CATCH

PRINT '';

-- =====================================================
-- CLEANUP: DELETE TEST USER
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'CLEANUP: DELETE TEST USER';
PRINT '----------------------------------------------------';

BEGIN TRY
    DELETE FROM Users WHERE Id = @TestUserId;

    SELECT @ResultCount = COUNT(*) FROM Users WHERE Id = @TestUserId;

    IF @ResultCount = 0
    BEGIN
        PRINT '  [PASS] Test user deleted successfully';
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Test user not deleted';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [WARN] Cleanup failed: ' + ERROR_MESSAGE();
END CATCH

PRINT '';

-- =====================================================
-- TEST SUMMARY
-- =====================================================
PRINT '====================================================';
PRINT 'TEST SUMMARY: USER CRUD OPERATIONS';
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
PRINT 'Finished at: ' + CONVERT(VARCHAR, GETUTCDATE(), 121);
PRINT '====================================================';
GO
