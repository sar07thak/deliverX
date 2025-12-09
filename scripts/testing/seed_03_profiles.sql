-- =====================================================
-- DELIVERYDOST SEED DATA - STEP 3: PROFILES & ENTITIES
-- =====================================================
-- Run AFTER seed_02_users.sql
-- Creates: DPCMs, DPs, BCs, Inspectors, Wallets, Saved Addresses
-- =====================================================

USE DeliveryDost_Dev;
GO

SET NOCOUNT ON;
PRINT '====================================';
PRINT 'STEP 3: SEEDING PROFILES & ENTITIES';
PRINT '====================================';

-- =====================================================
-- 3.1 DPCM MANAGERS
-- =====================================================
PRINT 'Creating DPCManagers...';

DECLARE @OrgNames TABLE (Id INT, Name NVARCHAR(100));
INSERT INTO @OrgNames VALUES
(1, 'Swift Logistics Pvt Ltd'), (2, 'RapidGo Delivery Services'), (3, 'Urban Movers Inc'),
(4, 'QuickShip Solutions'), (5, 'Metro Couriers'), (6, 'Express Lane Logistics'),
(7, 'Speedy Parcels Co'), (8, 'City Connect Delivery'), (9, 'Prime Move Services'),
(10, 'Fast Track Logistics'), (11, 'Royal Couriers'), (12, 'Instant Delivery Hub'),
(13, 'Smart Logistics Pro'), (14, 'Reliable Runners'), (15, 'Zoom Delivery Network');

DECLARE @DPCMCount INT = 0;

INSERT INTO DPCManagers (Id, UserId, OrganizationName, ContactPersonName, PAN, ServiceRegions, CommissionType, CommissionValue, MinCommissionAmount, SecurityDeposit, SecurityDepositStatus, IsActive, ActivatedAt, CreatedAt, UpdatedAt)
SELECT
    NEWID(),
    u.Id,
    o.Name,
    u.FullName,
    'AAAAA' + RIGHT('0000' + CAST(ROW_NUMBER() OVER (ORDER BY u.CreatedAt) AS VARCHAR), 4) + 'A',
    CASE ROW_NUMBER() OVER (ORDER BY u.CreatedAt) % 3
        WHEN 0 THEN '["Jaipur","Jodhpur"]'
        WHEN 1 THEN '["Delhi","Noida","Gurgaon"]'
        ELSE '["Mumbai","Thane","Navi Mumbai"]'
    END,
    CASE ROW_NUMBER() OVER (ORDER BY u.CreatedAt) % 3 WHEN 0 THEN 'PERCENTAGE' WHEN 1 THEN 'FLAT' ELSE 'HYBRID' END,
    CASE ROW_NUMBER() OVER (ORDER BY u.CreatedAt) % 3 WHEN 0 THEN 10.00 WHEN 1 THEN 15.00 ELSE 8.00 END,
    CASE ROW_NUMBER() OVER (ORDER BY u.CreatedAt) % 3 WHEN 2 THEN 10.00 ELSE NULL END,
    CASE ROW_NUMBER() OVER (ORDER BY u.CreatedAt) % 4 WHEN 0 THEN 50000 WHEN 1 THEN 100000 ELSE 25000 END,
    CASE ROW_NUMBER() OVER (ORDER BY u.CreatedAt) % 5 WHEN 0 THEN 'PENDING' ELSE 'RECEIVED' END,
    CASE ROW_NUMBER() OVER (ORDER BY u.CreatedAt) % 5 WHEN 0 THEN 0 ELSE 1 END,
    CASE ROW_NUMBER() OVER (ORDER BY u.CreatedAt) % 5 WHEN 0 THEN NULL ELSE DATEADD(DAY, -30, GETUTCDATE()) END,
    u.CreatedAt,
    GETUTCDATE()
FROM Users u
INNER JOIN @OrgNames o ON o.Id = ROW_NUMBER() OVER (ORDER BY u.CreatedAt)
WHERE u.Role = 'DPCM'
  AND NOT EXISTS (SELECT 1 FROM DPCManagers dm WHERE dm.UserId = u.Id);

