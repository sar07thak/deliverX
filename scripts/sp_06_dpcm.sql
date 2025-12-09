-- =====================================================
-- DELIVERYDOST STORED PROCEDURES - DPCM MODULE
-- (Delivery Partner Channel Manager)
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- =====================================================
-- PROCEDURE: Insert DPCM
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DPCM_Insert')
    DROP PROCEDURE usp_DPCM_Insert;
GO

CREATE PROCEDURE usp_DPCM_Insert
    @UserId UNIQUEIDENTIFIER,
    @OrganizationName NVARCHAR(255),
    @ContactPersonName NVARCHAR(255),
    @PAN NVARCHAR(20),
    @ServiceRegions NVARCHAR(MAX) = NULL, -- JSON array
    @CommissionType NVARCHAR(20) = 'PERCENTAGE', -- PERCENTAGE, FLAT, HYBRID
    @CommissionValue DECIMAL(10,2) = NULL,
    @MinCommissionAmount DECIMAL(10,2) = NULL,
    @SecurityDeposit DECIMAL(18,2) = 0,
    @NewDPCMId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        -- Check user exists
        IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = @UserId)
        BEGIN
            RAISERROR('User not found', 16, 1);
            RETURN;
        END

        -- Check if DPCM profile already exists
        IF EXISTS (SELECT 1 FROM DPCManagers WHERE UserId = @UserId)
        BEGIN
            RAISERROR('DPCM profile already exists for this user', 16, 1);
            RETURN;
        END

        -- Validate PAN uniqueness
        IF EXISTS (SELECT 1 FROM DPCManagers WHERE PAN = @PAN)
        BEGIN
            RAISERROR('PAN already registered', 16, 1);
            RETURN;
        END

        SET @NewDPCMId = NEWID();

        INSERT INTO DPCManagers (
            Id, UserId, OrganizationName, ContactPersonName, PAN,
            ServiceRegions, CommissionType, CommissionValue, MinCommissionAmount,
            SecurityDeposit, SecurityDepositStatus,
            IsActive, CreatedAt, UpdatedAt
        )
        VALUES (
            @NewDPCMId, @UserId, @OrganizationName, @ContactPersonName, @PAN,
            @ServiceRegions, @CommissionType, @CommissionValue, @MinCommissionAmount,
            @SecurityDeposit, CASE WHEN @SecurityDeposit > 0 THEN 'PENDING' ELSE NULL END,
            0, GETUTCDATE(), GETUTCDATE()
        );

        -- Update user role
        UPDATE Users SET Role = 'DPCM', UpdatedAt = GETUTCDATE() WHERE Id = @UserId;

        -- Create wallet for DPCM
        DECLARE @WalletId UNIQUEIDENTIFIER;
        EXEC usp_Wallet_Create
            @UserId = @UserId,
            @WalletType = 'DPCM',
            @NewWalletId = @WalletId OUTPUT;

        -- Return created DPCM
        SELECT
            d.Id, d.UserId, d.OrganizationName, d.ContactPersonName, d.PAN,
            d.ServiceRegions, d.CommissionType, d.CommissionValue, d.MinCommissionAmount,
            d.SecurityDeposit, d.SecurityDepositStatus,
            d.IsActive, d.CreatedAt, d.UpdatedAt,
            u.Phone, u.Email, u.FullName
        FROM DPCManagers d
        INNER JOIN Users u ON u.Id = d.UserId
        WHERE d.Id = @NewDPCMId;

    END TRY
    BEGIN CATCH
        EXEC usp_LogError @ErrorContext = 'usp_DPCM_Insert', @UserId = @UserId;
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_DPCM_Insert';

-- =====================================================
-- PROCEDURE: Update DPCM
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DPCM_Update')
    DROP PROCEDURE usp_DPCM_Update;
GO

