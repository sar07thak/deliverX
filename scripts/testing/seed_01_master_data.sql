-- =====================================================
-- DELIVERYDOST SEED DATA - STEP 1: MASTER/LOOKUP TABLES
-- =====================================================
-- Run this FIRST before any other seed scripts
-- =====================================================

USE DeliveryDost_Dev;
GO

SET NOCOUNT ON;
PRINT '====================================';
PRINT 'STEP 1: SEEDING MASTER/LOOKUP DATA';
PRINT '====================================';

-- =====================================================
-- 1.1 MASTER LANGUAGES
-- =====================================================
PRINT 'Seeding MasterLanguages...';

IF NOT EXISTS (SELECT 1 FROM MasterLanguages WHERE Code = 'HI')
BEGIN
    SET IDENTITY_INSERT MasterLanguages ON;
    INSERT INTO MasterLanguages (Id, Code, Name, NativeName, IsActive)
    VALUES
        (1, 'HI', 'Hindi', N'हिन्दी', 1),
        (2, 'EN', 'English', 'English', 1),
        (3, 'TE', 'Telugu', N'తెలుగు', 1),
        (4, 'TA', 'Tamil', N'தமிழ்', 1),
        (5, 'MR', 'Marathi', N'मराठी', 1),
        (6, 'GU', 'Gujarati', N'ગુજરાતી', 1),
        (7, 'KN', 'Kannada', N'ಕನ್ನಡ', 1),
        (8, 'BN', 'Bengali', N'বাংলা', 1),
        (9, 'PA', 'Punjabi', N'ਪੰਜਾਬੀ', 1),
        (10, 'RJ', 'Rajasthani', N'राजस्थानी', 1);
    SET IDENTITY_INSERT MasterLanguages OFF;
    PRINT '  -> Inserted 10 languages';
END
ELSE
    PRINT '  -> Languages already exist, skipping';
GO

-- =====================================================
-- 1.2 MASTER PACKAGE TYPES
-- =====================================================
PRINT 'Seeding MasterPackageTypes...';

IF NOT EXISTS (SELECT 1 FROM MasterPackageTypes WHERE Code = 'DOC')
BEGIN
    SET IDENTITY_INSERT MasterPackageTypes ON;
    INSERT INTO MasterPackageTypes (Id, Code, Name, Description, RequiresSpecialHandling, SortOrder, IsActive)
    VALUES
        (1, 'DOC', 'Document', 'Letters, papers, certificates', 0, 1, 1),
        (2, 'PARCEL', 'Parcel', 'General packages and boxes', 0, 2, 1),
        (3, 'FOOD', 'Food', 'Prepared food, perishables', 1, 3, 1),
        (4, 'MEDICINE', 'Medicine', 'Pharmaceutical products', 1, 4, 1),
        (5, 'ELECTRONICS', 'Electronics', 'Gadgets, devices', 1, 5, 1),
        (6, 'FRAGILE', 'Fragile', 'Glass, ceramics, delicate items', 1, 6, 1),
        (7, 'HEAVY', 'Heavy Goods', 'Items over 20kg', 1, 7, 1),
        (8, 'LIQUID', 'Liquid', 'Bottled liquids, beverages', 1, 8, 1);
    SET IDENTITY_INSERT MasterPackageTypes OFF;
    PRINT '  -> Inserted 8 package types';
END
ELSE
    PRINT '  -> Package types already exist, skipping';
GO

-- =====================================================
-- 1.3 MASTER CAUTION TYPES
-- =====================================================
PRINT 'Seeding MasterCautionTypes...';

