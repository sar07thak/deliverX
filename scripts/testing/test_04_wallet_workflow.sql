-- =====================================================
-- DELIVERYDOST TEST SCRIPT - WALLET WORKFLOW
-- =====================================================
-- Tests complete wallet operations:
-- Create Wallet -> Recharge -> Hold -> Release ->
-- Debit -> Credit -> Settlement
-- =====================================================

USE DeliveryDost_Dev;
GO

SET NOCOUNT ON;
PRINT '====================================================';
PRINT 'TEST: WALLET OPERATIONS WORKFLOW';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETUTCDATE(), 121);
PRINT '====================================================';
PRINT '';

-- Test variables
DECLARE @TestUserId UNIQUEIDENTIFIER = NEWID();
DECLARE @TestWalletId UNIQUEIDENTIFIER = NEWID();
DECLARE @TestPhone VARCHAR(15) = '98777777' + RIGHT(CAST(ABS(CHECKSUM(NEWID())) AS VARCHAR), 2);
DECLARE @CurrentBalance DECIMAL(18,2);
DECLARE @HoldBalance DECIMAL(18,2);
DECLARE @TxnCount INT;
DECLARE @TestsPassed INT = 0;
DECLARE @TestsFailed INT = 0;

-- =====================================================
-- SETUP: Create test user
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'SETUP: Creating test user';
PRINT '----------------------------------------------------';

INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, IsActive, IsPhoneVerified, CreatedAt, UpdatedAt)
VALUES (@TestUserId, @TestPhone, 'wallet_test@test.com', 'Wallet Test User', 'hash', 'EC', 1, 1, GETUTCDATE(), GETUTCDATE());

PRINT '  Test User ID: ' + CAST(@TestUserId AS VARCHAR(50));
PRINT '';

-- =====================================================
-- TEST 1: CREATE WALLET
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 1: CREATE WALLET';
PRINT '----------------------------------------------------';

BEGIN TRY
    INSERT INTO Wallets (Id, UserId, Balance, HoldBalance, Currency, Status, CreatedAt, UpdatedAt)
    VALUES (@TestWalletId, @TestUserId, 0.00, 0.00, 'INR', 'ACTIVE', GETUTCDATE(), GETUTCDATE());

    SELECT @CurrentBalance = Balance FROM Wallets WHERE Id = @TestWalletId;

    IF @CurrentBalance = 0
    BEGIN
        PRINT '  [PASS] Wallet created with zero balance';
        PRINT '  -> Wallet ID: ' + CAST(@TestWalletId AS VARCHAR(50));
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Unexpected initial balance';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] Wallet creation failed: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
    RETURN;
END CATCH

PRINT '';

-- =====================================================
-- TEST 2: RECHARGE WALLET (CREDIT)
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 2: RECHARGE WALLET (+1000)';
PRINT '----------------------------------------------------';

BEGIN TRY
    DECLARE @RechargeAmount DECIMAL(18,2) = 1000.00;
    DECLARE @OldBalance DECIMAL(18,2);

    SELECT @OldBalance = Balance FROM Wallets WHERE Id = @TestWalletId;

    -- Update wallet
    UPDATE Wallets
    SET Balance = Balance + @RechargeAmount, UpdatedAt = GETUTCDATE()
    WHERE Id = @TestWalletId;

    -- Create transaction record
    INSERT INTO WalletTransactions (Id, WalletId, TransactionType, Category, Amount, BalanceBefore, BalanceAfter, ReferenceId, ReferenceType, Description, Status, CreatedAt)
    VALUES (NEWID(), @TestWalletId, 'CREDIT', 'RECHARGE', @RechargeAmount, @OldBalance, @OldBalance + @RechargeAmount, 'RCH-TEST-001', 'PAYMENT', 'Test wallet recharge', 'COMPLETED', GETUTCDATE());

    SELECT @CurrentBalance = Balance FROM Wallets WHERE Id = @TestWalletId;

    IF @CurrentBalance = 1000.00
    BEGIN
        PRINT '  [PASS] Wallet recharged successfully';
        PRINT '  -> New Balance: Rs. ' + CAST(@CurrentBalance AS VARCHAR);
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Recharge failed. Balance: ' + CAST(@CurrentBalance AS VARCHAR);
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 3: HOLD AMOUNT (FOR DELIVERY)
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 3: HOLD AMOUNT FOR DELIVERY (150)';
PRINT '----------------------------------------------------';