CREATE PROCEDURE usp_DPCM_Update
    @DPCMId UNIQUEIDENTIFIER,
    @OrganizationName NVARCHAR(255) = NULL,
    @ContactPersonName NVARCHAR(255) = NULL,
    @ServiceRegions NVARCHAR(MAX) = NULL,
    @CommissionType NVARCHAR(20) = NULL,
    @CommissionValue DECIMAL(10,2) = NULL,
    @MinCommissionAmount DECIMAL(10,2) = NULL,
    @AgreementDocumentUrl NVARCHAR(500) = NULL,
    @AgreementVersion NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM DPCManagers WHERE Id = @DPCMId)
        BEGIN
            RAISERROR('DPCM not found', 16, 1);
            RETURN;
        END

        UPDATE DPCManagers
        SET
            OrganizationName = ISNULL(@OrganizationName, OrganizationName),
            ContactPersonName = ISNULL(@ContactPersonName, ContactPersonName),
            ServiceRegions = ISNULL(@ServiceRegions, ServiceRegions),
            CommissionType = ISNULL(@CommissionType, CommissionType),
            CommissionValue = ISNULL(@CommissionValue, CommissionValue),
            MinCommissionAmount = ISNULL(@MinCommissionAmount, MinCommissionAmount),
            AgreementDocumentUrl = ISNULL(@AgreementDocumentUrl, AgreementDocumentUrl),
            AgreementVersion = ISNULL(@AgreementVersion, AgreementVersion),
            AgreementSignedAt = CASE WHEN @AgreementDocumentUrl IS NOT NULL THEN GETUTCDATE() ELSE AgreementSignedAt END,
            UpdatedAt = GETUTCDATE()
        WHERE Id = @DPCMId;

        SELECT
            d.*, u.Phone, u.Email, u.FullName
        FROM DPCManagers d
        INNER JOIN Users u ON u.Id = d.UserId
        WHERE d.Id = @DPCMId;

    END TRY
    BEGIN CATCH
        EXEC usp_LogError @ErrorContext = 'usp_DPCM_Update';
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_DPCM_Update';

-- =====================================================
-- PROCEDURE: Get DPCM By ID
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DPCM_GetById')
    DROP PROCEDURE usp_DPCM_GetById;
GO

CREATE PROCEDURE usp_DPCM_GetById
    @DPCMId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    -- Main DPCM info
    SELECT
        d.Id, d.UserId, d.OrganizationName, d.ContactPersonName, d.PAN,
        d.RegistrationCertificateUrl, d.ServiceRegions,
        d.CommissionType, d.CommissionValue, d.MinCommissionAmount,
        d.BankAccountEncrypted, d.SecurityDeposit, d.SecurityDepositStatus,
        d.SecurityDepositReceivedAt, d.SecurityDepositTransactionRef,
        d.AgreementDocumentUrl, d.AgreementSignedAt, d.AgreementVersion,
        d.IsActive, d.ActivatedAt, d.CreatedAt, d.UpdatedAt,
        u.Phone, u.Email, u.FullName, u.IsPhoneVerified, u.IsEmailVerified
    FROM DPCManagers d
    INNER JOIN Users u ON u.Id = d.UserId
    WHERE d.Id = @DPCMId;

    -- Statistics
    SELECT
        (SELECT COUNT(*) FROM DeliveryPartnerProfiles WHERE DPCMId = @DPCMId) AS TotalDeliveryPartners,
        (SELECT COUNT(*) FROM DeliveryPartnerProfiles WHERE DPCMId = @DPCMId AND IsActive = 1) AS ActiveDeliveryPartners,
        (SELECT COUNT(*) FROM DeliveryPartnerProfiles WHERE DPCMId = @DPCMId AND IsOnline = 1) AS OnlineDeliveryPartners,
        (SELECT COUNT(*) FROM PincodeDPCMMappings WHERE DPCMId = @DPCMId AND IsActive = 1) AS AssignedPincodes;

    -- Wallet balance
    SELECT
        w.Balance, w.HoldBalance, w.Balance - w.HoldBalance AS AvailableBalance
    FROM Wallets w
    INNER JOIN DPCManagers d ON d.UserId = w.UserId
    WHERE d.Id = @DPCMId AND w.IsActive = 1;
END
GO

PRINT 'Created usp_DPCM_GetById';

-- =====================================================
-- PROCEDURE: Get DPCM By User ID
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DPCM_GetByUserId')
    DROP PROCEDURE usp_DPCM_GetByUserId;
GO

CREATE PROCEDURE usp_DPCM_GetByUserId
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DPCMId UNIQUEIDENTIFIER;
    SELECT @DPCMId = Id FROM DPCManagers WHERE UserId = @UserId;

    IF @DPCMId IS NOT NULL
        EXEC usp_DPCM_GetById @DPCMId = @DPCMId;
END
GO

PRINT 'Created usp_DPCM_GetByUserId';