SET @DPCMCount = @@ROWCOUNT;
PRINT '  -> Created ' + CAST(@DPCMCount AS VARCHAR) + ' DPCManagers';
GO

-- =====================================================
-- 3.2 DELIVERY PARTNER PROFILES
-- =====================================================
PRINT 'Creating DeliveryPartnerProfiles...';

-- Location data for cities
DECLARE @Locations TABLE (City VARCHAR(20), BaseLat DECIMAL(10,8), BaseLng DECIMAL(11,8));
INSERT INTO @Locations VALUES
('Jaipur', 26.9124, 75.7873),
('Delhi', 28.6139, 77.2090),
('Mumbai', 19.0760, 72.8777);

DECLARE @VehicleTypes TABLE (Id INT, Type VARCHAR(20));
INSERT INTO @VehicleTypes VALUES (1, 'BIKE'), (2, 'SCOOTER'), (3, 'CAR'), (4, 'MINI_TRUCK'), (5, 'CYCLE');

DECLARE @DPCount INT = 0;

-- Get DPCM IDs for assignment
DECLARE @DPCMIds TABLE (Id UNIQUEIDENTIFIER, City VARCHAR(20), RowNum INT);
INSERT INTO @DPCMIds
SELECT d.Id,
    CASE
        WHEN d.ServiceRegions LIKE '%Jaipur%' THEN 'Jaipur'
        WHEN d.ServiceRegions LIKE '%Delhi%' THEN 'Delhi'
        ELSE 'Mumbai'
    END,
    ROW_NUMBER() OVER (ORDER BY d.CreatedAt)
FROM DPCManagers d WHERE d.IsActive = 1;

INSERT INTO DeliveryPartnerProfiles (
    Id, UserId, DPCMId, FullName, DOB, Gender, Address, VehicleType, Languages, Availability,
    ServiceAreaCenterLat, ServiceAreaCenterLng, ServiceAreaRadiusKm,
    PerKmRate, PerKgRate, MinCharge, MaxDistanceKm, MaxConcurrentDeliveries, CurrentActiveDeliveries,
    IsActive, IsOnline, LastOnlineAt, ActivatedAt, CreatedAt, UpdatedAt, OneDirectionOnly, PreferredDirection
)
SELECT
    NEWID(),
    u.Id,
    (SELECT TOP 1 Id FROM @DPCMIds WHERE City = loc.City ORDER BY NEWID()),
    u.FullName,
    DATEADD(YEAR, -CAST(RAND(CHECKSUM(u.Id)) * 20 + 22 AS INT), GETUTCDATE()), -- Age 22-42
    CASE (ROW_NUMBER() OVER (ORDER BY u.CreatedAt)) % 5 WHEN 0 THEN 'Female' ELSE 'Male' END,
    CASE loc.City
        WHEN 'Jaipur' THEN N'Jaipur, Rajasthan'
        WHEN 'Delhi' THEN N'New Delhi, Delhi'
        ELSE N'Mumbai, Maharashtra'
    END,
    (SELECT TOP 1 Type FROM @VehicleTypes ORDER BY NEWID()),
    '["Hindi","English"]',
    'FULL_TIME',
    loc.BaseLat + (RAND(CHECKSUM(u.Id)) - 0.5) * 0.1,
    loc.BaseLng + (RAND(CHECKSUM(NEWID())) - 0.5) * 0.1,
    CAST(RAND(CHECKSUM(u.Id)) * 10 + 5 AS DECIMAL(5,2)), -- 5-15 km radius
    CAST(RAND(CHECKSUM(u.Id)) * 7 + 8 AS DECIMAL(5,2)), -- 8-15 per km
    CAST(RAND(CHECKSUM(u.Id)) * 3 + 2 AS DECIMAL(5,2)), -- 2-5 per kg
    CAST(RAND(CHECKSUM(u.Id)) * 50 + 30 AS DECIMAL(5,2)), -- 30-80 min charge
    CAST(RAND(CHECKSUM(u.Id)) * 70 + 30 AS DECIMAL(5,2)), -- 30-100 km max
    CAST(RAND(CHECKSUM(u.Id)) * 3 + 2 AS INT), -- 2-5 concurrent
    0, -- No active deliveries initially
    CASE (ROW_NUMBER() OVER (ORDER BY u.CreatedAt)) % 5 WHEN 0 THEN 0 ELSE 1 END, -- 80% active
    CASE (ROW_NUMBER() OVER (ORDER BY u.CreatedAt)) % 3 WHEN 0 THEN 0 ELSE 1 END, -- 66% online
    CASE (ROW_NUMBER() OVER (ORDER BY u.CreatedAt)) % 3
        WHEN 0 THEN DATEADD(HOUR, -24, GETUTCDATE())
        ELSE DATEADD(MINUTE, -CAST(RAND(CHECKSUM(u.Id)) * 60 AS INT), GETUTCDATE())
    END,
    CASE (ROW_NUMBER() OVER (ORDER BY u.CreatedAt)) % 5 WHEN 0 THEN NULL ELSE DATEADD(DAY, -30, GETUTCDATE()) END,
    u.CreatedAt,
    GETUTCDATE(),
    CASE (ROW_NUMBER() OVER (ORDER BY u.CreatedAt)) % 10 WHEN 0 THEN 1 ELSE 0 END,
    CASE (ROW_NUMBER() OVER (ORDER BY u.CreatedAt)) % 4 WHEN 0 THEN 'NORTH' WHEN 1 THEN 'SOUTH' WHEN 2 THEN 'EAST' ELSE NULL END