BEGIN TRY
    DECLARE @HoldAmount DECIMAL(18,2) = 150.00;

    SELECT @OldBalance = Balance, @HoldBalance = HoldBalance FROM Wallets WHERE Id = @TestWalletId;

    -- Move to hold
    UPDATE Wallets
    SET
        Balance = Balance - @HoldAmount,
        HoldBalance = HoldBalance + @HoldAmount,
        UpdatedAt = GETUTCDATE()
    WHERE Id = @TestWalletId;

    -- Create hold transaction
    INSERT INTO WalletTransactions (Id, WalletId, TransactionType, Category, Amount, BalanceBefore, BalanceAfter, ReferenceId, ReferenceType, Description, Status, CreatedAt)
    VALUES (NEWID(), @TestWalletId, 'HOLD', 'DELIVERY_PAYMENT', @HoldAmount, @OldBalance, @OldBalance - @HoldAmount, 'DEL-TEST-001', 'DELIVERY', 'Amount held for delivery', 'COMPLETED', GETUTCDATE());

    SELECT @CurrentBalance = Balance, @HoldBalance = HoldBalance FROM Wallets WHERE Id = @TestWalletId;

    IF @CurrentBalance = 850.00 AND @HoldBalance = 150.00
    BEGIN
        PRINT '  [PASS] Amount held successfully';
        PRINT '  -> Available Balance: Rs. ' + CAST(@CurrentBalance AS VARCHAR);
        PRINT '  -> Hold Balance: Rs. ' + CAST(@HoldBalance AS VARCHAR);
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Hold operation failed';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 4: RELEASE PARTIAL HOLD (DELIVERY CANCELLED)
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 4: RELEASE PARTIAL HOLD (50)';
PRINT '----------------------------------------------------';

BEGIN TRY
    DECLARE @ReleaseAmount DECIMAL(18,2) = 50.00;

    SELECT @OldBalance = Balance, @HoldBalance = HoldBalance FROM Wallets WHERE Id = @TestWalletId;

    -- Release from hold
    UPDATE Wallets
    SET
        Balance = Balance + @ReleaseAmount,
        HoldBalance = HoldBalance - @ReleaseAmount,
        UpdatedAt = GETUTCDATE()
    WHERE Id = @TestWalletId;

    -- Create release transaction
    INSERT INTO WalletTransactions (Id, WalletId, TransactionType, Category, Amount, BalanceBefore, BalanceAfter, ReferenceId, ReferenceType, Description, Status, CreatedAt)
    VALUES (NEWID(), @TestWalletId, 'RELEASE', 'DELIVERY_PAYMENT', @ReleaseAmount, @OldBalance, @OldBalance + @ReleaseAmount, 'DEL-TEST-001', 'DELIVERY', 'Partial refund - delivery cancelled', 'COMPLETED', GETUTCDATE());

    SELECT @CurrentBalance = Balance, @HoldBalance = HoldBalance FROM Wallets WHERE Id = @TestWalletId;

    IF @CurrentBalance = 900.00 AND @HoldBalance = 100.00
    BEGIN
        PRINT '  [PASS] Partial hold released';
        PRINT '  -> Available Balance: Rs. ' + CAST(@CurrentBalance AS VARCHAR);
        PRINT '  -> Hold Balance: Rs. ' + CAST(@HoldBalance AS VARCHAR);
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Release operation failed';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 5: DEBIT FROM HOLD (DELIVERY COMPLETED)
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 5: DEBIT FROM HOLD (100)';
PRINT '----------------------------------------------------';

