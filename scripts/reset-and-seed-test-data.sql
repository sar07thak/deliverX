-- ============================================
-- DeliveryDost - Reset and Seed Test Data
-- Run this script to clear all data and create test users
-- ============================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- ============================================
-- STEP 1: DISABLE ALL FOREIGN KEYS
-- ============================================
PRINT 'Disabling all foreign key constraints...'

DECLARE @sql NVARCHAR(MAX) = '';

SELECT @sql = @sql + 'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + '.' + QUOTENAME(OBJECT_NAME(parent_object_id)) + ' NOCHECK CONSTRAINT ' + QUOTENAME(name) + ';' + CHAR(13)
FROM sys.foreign_keys;

EXEC sp_executesql @sql;

PRINT 'Foreign key constraints disabled.'

-- ============================================
-- STEP 2: DELETE ALL DATA FROM ALL TABLES
-- ============================================
PRINT 'Deleting all data from tables...'

-- Delete from child tables first (in dependency order)
DELETE FROM WalletTransactions;
DELETE FROM Wallets;
DELETE FROM Ratings;
DELETE FROM ProofOfDeliveries;
DELETE FROM DeliveryEvents;
DELETE FROM DeliveryEventsArchive;
DELETE FROM DeliveryBids;
DELETE FROM DeliveryMatchingHistories;
DELETE FROM DeliveryAddresses;
DELETE FROM DeliveryPackages;
DELETE FROM DeliveryRoutes;
DELETE FROM DeliveryStatusHistory;
DELETE FROM DeliveriesArchive;
DELETE FROM Deliveries;
DELETE FROM ComplaintComments;
DELETE FROM ComplaintEvidences;
DELETE FROM Complaints;
DELETE FROM DPAvailabilities;
DELETE FROM DPAvailabilitySchedules;
DELETE FROM DPServiceAreaPincodes;
DELETE FROM DPServiceAreas;
DELETE FROM ServiceAreas;
DELETE FROM BCPickupLocations;
DELETE FROM BCAddresses;
DELETE FROM BCSubscriptionHistory;
DELETE FROM SavedAddresses;
DELETE FROM KYCRequests;
DELETE FROM AadhaarVerifications;
DELETE FROM PANVerifications;
DELETE FROM BankVerifications;
DELETE FROM VehicleLicenseVerifications;
DELETE FROM PoliceVerifications;
DELETE FROM PincodeDPCMMappings;
DELETE FROM DPCMServiceRegions;
DELETE FROM DPCMSecurityDeposits;
DELETE FROM CommissionRecords;
DELETE FROM SettlementItems;
DELETE FROM Settlements;
DELETE FROM Payments;
DELETE FROM PromoCodeUsages;
DELETE FROM PromoCodes;
DELETE FROM Donations;
DELETE FROM DonationPreferences;
DELETE FROM DeliveryPartnerProfiles;
DELETE FROM DeliveryPartners;
DELETE FROM DPCMCommissionConfigs;
DELETE FROM DPCManagers;
DELETE FROM BusinessConsumerProfiles;
DELETE FROM BusinessConsumers;
DELETE FROM Inspectors;
DELETE FROM SubscriptionInvoices;
DELETE FROM UserSubscriptions;
DELETE FROM Referrals;
DELETE FROM ReferralCodes;
DELETE FROM UserSessions;
DELETE FROM OTPVerifications;
DELETE FROM AuthAuditLogs;
DELETE FROM AdminAuditLogs;
DELETE FROM RolePermissions;
DELETE FROM DPLanguages;
DELETE FROM DPOperationalStatus;
DELETE FROM DPPricingConfigs;
DELETE FROM BehaviorIndexes;
DELETE FROM ChangeTrackingLog;
DELETE FROM AuditLogsArchive;
DELETE FROM Users;

PRINT 'All data deleted!'

-- ============================================
-- STEP 3: RE-ENABLE ALL FOREIGN KEYS
-- ============================================
PRINT 'Re-enabling foreign key constraints...'

SET @sql = '';

SELECT @sql = @sql + 'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + '.' + QUOTENAME(OBJECT_NAME(parent_object_id)) + ' CHECK CONSTRAINT ' + QUOTENAME(name) + ';' + CHAR(13)
FROM sys.foreign_keys;

EXEC sp_executesql @sql;

PRINT 'Foreign key constraints re-enabled.'

-- ============================================
-- STEP 4: CREATE TEST USERS
-- ============================================
PRINT 'Creating test users...'

