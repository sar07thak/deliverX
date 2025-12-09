-- =====================================================
-- DELIVERYDOST SEED DATA - STEP 2: USERS
-- =====================================================
-- Run AFTER seed_01_master_data.sql
-- Creates users for all roles: Admin, DPCM, DP, BC, EC, Inspector
-- =====================================================

USE DeliveryDost_Dev;
GO

SET NOCOUNT ON;
PRINT '====================================';
PRINT 'STEP 2: SEEDING USERS';
PRINT '====================================';

-- =====================================================
-- Helper: Indian Names Arrays (using temp table)
-- =====================================================
CREATE TABLE #IndianNames (
    Id INT IDENTITY(1,1),
    FirstName NVARCHAR(50),
    LastName NVARCHAR(50)
);

INSERT INTO #IndianNames (FirstName, LastName) VALUES
('Rahul', 'Sharma'), ('Priya', 'Patel'), ('Amit', 'Singh'), ('Neha', 'Gupta'), ('Vikram', 'Verma'),
('Anjali', 'Joshi'), ('Rajesh', 'Kumar'), ('Sunita', 'Agarwal'), ('Deepak', 'Mehta'), ('Kavita', 'Reddy'),
('Sunil', 'Rao'), ('Meera', 'Iyer'), ('Arun', 'Nair'), ('Pooja', 'Menon'), ('Sanjay', 'Das'),
('Ritu', 'Banerjee'), ('Manoj', 'Chatterjee'), ('Swati', 'Sinha'), ('Nitin', 'Saxena'), ('Rekha', 'Tiwari'),
('Vijay', 'Pandey'), ('Anita', 'Mishra'), ('Rakesh', 'Bhatia'), ('Geeta', 'Khanna'), ('Ashok', 'Malhotra'),
('Shikha', 'Arora'), ('Vivek', 'Kapoor'), ('Divya', 'Soni'), ('Harsh', 'Jain'), ('Nisha', 'Goyal'),
('Kiran', 'Sethi'), ('Ravi', 'Choudhary'), ('Pallavi', 'Dubey'), ('Gaurav', 'Yadav'), ('Sakshi', 'Tripathi'),
('Yogesh', 'Chauhan'), ('Bhavna', 'Khatri'), ('Naveen', 'Bhatt'), ('Shruti', 'Pillai'), ('Anand', 'Rajan'),
('Preeti', 'Hegde'), ('Mohit', 'Kulkarni'), ('Tanvi', 'Deshpande'), ('Rohan', 'Patil'), ('Simran', 'Kaur'),
('Ajay', 'Gill'), ('Megha', 'Bajaj'), ('Kunal', 'Garg'), ('Aarti', 'Thakur'), ('Vishal', 'Rawat');

-- =====================================================
-- 2.1 ADMIN USERS (5)
-- =====================================================
PRINT 'Creating Admin Users...';

DECLARE @AdminCount INT = 0;
DECLARE @i INT = 1;
DECLARE @UserId UNIQUEIDENTIFIER;
DECLARE @Phone VARCHAR(15);
DECLARE @Email NVARCHAR(255);
DECLARE @FullName NVARCHAR(255);

WHILE @i <= 5
BEGIN
    SET @Phone = '90000000' + RIGHT('0' + CAST(@i AS VARCHAR), 2);
    SET @Email = 'admin' + CAST(@i AS VARCHAR) + '@deliverydost.com';

    IF NOT EXISTS (SELECT 1 FROM Users WHERE Phone = @Phone OR Email = @Email)
    BEGIN
        SELECT @FullName = FirstName + ' ' + LastName FROM #IndianNames WHERE Id = @i;

        INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, Is2FAEnabled, IsActive, IsPhoneVerified, IsEmailVerified, FailedLoginAttempts, CreatedAt, UpdatedAt)
        VALUES (NEWID(), @Phone, @Email, @FullName, 'AQAAAAIAAYagAAAAEK...(hashed)', 'ADMIN', 0, 1, 1, 1, 0, DATEADD(DAY, -90, GETUTCDATE()), GETUTCDATE());

        SET @AdminCount = @AdminCount + 1;
    END
    SET @i = @i + 1;
END
PRINT '  -> Created ' + CAST(@AdminCount AS VARCHAR) + ' admin users';
GO

-- =====================================================
-- 2.2 DPCM USERS (15)
-- =====================================================
PRINT 'Creating DPCM Users...';

DECLARE @DPCMCount INT = 0;
DECLARE @i INT = 1;
DECLARE @Phone VARCHAR(15);
DECLARE @Email NVARCHAR(255);
DECLARE @FullName NVARCHAR(255);

