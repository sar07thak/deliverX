-- =====================================================
-- DELIVERYDOST STORED PROCEDURES - WALLET MODULE
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- =====================================================
-- SEQUENCE: Payment Numbers
-- =====================================================

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'seq_PaymentNumber')
BEGIN
    CREATE SEQUENCE seq_PaymentNumber
        AS INT
        START WITH 10001
        INCREMENT BY 1
        NO CACHE;
    PRINT 'Created seq_PaymentNumber';
END
GO

-- =====================================================
-- SEQUENCE: Settlement Numbers
-- =====================================================

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'seq_SettlementNumber')
BEGIN
    CREATE SEQUENCE seq_SettlementNumber
        AS INT
        START WITH 1001
        INCREMENT BY 1
        NO CACHE;
    PRINT 'Created seq_SettlementNumber';
END
GO

-- =====================================================
-- FUNCTION: Generate Payment Number
-- Format: PAY-YYYYMMDD-XXXXX
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'ufn_GeneratePaymentNumber' AND type = 'FN')
    DROP FUNCTION ufn_GeneratePaymentNumber;
GO

CREATE FUNCTION ufn_GeneratePaymentNumber ()
RETURNS NVARCHAR(20)
AS
BEGIN
    DECLARE @SeqNum INT = NEXT VALUE FOR seq_PaymentNumber;
    RETURN 'PAY-' + FORMAT(GETUTCDATE(), 'yyyyMMdd') + '-' + RIGHT('00000' + CAST(@SeqNum AS NVARCHAR), 5);
END
GO

PRINT 'Created ufn_GeneratePaymentNumber';

-- =====================================================
-- FUNCTION: Generate Settlement Number
-- Format: STL-YYYYMMDD-XXXX
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'ufn_GenerateSettlementNumber' AND type = 'FN')
    DROP FUNCTION ufn_GenerateSettlementNumber;
GO

CREATE FUNCTION ufn_GenerateSettlementNumber ()
RETURNS NVARCHAR(20)
AS
BEGIN
    DECLARE @SeqNum INT = NEXT VALUE FOR seq_SettlementNumber;
    RETURN 'STL-' + FORMAT(GETUTCDATE(), 'yyyyMMdd') + '-' + RIGHT('0000' + CAST(@SeqNum AS NVARCHAR), 4);
END
GO

PRINT 'Created ufn_GenerateSettlementNumber';

-- =====================================================
-- PROCEDURE: Create Wallet
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Wallet_Create')
    DROP PROCEDURE usp_Wallet_Create;
GO

CREATE PROCEDURE usp_Wallet_Create
    @UserId UNIQUEIDENTIFIER,
    @WalletType NVARCHAR(20), -- USER, DP, EC, BC, DPCM, PLATFORM
    @Currency NVARCHAR(3) = 'INR',
    @NewWalletId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        -- Check if wallet already exists for this user and type
        IF EXISTS (SELECT 1 FROM Wallets WHERE UserId = @UserId AND WalletType = @WalletType AND IsActive = 1)
        BEGIN
            SELECT @NewWalletId = Id FROM Wallets WHERE UserId = @UserId AND WalletType = @WalletType AND IsActive = 1;

            SELECT
                Id, UserId, WalletType, Balance, HoldBalance,
                Currency, IsActive, CreatedAt, UpdatedAt
            FROM Wallets
            WHERE Id = @NewWalletId;

            RETURN;
        END

        SET @NewWalletId = NEWID();

        INSERT INTO Wallets (
            Id, UserId, WalletType, Balance, HoldBalance,
            Currency, IsActive, CreatedAt, UpdatedAt
        )
        VALUES (
            @NewWalletId, @UserId, @WalletType, 0, 0,
            @Currency, 1, GETUTCDATE(), GETUTCDATE()
        );

        SELECT
            Id, UserId, WalletType, Balance, HoldBalance,
            Currency, IsActive, CreatedAt, UpdatedAt
        FROM Wallets
        WHERE Id = @NewWalletId;

    END TRY
    BEGIN CATCH
        EXEC usp_LogError @ErrorContext = 'usp_Wallet_Create', @UserId = @UserId;
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_Wallet_Create';

-- =====================================================
-- PROCEDURE: Get Wallet By User ID
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Wallet_GetByUserId')
    DROP PROCEDURE usp_Wallet_GetByUserId;
