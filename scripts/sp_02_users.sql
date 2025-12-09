-- =====================================================
-- DELIVERYDOST STORED PROCEDURES - USERS MODULE
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- =====================================================
-- PROCEDURE: Insert User
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_User_Insert')
    DROP PROCEDURE usp_User_Insert;
GO

CREATE PROCEDURE usp_User_Insert
    @Phone VARCHAR(15) = NULL,
    @Email NVARCHAR(255) = NULL,
    @FullName NVARCHAR(255) = NULL,
    @PasswordHash NVARCHAR(500) = NULL,
    @Role NVARCHAR(20),
    @IsPhoneVerified BIT = 0,
    @IsEmailVerified BIT = 0,
    @NewUserId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        -- Validate: At least phone or email required
        IF @Phone IS NULL AND @Email IS NULL
        BEGIN
            RAISERROR('Either Phone or Email is required', 16, 1);
            RETURN;
        END

        -- Check for duplicate phone
        IF @Phone IS NOT NULL AND EXISTS (SELECT 1 FROM Users WHERE Phone = @Phone)
        BEGIN
            RAISERROR('Phone number already registered', 16, 1);
            RETURN;
        END

        -- Check for duplicate email
        IF @Email IS NOT NULL AND EXISTS (SELECT 1 FROM Users WHERE Email = @Email)
        BEGIN
            RAISERROR('Email already registered', 16, 1);
            RETURN;
        END

        SET @NewUserId = NEWID();

        INSERT INTO Users (
            Id, Phone, Email, FullName, PasswordHash, Role,
            Is2FAEnabled, IsActive, IsPhoneVerified, IsEmailVerified,
            FailedLoginAttempts, CreatedAt, UpdatedAt
        )
        VALUES (
            @NewUserId, @Phone, @Email, @FullName, @PasswordHash, @Role,
            0, 1, @IsPhoneVerified, @IsEmailVerified,
            0, GETUTCDATE(), GETUTCDATE()
        );

        -- Return the created user
        SELECT
            Id, Phone, Email, FullName, Role,
            IsActive, IsPhoneVerified, IsEmailVerified,
            CreatedAt
        FROM Users
        WHERE Id = @NewUserId;

    END TRY
    BEGIN CATCH
        EXEC usp_LogError @ErrorContext = 'usp_User_Insert';
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_User_Insert';

-- =====================================================
-- PROCEDURE: Update User
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_User_Update')
    DROP PROCEDURE usp_User_Update;
GO

CREATE PROCEDURE usp_User_Update
    @UserId UNIQUEIDENTIFIER,
    @FullName NVARCHAR(255) = NULL,
    @Email NVARCHAR(255) = NULL,
    @IsPhoneVerified BIT = NULL,
    @IsEmailVerified BIT = NULL,
    @Is2FAEnabled BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Check user exists
        IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = @UserId)
        BEGIN
            RAISERROR('User not found', 16, 1);
            RETURN;
        END

        -- Check email uniqueness if changing
        IF @Email IS NOT NULL AND EXISTS (
            SELECT 1 FROM Users WHERE Email = @Email AND Id <> @UserId
        )
        BEGIN
            RAISERROR('Email already in use', 16, 1);
            RETURN;
        END

        UPDATE Users
        SET
            FullName = ISNULL(@FullName, FullName),
            Email = ISNULL(@Email, Email),
            IsPhoneVerified = ISNULL(@IsPhoneVerified, IsPhoneVerified),
            IsEmailVerified = ISNULL(@IsEmailVerified, IsEmailVerified),
            Is2FAEnabled = ISNULL(@Is2FAEnabled, Is2FAEnabled),
            UpdatedAt = GETUTCDATE()
        WHERE Id = @UserId;

        -- Return updated user
        SELECT
            Id, Phone, Email, FullName, Role,
            IsActive, IsPhoneVerified, IsEmailVerified, Is2FAEnabled,
            LastLoginAt, CreatedAt, UpdatedAt
        FROM Users
        WHERE Id = @UserId;

    END TRY
    BEGIN CATCH
        EXEC usp_LogError @ErrorContext = 'usp_User_Update', @UserId = @UserId;
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_User_Update';

-- =====================================================
-- PROCEDURE: Get User By ID
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_User_GetById')
    DROP PROCEDURE usp_User_GetById;
GO

CREATE PROCEDURE usp_User_GetById
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.Id,
        u.Phone,
        u.Email,
        u.FullName,
        u.Role,
        u.Is2FAEnabled,
        u.IsActive,
        u.IsPhoneVerified,
        u.IsEmailVerified,
        u.LastLoginAt,
        u.FailedLoginAttempts,
        u.LockedUntil,
        u.CreatedAt,
        u.UpdatedAt,
        -- Include wallet balance if exists
        w.Balance AS WalletBalance,
        w.HoldBalance AS WalletHoldBalance
    FROM Users u
    LEFT JOIN Wallets w ON w.UserId = u.Id AND w.IsActive = 1
    WHERE u.Id = @UserId;
END
GO

PRINT 'Created usp_User_GetById';

-- =====================================================
-- PROCEDURE: Get User By Phone
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_User_GetByPhone')
    DROP PROCEDURE usp_User_GetByPhone;
GO

CREATE PROCEDURE usp_User_GetByPhone
    @Phone VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id, Phone, Email, FullName, Role,
        PasswordHash, Is2FAEnabled, TotpSecret,
        IsActive, IsPhoneVerified, IsEmailVerified,
        LastLoginAt, FailedLoginAttempts, LockedUntil,
        CreatedAt, UpdatedAt
    FROM Users
    WHERE Phone = @Phone;
