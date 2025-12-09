-- =====================================================
-- DELIVERYDOST SEED DATA - STEP 5: TRANSACTIONS
-- =====================================================
-- Run AFTER seed_04_deliveries.sql
-- Creates: WalletTransactions, Payments, CommissionRecords, Settlements
-- =====================================================

USE DeliveryDost_Dev;
GO

SET NOCOUNT ON;
PRINT '====================================';
PRINT 'STEP 5: SEEDING TRANSACTIONS';
PRINT '====================================';

-- =====================================================
-- 5.1 WALLET TRANSACTIONS (Credits and Debits)
-- =====================================================
PRINT 'Creating WalletTransactions...';

DECLARE @TxnCount INT = 0;

-- Initial balance credits for all wallets
INSERT INTO WalletTransactions (Id, WalletId, TransactionType, Category, Amount, BalanceBefore, BalanceAfter, ReferenceId, ReferenceType, Description, Status, CreatedAt)
SELECT
    NEWID(),
    w.Id,
    'CREDIT',
    'RECHARGE',
    w.Balance + w.HoldBalance,
    0,
    w.Balance + w.HoldBalance,
    'INIT-' + CAST(ROW_NUMBER() OVER (ORDER BY w.Id) AS NVARCHAR),
    'SYSTEM',
    'Initial wallet balance',
    'COMPLETED',
    DATEADD(DAY, -60, w.CreatedAt)
FROM Wallets w
WHERE NOT EXISTS (SELECT 1 FROM WalletTransactions t WHERE t.WalletId = w.Id AND t.Category = 'RECHARGE' AND t.Description = 'Initial wallet balance');

SET @TxnCount = @TxnCount + @@ROWCOUNT;

-- Delivery payment debits from EC/BC wallets
INSERT INTO WalletTransactions (Id, WalletId, TransactionType, Category, Amount, BalanceBefore, BalanceAfter, ReferenceId, ReferenceType, Description, Status, CreatedAt)
SELECT
    NEWID(),
    w.Id,
    'DEBIT',
    'DELIVERY_PAYMENT',
    d.FinalPrice,
    w.Balance + d.FinalPrice,
    w.Balance,
    CAST(d.Id AS NVARCHAR(50)),
    'DELIVERY',
    'Payment for delivery #' + LEFT(CAST(d.Id AS NVARCHAR(50)), 8),
    'COMPLETED',
    DATEADD(MINUTE, CAST(d.EstimatedDurationMinutes AS INT) + 35, d.CreatedAt)
FROM Deliveries d
INNER JOIN Wallets w ON w.UserId = d.RequesterId
WHERE d.Status = 'DELIVERED'
  AND d.FinalPrice IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM WalletTransactions t WHERE t.WalletId = w.Id AND t.ReferenceId = CAST(d.Id AS NVARCHAR(50)) AND t.Category = 'DELIVERY_PAYMENT');

SET @TxnCount = @TxnCount + @@ROWCOUNT;

-- Delivery earning credits for DP wallets
INSERT INTO WalletTransactions (Id, WalletId, TransactionType, Category, Amount, BalanceBefore, BalanceAfter, ReferenceId, ReferenceType, Description, Status, CreatedAt)
SELECT
    NEWID(),
    w.Id,
    'CREDIT',
    'DELIVERY_EARNING',
    d.FinalPrice * 0.85, -- DP gets 85%
    w.Balance - (d.FinalPrice * 0.85),
    w.Balance,
    CAST(d.Id AS NVARCHAR(50)),
    'DELIVERY',
    'Earning from delivery #' + LEFT(CAST(d.Id AS NVARCHAR(50)), 8),
    'COMPLETED',
    DATEADD(MINUTE, CAST(d.EstimatedDurationMinutes AS INT) + 40, d.CreatedAt)
FROM Deliveries d
INNER JOIN Wallets w ON w.UserId = d.AssignedDPId
WHERE d.Status = 'DELIVERED'
  AND d.FinalPrice IS NOT NULL
  AND d.AssignedDPId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM WalletTransactions t WHERE t.WalletId = w.Id AND t.ReferenceId = CAST(d.Id AS NVARCHAR(50)) AND t.Category = 'DELIVERY_EARNING');

SET @TxnCount = @TxnCount + @@ROWCOUNT;