FROM Users u
CROSS APPLY (
    SELECT TOP 1 City, BaseLat, BaseLng
    FROM @Locations
    WHERE City = CASE (ROW_NUMBER() OVER (ORDER BY u.CreatedAt)) % 3 WHEN 0 THEN 'Jaipur' WHEN 1 THEN 'Delhi' ELSE 'Mumbai' END
) loc
WHERE u.Role = 'DP'
  AND NOT EXISTS (SELECT 1 FROM DeliveryPartnerProfiles dp WHERE dp.UserId = u.Id);

SET @DPCount = @@ROWCOUNT;
PRINT '  -> Created ' + CAST(@DPCount AS VARCHAR) + ' DeliveryPartnerProfiles';
GO

-- =====================================================
-- 3.3 BUSINESS CONSUMER PROFILES
-- =====================================================
PRINT 'Creating BusinessConsumerProfiles...';

DECLARE @BCCount INT = 0;

INSERT INTO BusinessConsumerProfiles (
    Id, UserId, BusinessName, ContactPersonName, GSTIN, PAN, BusinessCategory, BusinessAddress,
    SubscriptionPlanId, IsActive, ActivatedAt, CreatedAt, UpdatedAt
)
SELECT
    NEWID(),
    u.Id,
    u.FullName,
    (SELECT TOP 1 FullName FROM Users WHERE Role = 'BC' ORDER BY NEWID()),
    '29AAAAA' + RIGHT('0000' + CAST(ROW_NUMBER() OVER (ORDER BY u.CreatedAt) AS VARCHAR), 4) + 'A1Z5',
    'BBBBB' + RIGHT('0000' + CAST(ROW_NUMBER() OVER (ORDER BY u.CreatedAt) AS VARCHAR), 4) + 'B',
    CASE (ROW_NUMBER() OVER (ORDER BY u.CreatedAt)) % 8
        WHEN 0 THEN 'Retail' WHEN 1 THEN 'E-Commerce' WHEN 2 THEN 'Food & Beverage'
        WHEN 3 THEN 'Pharmaceutical' WHEN 4 THEN 'Manufacturing' WHEN 5 THEN 'Services'
        WHEN 6 THEN 'Logistics' ELSE 'Other'
    END,
    CASE (ROW_NUMBER() OVER (ORDER BY u.CreatedAt)) % 3
        WHEN 0 THEN N'Jaipur, Rajasthan'
        WHEN 1 THEN N'New Delhi, Delhi'
        ELSE N'Mumbai, Maharashtra'
    END,
    (SELECT TOP 1 Id FROM SubscriptionPlans WHERE PlanType = 'BC' ORDER BY NEWID()),
    CASE (ROW_NUMBER() OVER (ORDER BY u.CreatedAt)) % 10 WHEN 0 THEN 0 ELSE 1 END,
    CASE (ROW_NUMBER() OVER (ORDER BY u.CreatedAt)) % 10 WHEN 0 THEN NULL ELSE DATEADD(DAY, -30, GETUTCDATE()) END,
    u.CreatedAt,
    GETUTCDATE()