IF NOT EXISTS (SELECT 1 FROM MasterCautionTypes WHERE Code = 'FRAGILE')
BEGIN
    SET IDENTITY_INSERT MasterCautionTypes ON;
    INSERT INTO MasterCautionTypes (Id, Code, Name, Description, Severity, HandlingInstructions, IconClass, SortOrder, IsActive)
    VALUES
        (1, 'FRAGILE', 'Fragile', 'Handle with extreme care', 3, 'Do not drop, keep upright', 'fa-wine-glass', 1, 1),
        (2, 'LIQUID', 'Contains Liquid', 'May leak if tilted', 2, 'Keep upright, avoid shaking', 'fa-tint', 2, 1),
        (3, 'HEAVY', 'Heavy Item', 'Requires careful lifting', 2, 'Use proper lifting technique', 'fa-weight', 3, 1),
        (4, 'PERISHABLE', 'Perishable', 'Time-sensitive delivery', 3, 'Deliver within time window', 'fa-clock', 4, 1),
        (5, 'VALUABLE', 'High Value', 'Valuable contents', 3, 'Handle with care, signature required', 'fa-gem', 5, 1),
        (6, 'HAZMAT', 'Hazardous', 'Contains hazardous material', 4, 'Follow safety protocols', 'fa-exclamation-triangle', 6, 1);
    SET IDENTITY_INSERT MasterCautionTypes OFF;
    PRINT '  -> Inserted 6 caution types';
END
ELSE
    PRINT '  -> Caution types already exist, skipping';
GO

-- =====================================================
-- 1.4 MASTER DELIVERY STATUSES
-- =====================================================
PRINT 'Seeding MasterDeliveryStatuses...';

IF NOT EXISTS (SELECT 1 FROM MasterDeliveryStatuses WHERE Code = 'PENDING')
BEGIN
    SET IDENTITY_INSERT MasterDeliveryStatuses ON;
    INSERT INTO MasterDeliveryStatuses (Id, Code, Name, Description, StatusGroup, DisplayColor, IconClass, SortOrder, AllowedNextStatuses, IsTerminal, IsActive)
    VALUES
        (1, 'PENDING', 'Pending', 'Waiting for DP assignment', 'CREATED', '#FFA500', 'fa-clock', 1, 'MATCHING,CANCELLED', 0, 1),
        (2, 'MATCHING', 'Matching', 'Finding delivery partner', 'CREATED', '#FFD700', 'fa-search', 2, 'ASSIGNED,CANCELLED', 0, 1),
        (3, 'ASSIGNED', 'Assigned', 'DP assigned, awaiting acceptance', 'ASSIGNED', '#1E90FF', 'fa-user-check', 3, 'ACCEPTED,REJECTED,CANCELLED', 0, 1),
        (4, 'ACCEPTED', 'Accepted', 'DP accepted the delivery', 'IN_PROGRESS', '#32CD32', 'fa-thumbs-up', 4, 'EN_ROUTE_PICKUP,CANCELLED', 0, 1),
        (5, 'EN_ROUTE_PICKUP', 'En Route to Pickup', 'DP heading to pickup location', 'IN_PROGRESS', '#00CED1', 'fa-motorcycle', 5, 'ARRIVED_PICKUP,CANCELLED', 0, 1),
        (6, 'ARRIVED_PICKUP', 'Arrived at Pickup', 'DP at pickup location', 'IN_PROGRESS', '#9370DB', 'fa-map-marker-alt', 6, 'PICKED_UP,CANCELLED', 0, 1),
        (7, 'PICKED_UP', 'Picked Up', 'Package collected', 'IN_PROGRESS', '#20B2AA', 'fa-box', 7, 'EN_ROUTE_DROP,CANCELLED', 0, 1),
        (8, 'EN_ROUTE_DROP', 'En Route to Drop', 'DP heading to delivery location', 'IN_PROGRESS', '#00FA9A', 'fa-shipping-fast', 8, 'ARRIVED_DROP,CANCELLED', 0, 1),
        (9, 'ARRIVED_DROP', 'Arrived at Drop', 'DP at delivery location', 'IN_PROGRESS', '#7B68EE', 'fa-map-pin', 9, 'DELIVERED,FAILED', 0, 1),
        (10, 'DELIVERED', 'Delivered', 'Successfully delivered', 'COMPLETED', '#228B22', 'fa-check-circle', 10, NULL, 1, 1),
        (11, 'FAILED', 'Failed', 'Delivery failed', 'COMPLETED', '#DC143C', 'fa-times-circle', 11, NULL, 1, 1),
        (12, 'CANCELLED', 'Cancelled', 'Delivery cancelled', 'COMPLETED', '#A9A9A9', 'fa-ban', 12, NULL, 1, 1);
    SET IDENTITY_INSERT MasterDeliveryStatuses OFF;
    PRINT '  -> Inserted 12 delivery statuses';