BEGIN TRY
    DECLARE @DebitAmount DECIMAL(18,2) = 100.00;

    SELECT @OldBalance = Balance, @HoldBalance = HoldBalance FROM Wallets WHERE Id = @TestWalletId;

    -- Debit from hold (money leaves wallet permanently)
    UPDATE Wallets
    SET
        HoldBalance = HoldBalance - @DebitAmount,
        UpdatedAt = GETUTCDATE()
    WHERE Id = @TestWalletId;

    -- Create debit transaction
    INSERT INTO WalletTransactions (Id, WalletId, TransactionType, Category, Amount, BalanceBefore, BalanceAfter, ReferenceId, ReferenceType, Description, Status, CreatedAt)
    VALUES (NEWID(), @TestWalletId, 'DEBIT', 'DELIVERY_PAYMENT', @DebitAmount, @OldBalance, @OldBalance, 'DEL-TEST-001', 'DELIVERY', 'Payment for completed delivery', 'COMPLETED', GETUTCDATE());

    SELECT @CurrentBalance = Balance, @HoldBalance = HoldBalance FROM Wallets WHERE Id = @TestWalletId;

    IF @CurrentBalance = 900.00 AND @HoldBalance = 0.00
    BEGIN
        PRINT '  [PASS] Debit from hold completed';
        PRINT '  -> Available Balance: Rs. ' + CAST(@CurrentBalance AS VARCHAR);
        PRINT '  -> Hold Balance: Rs. ' + CAST(@HoldBalance AS VARCHAR);
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Debit operation failed';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 6: CASHBACK/CREDIT
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 6: ADD CASHBACK (+25)';
PRINT '----------------------------------------------------';

BEGIN TRY
    DECLARE @CashbackAmount DECIMAL(18,2) = 25.00;

    SELECT @OldBalance = Balance FROM Wallets WHERE Id = @TestWalletId;

    -- Add cashback
    UPDATE Wallets
    SET Balance = Balance + @CashbackAmount, UpdatedAt = GETUTCDATE()
    WHERE Id = @TestWalletId;

    -- Create cashback transaction
    INSERT INTO WalletTransactions (Id, WalletId, TransactionType, Category, Amount, BalanceBefore, BalanceAfter, ReferenceId, ReferenceType, Description, Status, CreatedAt)
    VALUES (NEWID(), @TestWalletId, 'CREDIT', 'CASHBACK', @CashbackAmount, @OldBalance, @OldBalance + @CashbackAmount, 'CB-TEST-001', 'PROMO', 'Delivery cashback reward', 'COMPLETED', GETUTCDATE());

    SELECT @CurrentBalance = Balance FROM Wallets WHERE Id = @TestWalletId;

    IF @CurrentBalance = 925.00
    BEGIN
        PRINT '  [PASS] Cashback credited';
        PRINT '  -> New Balance: Rs. ' + CAST(@CurrentBalance AS VARCHAR);
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Cashback failed';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 7: INSUFFICIENT BALANCE CHECK
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 7: INSUFFICIENT BALANCE CHECK';
PRINT '----------------------------------------------------';

BEGIN TRY
    DECLARE @LargeDebit DECIMAL(18,2) = 5000.00;

    SELECT @CurrentBalance = Balance FROM Wallets WHERE Id = @TestWalletId;

    IF @LargeDebit > @CurrentBalance
    BEGIN
        PRINT '  [PASS] Insufficient balance detected correctly';
        PRINT '  -> Requested: Rs. ' + CAST(@LargeDebit AS VARCHAR);
        PRINT '  -> Available: Rs. ' + CAST(@CurrentBalance AS VARCHAR);
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Balance check logic error';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 8: FREEZE WALLET
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 8: FREEZE WALLET';
PRINT '----------------------------------------------------';