FROM Users u
WHERE u.Role = 'BC'
  AND NOT EXISTS (SELECT 1 FROM BusinessConsumerProfiles bc WHERE bc.UserId = u.Id);

SET @BCCount = @@ROWCOUNT;
PRINT '  -> Created ' + CAST(@BCCount AS VARCHAR) + ' BusinessConsumerProfiles';
GO

-- =====================================================
-- 3.4 INSPECTORS
-- =====================================================
PRINT 'Creating Inspectors...';

DECLARE @InspCount INT = 0;

INSERT INTO Inspectors (
    Id, UserId, InspectorCode, Name, Email, Phone, Zone,
    ActiveCases, TotalCasesHandled, ResolutionRate, AverageResolutionTimeHours,
    IsAvailable, CreatedAt, UpdatedAt
)
SELECT
    NEWID(),
    u.Id,
    'INS-' + RIGHT('0000' + CAST(ROW_NUMBER() OVER (ORDER BY u.CreatedAt) AS VARCHAR), 4),
    u.FullName,
    u.Email,
    u.Phone,
    CASE (ROW_NUMBER() OVER (ORDER BY u.CreatedAt)) % 3
        WHEN 0 THEN 'Jaipur'
        WHEN 1 THEN 'Delhi'
        ELSE 'Mumbai'
    END,
    CAST(RAND(CHECKSUM(u.Id)) * 5 AS INT),
    CAST(RAND(CHECKSUM(u.Id)) * 100 + 20 AS INT),
    CAST(RAND(CHECKSUM(u.Id)) * 30 + 70 AS DECIMAL(5,2)), -- 70-100%
    CAST(RAND(CHECKSUM(u.Id)) * 20 + 4 AS DECIMAL(5,2)), -- 4-24 hours
    CASE (ROW_NUMBER() OVER (ORDER BY u.CreatedAt)) % 5 WHEN 0 THEN 0 ELSE 1 END,
    u.CreatedAt,
    GETUTCDATE()
FROM Users u
WHERE u.Role = 'INSPECTOR'
  AND NOT EXISTS (SELECT 1 FROM Inspectors i WHERE i.UserId = u.Id);

SET @InspCount = @@ROWCOUNT;
PRINT '  -> Created ' + CAST(@InspCount AS VARCHAR) + ' Inspectors';
GO

-- =====================================================
-- 3.5 WALLETS (For all users)
-- =====================================================
PRINT 'Creating Wallets...';

DECLARE @WalletCount INT = 0;

INSERT INTO Wallets (Id, UserId, WalletType, Balance, HoldBalance, Currency, IsActive, CreatedAt, UpdatedAt)
SELECT
    NEWID(),
    u.Id,
    u.Role,
    CASE u.Role
        WHEN 'DP' THEN CAST(RAND(CHECKSUM(u.Id)) * 5000 + 500 AS DECIMAL(18,2))
        WHEN 'DPCM' THEN CAST(RAND(CHECKSUM(u.Id)) * 50000 + 5000 AS DECIMAL(18,2))
        WHEN 'BC' THEN CAST(RAND(CHECKSUM(u.Id)) * 10000 + 1000 AS DECIMAL(18,2))
        WHEN 'EC' THEN CAST(RAND(CHECKSUM(u.Id)) * 2000 + 100 AS DECIMAL(18,2))
        ELSE 0
    END,
    CASE u.Role
        WHEN 'DP' THEN CAST(RAND(CHECKSUM(u.Id)) * 500 AS DECIMAL(18,2))
        ELSE 0
    END,
    'INR',
    1,
    u.CreatedAt,
    GETUTCDATE()