END
ELSE
    PRINT '  -> Delivery statuses already exist, skipping';
GO

-- =====================================================
-- 1.5 MASTER BUSINESS CATEGORIES
-- =====================================================
PRINT 'Seeding MasterBusinessCategories...';

IF NOT EXISTS (SELECT 1 FROM MasterBusinessCategories WHERE Code = 'RETAIL')
BEGIN
    SET IDENTITY_INSERT MasterBusinessCategories ON;
    INSERT INTO MasterBusinessCategories (Id, Code, Name, Description, ParentId, SortOrder, IsActive)
    VALUES
        (1, 'RETAIL', 'Retail', 'Retail businesses', NULL, 1, 1),
        (2, 'ECOMMERCE', 'E-Commerce', 'Online stores', NULL, 2, 1),
        (3, 'FOOD', 'Food & Beverage', 'Restaurants, cafes, food businesses', NULL, 3, 1),
        (4, 'PHARMA', 'Pharmaceutical', 'Pharmacies and medical stores', NULL, 4, 1),
        (5, 'LOGISTICS', 'Logistics', 'Logistics and courier companies', NULL, 5, 1),
        (6, 'MANUFACTURING', 'Manufacturing', 'Manufacturing units', NULL, 6, 1),
        (7, 'SERVICES', 'Services', 'Service providers', NULL, 7, 1),
        (8, 'OTHER', 'Other', 'Other businesses', NULL, 8, 1);
    SET IDENTITY_INSERT MasterBusinessCategories OFF;
    PRINT '  -> Inserted 8 business categories';
END
ELSE
    PRINT '  -> Business categories already exist, skipping';
GO

-- =====================================================
-- 1.6 SUBSCRIPTION PLANS
-- =====================================================
PRINT 'Seeding SubscriptionPlans...';

IF NOT EXISTS (SELECT 1 FROM SubscriptionPlans WHERE Name = 'Free')
BEGIN
    INSERT INTO SubscriptionPlans (Id, Name, Description, PlanType, BillingCycle, Price, DiscountedPrice, DeliveryQuota, PerDeliveryDiscount, PrioritySupport, AdvancedAnalytics, Features, IsActive, SortOrder, CreatedAt, UpdatedAt)
    VALUES
        (NEWID(), 'Free', 'Basic free plan for individuals', 'EC', 'MONTHLY', 0, NULL, 10, 0, 0, 0, '["10 deliveries/month","Standard support","Basic tracking"]', 1, 1, GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'Basic EC', 'Basic plan for end consumers', 'EC', 'MONTHLY', 99, 79, 50, 5, 0, 0, '["50 deliveries/month","Email support","Real-time tracking","5% discount"]', 1, 2, GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'Pro EC', 'Professional plan for frequent users', 'EC', 'MONTHLY', 299, 249, 200, 10, 1, 1, '["200 deliveries/month","Priority support","Advanced tracking","10% discount","Analytics"]', 1, 3, GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'Starter BC', 'Starter plan for small businesses', 'BC', 'MONTHLY', 499, 399, 100, 8, 0, 0, '["100 deliveries/month","Business dashboard","8% discount","API access"]', 1, 4, GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'Business BC', 'Business plan for growing companies', 'BC', 'MONTHLY', 1499, 1299, 500, 12, 1, 1, '["500 deliveries/month","Priority support","12% discount","Full analytics","Dedicated manager"]', 1, 5, GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'Enterprise BC', 'Enterprise plan for large businesses', 'BC', 'MONTHLY', 4999, 3999, 2000, 15, 1, 1, '["Unlimited deliveries","24/7 support","15% discount","Custom integration","SLA guarantee"]', 1, 6, GETUTCDATE(), GETUTCDATE());
    PRINT '  -> Inserted 6 subscription plans';
END
ELSE
    PRINT '  -> Subscription plans already exist, skipping';
GO

-- =====================================================
-- 1.7 CHARITIES
-- =====================================================
PRINT 'Seeding Charities...';