GO

CREATE PROCEDURE usp_Wallet_GetByUserId
    @UserId UNIQUEIDENTIFIER,
    @WalletType NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        w.Id,
        w.UserId,
        w.WalletType,
        w.Balance,
        w.HoldBalance,
        w.Balance - w.HoldBalance AS AvailableBalance,
        w.Currency,
        w.IsActive,
        w.CreatedAt,
        w.UpdatedAt,
        u.FullName AS UserName,
        u.Phone AS UserPhone
    FROM Wallets w
    INNER JOIN Users u ON u.Id = w.UserId
    WHERE w.UserId = @UserId
      AND (@WalletType IS NULL OR w.WalletType = @WalletType)
      AND w.IsActive = 1;
END
GO

PRINT 'Created usp_Wallet_GetByUserId';

-- =====================================================
-- PROCEDURE: Apply Wallet Transaction (Credit/Debit with idempotency)
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Wallet_ApplyTransaction')
    DROP PROCEDURE usp_Wallet_ApplyTransaction;
GO

CREATE PROCEDURE usp_Wallet_ApplyTransaction
    @WalletId UNIQUEIDENTIFIER,
    @TransactionType NVARCHAR(20), -- CREDIT, DEBIT, HOLD, RELEASE, REFUND
    @Category NVARCHAR(50), -- DELIVERY_PAYMENT, DELIVERY_EARNING, COMMISSION, etc.
    @Amount DECIMAL(18,2),
    @ReferenceId NVARCHAR(100) = NULL,
    @ReferenceType NVARCHAR(50) = NULL,
    @Description NVARCHAR(500) = NULL,
    @IdempotencyKey NVARCHAR(100) = NULL, -- For preventing duplicate transactions
    @NewTransactionId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Idempotency check: If transaction with same key exists, return it
        IF @IdempotencyKey IS NOT NULL
        BEGIN
            IF EXISTS (
                SELECT 1 FROM WalletTransactions
                WHERE WalletId = @WalletId
                  AND ReferenceId = @IdempotencyKey
                  AND Status = 'COMPLETED'
            )
            BEGIN
                SELECT @NewTransactionId = Id
                FROM WalletTransactions
                WHERE WalletId = @WalletId AND ReferenceId = @IdempotencyKey;

                SELECT
                    Id, WalletId, TransactionType, Category, Amount,
                    BalanceBefore, BalanceAfter, ReferenceId, ReferenceType,
                    Description, Status, CreatedAt
                FROM WalletTransactions
                WHERE Id = @NewTransactionId;

                COMMIT;
                RETURN;
            END
        END

        -- Get current balance
        DECLARE @CurrentBalance DECIMAL(18,2);
        DECLARE @CurrentHoldBalance DECIMAL(18,2);
        DECLARE @NewBalance DECIMAL(18,2);
        DECLARE @NewHoldBalance DECIMAL(18,2);

        SELECT
            @CurrentBalance = Balance,
            @CurrentHoldBalance = HoldBalance
        FROM Wallets WITH (UPDLOCK, HOLDLOCK)
        WHERE Id = @WalletId AND IsActive = 1;

        IF @CurrentBalance IS NULL
        BEGIN
            RAISERROR('Wallet not found or inactive', 16, 1);
            RETURN;
        END

        -- Calculate new balances based on transaction type
        SET @NewBalance = @CurrentBalance;
        SET @NewHoldBalance = @CurrentHoldBalance;

        IF @TransactionType = 'CREDIT'
        BEGIN
            SET @NewBalance = @CurrentBalance + @Amount;
        END
        ELSE IF @TransactionType = 'DEBIT'
        BEGIN
            IF @CurrentBalance - @CurrentHoldBalance < @Amount
            BEGIN
                RAISERROR('Insufficient available balance', 16, 1);
                RETURN;
            END
            SET @NewBalance = @CurrentBalance - @Amount;
        END
        ELSE IF @TransactionType = 'HOLD'
        BEGIN
            IF @CurrentBalance - @CurrentHoldBalance < @Amount
            BEGIN
                RAISERROR('Insufficient available balance for hold', 16, 1);
                RETURN;
            END
            SET @NewHoldBalance = @CurrentHoldBalance + @Amount;
        END
        ELSE IF @TransactionType = 'RELEASE'
        BEGIN
            IF @CurrentHoldBalance < @Amount
            BEGIN
                RAISERROR('Hold amount exceeds current hold balance', 16, 1);
                RETURN;
            END
            SET @NewHoldBalance = @CurrentHoldBalance - @Amount;
        END
        ELSE IF @TransactionType = 'REFUND'
        BEGIN
            SET @NewBalance = @CurrentBalance + @Amount;
        END

        -- Update wallet
        UPDATE Wallets
        SET
            Balance = @NewBalance,
            HoldBalance = @NewHoldBalance,
            UpdatedAt = GETUTCDATE()
        WHERE Id = @WalletId;

        -- Create transaction record
        SET @NewTransactionId = NEWID();

        INSERT INTO WalletTransactions (
            Id, WalletId, TransactionType, Category, Amount,
            BalanceBefore, BalanceAfter, ReferenceId, ReferenceType,
            Description, Status, CreatedAt
        )
        VALUES (
            @NewTransactionId, @WalletId, @TransactionType, @Category, @Amount,
            @CurrentBalance, @NewBalance,
            ISNULL(@IdempotencyKey, @ReferenceId), @ReferenceType,
            @Description, 'COMPLETED', GETUTCDATE()
        );

        COMMIT;

        -- Return transaction details
        SELECT
            t.Id, t.WalletId, t.TransactionType, t.Category, t.Amount,
            t.BalanceBefore, t.BalanceAfter, t.ReferenceId, t.ReferenceType,
            t.Description, t.Status, t.CreatedAt,
            w.Balance AS CurrentBalance,
            w.HoldBalance AS CurrentHoldBalance,
            w.Balance - w.HoldBalance AS AvailableBalance
        FROM WalletTransactions t
        INNER JOIN Wallets w ON w.Id = t.WalletId
        WHERE t.Id = @NewTransactionId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        EXEC usp_LogError @ErrorContext = 'usp_Wallet_ApplyTransaction';
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_Wallet_ApplyTransaction';