FROM Users u
WHERE NOT EXISTS (SELECT 1 FROM Wallets w WHERE w.UserId = u.Id);

SET @WalletCount = @@ROWCOUNT;
PRINT '  -> Created ' + CAST(@WalletCount AS VARCHAR) + ' Wallets';
GO

-- =====================================================
-- 3.6 SAVED ADDRESSES (For EC and BC users)
-- =====================================================
PRINT 'Creating SavedAddresses...';

DECLARE @AddrCount INT = 0;

-- Create 2-3 addresses per EC/BC user
INSERT INTO SavedAddresses (Id, UserId, Label, AddressLine1, AddressLine2, Landmark, City, State, Pincode, Latitude, Longitude, ContactName, ContactPhone, IsDefault, IsActive, CreatedAt, UpdatedAt)
SELECT
    NEWID(),
    u.Id,
    CASE n.Num WHEN 1 THEN 'Home' WHEN 2 THEN 'Office' ELSE 'Other' END,
    CASE (ROW_NUMBER() OVER (ORDER BY u.Id, n.Num)) % 10
        WHEN 0 THEN '123 MG Road' WHEN 1 THEN '45 Gandhi Nagar' WHEN 2 THEN '78 Nehru Street'
        WHEN 3 THEN '90 Ambedkar Marg' WHEN 4 THEN '12 Patel Colony' WHEN 5 THEN '34 Tilak Nagar'
        WHEN 6 THEN '56 Rajiv Chowk' WHEN 7 THEN '89 Indira Road' WHEN 8 THEN '23 Sardar Patel Way'
        ELSE '67 Subhash Marg'
    END,
    CASE n.Num
        WHEN 1 THEN 'Near City Mall'
        WHEN 2 THEN 'Opposite Metro Station'
        ELSE 'Behind Temple'
    END,
    CASE n.Num WHEN 1 THEN 'Near Hospital' WHEN 2 THEN 'Near Bus Stand' ELSE 'Near School' END,
    CASE (ROW_NUMBER() OVER (ORDER BY u.Id)) % 3 WHEN 0 THEN 'Jaipur' WHEN 1 THEN 'Delhi' ELSE 'Mumbai' END,
    CASE (ROW_NUMBER() OVER (ORDER BY u.Id)) % 3 WHEN 0 THEN 'Rajasthan' WHEN 1 THEN 'Delhi' ELSE 'Maharashtra' END,
    CASE (ROW_NUMBER() OVER (ORDER BY u.Id)) % 3
        WHEN 0 THEN '30200' + RIGHT('0' + CAST((ROW_NUMBER() OVER (ORDER BY u.Id)) % 20 + 1 AS VARCHAR), 2)
        WHEN 1 THEN '11000' + RIGHT('0' + CAST((ROW_NUMBER() OVER (ORDER BY u.Id)) % 96 + 1 AS VARCHAR), 2)
        ELSE '40000' + RIGHT('0' + CAST((ROW_NUMBER() OVER (ORDER BY u.Id)) % 104 + 1 AS VARCHAR), 2)
    END,
    CASE (ROW_NUMBER() OVER (ORDER BY u.Id)) % 3
        WHEN 0 THEN 26.9124 + (RAND(CHECKSUM(u.Id)) - 0.5) * 0.1
        WHEN 1 THEN 28.6139 + (RAND(CHECKSUM(u.Id)) - 0.5) * 0.1
        ELSE 19.0760 + (RAND(CHECKSUM(u.Id)) - 0.5) * 0.1
    END,
    CASE (ROW_NUMBER() OVER (ORDER BY u.Id)) % 3
        WHEN 0 THEN 75.7873 + (RAND(CHECKSUM(NEWID())) - 0.5) * 0.1
        WHEN 1 THEN 77.2090 + (RAND(CHECKSUM(NEWID())) - 0.5) * 0.1
        ELSE 72.8777 + (RAND(CHECKSUM(NEWID())) - 0.5) * 0.1
    END,
    u.FullName,
    u.Phone,
    CASE n.Num WHEN 1 THEN 1 ELSE 0 END,
    1,
    u.CreatedAt,
    GETUTCDATE()