-- Define GUIDs for test users
DECLARE @AdminUserId UNIQUEIDENTIFIER = NEWID();
DECLARE @SuperAdminUserId UNIQUEIDENTIFIER = NEWID();
DECLARE @DPCMUser1Id UNIQUEIDENTIFIER = NEWID();
DECLARE @DPCMUser2Id UNIQUEIDENTIFIER = NEWID();
DECLARE @DPUser1Id UNIQUEIDENTIFIER = NEWID();
DECLARE @DPUser2Id UNIQUEIDENTIFIER = NEWID();
DECLARE @DPUser3Id UNIQUEIDENTIFIER = NEWID();
DECLARE @BCUser1Id UNIQUEIDENTIFIER = NEWID();
DECLARE @BCUser2Id UNIQUEIDENTIFIER = NEWID();
DECLARE @ECUser1Id UNIQUEIDENTIFIER = NEWID();
DECLARE @DBCUser1Id UNIQUEIDENTIFIER = NEWID();

-- ============================================
-- INSERT TEST USERS
-- ============================================

-- SuperAdmin (with completed profile)
INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, Is2FAEnabled, IsActive, IsEmailVerified, IsPhoneVerified, FailedLoginAttempts, CreatedAt, UpdatedAt)
VALUES (@SuperAdminUserId, '9999999999', 'superadmin@deliverydost.com', 'Super Admin', NULL, 'SuperAdmin', 0, 1, 0, 1, 0, GETUTCDATE(), GETUTCDATE());

-- Admin (with completed profile)
INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, Is2FAEnabled, IsActive, IsEmailVerified, IsPhoneVerified, FailedLoginAttempts, CreatedAt, UpdatedAt)
VALUES (@AdminUserId, '9999999998', 'admin@deliverydost.com', 'Test Admin', NULL, 'Admin', 0, 1, 0, 1, 0, GETUTCDATE(), GETUTCDATE());

-- ============================================
-- DPCM Users
-- ============================================

-- DPCM 1: NEW - Needs to complete registration
INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, Is2FAEnabled, IsActive, IsEmailVerified, IsPhoneVerified, FailedLoginAttempts, CreatedAt, UpdatedAt)
VALUES (@DPCMUser1Id, '9100000001', NULL, NULL, NULL, 'DPCM', 0, 1, 0, 1, 0, GETUTCDATE(), GETUTCDATE());

-- DPCM 2: COMPLETED - Has profile already
INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, Is2FAEnabled, IsActive, IsEmailVerified, IsPhoneVerified, FailedLoginAttempts, CreatedAt, UpdatedAt)
VALUES (@DPCMUser2Id, '9100000002', 'dpcm.complete@test.com', 'Completed DPCM', NULL, 'DPCM', 0, 1, 0, 1, 0, GETUTCDATE(), GETUTCDATE());

-- Create DPCManager profile for DPCM2
INSERT INTO DPCManagers (Id, UserId, OrganizationName, ContactPersonName, PAN, ServiceRegions, CommissionType, CommissionValue, IsActive, ActivatedAt, CreatedAt, UpdatedAt)
VALUES (NEWID(), @DPCMUser2Id, 'Test DPCM Organization', 'Completed DPCM', 'ABCDE1234F', '["Mumbai", "Delhi"]', 'PERCENTAGE', 5.0, 1, GETUTCDATE(), GETUTCDATE(), GETUTCDATE());

-- ============================================
-- DP Users (Delivery Partners)
-- ============================================

-- DP 1: NEW - Needs to complete registration
INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, Is2FAEnabled, IsActive, IsEmailVerified, IsPhoneVerified, FailedLoginAttempts, CreatedAt, UpdatedAt)
VALUES (@DPUser1Id, '9200000001', NULL, NULL, NULL, 'DP', 0, 1, 0, 1, 0, GETUTCDATE(), GETUTCDATE());

-- DP 2: NEW - Needs to complete registration
INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, Is2FAEnabled, IsActive, IsEmailVerified, IsPhoneVerified, FailedLoginAttempts, CreatedAt, UpdatedAt)
VALUES (@DPUser2Id, '9200000002', NULL, NULL, NULL, 'DP', 0, 1, 0, 1, 0, GETUTCDATE(), GETUTCDATE());

-- DP 3: COMPLETED - Has profile already
INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, Is2FAEnabled, IsActive, IsEmailVerified, IsPhoneVerified, FailedLoginAttempts, CreatedAt, UpdatedAt)
VALUES (@DPUser3Id, '9200000003', 'dp.complete@test.com', 'Active Delivery Partner', NULL, 'DP', 0, 1, 0, 1, 0, GETUTCDATE(), GETUTCDATE());