-- =====================================================
-- PROCEDURE: Get Wallet Transactions
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Wallet_GetTransactions')
    DROP PROCEDURE usp_Wallet_GetTransactions;
GO

CREATE PROCEDURE usp_Wallet_GetTransactions
    @WalletId UNIQUEIDENTIFIER,
    @PageNumber INT = 1,
    @PageSize INT = 20,
    @TransactionType NVARCHAR(20) = NULL,
    @Category NVARCHAR(50) = NULL,
    @FromDate DATETIME2 = NULL,
    @ToDate DATETIME2 = NULL,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    -- Get total count
    SELECT @TotalCount = COUNT(*)
    FROM WalletTransactions
    WHERE WalletId = @WalletId
      AND (@TransactionType IS NULL OR TransactionType = @TransactionType)
      AND (@Category IS NULL OR Category = @Category)
      AND (@FromDate IS NULL OR CreatedAt >= @FromDate)
      AND (@ToDate IS NULL OR CreatedAt <= @ToDate);

    -- Get paged results
    SELECT
        Id, WalletId, TransactionType, Category, Amount,
        BalanceBefore, BalanceAfter, ReferenceId, ReferenceType,
        Description, Status, CreatedAt
    FROM WalletTransactions
    WHERE WalletId = @WalletId
      AND (@TransactionType IS NULL OR TransactionType = @TransactionType)
      AND (@Category IS NULL OR Category = @Category)
      AND (@FromDate IS NULL OR CreatedAt >= @FromDate)
      AND (@ToDate IS NULL OR CreatedAt <= @ToDate)
    ORDER BY CreatedAt DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    SELECT @TotalCount AS TotalCount;
END
GO

PRINT 'Created usp_Wallet_GetTransactions';

-- =====================================================
-- PROCEDURE: Hold Balance (for pending deliveries)
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Wallet_HoldBalance')
    DROP PROCEDURE usp_Wallet_HoldBalance;
GO

CREATE PROCEDURE usp_Wallet_HoldBalance
    @WalletId UNIQUEIDENTIFIER,
    @Amount DECIMAL(18,2),
    @ReferenceId NVARCHAR(100), -- DeliveryId
    @Description NVARCHAR(500) = NULL,
    @TransactionId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    EXEC usp_Wallet_ApplyTransaction
        @WalletId = @WalletId,
        @TransactionType = 'HOLD',
        @Category = 'DELIVERY_HOLD',
        @Amount = @Amount,
        @ReferenceId = @ReferenceId,
        @ReferenceType = 'DELIVERY',
        @Description = @Description,
        @IdempotencyKey = @ReferenceId,
        @NewTransactionId = @TransactionId OUTPUT;