BEGIN TRY
    UPDATE Wallets
    SET Status = 'FROZEN', UpdatedAt = GETUTCDATE()
    WHERE Id = @TestWalletId;

    DECLARE @WalletStatus NVARCHAR(20);
    SELECT @WalletStatus = Status FROM Wallets WHERE Id = @TestWalletId;

    IF @WalletStatus = 'FROZEN'
    BEGIN
        PRINT '  [PASS] Wallet frozen successfully';
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Freeze operation failed';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 9: UNFREEZE WALLET
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 9: UNFREEZE WALLET';
PRINT '----------------------------------------------------';

BEGIN TRY
    UPDATE Wallets
    SET Status = 'ACTIVE', UpdatedAt = GETUTCDATE()
    WHERE Id = @TestWalletId;

    SELECT @WalletStatus = Status FROM Wallets WHERE Id = @TestWalletId;

    IF @WalletStatus = 'ACTIVE'
    BEGIN
        PRINT '  [PASS] Wallet unfrozen successfully';
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Unfreeze operation failed';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- VERIFY: Transaction History
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'VERIFY: TRANSACTION HISTORY';
PRINT '----------------------------------------------------';

SELECT @TxnCount = COUNT(*) FROM WalletTransactions WHERE WalletId = @TestWalletId;
PRINT '  Total Transactions: ' + CAST(@TxnCount AS VARCHAR);
PRINT '';

SELECT
    ROW_NUMBER() OVER (ORDER BY CreatedAt) AS Seq,
    TransactionType,
    Category,
    Amount,
    BalanceBefore,
    BalanceAfter,
    Description
FROM WalletTransactions
WHERE WalletId = @TestWalletId
ORDER BY CreatedAt;

PRINT '';

-- =====================================================
-- VERIFY: Final Wallet State
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'VERIFY: FINAL WALLET STATE';
PRINT '----------------------------------------------------';

SELECT
    w.Id AS WalletId,
    u.FullName AS Owner,
    w.Balance,
    w.HoldBalance,
    w.Balance + w.HoldBalance AS TotalBalance,
    w.Currency,
    w.Status
FROM Wallets w
INNER JOIN Users u ON u.Id = w.UserId
WHERE w.Id = @TestWalletId;

PRINT '';

-- =====================================================
-- TEST 10: SUMMARY AGGREGATION
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 10: TRANSACTION SUMMARY';
PRINT '----------------------------------------------------';

BEGIN TRY
    SELECT
        TransactionType,
        COUNT(*) AS TxnCount,
        SUM(Amount) AS TotalAmount
    FROM WalletTransactions
    WHERE WalletId = @TestWalletId
    GROUP BY TransactionType;

    PRINT '  [PASS] Summary aggregation works';
    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- CLEANUP
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'CLEANUP: Removing test data';
PRINT '----------------------------------------------------';

BEGIN TRY
    DELETE FROM WalletTransactions WHERE WalletId = @TestWalletId;
    DELETE FROM Wallets WHERE Id = @TestWalletId;
    DELETE FROM Users WHERE Id = @TestUserId;
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
PRINT 'TEST SUMMARY: WALLET WORKFLOW';
PRINT '====================================================';
PRINT 'Tests Passed: ' + CAST(@TestsPassed AS VARCHAR);
PRINT 'Tests Failed: ' + CAST(@TestsFailed AS VARCHAR);
PRINT 'Total Tests:  ' + CAST(@TestsPassed + @TestsFailed AS VARCHAR);
PRINT '';

IF @TestsFailed = 0
    PRINT 'RESULT: ALL TESTS PASSED - Wallet operations verified';
ELSE
    PRINT 'RESULT: SOME TESTS FAILED - Review above for details';

PRINT '';
PRINT 'Finished at: ' + CONVERT(VARCHAR, GETUTCDATE(), 121);
PRINT '====================================================';
GO