FROM Users u
CROSS JOIN (SELECT 1 AS Num UNION ALL SELECT 2 UNION ALL SELECT 3) n
WHERE u.Role IN ('EC', 'BC')
  AND NOT EXISTS (SELECT 1 FROM SavedAddresses sa WHERE sa.UserId = u.Id AND sa.Label = CASE n.Num WHEN 1 THEN 'Home' WHEN 2 THEN 'Office' ELSE 'Other' END);

SET @AddrCount = @@ROWCOUNT;
PRINT '  -> Created ' + CAST(@AddrCount AS VARCHAR) + ' SavedAddresses';
GO

-- =====================================================
-- 3.7 PINCODE-DPCM MAPPINGS
-- =====================================================
PRINT 'Creating PincodeDPCMMappings...';

DECLARE @MappingCount INT = 0;

INSERT INTO PincodeDPCMMappings (Id, Pincode, DPCMId, StateName, DistrictName, IsActive, AssignedAt, AssignedByUserId)
SELECT
    NEWID(),
    p.Pincode,
    d.Id,
    p.StateName,
    p.DistrictName,
    1,
    GETUTCDATE(),
    (SELECT TOP 1 Id FROM Users WHERE Role = 'ADMIN')
FROM PincodeMasters p
CROSS APPLY (
    SELECT TOP 1 dm.Id
    FROM DPCManagers dm
    WHERE dm.IsActive = 1
      AND (
        (p.StateName = 'Rajasthan' AND dm.ServiceRegions LIKE '%Jaipur%') OR
        (p.StateName = 'Delhi' AND dm.ServiceRegions LIKE '%Delhi%') OR
        (p.StateName = 'Maharashtra' AND dm.ServiceRegions LIKE '%Mumbai%')
      )
    ORDER BY NEWID()
) d
WHERE p.IsServiceable = 1
  AND NOT EXISTS (SELECT 1 FROM PincodeDPCMMappings m WHERE m.Pincode = p.Pincode AND m.IsActive = 1);

SET @MappingCount = @@ROWCOUNT;
PRINT '  -> Created ' + CAST(@MappingCount AS VARCHAR) + ' PincodeDPCMMappings';
GO

-- =====================================================
-- SUMMARY
-- =====================================================
PRINT '';
PRINT '====================================';
PRINT 'STEP 3 COMPLETE: Profiles Summary';
PRINT '====================================';

SELECT 'DPCManagers' AS Entity, COUNT(*) AS Total, SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS Active FROM DPCManagers
UNION ALL SELECT 'DeliveryPartnerProfiles', COUNT(*), SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) FROM DeliveryPartnerProfiles
UNION ALL SELECT 'BusinessConsumerProfiles', COUNT(*), SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) FROM BusinessConsumerProfiles
UNION ALL SELECT 'Inspectors', COUNT(*), SUM(CASE WHEN IsAvailable = 1 THEN 1 ELSE 0 END) FROM Inspectors
UNION ALL SELECT 'Wallets', COUNT(*), SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) FROM Wallets
UNION ALL SELECT 'SavedAddresses', COUNT(*), SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) FROM SavedAddresses
UNION ALL SELECT 'PincodeDPCMMappings', COUNT(*), SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) FROM PincodeDPCMMappings;
GO