-- Some recharge transactions
INSERT INTO WalletTransactions (Id, WalletId, TransactionType, Category, Amount, BalanceBefore, BalanceAfter, ReferenceId, ReferenceType, Description, Status, CreatedAt)
SELECT
    NEWID(),
    w.Id,
    'CREDIT',
    'RECHARGE',
    CASE ROW_NUMBER() OVER (ORDER BY w.Id) % 5 WHEN 0 THEN 500 WHEN 1 THEN 1000 WHEN 2 THEN 2000 WHEN 3 THEN 5000 ELSE 1500 END,
    w.Balance * 0.7,
    w.Balance * 0.7 + CASE ROW_NUMBER() OVER (ORDER BY w.Id) % 5 WHEN 0 THEN 500 WHEN 1 THEN 1000 WHEN 2 THEN 2000 WHEN 3 THEN 5000 ELSE 1500 END,
    'RCH-' + CAST(ROW_NUMBER() OVER (ORDER BY w.Id) AS NVARCHAR),
    'PAYMENT',
    'Wallet recharge',
    'COMPLETED',
    DATEADD(DAY, -CAST(RAND(CHECKSUM(w.Id)) * 30 AS INT), GETUTCDATE())
FROM Wallets w
INNER JOIN Users u ON u.Id = w.UserId
WHERE u.Role IN ('EC', 'BC')
  AND ROW_NUMBER() OVER (ORDER BY w.Id) % 2 = 0;

SET @TxnCount = @TxnCount + @@ROWCOUNT;

PRINT '  -> Created ' + CAST(@TxnCount AS VARCHAR) + ' WalletTransactions';
GO

-- =====================================================
-- 5.2 PAYMENTS
-- =====================================================
PRINT 'Creating Payments...';

DECLARE @PaymentCount INT = 0;

INSERT INTO Payments (Id, PaymentNumber, UserId, DeliveryId, PaymentType, Amount, PlatformFee, Tax, TotalAmount, PaymentMethod, PaymentGateway, GatewayTransactionId, Status, CompletedAt, CreatedAt, UpdatedAt)
SELECT
    NEWID(),
    'PAY-' + FORMAT(d.CreatedAt, 'yyyyMMdd') + '-' + RIGHT('00000' + CAST(ROW_NUMBER() OVER (ORDER BY d.CreatedAt) AS VARCHAR), 5),
    d.RequesterId,
    d.Id,
    'DELIVERY',
    d.FinalPrice,
    d.FinalPrice * 0.05, -- 5% platform fee
    d.FinalPrice * 0.05 * 0.18, -- 18% GST on platform fee
    d.FinalPrice + (d.FinalPrice * 0.05) + (d.FinalPrice * 0.05 * 0.18),
    CASE ROW_NUMBER() OVER (ORDER BY d.CreatedAt) % 5
        WHEN 0 THEN 'WALLET' WHEN 1 THEN 'UPI' WHEN 2 THEN 'CARD' WHEN 3 THEN 'NETBANKING' ELSE 'COD'
    END,
    CASE ROW_NUMBER() OVER (ORDER BY d.CreatedAt) % 3 WHEN 0 THEN 'RAZORPAY' WHEN 1 THEN 'PHONEPE' ELSE 'PAYTM' END,
    'pay_' + CAST(ROW_NUMBER() OVER (ORDER BY d.CreatedAt) AS NVARCHAR) + '_' + LEFT(REPLACE(CAST(NEWID() AS NVARCHAR(50)), '-', ''), 10),
    'COMPLETED',
    DATEADD(MINUTE, 5, d.CreatedAt),
    d.CreatedAt,
    DATEADD(MINUTE, 5, d.CreatedAt)
FROM Deliveries d
WHERE d.Status = 'DELIVERED'
  AND d.FinalPrice IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM Payments p WHERE p.DeliveryId = d.Id);

SET @PaymentCount = @@ROWCOUNT;
PRINT '  -> Created ' + CAST(@PaymentCount AS VARCHAR) + ' Payments';
GO

-- =====================================================
-- 5.3 COMMISSION RECORDS
-- =====================================================
PRINT 'Creating CommissionRecords...';

DECLARE @CommCount INT = 0;