END
GO

PRINT 'Created usp_Wallet_HoldBalance';

-- =====================================================
-- PROCEDURE: Release Hold (after delivery completion/cancellation)
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Wallet_ReleaseHold')
    DROP PROCEDURE usp_Wallet_ReleaseHold;
GO

CREATE PROCEDURE usp_Wallet_ReleaseHold
    @WalletId UNIQUEIDENTIFIER,
    @Amount DECIMAL(18,2),
    @ReferenceId NVARCHAR(100),
    @Description NVARCHAR(500) = NULL,
    @TransactionId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ReleaseKey NVARCHAR(100) = @ReferenceId + '_RELEASE';

    EXEC usp_Wallet_ApplyTransaction
        @WalletId = @WalletId,
        @TransactionType = 'RELEASE',
        @Category = 'DELIVERY_RELEASE',
        @Amount = @Amount,
        @ReferenceId = @ReferenceId,
        @ReferenceType = 'DELIVERY',
        @Description = @Description,
        @IdempotencyKey = @ReleaseKey,
        @NewTransactionId = @TransactionId OUTPUT;
END
GO

PRINT 'Created usp_Wallet_ReleaseHold';

-- =====================================================
-- PROCEDURE: Create Payment
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Payment_Create')
    DROP PROCEDURE usp_Payment_Create;
GO

CREATE PROCEDURE usp_Payment_Create
    @UserId UNIQUEIDENTIFIER,
    @DeliveryId UNIQUEIDENTIFIER = NULL,
    @PaymentType NVARCHAR(20), -- DELIVERY, RECHARGE, SUBSCRIPTION
    @Amount DECIMAL(18,2),
    @PlatformFee DECIMAL(18,2) = NULL,
    @Tax DECIMAL(18,2) = NULL,
    @PaymentMethod NVARCHAR(20), -- WALLET, UPI, CARD, NETBANKING, COD
    @PaymentGateway NVARCHAR(50) = NULL,
    @NewPaymentId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        SET @NewPaymentId = NEWID();

        DECLARE @PaymentNumber NVARCHAR(20) = dbo.ufn_GeneratePaymentNumber();
        DECLARE @TotalAmount DECIMAL(18,2) = @Amount + ISNULL(@PlatformFee, 0) + ISNULL(@Tax, 0);

        INSERT INTO Payments (
            Id, PaymentNumber, UserId, DeliveryId, PaymentType,
            Amount, PlatformFee, Tax, TotalAmount,
            PaymentMethod, PaymentGateway, Status,
            CreatedAt, UpdatedAt
        )
        VALUES (
            @NewPaymentId, @PaymentNumber, @UserId, @DeliveryId, @PaymentType,
            @Amount, @PlatformFee, @Tax, @TotalAmount,
            @PaymentMethod, @PaymentGateway, 'PENDING',
            GETUTCDATE(), GETUTCDATE()
        );

        SELECT
            Id, PaymentNumber, UserId, DeliveryId, PaymentType,
            Amount, PlatformFee, Tax, TotalAmount,
            PaymentMethod, PaymentGateway, Status,
            CreatedAt
        FROM Payments
        WHERE Id = @NewPaymentId;

    END TRY
    BEGIN CATCH
        EXEC usp_LogError @ErrorContext = 'usp_Payment_Create', @UserId = @UserId;
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_Payment_Create';

-- =====================================================
-- PROCEDURE: Complete Payment
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Payment_Complete')
    DROP PROCEDURE usp_Payment_Complete;
GO