WHILE @i <= 15
BEGIN
    SET @Phone = '91000000' + RIGHT('0' + CAST(@i AS VARCHAR), 2);
    SET @Email = 'dpcm' + CAST(@i AS VARCHAR) + '@deliverydost.com';

    IF NOT EXISTS (SELECT 1 FROM Users WHERE Phone = @Phone OR Email = @Email)
    BEGIN
        SELECT @FullName = FirstName + ' ' + LastName FROM #IndianNames WHERE Id = @i + 5;

        INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, Is2FAEnabled, IsActive, IsPhoneVerified, IsEmailVerified, FailedLoginAttempts, CreatedAt, UpdatedAt)
        VALUES (NEWID(), @Phone, @Email, @FullName, 'AQAAAAIAAYagAAAAEK...(hashed)', 'DPCM', 0, 1, 1, 1, 0, DATEADD(DAY, -CAST(RAND(CHECKSUM(NEWID())) * 60 + 30 AS INT), GETUTCDATE()), GETUTCDATE());

        SET @DPCMCount = @DPCMCount + 1;
    END
    SET @i = @i + 1;
END
PRINT '  -> Created ' + CAST(@DPCMCount AS VARCHAR) + ' DPCM users';
GO

-- =====================================================
-- 2.3 DELIVERY PARTNER USERS (80)
-- =====================================================
PRINT 'Creating Delivery Partner Users...';

DECLARE @DPCount INT = 0;
DECLARE @i INT = 1;
DECLARE @Phone VARCHAR(15);
DECLARE @Email NVARCHAR(255);
DECLARE @FullName NVARCHAR(255);
DECLARE @City VARCHAR(20);

WHILE @i <= 80
BEGIN
    SET @Phone = '92000000' + RIGHT('0' + CAST(@i AS VARCHAR), 2);
    SET @City = CASE @i % 3 WHEN 0 THEN 'jaipur' WHEN 1 THEN 'delhi' ELSE 'mumbai' END;
    SET @Email = 'dp.' + @City + '.' + CAST(@i AS VARCHAR) + '@deliverydost.com';

    IF NOT EXISTS (SELECT 1 FROM Users WHERE Phone = @Phone)
    BEGIN
        SELECT @FullName = FirstName + ' ' + LastName FROM #IndianNames WHERE Id = ((@i - 1) % 50) + 1;

        INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, Is2FAEnabled, IsActive, IsPhoneVerified, IsEmailVerified, FailedLoginAttempts, CreatedAt, UpdatedAt)
        VALUES (NEWID(), @Phone, @Email, @FullName, 'AQAAAAIAAYagAAAAEK...(hashed)', 'DP', 0, 1, 1,
                CASE WHEN @i % 10 = 0 THEN 0 ELSE 1 END, -- 10% without email verified
                0,
                DATEADD(DAY, -CAST(RAND(CHECKSUM(NEWID())) * 90 AS INT), GETUTCDATE()),
                GETUTCDATE());

        SET @DPCount = @DPCount + 1;
    END
    SET @i = @i + 1;
END
PRINT '  -> Created ' + CAST(@DPCount AS VARCHAR) + ' DP users';
GO

-- =====================================================
-- 2.4 BUSINESS CONSUMER USERS (50)
-- =====================================================
PRINT 'Creating Business Consumer Users...';

DECLARE @BCCount INT = 0;
DECLARE @i INT = 1;
DECLARE @Phone VARCHAR(15);
DECLARE @Email NVARCHAR(255);
DECLARE @FullName NVARCHAR(255);
DECLARE @BusinessNames TABLE (Id INT, Name NVARCHAR(100));

INSERT INTO @BusinessNames VALUES
(1, 'Quick Mart'), (2, 'Fresh Foods'), (3, 'MediCare Pharmacy'), (4, 'Tech Solutions'),
(5, 'Urban Eats'), (6, 'BookWorm Store'), (7, 'Fashion Hub'), (8, 'HomeStyle Decor'),
(9, 'Green Grocers'), (10, 'Digital Dreams'), (11, 'Spice Kingdom'), (12, 'Pet Paradise'),
(13, 'Office Supplies Co'), (14, 'Beauty Bliss'), (15, 'Sports Zone'), (16, 'Toy World'),
(17, 'Auto Parts Plus'), (18, 'Garden Glory'), (19, 'Music Mania'), (20, 'Art & Craft');