IF NOT EXISTS (SELECT 1 FROM Charities WHERE Name = 'Round Up for Good')
BEGIN
    INSERT INTO Charities (Id, Name, Description, LogoUrl, IsActive, CreatedAt)
    VALUES
        (NEWID(), 'Round Up for Good', 'General charity fund for social causes', '/images/charities/roundup.png', 1, GETUTCDATE()),
        (NEWID(), 'Feed India', 'Providing meals to underprivileged', '/images/charities/feedindia.png', 1, GETUTCDATE()),
        (NEWID(), 'Education First', 'Supporting education for children', '/images/charities/education.png', 1, GETUTCDATE()),
        (NEWID(), 'Green Earth', 'Environmental conservation', '/images/charities/greenearth.png', 1, GETUTCDATE()),
        (NEWID(), 'Health for All', 'Healthcare for rural areas', '/images/charities/health.png', 1, GETUTCDATE());
    PRINT '  -> Inserted 5 charities';
END
ELSE
    PRINT '  -> Charities already exist, skipping';
GO

-- =====================================================
-- 1.8 COMPLAINT SLA CONFIGS
-- =====================================================
PRINT 'Seeding ComplaintSLAConfigs...';

IF NOT EXISTS (SELECT 1 FROM ComplaintSLAConfigs WHERE Category = 'DAMAGE')
BEGIN
    INSERT INTO ComplaintSLAConfigs (Id, Category, Severity, ResponseTimeHours, ResolutionTimeHours, IsActive, CreatedAt)
    VALUES
        (NEWID(), 'DAMAGE', 'CRITICAL', 1, 24, 1, GETUTCDATE()),
        (NEWID(), 'DAMAGE', 'HIGH', 2, 48, 1, GETUTCDATE()),
        (NEWID(), 'DAMAGE', 'MEDIUM', 4, 72, 1, GETUTCDATE()),
        (NEWID(), 'THEFT', 'CRITICAL', 1, 24, 1, GETUTCDATE()),
        (NEWID(), 'THEFT', 'HIGH', 2, 48, 1, GETUTCDATE()),
        (NEWID(), 'DELAY', 'HIGH', 2, 24, 1, GETUTCDATE()),
        (NEWID(), 'DELAY', 'MEDIUM', 4, 48, 1, GETUTCDATE()),
        (NEWID(), 'BEHAVIOR', 'HIGH', 2, 48, 1, GETUTCDATE()),
        (NEWID(), 'BEHAVIOR', 'MEDIUM', 4, 72, 1, GETUTCDATE()),
        (NEWID(), 'FRAUD', 'CRITICAL', 1, 24, 1, GETUTCDATE()),
        (NEWID(), 'OTHER', 'LOW', 8, 120, 1, GETUTCDATE());
    PRINT '  -> Inserted 11 SLA configs';
END
ELSE
    PRINT '  -> SLA configs already exist, skipping';
GO

-- =====================================================
-- 1.9 PLATFORM FEE CONFIGS
-- =====================================================
PRINT 'Seeding PlatformFeeConfigs...';

IF NOT EXISTS (SELECT 1 FROM PlatformFeeConfigs WHERE FeeType = 'PLATFORM_PERCENTAGE')
BEGIN
    INSERT INTO PlatformFeeConfigs (Id, FeeType, FeeValue, MinValue, MaxValue, ApplicableTo, Description, IsActive, EffectiveFrom, CreatedAt, UpdatedAt)
    VALUES
        (NEWID(), 'PLATFORM_PERCENTAGE', 5.00, 5.00, 100.00, 'ALL', 'Platform fee as percentage of delivery charge', 1, GETUTCDATE(), GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'GST_PERCENTAGE', 18.00, NULL, NULL, 'ALL', 'GST on platform fee', 1, GETUTCDATE(), GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'CONVENIENCE_FEE', 5.00, NULL, NULL, 'COD', 'Additional fee for COD orders', 1, GETUTCDATE(), GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'INSURANCE_PERCENTAGE', 1.00, 10.00, 500.00, 'VALUABLE', 'Insurance fee for valuable packages', 1, GETUTCDATE(), GETUTCDATE(), GETUTCDATE());
    PRINT '  -> Inserted 4 platform fee configs';
