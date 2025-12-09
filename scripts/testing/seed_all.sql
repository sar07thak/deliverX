-- =====================================================
-- DELIVERYDOST - MASTER SEED DATA RUNNER
-- =====================================================
-- This script runs all seed scripts in the correct order
-- respecting foreign key dependencies
-- =====================================================

USE DeliveryDost_Dev;
GO

SET NOCOUNT ON;
PRINT '====================================================';
PRINT 'DELIVERYDOST - MASTER SEED DATA RUNNER';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETUTCDATE(), 121);
PRINT '====================================================';
PRINT '';

-- =====================================================
-- PRE-SEED: Check Database Connection
-- =====================================================
IF DB_NAME() != 'DeliveryDost_Dev'
BEGIN
    RAISERROR('ERROR: Must be connected to DeliveryDost_Dev database!', 16, 1);
    RETURN;
END

-- =====================================================
-- PRE-SEED: Optional - Clear Existing Data
-- =====================================================
-- Uncomment the following section to clear existing data before seeding
-- WARNING: This will DELETE ALL DATA from the database!
/*
PRINT 'Clearing existing data...';
PRINT '';

-- Disable all foreign key constraints
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';

-- Delete in reverse dependency order
DELETE FROM ComplaintEvidences;
DELETE FROM ComplaintComments;
DELETE FROM Complaints;
DELETE FROM Settlements;
DELETE FROM CommissionRecords;
DELETE FROM Payments;
DELETE FROM WalletTransactions;
DELETE FROM Ratings;
DELETE FROM ProofOfDeliveries;
DELETE FROM DeliveryEvents;
DELETE FROM DeliveryBids;
DELETE FROM Deliveries;
DELETE FROM SavedAddresses;
DELETE FROM Wallets;
DELETE FROM Inspectors;
DELETE FROM BusinessConsumerProfiles;
DELETE FROM DeliveryPartnerProfiles;
DELETE FROM PincodeDPCMMappings;
DELETE FROM DPCManagers;
DELETE FROM Users;
DELETE FROM PincodeMasters;
DELETE FROM ComplaintSLAConfigs;
DELETE FROM Charities;
DELETE FROM SubscriptionPlans;
DELETE FROM MasterDeliveryStatuses;
DELETE FROM MasterCautionTypes;
DELETE FROM MasterPackageTypes;
DELETE FROM VehicleTypes;
DELETE FROM MasterLanguages;

-- Re-enable all foreign key constraints
EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';

PRINT 'Existing data cleared.';
PRINT '';
*/

-- =====================================================
-- STEP 1: Master Data (Lookup Tables)
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'STEP 1: Seeding Master Data...';
PRINT '----------------------------------------------------';

-- Languages
IF NOT EXISTS (SELECT 1 FROM MasterLanguages WHERE Code = 'hi')
BEGIN
    INSERT INTO MasterLanguages (Id, Code, Name, NativeName, IsActive, SortOrder, CreatedAt, UpdatedAt)
    VALUES
    (NEWID(), 'hi', 'Hindi', N'हिन्दी', 1, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'en', 'English', 'English', 1, 2, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'mr', 'Marathi', N'मराठी', 1, 3, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'gu', 'Gujarati', N'ગુજરાતી', 1, 4, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'ta', 'Tamil', N'தமிழ்', 1, 5, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'te', 'Telugu', N'తెలుగు', 1, 6, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'kn', 'Kannada', N'ಕನ್ನಡ', 1, 7, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'ml', 'Malayalam', N'മലയാളം', 1, 8, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'pa', 'Punjabi', N'ਪੰਜਾਬੀ', 1, 9, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'bn', 'Bengali', N'বাংলা', 1, 10, GETUTCDATE(), GETUTCDATE());
    PRINT '  -> Created 10 languages';
END
ELSE
    PRINT '  -> Languages already exist, skipping';

-- Package Types
IF NOT EXISTS (SELECT 1 FROM MasterPackageTypes WHERE Code = 'DOCUMENT')
BEGIN
    INSERT INTO MasterPackageTypes (Id, Code, Name, Description, MaxWeightKg, IsActive, SortOrder, CreatedAt, UpdatedAt)
    VALUES
    (NEWID(), 'DOCUMENT', 'Document', 'Papers, letters, certificates', 2, 1, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'SMALL_PARCEL', 'Small Parcel', 'Small boxes, envelopes', 5, 1, 2, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'MEDIUM_PARCEL', 'Medium Parcel', 'Medium sized packages', 15, 1, 3, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'LARGE_PARCEL', 'Large Parcel', 'Large boxes and packages', 30, 1, 4, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'FRAGILE', 'Fragile', 'Breakable items, electronics', 20, 1, 5, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'FOOD', 'Food Items', 'Perishable food items', 10, 1, 6, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'MEDICINE', 'Medicine', 'Pharmaceutical products', 5, 1, 7, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'VALUABLE', 'Valuable', 'High value items, jewelry', 5, 1, 8, GETUTCDATE(), GETUTCDATE());
    PRINT '  -> Created 8 package types';