-- =====================================================
-- PROCEDURE: List DPCMs (with pagination)
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DPCM_List')
    DROP PROCEDURE usp_DPCM_List;
GO

CREATE PROCEDURE usp_DPCM_List
    @PageNumber INT = 1,
    @PageSize INT = 20,
    @IsActive BIT = NULL,
    @SearchTerm NVARCHAR(100) = NULL,
    @ServiceRegion NVARCHAR(100) = NULL,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    -- Total count
    SELECT @TotalCount = COUNT(*)
    FROM DPCManagers d
    INNER JOIN Users u ON u.Id = d.UserId
    WHERE (@IsActive IS NULL OR d.IsActive = @IsActive)
      AND (@SearchTerm IS NULL OR
           d.OrganizationName LIKE '%' + @SearchTerm + '%' OR
           d.ContactPersonName LIKE '%' + @SearchTerm + '%' OR
           u.Phone LIKE '%' + @SearchTerm + '%' OR
           u.Email LIKE '%' + @SearchTerm + '%')
      AND (@ServiceRegion IS NULL OR d.ServiceRegions LIKE '%' + @ServiceRegion + '%');

    -- Paged results
    SELECT
        d.Id, d.UserId, d.OrganizationName, d.ContactPersonName,
        d.CommissionType, d.CommissionValue,
        d.SecurityDeposit, d.SecurityDepositStatus,
        d.IsActive, d.ActivatedAt, d.CreatedAt,
        u.Phone, u.Email, u.FullName,
        (SELECT COUNT(*) FROM DeliveryPartnerProfiles WHERE DPCMId = d.Id) AS DeliveryPartnerCount,
        (SELECT COUNT(*) FROM DeliveryPartnerProfiles WHERE DPCMId = d.Id AND IsActive = 1) AS ActiveDPCount,
        (SELECT COUNT(*) FROM PincodeDPCMMappings WHERE DPCMId = d.Id AND IsActive = 1) AS PincodeCount
    FROM DPCManagers d
    INNER JOIN Users u ON u.Id = d.UserId
    WHERE (@IsActive IS NULL OR d.IsActive = @IsActive)
      AND (@SearchTerm IS NULL OR
           d.OrganizationName LIKE '%' + @SearchTerm + '%' OR
           d.ContactPersonName LIKE '%' + @SearchTerm + '%' OR
           u.Phone LIKE '%' + @SearchTerm + '%' OR
           u.Email LIKE '%' + @SearchTerm + '%')
      AND (@ServiceRegion IS NULL OR d.ServiceRegions LIKE '%' + @ServiceRegion + '%')
    ORDER BY d.CreatedAt DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    SELECT @TotalCount AS TotalCount;
END
GO

PRINT 'Created usp_DPCM_List';

