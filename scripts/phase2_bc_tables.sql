-- =====================================================
-- PHASE 2: BUSINESS CONSUMER - NORMALIZED TABLES
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- BusinessConsumers - Core Identity (replaces static part of BusinessConsumerProfiles)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BusinessConsumers')
BEGIN
    CREATE TABLE BusinessConsumers (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        UserId UNIQUEIDENTIFIER NOT NULL,

        -- Business Identity
        BusinessName NVARCHAR(255) NOT NULL,
        ContactPersonName NVARCHAR(255) NULL,
        BusinessCategory NVARCHAR(100) NULL,
        BusinessConstitution NVARCHAR(50) NULL,

        -- Tax Registration
        GSTIN NVARCHAR(15) NULL,
        GSTRegistrationType NVARCHAR(20) NULL,
        PAN NVARCHAR(10) NULL,

        -- Status
        IsActive BIT NOT NULL DEFAULT 0,
        ActivatedAt DATETIME2 NULL,
        DeactivatedAt DATETIME2 NULL,
        DeactivationReason NVARCHAR(500) NULL,

        -- Audit
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        -- Constraints
        CONSTRAINT FK_BusinessConsumers_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
        CONSTRAINT UQ_BusinessConsumers_UserId UNIQUE (UserId)
    );
    PRINT 'Created BusinessConsumers table';
END
GO

-- BCAddresses - Normalized business addresses (headquarters, branches, etc.)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BCAddresses')
BEGIN
    CREATE TABLE BCAddresses (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        BCId UNIQUEIDENTIFIER NOT NULL,

        -- Address type
        AddressType NVARCHAR(20) NOT NULL DEFAULT 'REGISTERED', -- REGISTERED, BRANCH, WAREHOUSE

        -- Address details
        AddressLabel NVARCHAR(100) NULL,
        AddressLine1 NVARCHAR(255) NOT NULL,
        AddressLine2 NVARCHAR(255) NULL,
        Landmark NVARCHAR(255) NULL,
        City NVARCHAR(100) NOT NULL,
        State NVARCHAR(100) NOT NULL,
        Pincode NVARCHAR(6) NOT NULL,

        -- Geolocation
        Latitude DECIMAL(10,8) NULL,
        Longitude DECIMAL(11,8) NULL,
        GeoLocation GEOGRAPHY NULL,

        -- Contact at this address
        ContactName NVARCHAR(255) NULL,
        ContactPhone NVARCHAR(15) NULL,
        ContactEmail NVARCHAR(255) NULL,

        -- Status
        IsPrimary BIT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,

        -- Audit
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_BCAddresses_BusinessConsumers FOREIGN KEY (BCId) REFERENCES BusinessConsumers(Id) ON DELETE CASCADE
    );
    PRINT 'Created BCAddresses table';
END
GO

-- BCSubscriptionHistory - Subscription history tracking
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BCSubscriptionHistory')
BEGIN
    CREATE TABLE BCSubscriptionHistory (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        BCId UNIQUEIDENTIFIER NOT NULL,
        SubscriptionPlanId UNIQUEIDENTIFIER NOT NULL,

        -- Period
        StartDate DATETIME2 NOT NULL,
        EndDate DATETIME2 NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'ACTIVE', -- ACTIVE, EXPIRED, CANCELLED, UPGRADED

        -- Audit
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_BCSubscriptionHistory_BusinessConsumers FOREIGN KEY (BCId) REFERENCES BusinessConsumers(Id),
        CONSTRAINT FK_BCSubscriptionHistory_Plans FOREIGN KEY (SubscriptionPlanId) REFERENCES SubscriptionPlans(Id)
    );
    PRINT 'Created BCSubscriptionHistory table';
END
GO

-- Master Business Categories
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MasterBusinessCategories')
BEGIN
    CREATE TABLE MasterBusinessCategories (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Code NVARCHAR(50) NOT NULL UNIQUE,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        ParentId INT NULL,
        SortOrder INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,

        CONSTRAINT FK_MasterBusinessCategories_Parent FOREIGN KEY (ParentId) REFERENCES MasterBusinessCategories(Id)
    );
    PRINT 'Created MasterBusinessCategories table';
END
GO

-- Seed business categories
IF NOT EXISTS (SELECT 1 FROM MasterBusinessCategories)
BEGIN
    INSERT INTO MasterBusinessCategories (Code, Name, SortOrder) VALUES
    ('RETAIL', 'Retail & E-commerce', 1),
    ('FOOD', 'Food & Restaurant', 2),
    ('PHARMA', 'Pharmaceutical & Medical', 3),
    ('ELECTRONICS', 'Electronics & Appliances', 4),
    ('FASHION', 'Fashion & Apparel', 5),
    ('GROCERY', 'Grocery & Supermarket', 6),
    ('DOCUMENTS', 'Documents & Courier', 7),
    ('MANUFACTURING', 'Manufacturing', 8),
    ('SERVICES', 'Professional Services', 9),
    ('OTHER', 'Other', 99);
    PRINT 'Seeded MasterBusinessCategories data';
END
GO

PRINT 'Phase 2 - BC Tables: COMPLETE';