END
ELSE
    PRINT '  -> Package types already exist, skipping';

-- Caution Types
IF NOT EXISTS (SELECT 1 FROM MasterCautionTypes WHERE Code = 'FRAGILE')
BEGIN
    INSERT INTO MasterCautionTypes (Id, Code, Name, Description, IconUrl, IsActive, SortOrder, CreatedAt, UpdatedAt)
    VALUES
    (NEWID(), 'FRAGILE', 'Fragile', 'Handle with care', '/icons/fragile.svg', 1, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'PERISHABLE', 'Perishable', 'Time sensitive delivery', '/icons/perishable.svg', 1, 2, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'KEEP_UPRIGHT', 'Keep Upright', 'Do not tilt or invert', '/icons/upright.svg', 1, 3, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'NO_STACK', 'Do Not Stack', 'Keep on top', '/icons/nostack.svg', 1, 4, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'TEMPERATURE', 'Temperature Sensitive', 'Keep cool/warm', '/icons/temperature.svg', 1, 5, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'LIQUID', 'Contains Liquid', 'May spill if tilted', '/icons/liquid.svg', 1, 6, GETUTCDATE(), GETUTCDATE());
    PRINT '  -> Created 6 caution types';
END
ELSE
    PRINT '  -> Caution types already exist, skipping';

-- Delivery Statuses
IF NOT EXISTS (SELECT 1 FROM MasterDeliveryStatuses WHERE Code = 'PENDING')
BEGIN
    INSERT INTO MasterDeliveryStatuses (Id, Code, Name, Description, ColorHex, SortOrder, IsTerminal, IsActive, CreatedAt, UpdatedAt)
    VALUES
    (NEWID(), 'PENDING', 'Pending', 'Awaiting DP assignment', '#FFA500', 1, 0, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'MATCHING', 'Matching', 'Finding delivery partner', '#17A2B8', 2, 0, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'ASSIGNED', 'Assigned', 'DP assigned, awaiting acceptance', '#007BFF', 3, 0, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'ACCEPTED', 'Accepted', 'DP has accepted the delivery', '#28A745', 4, 0, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'REACHED_PICKUP', 'Reached Pickup', 'DP at pickup location', '#6F42C1', 5, 0, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'PICKED_UP', 'Picked Up', 'Package collected', '#20C997', 6, 0, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'IN_TRANSIT', 'In Transit', 'On the way to destination', '#17A2B8', 7, 0, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'REACHED_DROP', 'Reached Drop', 'DP at drop location', '#6F42C1', 8, 0, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'DELIVERED', 'Delivered', 'Successfully delivered', '#28A745', 9, 1, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'CANCELLED', 'Cancelled', 'Delivery cancelled', '#DC3545', 10, 1, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'FAILED', 'Failed', 'Delivery failed', '#DC3545', 11, 1, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'RETURNED', 'Returned', 'Package returned to sender', '#6C757D', 12, 1, 1, GETUTCDATE(), GETUTCDATE());
    PRINT '  -> Created 12 delivery statuses';
END
ELSE
    PRINT '  -> Delivery statuses already exist, skipping';

-- Subscription Plans
IF NOT EXISTS (SELECT 1 FROM SubscriptionPlans WHERE Code = 'BC_FREE')
BEGIN
    INSERT INTO SubscriptionPlans (Id, Code, Name, Description, PlanType, Price, DurationDays, FreeDeliveries, DiscountPercentage, MaxMonthlyDeliveries, PrioritySupport, Features, IsActive, CreatedAt, UpdatedAt)
    VALUES
    (NEWID(), 'BC_FREE', 'BC Free', 'Basic plan for business consumers', 'BC', 0, 30, 5, 0, 50, 0, '{"features": ["Basic support", "Standard delivery"]}', 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'BC_BASIC', 'BC Basic', 'Basic paid plan', 'BC', 499, 30, 20, 5, 200, 0, '{"features": ["Email support", "5% discount"]}', 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'BC_PREMIUM', 'BC Premium', 'Premium business plan', 'BC', 1999, 30, 100, 15, 1000, 1, '{"features": ["Priority support", "15% discount", "Dedicated manager"]}', 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'EC_FREE', 'EC Free', 'Free plan for end consumers', 'EC', 0, 30, 2, 0, 20, 0, '{"features": ["Basic support"]}', 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'EC_PLUS', 'EC Plus', 'Plus plan for regular users', 'EC', 99, 30, 10, 10, 50, 0, '{"features": ["Priority delivery", "10% discount"]}', 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'EC_PREMIUM', 'EC Premium', 'Premium consumer plan', 'EC', 299, 30, 30, 20, 100, 1, '{"features": ["Priority support", "20% discount", "Free cancellation"]}', 1, GETUTCDATE(), GETUTCDATE());
    PRINT '  -> Created 6 subscription plans';