END
GO

PRINT 'Created usp_User_GetByPhone';

-- =====================================================
-- PROCEDURE: Get User By Email
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_User_GetByEmail')
    DROP PROCEDURE usp_User_GetByEmail;
GO

CREATE PROCEDURE usp_User_GetByEmail
    @Email NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id, Phone, Email, FullName, Role,
        PasswordHash, Is2FAEnabled, TotpSecret,
        IsActive, IsPhoneVerified, IsEmailVerified,
        LastLoginAt, FailedLoginAttempts, LockedUntil,
        CreatedAt, UpdatedAt
    FROM Users
    WHERE Email = @Email;
END
GO

PRINT 'Created usp_User_GetByEmail';

-- =====================================================
-- PROCEDURE: List Users (with pagination and filters)
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_User_List')
    DROP PROCEDURE usp_User_List;
GO

CREATE PROCEDURE usp_User_List
    @PageNumber INT = 1,
    @PageSize INT = 20,
    @Role NVARCHAR(20) = NULL,
    @IsActive BIT = NULL,
    @SearchTerm NVARCHAR(100) = NULL,
    @FromDate DATETIME2 = NULL,
    @ToDate DATETIME2 = NULL,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Calculate offset
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    -- Get total count
    SELECT @TotalCount = COUNT(*)
    FROM Users u
    WHERE (@Role IS NULL OR u.Role = @Role)
      AND (@IsActive IS NULL OR u.IsActive = @IsActive)
      AND (@SearchTerm IS NULL OR
           u.FullName LIKE '%' + @SearchTerm + '%' OR
           u.Phone LIKE '%' + @SearchTerm + '%' OR
           u.Email LIKE '%' + @SearchTerm + '%')
      AND (@FromDate IS NULL OR u.CreatedAt >= @FromDate)
      AND (@ToDate IS NULL OR u.CreatedAt <= @ToDate);

    -- Get paged results
    SELECT
        u.Id,
        u.Phone,
        u.Email,
        u.FullName,
        u.Role,
        u.IsActive,
        u.IsPhoneVerified,
        u.IsEmailVerified,
        u.LastLoginAt,
        u.CreatedAt,
        u.UpdatedAt
    FROM Users u
    WHERE (@Role IS NULL OR u.Role = @Role)
      AND (@IsActive IS NULL OR u.IsActive = @IsActive)
      AND (@SearchTerm IS NULL OR
           u.FullName LIKE '%' + @SearchTerm + '%' OR
           u.Phone LIKE '%' + @SearchTerm + '%' OR
           u.Email LIKE '%' + @SearchTerm + '%')
      AND (@FromDate IS NULL OR u.CreatedAt >= @FromDate)
      AND (@ToDate IS NULL OR u.CreatedAt <= @ToDate)
    ORDER BY u.CreatedAt DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    -- Return total count as second result
    SELECT @TotalCount AS TotalCount;
END
GO

PRINT 'Created usp_User_List';

-- =====================================================
-- PROCEDURE: Deactivate User (Soft Delete)
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

        -- Deactivate user
        UPDATE Users
        SET IsActive = 0, UpdatedAt = GETUTCDATE()
        WHERE Id = @UserId;

        -- Log the action
        INSERT INTO AdminAuditLogs (
            Id, AdminId, Action, EntityType, EntityId,
            NewValues, IpAddress, CreatedAt
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

PRINT 'Created usp_User_Deactivate';

-- =====================================================
-- PROCEDURE: Activate User
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

        -- Log the action
        INSERT INTO AdminAuditLogs (
            Id, AdminId, Action, EntityType, EntityId,
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

PRINT 'Created usp_User_Activate';

-- =====================================================
-- PROCEDURE: Update Login Status
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

        IF @IsSuccess = 1
        BEGIN
            -- Successful login
            UPDATE Users
            SET
                LastLoginAt = GETUTCDATE(),
                FailedLoginAttempts = 0,
                LockedUntil = NULL,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @UserId;

            -- Log auth audit
            INSERT INTO AuthAuditLogs (Id, UserId, Action, IpAddress, UserAgent, IsSuccess, CreatedAt)
            VALUES (NEWID(), @UserId, 'LOGIN', @IpAddress, @UserAgent, 1, GETUTCDATE());
        END
        ELSE
        BEGIN
            -- Failed login
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

            -- Log auth audit
            INSERT INTO AuthAuditLogs (Id, UserId, Action, IpAddress, UserAgent, IsSuccess, FailureReason, CreatedAt)
            VALUES (NEWID(), @UserId, 'LOGIN', @IpAddress, @UserAgent, 0, 'Invalid credentials', GETUTCDATE());
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

PRINT 'Created usp_User_UpdateLoginStatus';

-- =====================================================
-- PROCEDURE: Check User Lock Status
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_User_CheckLockStatus')
    DROP PROCEDURE usp_User_CheckLockStatus;
GO

CREATE PROCEDURE usp_User_CheckLockStatus
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        CASE
            WHEN LockedUntil IS NOT NULL AND LockedUntil > GETUTCDATE()
            THEN 1
            ELSE 0
        END AS IsLocked,
        LockedUntil,
        FailedLoginAttempts,
        IsActive
    FROM Users
    WHERE Id = @UserId;
END
GO

PRINT 'Created usp_User_CheckLockStatus';

PRINT 'Users module: COMPLETE';