END
ELSE
    PRINT '  -> Platform fee configs already exist, skipping';
GO

-- =====================================================
-- 1.10 PINCODE MASTERS (Sample for 3 cities)
-- =====================================================
PRINT 'Seeding PincodeMasters (sample)...';

IF NOT EXISTS (SELECT 1 FROM PincodeMasters WHERE Pincode = '302001')
BEGIN
    INSERT INTO PincodeMasters (Id, Pincode, AreaName, CityName, DistrictName, StateName, Latitude, Longitude, IsServiceable, CreatedAt)
    VALUES
        -- Jaipur Pincodes
        (NEWID(), '302001', 'MI Road', 'Jaipur', 'Jaipur', 'Rajasthan', 26.9124, 75.7873, 1, GETUTCDATE()),
        (NEWID(), '302002', 'Bani Park', 'Jaipur', 'Jaipur', 'Rajasthan', 26.9350, 75.7850, 1, GETUTCDATE()),
        (NEWID(), '302003', 'Johri Bazaar', 'Jaipur', 'Jaipur', 'Rajasthan', 26.9220, 75.8220, 1, GETUTCDATE()),
        (NEWID(), '302004', 'Raja Park', 'Jaipur', 'Jaipur', 'Rajasthan', 26.8950, 75.8120, 1, GETUTCDATE()),
        (NEWID(), '302005', 'Ashok Nagar', 'Jaipur', 'Jaipur', 'Rajasthan', 26.8820, 75.7950, 1, GETUTCDATE()),
        (NEWID(), '302015', 'Vaishali Nagar', 'Jaipur', 'Jaipur', 'Rajasthan', 26.9100, 75.7350, 1, GETUTCDATE()),
        (NEWID(), '302016', 'Jagatpura', 'Jaipur', 'Jaipur', 'Rajasthan', 26.8300, 75.8500, 1, GETUTCDATE()),
        (NEWID(), '302017', 'Malviya Nagar', 'Jaipur', 'Jaipur', 'Rajasthan', 26.8550, 75.8150, 1, GETUTCDATE()),
        (NEWID(), '302018', 'Tonk Road', 'Jaipur', 'Jaipur', 'Rajasthan', 26.8650, 75.8050, 1, GETUTCDATE()),
        (NEWID(), '302019', 'Sanganer', 'Jaipur', 'Jaipur', 'Rajasthan', 26.8200, 75.7900, 1, GETUTCDATE()),
        -- Delhi Pincodes
        (NEWID(), '110001', 'Connaught Place', 'New Delhi', 'Central Delhi', 'Delhi', 28.6315, 77.2167, 1, GETUTCDATE()),
        (NEWID(), '110002', 'Darya Ganj', 'New Delhi', 'Central Delhi', 'Delhi', 28.6480, 77.2410, 1, GETUTCDATE()),
        (NEWID(), '110003', 'Civil Lines', 'New Delhi', 'North Delhi', 'Delhi', 28.6800, 77.2250, 1, GETUTCDATE()),
        (NEWID(), '110016', 'Hauz Khas', 'New Delhi', 'South Delhi', 'Delhi', 28.5494, 77.2001, 1, GETUTCDATE()),
        (NEWID(), '110017', 'Malviya Nagar', 'New Delhi', 'South Delhi', 'Delhi', 28.5275, 77.2134, 1, GETUTCDATE()),
        (NEWID(), '110019', 'Kalkaji', 'New Delhi', 'South Delhi', 'Delhi', 28.5400, 77.2600, 1, GETUTCDATE()),
        (NEWID(), '110020', 'Okhla', 'New Delhi', 'South Delhi', 'Delhi', 28.5200, 77.2700, 1, GETUTCDATE()),
        (NEWID(), '110025', 'Lajpat Nagar', 'New Delhi', 'South Delhi', 'Delhi', 28.5650, 77.2400, 1, GETUTCDATE()),
        (NEWID(), '110048', 'Chanakyapuri', 'New Delhi', 'New Delhi', 'Delhi', 28.5900, 77.1850, 1, GETUTCDATE()),
        (NEWID(), '110065', 'Saket', 'New Delhi', 'South Delhi', 'Delhi', 28.5225, 77.2100, 1, GETUTCDATE()),
        -- Mumbai Pincodes
        (NEWID(), '400001', 'Fort', 'Mumbai', 'Mumbai City', 'Maharashtra', 18.9350, 72.8350, 1, GETUTCDATE()),
        (NEWID(), '400002', 'Marine Lines', 'Mumbai', 'Mumbai City', 'Maharashtra', 18.9430, 72.8250, 1, GETUTCDATE()),
        (NEWID(), '400003', 'Masjid Bandar', 'Mumbai', 'Mumbai City', 'Maharashtra', 18.9550, 72.8400, 1, GETUTCDATE()),
        (NEWID(), '400050', 'Bandra West', 'Mumbai', 'Mumbai Suburban', 'Maharashtra', 19.0600, 72.8300, 1, GETUTCDATE()),
        (NEWID(), '400051', 'Bandra East', 'Mumbai', 'Mumbai Suburban', 'Maharashtra', 19.0650, 72.8500, 1, GETUTCDATE()),
        (NEWID(), '400053', 'Andheri West', 'Mumbai', 'Mumbai Suburban', 'Maharashtra', 19.1350, 72.8250, 1, GETUTCDATE()),
        (NEWID(), '400058', 'Andheri East', 'Mumbai', 'Mumbai Suburban', 'Maharashtra', 19.1150, 72.8700, 1, GETUTCDATE()),
        (NEWID(), '400069', 'Powai', 'Mumbai', 'Mumbai Suburban', 'Maharashtra', 19.1180, 72.9050, 1, GETUTCDATE()),
        (NEWID(), '400076', 'Ghatkopar', 'Mumbai', 'Mumbai Suburban', 'Maharashtra', 19.0860, 72.9080, 1, GETUTCDATE()),
        (NEWID(), '400097', 'Malad', 'Mumbai', 'Mumbai Suburban', 'Maharashtra', 19.1870, 72.8485, 1, GETUTCDATE());
    PRINT '  -> Inserted 30 pincodes (10 each for Jaipur, Delhi, Mumbai)';