END
ELSE
    PRINT '  -> Subscription plans already exist, skipping';

-- Charities
IF NOT EXISTS (SELECT 1 FROM Charities WHERE Name LIKE '%Akshaya Patra%')
BEGIN
    INSERT INTO Charities (Id, Name, Description, LogoUrl, IsActive, CreatedAt, UpdatedAt)
    VALUES
    (NEWID(), 'Akshaya Patra Foundation', 'Providing mid-day meals to school children', '/charities/akshaya-patra.png', 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'GiveIndia', 'Platform for charitable giving', '/charities/giveindia.png', 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'CRY India', 'Child Rights and You', '/charities/cry.png', 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'Smile Foundation', 'Education and healthcare for underprivileged', '/charities/smile.png', 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'HelpAge India', 'Supporting elderly citizens', '/charities/helpage.png', 1, GETUTCDATE(), GETUTCDATE());
    PRINT '  -> Created 5 charities';
END
ELSE
    PRINT '  -> Charities already exist, skipping';

-- Complaint SLA Configs
IF NOT EXISTS (SELECT 1 FROM ComplaintSLAConfigs WHERE Category = 'DAMAGE')
BEGIN
    INSERT INTO ComplaintSLAConfigs (Id, Category, Severity, ResponseTimeHours, ResolutionTimeHours, EscalationTimeHours, IsActive, CreatedAt, UpdatedAt)
    VALUES
    (NEWID(), 'DAMAGE', 'LOW', 24, 72, 48, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'DAMAGE', 'MEDIUM', 12, 48, 24, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'DAMAGE', 'HIGH', 4, 24, 12, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'DAMAGE', 'CRITICAL', 1, 12, 4, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'DELAY', 'LOW', 48, 96, 72, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'DELAY', 'MEDIUM', 24, 72, 48, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'THEFT', 'HIGH', 2, 24, 8, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'THEFT', 'CRITICAL', 1, 12, 4, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'FRAUD', 'HIGH', 2, 24, 8, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'FRAUD', 'CRITICAL', 1, 12, 4, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'BEHAVIOR', 'MEDIUM', 24, 72, 48, 1, GETUTCDATE(), GETUTCDATE());
    PRINT '  -> Created 11 SLA configs';
END
ELSE
    PRINT '  -> SLA configs already exist, skipping';

-- Pincode Masters (Jaipur, Delhi, Mumbai)
IF NOT EXISTS (SELECT 1 FROM PincodeMasters WHERE Pincode = '302001')
BEGIN
    INSERT INTO PincodeMasters (Id, Pincode, City, State, Latitude, Longitude, IsServiceable, CreatedAt, UpdatedAt)
    VALUES
    -- Jaipur Pincodes
    (NEWID(), '302001', 'Jaipur', 'Rajasthan', 26.9124, 75.7873, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '302002', 'Jaipur', 'Rajasthan', 26.9260, 75.8020, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '302003', 'Jaipur', 'Rajasthan', 26.8890, 75.8030, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '302004', 'Jaipur', 'Rajasthan', 26.8750, 75.7650, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '302005', 'Jaipur', 'Rajasthan', 26.8450, 75.8050, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '302015', 'Jaipur', 'Rajasthan', 26.9500, 75.7200, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '302016', 'Jaipur', 'Rajasthan', 26.9350, 75.7400, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '302017', 'Jaipur', 'Rajasthan', 26.9050, 75.7300, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '302018', 'Jaipur', 'Rajasthan', 26.8800, 75.7500, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '302019', 'Jaipur', 'Rajasthan', 26.8600, 75.7800, 1, GETUTCDATE(), GETUTCDATE()),
    -- Delhi Pincodes
    (NEWID(), '110001', 'Delhi', 'Delhi', 28.6328, 77.2197, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '110002', 'Delhi', 'Delhi', 28.6400, 77.2300, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '110003', 'Delhi', 'Delhi', 28.6500, 77.2100, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '110005', 'Delhi', 'Delhi', 28.6350, 77.2000, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '110006', 'Delhi', 'Delhi', 28.6600, 77.2200, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '110007', 'Delhi', 'Delhi', 28.6700, 77.2050, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '110008', 'Delhi', 'Delhi', 28.6800, 77.1900, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '110009', 'Delhi', 'Delhi', 28.6900, 77.1800, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '110010', 'Delhi', 'Delhi', 28.5900, 77.2300, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '110011', 'Delhi', 'Delhi', 28.5800, 77.2400, 1, GETUTCDATE(), GETUTCDATE()),
    -- Mumbai Pincodes
    (NEWID(), '400001', 'Mumbai', 'Maharashtra', 18.9388, 72.8354, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '400002', 'Mumbai', 'Maharashtra', 18.9500, 72.8400, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '400003', 'Mumbai', 'Maharashtra', 18.9600, 72.8300, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '400004', 'Mumbai', 'Maharashtra', 18.9700, 72.8200, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '400005', 'Mumbai', 'Maharashtra', 18.9550, 72.8250, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '400007', 'Mumbai', 'Maharashtra', 18.9800, 72.8100, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '400008', 'Mumbai', 'Maharashtra', 18.9900, 72.8050, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '400009', 'Mumbai', 'Maharashtra', 19.0000, 72.8200, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '400010', 'Mumbai', 'Maharashtra', 19.0100, 72.8350, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), '400011', 'Mumbai', 'Maharashtra', 19.0200, 72.8450, 1, GETUTCDATE(), GETUTCDATE());
    PRINT '  -> Created 30 pincodes';