WHILE @i <= 50
BEGIN
    SET @Phone = '93000000' + RIGHT('0' + CAST(@i AS VARCHAR), 2);
    SET @Email = 'bc' + CAST(@i AS VARCHAR) + '@business.com';

    IF NOT EXISTS (SELECT 1 FROM Users WHERE Phone = @Phone)
    BEGIN
        SELECT @FullName = Name + ' - ' + CAST(@i AS NVARCHAR) FROM @BusinessNames WHERE Id = ((@i - 1) % 20) + 1;

        INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, Is2FAEnabled, IsActive, IsPhoneVerified, IsEmailVerified, FailedLoginAttempts, CreatedAt, UpdatedAt)
        VALUES (NEWID(), @Phone, @Email, @FullName, 'AQAAAAIAAYagAAAAEK...(hashed)', 'BC', 0, 1, 1, 1, 0,
                DATEADD(DAY, -CAST(RAND(CHECKSUM(NEWID())) * 60 + 30 AS INT), GETUTCDATE()),
                GETUTCDATE());

        SET @BCCount = @BCCount + 1;
    END
    SET @i = @i + 1;
END
PRINT '  -> Created ' + CAST(@BCCount AS VARCHAR) + ' BC users';
GO

-- =====================================================
-- 2.5 END CONSUMER USERS (60)
-- =====================================================
PRINT 'Creating End Consumer Users...';

DECLARE @ECCount INT = 0;
DECLARE @i INT = 1;
DECLARE @Phone VARCHAR(15);
DECLARE @Email NVARCHAR(255);
DECLARE @FullName NVARCHAR(255);

WHILE @i <= 60
BEGIN
    SET @Phone = '94000000' + RIGHT('0' + CAST(@i AS VARCHAR), 2);
    SET @Email = 'user' + CAST(@i AS VARCHAR) + '@gmail.com';

    IF NOT EXISTS (SELECT 1 FROM Users WHERE Phone = @Phone)
    BEGIN
        SELECT @FullName = FirstName + ' ' + LastName FROM #IndianNames WHERE Id = ((@i - 1) % 50) + 1;

        INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, Is2FAEnabled, IsActive, IsPhoneVerified, IsEmailVerified, FailedLoginAttempts, CreatedAt, UpdatedAt)
        VALUES (NEWID(), @Phone, @Email, @FullName, 'AQAAAAIAAYagAAAAEK...(hashed)', 'EC', 0,
                CASE WHEN @i % 15 = 0 THEN 0 ELSE 1 END, -- Some inactive
                1,
                CASE WHEN @i % 8 = 0 THEN 0 ELSE 1 END, -- Some without email verified
                0,
                DATEADD(DAY, -CAST(RAND(CHECKSUM(NEWID())) * 90 AS INT), GETUTCDATE()),
                GETUTCDATE());

        SET @ECCount = @ECCount + 1;
    END
    SET @i = @i + 1;
END
PRINT '  -> Created ' + CAST(@ECCount AS VARCHAR) + ' EC users';
GO

-- =====================================================
-- 2.6 INSPECTOR USERS (15)
-- =====================================================
PRINT 'Creating Inspector Users...';

DECLARE @InspCount INT = 0;
DECLARE @i INT = 1;
DECLARE @Phone VARCHAR(15);
DECLARE @Email NVARCHAR(255);
DECLARE @FullName NVARCHAR(255);

WHILE @i <= 15
BEGIN
    SET @Phone = '95000000' + RIGHT('0' + CAST(@i AS VARCHAR), 2);
    SET @Email = 'inspector' + CAST(@i AS VARCHAR) + '@deliverydost.com';

    IF NOT EXISTS (SELECT 1 FROM Users WHERE Phone = @Phone)
    BEGIN
        SELECT @FullName = FirstName + ' ' + LastName FROM #IndianNames WHERE Id = ((@i - 1) % 50) + 1;

        INSERT INTO Users (Id, Phone, Email, FullName, PasswordHash, Role, Is2FAEnabled, IsActive, IsPhoneVerified, IsEmailVerified, FailedLoginAttempts, CreatedAt, UpdatedAt)
        VALUES (NEWID(), @Phone, @Email, @FullName, 'AQAAAAIAAYagAAAAEK...(hashed)', 'INSPECTOR', 0, 1, 1, 1, 0,
                DATEADD(DAY, -CAST(RAND(CHECKSUM(NEWID())) * 30 + 60 AS INT), GETUTCDATE()),
                GETUTCDATE());

        SET @InspCount = @InspCount + 1;
    END
    SET @i = @i + 1;
END
PRINT '  -> Created ' + CAST(@InspCount AS VARCHAR) + ' Inspector users';
GO

-- =====================================================
-- CLEANUP
-- =====================================================
DROP TABLE #IndianNames;
GO

-- =====================================================
-- SUMMARY
-- =====================================================
PRINT '';
PRINT '====================================';
PRINT 'STEP 2 COMPLETE: Users Summary';
PRINT '====================================';

SELECT Role, COUNT(*) AS Count,
       SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS Active,
       SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS Inactive
FROM Users
GROUP BY Role
ORDER BY Role;
GO