END
ELSE
    PRINT '  -> Pincodes already exist, skipping';
GO

-- =====================================================
-- 1.11 STATE MASTERS
-- =====================================================
PRINT 'Seeding StateMasters...';

IF NOT EXISTS (SELECT 1 FROM StateMasters WHERE Code = 'RJ')
BEGIN
    INSERT INTO StateMasters (Id, Code, Name, IsActive, CreatedAt)
    VALUES
        (NEWID(), 'RJ', 'Rajasthan', 1, GETUTCDATE()),
        (NEWID(), 'DL', 'Delhi', 1, GETUTCDATE()),
        (NEWID(), 'MH', 'Maharashtra', 1, GETUTCDATE()),
        (NEWID(), 'UP', 'Uttar Pradesh', 1, GETUTCDATE()),
        (NEWID(), 'GJ', 'Gujarat', 1, GETUTCDATE()),
        (NEWID(), 'KA', 'Karnataka', 1, GETUTCDATE()),
        (NEWID(), 'TN', 'Tamil Nadu', 1, GETUTCDATE()),
        (NEWID(), 'AP', 'Andhra Pradesh', 1, GETUTCDATE()),
        (NEWID(), 'TS', 'Telangana', 1, GETUTCDATE()),
        (NEWID(), 'WB', 'West Bengal', 1, GETUTCDATE());
    PRINT '  -> Inserted 10 states';
END
ELSE
    PRINT '  -> States already exist, skipping';
GO

-- =====================================================
-- 1.12 DISTRICT MASTERS (Sample)
-- =====================================================
PRINT 'Seeding DistrictMasters...';