CREATE PROCEDURE usp_Payment_Complete
    @PaymentId UNIQUEIDENTIFIER,
    @GatewayTransactionId NVARCHAR(100) = NULL,
    @GatewayOrderId NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @UserId UNIQUEIDENTIFIER;
        DECLARE @PaymentType NVARCHAR(20);
        DECLARE @TotalAmount DECIMAL(18,2);
        DECLARE @PaymentMethod NVARCHAR(20);

        SELECT
            @UserId = UserId,
            @PaymentType = PaymentType,
            @TotalAmount = TotalAmount,
            @PaymentMethod = PaymentMethod
        FROM Payments
        WHERE Id = @PaymentId AND Status = 'PENDING';

        IF @UserId IS NULL
        BEGIN
            RAISERROR('Payment not found or already processed', 16, 1);
            RETURN;
        END

        -- Update payment status
        UPDATE Payments
        SET
            Status = 'COMPLETED',
            GatewayTransactionId = @GatewayTransactionId,
            GatewayOrderId = @GatewayOrderId,
            CompletedAt = GETUTCDATE(),
            UpdatedAt = GETUTCDATE()
        WHERE Id = @PaymentId;

        -- If payment is RECHARGE, credit to wallet
        IF @PaymentType = 'RECHARGE'
        BEGIN
            DECLARE @WalletId UNIQUEIDENTIFIER;
            DECLARE @TransactionId UNIQUEIDENTIFIER;

            SELECT @WalletId = Id FROM Wallets WHERE UserId = @UserId AND IsActive = 1;

            IF @WalletId IS NOT NULL
            BEGIN
                EXEC usp_Wallet_ApplyTransaction
                    @WalletId = @WalletId,
                    @TransactionType = 'CREDIT',
                    @Category = 'RECHARGE',
                    @Amount = @TotalAmount,
                    @ReferenceId = @PaymentId,
                    @ReferenceType = 'PAYMENT',
                    @Description = 'Wallet recharge',
                    @IdempotencyKey = @PaymentId,
                    @NewTransactionId = @TransactionId OUTPUT;
            END
        END

        COMMIT;

        SELECT
            Id, PaymentNumber, UserId, DeliveryId, PaymentType,
            Amount, PlatformFee, Tax, TotalAmount,
            PaymentMethod, PaymentGateway, GatewayTransactionId,
            Status, CompletedAt, CreatedAt
        FROM Payments
        WHERE Id = @PaymentId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        EXEC usp_LogError @ErrorContext = 'usp_Payment_Complete';
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_Payment_Complete';

-- =====================================================
-- PROCEDURE: Fail Payment
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Payment_Fail')
    DROP PROCEDURE usp_Payment_Fail;
GO

CREATE PROCEDURE usp_Payment_Fail
    @PaymentId UNIQUEIDENTIFIER,
    @FailureReason NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Payments
    SET
        Status = 'FAILED',
        FailureReason = @FailureReason,
        UpdatedAt = GETUTCDATE()
    WHERE Id = @PaymentId;

    SELECT
        Id, PaymentNumber, UserId, Status, FailureReason, UpdatedAt
    FROM Payments
    WHERE Id = @PaymentId;
END
GO

PRINT 'Created usp_Payment_Fail';

-- =====================================================
-- PROCEDURE: Create Commission Record
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Commission_Create')
    DROP PROCEDURE usp_Commission_Create;
GO

CREATE PROCEDURE usp_Commission_Create
    @DeliveryId UNIQUEIDENTIFIER,
    @DPId UNIQUEIDENTIFIER,
    @DPCMId UNIQUEIDENTIFIER = NULL,
    @DeliveryAmount DECIMAL(18,2),
    @DPEarning DECIMAL(18,2),
    @DPCMCommission DECIMAL(18,2) = 0,
    @PlatformFee DECIMAL(18,2),
    @NewCommissionId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        -- Check for existing commission for this delivery
        IF EXISTS (SELECT 1 FROM CommissionRecords WHERE DeliveryId = @DeliveryId)
        BEGIN
            SELECT @NewCommissionId = Id FROM CommissionRecords WHERE DeliveryId = @DeliveryId;

            SELECT * FROM CommissionRecords WHERE Id = @NewCommissionId;
            RETURN;
        END

        SET @NewCommissionId = NEWID();

        INSERT INTO CommissionRecords (
            Id, DeliveryId, DPId, DPCMId,
            DeliveryAmount, DPEarning, DPCMCommission, PlatformFee,
            Status, CreatedAt
        )
        VALUES (
            @NewCommissionId, @DeliveryId, @DPId, @DPCMId,
            @DeliveryAmount, @DPEarning, @DPCMCommission, @PlatformFee,
            'PENDING', GETUTCDATE()
        );

        SELECT * FROM CommissionRecords WHERE Id = @NewCommissionId;

    END TRY
    BEGIN CATCH
        EXEC usp_LogError @ErrorContext = 'usp_Commission_Create';
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_Commission_Create';