INSERT INTO CommissionRecords (Id, DeliveryId, DPId, DPCMId, DeliveryAmount, DPEarning, DPCMCommission, PlatformFee, Status, CreatedAt)
SELECT
    NEWID(),
    d.Id,
    d.AssignedDPId,
    dp.DPCMId,
    d.FinalPrice,
    d.FinalPrice * 0.85, -- DP gets 85%
    CASE WHEN dpcm.CommissionType = 'PERCENTAGE' THEN d.FinalPrice * (dpcm.CommissionValue / 100) ELSE ISNULL(dpcm.CommissionValue, 0) END,
    d.FinalPrice * 0.05,
    CASE ROW_NUMBER() OVER (ORDER BY d.CreatedAt) % 3 WHEN 0 THEN 'PENDING' ELSE 'SETTLED' END,
    DATEADD(HOUR, 1, d.UpdatedAt)
FROM Deliveries d
INNER JOIN DeliveryPartnerProfiles dp ON dp.UserId = d.AssignedDPId
LEFT JOIN DPCManagers dpcm ON dpcm.Id = dp.DPCMId
WHERE d.Status = 'DELIVERED'
  AND d.FinalPrice IS NOT NULL
  AND d.AssignedDPId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM CommissionRecords c WHERE c.DeliveryId = d.Id);

SET @CommCount = @@ROWCOUNT;
PRINT '  -> Created ' + CAST(@CommCount AS VARCHAR) + ' CommissionRecords';
GO

-- =====================================================
-- 5.4 SETTLEMENTS
-- =====================================================
PRINT 'Creating Settlements...';

DECLARE @SettlementCount INT = 0;

-- Create settlements for DPs with settled commissions
INSERT INTO Settlements (Id, SettlementNumber, BeneficiaryId, BeneficiaryType, GrossAmount, TdsAmount, NetAmount, PayoutMethod, Status, SettlementDate, ProcessedAt, CreatedAt, UpdatedAt)
SELECT
    NEWID(),
    'STL-' + FORMAT(GETUTCDATE(), 'yyyyMMdd') + '-' + RIGHT('0000' + CAST(ROW_NUMBER() OVER (ORDER BY c.DPId) AS VARCHAR), 4),
    c.DPId,
    'DP',
    SUM(c.DPEarning),
    SUM(c.DPEarning) * 0.01, -- 1% TDS
    SUM(c.DPEarning) * 0.99,
    CASE ROW_NUMBER() OVER (ORDER BY c.DPId) % 2 WHEN 0 THEN 'BANK_TRANSFER' ELSE 'UPI' END,
    CASE ROW_NUMBER() OVER (ORDER BY c.DPId) % 4 WHEN 0 THEN 'PENDING' ELSE 'COMPLETED' END,
    DATEADD(DAY, -CAST(RAND(CHECKSUM(c.DPId)) * 7 AS INT), GETUTCDATE()),
    CASE ROW_NUMBER() OVER (ORDER BY c.DPId) % 4 WHEN 0 THEN NULL ELSE DATEADD(DAY, -CAST(RAND(CHECKSUM(c.DPId)) * 5 AS INT), GETUTCDATE()) END,
    DATEADD(DAY, -7, GETUTCDATE()),
    GETUTCDATE()
FROM CommissionRecords c
WHERE c.Status = 'SETTLED'
GROUP BY c.DPId
HAVING SUM(c.DPEarning) > 500
  AND NOT EXISTS (SELECT 1 FROM Settlements s WHERE s.BeneficiaryId = c.DPId AND s.BeneficiaryType = 'DP' AND s.CreatedAt > DATEADD(DAY, -7, GETUTCDATE()));

SET @SettlementCount = @@ROWCOUNT;
PRINT '  -> Created ' + CAST(@SettlementCount AS VARCHAR) + ' Settlements';
GO

-- =====================================================
-- SUMMARY
-- =====================================================
PRINT '';
PRINT '====================================';
PRINT 'STEP 5 COMPLETE: Transactions Summary';
PRINT '====================================';

SELECT 'WalletTransactions' AS Entity, COUNT(*) AS Count FROM WalletTransactions
UNION ALL SELECT 'Payments', COUNT(*) FROM Payments
UNION ALL SELECT 'CommissionRecords', COUNT(*) FROM CommissionRecords
UNION ALL SELECT 'Settlements', COUNT(*) FROM Settlements;

SELECT TransactionType, Category, COUNT(*) AS Count, SUM(Amount) AS TotalAmount
FROM WalletTransactions
GROUP BY TransactionType, Category
ORDER BY TransactionType, Category;
GO