-- =====================================================
-- PROCEDURE: Activate DPCM
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

        -- Check DPCM exists
        IF NOT EXISTS (SELECT 1 FROM DPCManagers WHERE Id = @DPCMId)
        BEGIN
            RAISERROR('DPCM not found', 16, 1);
            RETURN;
        END

        -- Check security deposit received (if required)
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

        -- Audit log
        INSERT INTO AdminAuditLogs (
            Id, AdminId, Action, EntityType, EntityId, IpAddress, CreatedAt
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

PRINT 'Created usp_DPCM_Activate';

-- =====================================================
-- PROCEDURE: Update Security Deposit
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DPCM_UpdateSecurityDeposit')
    DROP PROCEDURE usp_DPCM_UpdateSecurityDeposit;
GO

CREATE PROCEDURE usp_DPCM_UpdateSecurityDeposit
    @DPCMId UNIQUEIDENTIFIER,
    @Status NVARCHAR(20), -- PENDING, RECEIVED, REFUNDED
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

        -- Audit log
        INSERT INTO AdminAuditLogs (
            Id, AdminId, Action, EntityType, EntityId,
            OldValues, NewValues, IpAddress, CreatedAt
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

PRINT 'Created usp_DPCM_UpdateSecurityDeposit';

-- =====================================================
-- PROCEDURE: Get DPCM's Delivery Partners
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
        dp.TotalDeliveries, dp.TotalEarnings,
        dp.AverageRating, dp.TotalRatings,
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

PRINT 'Created usp_DPCM_GetDeliveryPartners';

-- =====================================================
-- PROCEDURE: Assign Pincode to DPCM
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DPCM_AssignPincode')
    DROP PROCEDURE usp_DPCM_AssignPincode;
GO

CREATE PROCEDURE usp_DPCM_AssignPincode
    @DPCMId UNIQUEIDENTIFIER,
    @Pincode NVARCHAR(10),
    @StateName NVARCHAR(100) = NULL,
    @DistrictName NVARCHAR(100) = NULL,
    @AssignedBy UNIQUEIDENTIFIER,
    @NewMappingId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Check if pincode already assigned to another DPCM
        IF EXISTS (
            SELECT 1 FROM PincodeDPCMMappings
            WHERE Pincode = @Pincode AND IsActive = 1 AND DPCMId <> @DPCMId
        )
        BEGIN
            RAISERROR('Pincode already assigned to another DPCM', 16, 1);
            RETURN;
        END

        -- Check if already assigned to this DPCM
        IF EXISTS (
            SELECT 1 FROM PincodeDPCMMappings
            WHERE Pincode = @Pincode AND DPCMId = @DPCMId AND IsActive = 1
        )
        BEGIN
            SELECT @NewMappingId = Id
            FROM PincodeDPCMMappings
            WHERE Pincode = @Pincode AND DPCMId = @DPCMId AND IsActive = 1;

            SELECT * FROM PincodeDPCMMappings WHERE Id = @NewMappingId;
            COMMIT;
            RETURN;
        END

        SET @NewMappingId = NEWID();

        INSERT INTO PincodeDPCMMappings (
            Id, Pincode, DPCMId, StateName, DistrictName,
            IsActive, AssignedAt, AssignedByUserId
        )
        VALUES (
            @NewMappingId, @Pincode, @DPCMId, @StateName, @DistrictName,
            1, GETUTCDATE(), @AssignedBy
        );

        COMMIT;

        SELECT * FROM PincodeDPCMMappings WHERE Id = @NewMappingId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        EXEC usp_LogError @ErrorContext = 'usp_DPCM_AssignPincode';
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_DPCM_AssignPincode';

-- =====================================================
-- PROCEDURE: Remove Pincode from DPCM
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DPCM_RemovePincode')
    DROP PROCEDURE usp_DPCM_RemovePincode;
GO

CREATE PROCEDURE usp_DPCM_RemovePincode
    @MappingId UNIQUEIDENTIFIER,
    @Reason NVARCHAR(500) = NULL,
    @RemovedBy UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE PincodeDPCMMappings
    SET
        IsActive = 0,
        DeactivatedAt = GETUTCDATE(),
        DeactivationReason = @Reason
    WHERE Id = @MappingId;

    SELECT 1 AS Success, 'Pincode mapping removed' AS Message;
END
GO

PRINT 'Created usp_DPCM_RemovePincode';

-- =====================================================
-- PROCEDURE: Get DPCM by Pincode
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DPCM_GetByPincode')
    DROP PROCEDURE usp_DPCM_GetByPincode;
GO

CREATE PROCEDURE usp_DPCM_GetByPincode
    @Pincode NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.Id, d.UserId, d.OrganizationName, d.ContactPersonName,
        d.CommissionType, d.CommissionValue, d.MinCommissionAmount,
        d.IsActive,
        u.Phone, u.Email,
        p.StateName, p.DistrictName
    FROM PincodeDPCMMappings p
    INNER JOIN DPCManagers d ON d.Id = p.DPCMId
    INNER JOIN Users u ON u.Id = d.UserId
    WHERE p.Pincode = @Pincode
      AND p.IsActive = 1
      AND d.IsActive = 1;
END
GO

PRINT 'Created usp_DPCM_GetByPincode';

-- =====================================================
-- PROCEDURE: Get DPCM Pincodes
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DPCM_GetPincodes')
    DROP PROCEDURE usp_DPCM_GetPincodes;
GO

CREATE PROCEDURE usp_DPCM_GetPincodes
    @DPCMId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.Id, p.Pincode, p.StateName, p.DistrictName,
        p.IsActive, p.AssignedAt, p.DeactivatedAt, p.DeactivationReason
    FROM PincodeDPCMMappings p
    WHERE p.DPCMId = @DPCMId
    ORDER BY p.IsActive DESC, p.AssignedAt DESC;
END
GO

PRINT 'Created usp_DPCM_GetPincodes';

-- =====================================================
-- PROCEDURE: Calculate DPCM Commission for Delivery
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DPCM_CalculateCommission')
    DROP PROCEDURE usp_DPCM_CalculateCommission;
GO

CREATE PROCEDURE usp_DPCM_CalculateCommission
    @DPCMId UNIQUEIDENTIFIER,
    @DeliveryAmount DECIMAL(18,2),
    @Commission DECIMAL(18,2) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CommissionType NVARCHAR(20);
    DECLARE @CommissionValue DECIMAL(10,2);
    DECLARE @MinCommissionAmount DECIMAL(10,2);

    SELECT
        @CommissionType = CommissionType,
        @CommissionValue = CommissionValue,
        @MinCommissionAmount = MinCommissionAmount
    FROM DPCManagers
    WHERE Id = @DPCMId AND IsActive = 1;

    IF @CommissionType IS NULL
    BEGIN
        SET @Commission = 0;
        RETURN;
    END

    IF @CommissionType = 'PERCENTAGE'
    BEGIN
        SET @Commission = @DeliveryAmount * (@CommissionValue / 100);
    END
    ELSE IF @CommissionType = 'FLAT'
    BEGIN
        SET @Commission = @CommissionValue;
    END
    ELSE IF @CommissionType = 'HYBRID'
    BEGIN
        -- Higher of percentage or minimum
        DECLARE @PercentageCommission DECIMAL(18,2) = @DeliveryAmount * (@CommissionValue / 100);
        SET @Commission = CASE
            WHEN @PercentageCommission > @MinCommissionAmount THEN @PercentageCommission
            ELSE @MinCommissionAmount
        END;
    END
    ELSE
    BEGIN
        SET @Commission = 0;
    END
END
GO

PRINT 'Created usp_DPCM_CalculateCommission';

-- =====================================================
-- PROCEDURE: DPCM Earnings Report
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_DPCM_GetEarningsReport')
    DROP PROCEDURE usp_DPCM_GetEarningsReport;
GO

CREATE PROCEDURE usp_DPCM_GetEarningsReport
    @DPCMId UNIQUEIDENTIFIER,
    @FromDate DATE,
    @ToDate DATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DPCMUserId UNIQUEIDENTIFIER;
    SELECT @DPCMUserId = UserId FROM DPCManagers WHERE Id = @DPCMId;

    -- Summary
    SELECT
        COUNT(*) AS TotalDeliveries,
        SUM(c.DeliveryAmount) AS TotalDeliveryAmount,
        SUM(c.DPCMCommission) AS TotalCommission,
        SUM(CASE WHEN c.Status = 'SETTLED' THEN c.DPCMCommission ELSE 0 END) AS SettledCommission,
        SUM(CASE WHEN c.Status = 'PENDING' THEN c.DPCMCommission ELSE 0 END) AS PendingCommission
    FROM CommissionRecords c
    WHERE c.DPCMId = @DPCMUserId
      AND c.CreatedAt >= @FromDate
      AND c.CreatedAt < DATEADD(DAY, 1, @ToDate);

    -- Daily breakdown
    SELECT
        CAST(c.CreatedAt AS DATE) AS Date,
        COUNT(*) AS DeliveryCount,
        SUM(c.DPCMCommission) AS Commission
    FROM CommissionRecords c
    WHERE c.DPCMId = @DPCMUserId
      AND c.CreatedAt >= @FromDate
      AND c.CreatedAt < DATEADD(DAY, 1, @ToDate)
    GROUP BY CAST(c.CreatedAt AS DATE)
    ORDER BY Date;

    -- DP-wise breakdown
    SELECT
        dp.FullName AS DeliveryPartnerName,
        COUNT(*) AS DeliveryCount,
        SUM(c.DeliveryAmount) AS TotalDeliveryAmount,
        SUM(c.DPCMCommission) AS Commission
    FROM CommissionRecords c
    INNER JOIN DeliveryPartnerProfiles dp ON dp.UserId = c.DPId
    WHERE c.DPCMId = @DPCMUserId
      AND c.CreatedAt >= @FromDate
      AND c.CreatedAt < DATEADD(DAY, 1, @ToDate)
    GROUP BY dp.FullName
    ORDER BY Commission DESC;
END
GO

PRINT 'Created usp_DPCM_GetEarningsReport';

PRINT 'DPCM module: COMPLETE';