-- =====================================================
-- PROCEDURE: Create Settlement
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Settlement_Create')
    DROP PROCEDURE usp_Settlement_Create;
GO

CREATE PROCEDURE usp_Settlement_Create
    @BeneficiaryId UNIQUEIDENTIFIER,
    @BeneficiaryType NVARCHAR(10), -- DP, DPCM
    @SettlementDate DATE,
    @NewSettlementId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Calculate totals from pending commissions
        DECLARE @GrossAmount DECIMAL(18,2) = 0;
        DECLARE @TdsRate DECIMAL(5,2) = 0.01; -- 1% TDS (adjust as needed)

        IF @BeneficiaryType = 'DP'
        BEGIN
            SELECT @GrossAmount = ISNULL(SUM(DPEarning), 0)
            FROM CommissionRecords
            WHERE DPId = @BeneficiaryId AND Status = 'PENDING';
        END
        ELSE IF @BeneficiaryType = 'DPCM'
        BEGIN
            SELECT @GrossAmount = ISNULL(SUM(DPCMCommission), 0)
            FROM CommissionRecords
            WHERE DPCMId = @BeneficiaryId AND Status = 'PENDING';
        END

        IF @GrossAmount = 0
        BEGIN
            RAISERROR('No pending commissions found for settlement', 16, 1);
            RETURN;
        END

        DECLARE @TdsAmount DECIMAL(18,2) = @GrossAmount * @TdsRate;
        DECLARE @NetAmount DECIMAL(18,2) = @GrossAmount - @TdsAmount;

        SET @NewSettlementId = NEWID();
        DECLARE @SettlementNumber NVARCHAR(20) = dbo.ufn_GenerateSettlementNumber();

        -- Get bank details (simplified)
        DECLARE @BankAccount NVARCHAR(500);
        DECLARE @BankIfsc NVARCHAR(20);
        DECLARE @UpiId NVARCHAR(100);

        -- Insert settlement
        INSERT INTO Settlements (
            Id, SettlementNumber, BeneficiaryId, BeneficiaryType,
            GrossAmount, TdsAmount, NetAmount,
            BankAccountNumber, BankIfscCode, UpiId,
            PayoutMethod, Status, SettlementDate,
            CreatedAt, UpdatedAt
        )
        VALUES (
            @NewSettlementId, @SettlementNumber, @BeneficiaryId, @BeneficiaryType,
            @GrossAmount, @TdsAmount, @NetAmount,
            @BankAccount, @BankIfsc, @UpiId,
            'BANK_TRANSFER', 'PENDING', @SettlementDate,
            GETUTCDATE(), GETUTCDATE()
        );

        -- Create settlement items and update commission status
        INSERT INTO SettlementItems (Id, SettlementId, DeliveryId, EarningAmount, CommissionAmount, NetAmount, EarnedAt)
        SELECT
            NEWID(),
            @NewSettlementId,
            DeliveryId,
            CASE WHEN @BeneficiaryType = 'DP' THEN DPEarning ELSE 0 END,
            CASE WHEN @BeneficiaryType = 'DPCM' THEN DPCMCommission ELSE 0 END,
            CASE WHEN @BeneficiaryType = 'DP' THEN DPEarning ELSE DPCMCommission END,
            CreatedAt
        FROM CommissionRecords
        WHERE (@BeneficiaryType = 'DP' AND DPId = @BeneficiaryId AND Status = 'PENDING')
           OR (@BeneficiaryType = 'DPCM' AND DPCMId = @BeneficiaryId AND Status = 'PENDING');

        -- Mark commissions as settled
        UPDATE CommissionRecords
        SET Status = 'SETTLED'
        WHERE (@BeneficiaryType = 'DP' AND DPId = @BeneficiaryId AND Status = 'PENDING')
           OR (@BeneficiaryType = 'DPCM' AND DPCMId = @BeneficiaryId AND Status = 'PENDING');

        COMMIT;

        SELECT
            s.*,
            (SELECT COUNT(*) FROM SettlementItems WHERE SettlementId = s.Id) AS ItemCount
        FROM Settlements s
        WHERE s.Id = @NewSettlementId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        EXEC usp_LogError @ErrorContext = 'usp_Settlement_Create';
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_Settlement_Create';