-- Create DP profile for DP3
INSERT INTO DeliveryPartnerProfiles (Id, UserId, DPCMId, FullName, DOB, Gender, VehicleType, Availability, IsActive, IsOnline, ActivatedAt, CreatedAt, UpdatedAt)
VALUES (NEWID(), @DPUser3Id, NULL, 'Active Delivery Partner', '1995-05-15', 'Male', 'BIKE', 'Full-time', 1, 0, GETUTCDATE(), GETUTCDATE(), GETUTCDATE());

-- ============================================
-- BC Users (Business Consumers)
-- ============================================

-- BC 1: NEW - Needs to complete registration
INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, Is2FAEnabled, IsActive, IsEmailVerified, IsPhoneVerified, FailedLoginAttempts, CreatedAt, UpdatedAt)
VALUES (@BCUser1Id, '9300000001', NULL, NULL, NULL, 'BC', 0, 1, 0, 1, 0, GETUTCDATE(), GETUTCDATE());

-- BC 2: COMPLETED - Has profile already
INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, Is2FAEnabled, IsActive, IsEmailVerified, IsPhoneVerified, FailedLoginAttempts, CreatedAt, UpdatedAt)
VALUES (@BCUser2Id, '9300000002', 'bc.complete@test.com', 'Test Business', NULL, 'BC', 0, 1, 0, 1, 0, GETUTCDATE(), GETUTCDATE());

-- Create BC profile for BC2
INSERT INTO BusinessConsumerProfiles (Id, UserId, BusinessName, ContactPersonName, PAN, BusinessCategory, IsActive, ActivatedAt, CreatedAt, UpdatedAt)
VALUES (NEWID(), @BCUser2Id, 'Test Business Pvt Ltd', 'Business Owner', 'BCDEF2345G', 'E-commerce', 1, GETUTCDATE(), GETUTCDATE(), GETUTCDATE());

-- ============================================
-- EC Users (Enterprise Clients)
-- ============================================

-- EC 1: NEW - Needs to complete registration
INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, Is2FAEnabled, IsActive, IsEmailVerified, IsPhoneVerified, FailedLoginAttempts, CreatedAt, UpdatedAt)
VALUES (@ECUser1Id, '9400000001', NULL, NULL, NULL, 'EC', 0, 1, 0, 1, 0, GETUTCDATE(), GETUTCDATE());

-- ============================================
-- DBC Users (Delivery Business Consumers)
-- ============================================

-- DBC 1: NEW - Needs to complete registration
INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, Is2FAEnabled, IsActive, IsEmailVerified, IsPhoneVerified, FailedLoginAttempts, CreatedAt, UpdatedAt)
VALUES (@DBCUser1Id, '9500000001', NULL, NULL, NULL, 'DBC', 0, 1, 0, 1, 0, GETUTCDATE(), GETUTCDATE());

-- ============================================
-- CREATE WALLETS FOR COMPLETED USERS
-- ============================================

INSERT INTO Wallets (Id, UserId, Balance, HoldBalance, Currency, WalletType, IsActive, CreatedAt, UpdatedAt)
VALUES
    (NEWID(), @SuperAdminUserId, 100000, 0, 'INR', 'MAIN', 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @AdminUserId, 50000, 0, 'INR', 'MAIN', 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DPCMUser2Id, 10000, 0, 'INR', 'MAIN', 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DPUser3Id, 5000, 0, 'INR', 'MAIN', 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @BCUser2Id, 25000, 0, 'INR', 'MAIN', 1, GETUTCDATE(), GETUTCDATE());

PRINT 'Test data created successfully!'

-- ============================================
-- PRINT SUMMARY
-- ============================================
PRINT ''
PRINT '============================================'
PRINT 'TEST ACCOUNTS CREATED'
PRINT '============================================'
PRINT ''
PRINT '--- ADMIN ACCOUNTS (Profile Complete) ---'
PRINT 'SuperAdmin: 9999999999'
PRINT 'Admin: 9999999998'
PRINT ''
PRINT '--- DPCM ACCOUNTS ---'
PRINT 'DPCM (New - Register): 9100000001'
PRINT 'DPCM (Complete): 9100000002'
PRINT ''
PRINT '--- DP ACCOUNTS ---'
PRINT 'DP (New - Register): 9200000001'
PRINT 'DP (New - Register): 9200000002'
PRINT 'DP (Complete): 9200000003'
PRINT ''
PRINT '--- BC ACCOUNTS ---'
PRINT 'BC (New - Register): 9300000001'
PRINT 'BC (Complete): 9300000002'
PRINT ''
PRINT '--- EC ACCOUNTS ---'
PRINT 'EC (New - Register): 9400000001'
PRINT ''
PRINT '--- DBC ACCOUNTS ---'
PRINT 'DBC (New - Register): 9500000001'
PRINT ''
PRINT '============================================'
PRINT 'Login with phone number, OTP will be sent'
PRINT '============================================'