IF NOT EXISTS (SELECT 1 FROM DistrictMasters WHERE Name = 'Jaipur')
BEGIN
    INSERT INTO DistrictMasters (Id, StateId, Code, Name, IsActive, CreatedAt)
    SELECT NEWID(), s.Id, 'JPR', 'Jaipur', 1, GETUTCDATE() FROM StateMasters s WHERE s.Code = 'RJ'
    UNION ALL
    SELECT NEWID(), s.Id, 'JDH', 'Jodhpur', 1, GETUTCDATE() FROM StateMasters s WHERE s.Code = 'RJ'
    UNION ALL
    SELECT NEWID(), s.Id, 'CDL', 'Central Delhi', 1, GETUTCDATE() FROM StateMasters s WHERE s.Code = 'DL'
    UNION ALL
    SELECT NEWID(), s.Id, 'SDL', 'South Delhi', 1, GETUTCDATE() FROM StateMasters s WHERE s.Code = 'DL'
    UNION ALL
    SELECT NEWID(), s.Id, 'NDL', 'North Delhi', 1, GETUTCDATE() FROM StateMasters s WHERE s.Code = 'DL'
    UNION ALL
    SELECT NEWID(), s.Id, 'MUM', 'Mumbai City', 1, GETUTCDATE() FROM StateMasters s WHERE s.Code = 'MH'
    UNION ALL
    SELECT NEWID(), s.Id, 'MMS', 'Mumbai Suburban', 1, GETUTCDATE() FROM StateMasters s WHERE s.Code = 'MH';
    PRINT '  -> Inserted 7 districts';
END
ELSE
    PRINT '  -> Districts already exist, skipping';
GO

-- =====================================================
-- 1.13 PERMISSIONS
-- =====================================================
PRINT 'Seeding Permissions...';

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Code = 'USER_VIEW')
BEGIN
    INSERT INTO Permissions (Id, Code, Name, Description, Module, IsActive, CreatedAt)
    VALUES
        (NEWID(), 'USER_VIEW', 'View Users', 'Can view user list and details', 'USER', 1, GETUTCDATE()),
        (NEWID(), 'USER_CREATE', 'Create Users', 'Can create new users', 'USER', 1, GETUTCDATE()),
        (NEWID(), 'USER_EDIT', 'Edit Users', 'Can edit user details', 'USER', 1, GETUTCDATE()),
        (NEWID(), 'USER_DELETE', 'Delete Users', 'Can deactivate users', 'USER', 1, GETUTCDATE()),
        (NEWID(), 'DP_VIEW', 'View DPs', 'Can view delivery partners', 'DP', 1, GETUTCDATE()),
        (NEWID(), 'DP_MANAGE', 'Manage DPs', 'Can manage delivery partners', 'DP', 1, GETUTCDATE()),
        (NEWID(), 'DELIVERY_VIEW', 'View Deliveries', 'Can view deliveries', 'DELIVERY', 1, GETUTCDATE()),
        (NEWID(), 'DELIVERY_MANAGE', 'Manage Deliveries', 'Can manage deliveries', 'DELIVERY', 1, GETUTCDATE()),
        (NEWID(), 'COMPLAINT_VIEW', 'View Complaints', 'Can view complaints', 'COMPLAINT', 1, GETUTCDATE()),
        (NEWID(), 'COMPLAINT_RESOLVE', 'Resolve Complaints', 'Can resolve complaints', 'COMPLAINT', 1, GETUTCDATE()),
        (NEWID(), 'WALLET_VIEW', 'View Wallets', 'Can view wallets', 'WALLET', 1, GETUTCDATE()),
        (NEWID(), 'WALLET_MANAGE', 'Manage Wallets', 'Can manage wallet transactions', 'WALLET', 1, GETUTCDATE()),
        (NEWID(), 'REPORT_VIEW', 'View Reports', 'Can view reports', 'REPORT', 1, GETUTCDATE()),
        (NEWID(), 'SETTINGS_MANAGE', 'Manage Settings', 'Can manage system settings', 'SETTINGS', 1, GETUTCDATE());
    PRINT '  -> Inserted 14 permissions';
END
ELSE
    PRINT '  -> Permissions already exist, skipping';
GO

PRINT '';
PRINT '====================================';
PRINT 'STEP 1 COMPLETE: Master data seeded';
PRINT '====================================';
GO