-- =====================================================
-- PROCEDURE: Complete Settlement
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Settlement_Complete')
    DROP PROCEDURE usp_Settlement_Complete;
GO

CREATE PROCEDURE usp_Settlement_Complete
    @SettlementId UNIQUEIDENTIFIER,
    @PayoutReference NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @BeneficiaryId UNIQUEIDENTIFIER;
        DECLARE @NetAmount DECIMAL(18,2);

        SELECT
            @BeneficiaryId = BeneficiaryId,
            @NetAmount = NetAmount
        FROM Settlements
        WHERE Id = @SettlementId AND Status = 'PENDING';

        IF @BeneficiaryId IS NULL
        BEGIN
            RAISERROR('Settlement not found or already processed', 16, 1);
            RETURN;
        END

        -- Update settlement
        UPDATE Settlements
        SET
            Status = 'COMPLETED',
            PayoutReference = @PayoutReference,
            ProcessedAt = GETUTCDATE(),
            UpdatedAt = GETUTCDATE()
        WHERE Id = @SettlementId;

        -- Credit to beneficiary wallet
        DECLARE @WalletId UNIQUEIDENTIFIER;
        DECLARE @TransactionId UNIQUEIDENTIFIER;

        SELECT @WalletId = Id FROM Wallets WHERE UserId = @BeneficiaryId AND IsActive = 1;

        IF @WalletId IS NOT NULL
        BEGIN
            EXEC usp_Wallet_ApplyTransaction
                @WalletId = @WalletId,
                @TransactionType = 'CREDIT',
                @Category = 'SETTLEMENT',
                @Amount = @NetAmount,
                @ReferenceId = @SettlementId,
                @ReferenceType = 'SETTLEMENT',
                @Description = 'Settlement payout',
                @IdempotencyKey = @SettlementId,
                @NewTransactionId = @TransactionId OUTPUT;
        END

        COMMIT;

        SELECT * FROM Settlements WHERE Id = @SettlementId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        EXEC usp_LogError @ErrorContext = 'usp_Settlement_Complete';
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_Settlement_Complete';

-- =====================================================
-- PROCEDURE: Get Wallet Summary (for dashboard)
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Wallet_GetSummary')
    DROP PROCEDURE usp_Wallet_GetSummary;
GO

CREATE PROCEDURE usp_Wallet_GetSummary
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    -- Wallet balances
    SELECT
        w.Id AS WalletId,
        w.WalletType,
        w.Balance,
        w.HoldBalance,
        w.Balance - w.HoldBalance AS AvailableBalance,
        w.Currency
    FROM Wallets w
    WHERE w.UserId = @UserId AND w.IsActive = 1;

    -- Recent transactions (last 10)
    SELECT TOP 10
        t.Id, t.TransactionType, t.Category, t.Amount,
        t.BalanceAfter, t.Description, t.CreatedAt
    FROM WalletTransactions t
    INNER JOIN Wallets w ON w.Id = t.WalletId
    WHERE w.UserId = @UserId
    ORDER BY t.CreatedAt DESC;

    -- Pending settlements
    SELECT
        COUNT(*) AS PendingSettlementCount,
        ISNULL(SUM(NetAmount), 0) AS PendingSettlementAmount
    FROM Settlements
    WHERE BeneficiaryId = @UserId AND Status = 'PENDING';

    -- This month's earnings (for DP/DPCM)
    SELECT
        ISNULL(SUM(CASE WHEN TransactionType = 'CREDIT' THEN Amount ELSE 0 END), 0) AS TotalCredits,
        ISNULL(SUM(CASE WHEN TransactionType = 'DEBIT' THEN Amount ELSE 0 END), 0) AS TotalDebits
    FROM WalletTransactions t
    INNER JOIN Wallets w ON w.Id = t.WalletId
    WHERE w.UserId = @UserId
      AND t.CreatedAt >= DATEADD(DAY, 1-DAY(GETUTCDATE()), CAST(GETUTCDATE() AS DATE))
      AND t.Status = 'COMPLETED';
END
GO

PRINT 'Created usp_Wallet_GetSummary';

PRINT 'Wallet module: COMPLETE';
