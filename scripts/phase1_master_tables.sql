-- =====================================================
-- PHASE 1: MASTER/LOOKUP TABLES
-- =====================================================

USE DeliveryDostDb;
GO

-- Master Vehicle Types
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MasterVehicleTypes')
BEGIN
    CREATE TABLE MasterVehicleTypes (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Code NVARCHAR(20) NOT NULL UNIQUE,
        Name NVARCHAR(50) NOT NULL,
        Description NVARCHAR(200) NULL,
        MaxWeightKg DECIMAL(5,2) NULL,
        MaxVolumeL DECIMAL(8,2) NULL,
        RequiresLicense BIT NOT NULL DEFAULT 1,
        IconUrl NVARCHAR(500) NULL,
        SortOrder INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    PRINT 'Created MasterVehicleTypes table';
END
GO

-- Seed data for vehicle types
IF NOT EXISTS (SELECT 1 FROM MasterVehicleTypes)
BEGIN
    INSERT INTO MasterVehicleTypes (Code, Name, RequiresLicense, SortOrder) VALUES
    ('BICYCLE', 'Bicycle', 0, 1),
    ('TWO_WHEELER', 'Two Wheeler (Bike/Scooter)', 1, 2),
    ('THREE_WHEELER', 'Three Wheeler (Auto)', 1, 3),
    ('FOUR_WHEELER', 'Four Wheeler (Car/Van)', 1, 4),
    ('MINI_TRUCK', 'Mini Truck', 1, 5);
    PRINT 'Seeded MasterVehicleTypes data';
END
GO

-- Master Languages
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MasterLanguages')
BEGIN
    CREATE TABLE MasterLanguages (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Code NVARCHAR(10) NOT NULL UNIQUE,
        Name NVARCHAR(50) NOT NULL,
        NativeName NVARCHAR(50) NULL,
        IsActive BIT NOT NULL DEFAULT 1
    );
    PRINT 'Created MasterLanguages table';
END
GO

-- Seed Indian languages
IF NOT EXISTS (SELECT 1 FROM MasterLanguages)
BEGIN
    INSERT INTO MasterLanguages (Code, Name, NativeName) VALUES
    ('hi', 'Hindi', N'हिन्दी'),
    ('en', 'English', 'English'),
    ('bn', 'Bengali', N'বাংলা'),
    ('te', 'Telugu', N'తెలుగు'),
    ('mr', 'Marathi', N'मराठी'),
    ('ta', 'Tamil', N'தமிழ்'),
    ('gu', 'Gujarati', N'ગુજરાતી'),
    ('kn', 'Kannada', N'ಕನ್ನಡ'),
    ('ml', 'Malayalam', N'മലയാളം'),
    ('pa', 'Punjabi', N'ਪੰਜਾਬੀ'),
    ('or', 'Odia', N'ଓଡ଼ିଆ');
    PRINT 'Seeded MasterLanguages data';
END
GO

-- Master Package Types
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MasterPackageTypes')
BEGIN
    CREATE TABLE MasterPackageTypes (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Code NVARCHAR(20) NOT NULL UNIQUE,
        Name NVARCHAR(50) NOT NULL,
        Description NVARCHAR(200) NULL,
        RequiresSpecialHandling BIT NOT NULL DEFAULT 0,
        SortOrder INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1
    );
    PRINT 'Created MasterPackageTypes table';
END
GO

IF NOT EXISTS (SELECT 1 FROM MasterPackageTypes)
BEGIN
    INSERT INTO MasterPackageTypes (Code, Name, SortOrder) VALUES
    ('DOCUMENT', 'Document/Envelope', 1),
    ('PARCEL', 'Parcel/Box', 2),
    ('FOOD', 'Food Items', 3),
    ('FRAGILE', 'Fragile Items', 4),
    ('ELECTRONICS', 'Electronics', 5),
    ('MEDICINE', 'Medicine/Pharma', 6),
    ('FLOWERS', 'Flowers/Perishables', 7),
    ('OTHER', 'Other', 99);
    PRINT 'Seeded MasterPackageTypes data';
END
GO

-- Master Caution Types
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MasterCautionTypes')
BEGIN
    CREATE TABLE MasterCautionTypes (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Code NVARCHAR(20) NOT NULL UNIQUE,
        Name NVARCHAR(50) NOT NULL,
        Description NVARCHAR(200) NULL,
        Severity INT NOT NULL DEFAULT 1,
        HandlingInstructions NVARCHAR(500) NULL,
        IconClass NVARCHAR(50) NULL,
        SortOrder INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1
    );
    PRINT 'Created MasterCautionTypes table';
END
GO

IF NOT EXISTS (SELECT 1 FROM MasterCautionTypes)
BEGIN
    INSERT INTO MasterCautionTypes (Code, Name, Severity, SortOrder) VALUES
    ('NONE', 'No Special Handling', 0, 0),
    ('FRAGILE', 'Fragile - Handle with Care', 2, 1),
    ('LIQUID', 'Contains Liquid', 2, 2),
    ('PERISHABLE', 'Perishable - Time Sensitive', 3, 3),
    ('ELECTRONICS', 'Electronic Items', 2, 4),
    ('HAZARDOUS', 'Hazardous Material', 3, 5),
    ('VALUABLE', 'High Value Item', 2, 6),
    ('KEEP_UPRIGHT', 'Keep Upright', 1, 7);
    PRINT 'Seeded MasterCautionTypes data';
END
GO

-- Master Availability Types
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MasterAvailabilityTypes')
BEGIN
    CREATE TABLE MasterAvailabilityTypes (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Code NVARCHAR(20) NOT NULL UNIQUE,
        Name NVARCHAR(50) NOT NULL,
        Description NVARCHAR(200) NULL,
        IsActive BIT NOT NULL DEFAULT 1
    );
    PRINT 'Created MasterAvailabilityTypes table';
END
GO

IF NOT EXISTS (SELECT 1 FROM MasterAvailabilityTypes)
BEGIN
    INSERT INTO MasterAvailabilityTypes (Code, Name, Description) VALUES
    ('FULL_TIME', 'Full Time', 'Available all days, all hours'),
    ('PART_TIME', 'Part Time', 'Available specific hours'),
    ('WEEKENDS', 'Weekends Only', 'Available only on weekends'),
    ('ON_DEMAND', 'On Demand', 'Available when manually turned on');
    PRINT 'Seeded MasterAvailabilityTypes data';
END
GO

PRINT 'Phase 1 - Master Tables: COMPLETE';