END
ELSE
    PRINT '  -> Pincodes already exist, skipping';

PRINT '';
PRINT 'Step 1 Complete: Master data seeded.';
PRINT '';

-- =====================================================
-- STEP 2-6: Run Individual Seed Scripts
-- =====================================================
-- Note: In production, you would use :r to include scripts
-- For now, this provides the structure and Step 1 inline

PRINT '----------------------------------------------------';
PRINT 'REMAINING STEPS:';
PRINT '----------------------------------------------------';
PRINT 'Execute the following scripts in order:';
PRINT '  1. seed_02_users.sql      - Creates 225+ users';
PRINT '  2. seed_03_profiles.sql   - Creates profiles & wallets';
PRINT '  3. seed_04_deliveries.sql - Creates 300 deliveries';
PRINT '  4. seed_05_transactions.sql - Creates transactions';
PRINT '  5. seed_06_complaints.sql - Creates complaints';
PRINT '';
PRINT 'Or use SQLCMD mode with :r command:';
PRINT '  :r seed_02_users.sql';
PRINT '  :r seed_03_profiles.sql';
PRINT '  etc.';
PRINT '';

-- =====================================================
-- FINAL VERIFICATION
-- =====================================================
PRINT '====================================================';
PRINT 'CURRENT DATA COUNTS';
PRINT '====================================================';

SELECT 'MasterLanguages' AS Entity, COUNT(*) AS Count FROM MasterLanguages
UNION ALL SELECT 'MasterPackageTypes', COUNT(*) FROM MasterPackageTypes
UNION ALL SELECT 'MasterCautionTypes', COUNT(*) FROM MasterCautionTypes
UNION ALL SELECT 'MasterDeliveryStatuses', COUNT(*) FROM MasterDeliveryStatuses
UNION ALL SELECT 'SubscriptionPlans', COUNT(*) FROM SubscriptionPlans
UNION ALL SELECT 'Charities', COUNT(*) FROM Charities
UNION ALL SELECT 'ComplaintSLAConfigs', COUNT(*) FROM ComplaintSLAConfigs
UNION ALL SELECT 'PincodeMasters', COUNT(*) FROM PincodeMasters
UNION ALL SELECT 'Users', COUNT(*) FROM Users
UNION ALL SELECT 'DPCManagers', COUNT(*) FROM DPCManagers
UNION ALL SELECT 'DeliveryPartnerProfiles', COUNT(*) FROM DeliveryPartnerProfiles
UNION ALL SELECT 'BusinessConsumerProfiles', COUNT(*) FROM BusinessConsumerProfiles
UNION ALL SELECT 'Inspectors', COUNT(*) FROM Inspectors
UNION ALL SELECT 'Wallets', COUNT(*) FROM Wallets
UNION ALL SELECT 'Deliveries', COUNT(*) FROM Deliveries
UNION ALL SELECT 'WalletTransactions', COUNT(*) FROM WalletTransactions
UNION ALL SELECT 'Complaints', COUNT(*) FROM Complaints
ORDER BY Entity;

PRINT '';
PRINT '====================================================';
PRINT 'SEED RUNNER COMPLETE';
PRINT 'Finished at: ' + CONVERT(VARCHAR, GETUTCDATE(), 121);
PRINT '====================================================';
GO
