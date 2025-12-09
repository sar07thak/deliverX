-- =====================================================
-- PHASE 3: FULL-TEXT SEARCH FOR ADDRESSES
-- Enables fast text search across address fields
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- =====================================================
-- NOTE: Full-Text Search requires SQL Server with FTS installed
-- Check if available: SELECT FULLTEXTSERVICEPROPERTY('IsFullTextInstalled')
-- =====================================================

PRINT 'Setting up full-text search...';

-- =====================================================
-- CREATE FULL-TEXT CATALOG
-- =====================================================

IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'DeliveryDostCatalog')
BEGIN
    CREATE FULLTEXT CATALOG DeliveryDostCatalog AS DEFAULT;
    PRINT 'Created DeliveryDostCatalog full-text catalog';
END
GO

-- =====================================================
-- FULL-TEXT INDEX: Deliveries (addresses)
-- =====================================================

-- Check if full-text index exists and drop if needed
IF EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('Deliveries'))
BEGIN
    DROP FULLTEXT INDEX ON Deliveries;
    PRINT 'Dropped existing full-text index on Deliveries';
END
GO

-- Create full-text index on address columns
CREATE FULLTEXT INDEX ON Deliveries (
    PickupAddress LANGUAGE 1033,
    DropAddress LANGUAGE 1033,
    PickupContactName LANGUAGE 1033,
    DropContactName LANGUAGE 1033,
    PackageDescription LANGUAGE 1033
)
KEY INDEX PK_Deliveries ON DeliveryDostCatalog
WITH CHANGE_TRACKING AUTO;
GO

PRINT 'Created full-text index on Deliveries';

-- =====================================================
-- FULL-TEXT INDEX: SavedAddresses
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('SavedAddresses'))
BEGIN
    DROP FULLTEXT INDEX ON SavedAddresses;
END
GO

CREATE FULLTEXT INDEX ON SavedAddresses (
    AddressLine1 LANGUAGE 1033,
    AddressLine2 LANGUAGE 1033,
    City LANGUAGE 1033,
    Landmark LANGUAGE 1033
)
KEY INDEX PK_SavedAddresses ON DeliveryDostCatalog
WITH CHANGE_TRACKING AUTO;
GO

PRINT 'Created full-text index on SavedAddresses';

-- =====================================================
-- FULL-TEXT INDEX: BCPickupLocations
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('BCPickupLocations'))
BEGIN
    DROP FULLTEXT INDEX ON BCPickupLocations;
END
GO

CREATE FULLTEXT INDEX ON BCPickupLocations (
    LocationName LANGUAGE 1033,
    AddressLine1 LANGUAGE 1033,
    AddressLine2 LANGUAGE 1033,
    City LANGUAGE 1033
)
KEY INDEX PK_BCPickupLocations ON DeliveryDostCatalog
WITH CHANGE_TRACKING AUTO;
GO

PRINT 'Created full-text index on BCPickupLocations';

-- =====================================================
-- FULL-TEXT INDEX: Users (names, emails)
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('Users'))
BEGIN
    DROP FULLTEXT INDEX ON Users;
END
GO

CREATE FULLTEXT INDEX ON Users (
    FullName LANGUAGE 1033,
    Email LANGUAGE 1033
)
KEY INDEX PK_Users ON DeliveryDostCatalog
WITH CHANGE_TRACKING AUTO;
GO

PRINT 'Created full-text index on Users';

-- =====================================================
-- FULL-TEXT INDEX: DeliveryAddresses (normalized)
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('DeliveryAddresses'))
BEGIN
    DROP FULLTEXT INDEX ON DeliveryAddresses;
END
GO

CREATE FULLTEXT INDEX ON DeliveryAddresses (
    AddressLine LANGUAGE 1033,
    AddressName LANGUAGE 1033,
    ContactName LANGUAGE 1033
)
KEY INDEX PK_DeliveryAddresses ON DeliveryDostCatalog
WITH CHANGE_TRACKING AUTO;
GO

PRINT 'Created full-text index on DeliveryAddresses';

-- =====================================================
-- FULL-TEXT INDEX: BCAddresses (normalized)
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('BCAddresses'))
BEGIN
    DROP FULLTEXT INDEX ON BCAddresses;
END
GO

CREATE FULLTEXT INDEX ON BCAddresses (
    AddressLine1 LANGUAGE 1033,
    AddressLine2 LANGUAGE 1033,
    AddressLabel LANGUAGE 1033,
    City LANGUAGE 1033,
    Landmark LANGUAGE 1033
)
KEY INDEX PK_BCAddresses ON DeliveryDostCatalog
WITH CHANGE_TRACKING AUTO;
GO

PRINT 'Created full-text index on BCAddresses';

-- =====================================================
-- SEARCH PROCEDURE: Search deliveries by address
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_SearchDeliveries')
    DROP PROCEDURE sp_SearchDeliveries;
GO

CREATE PROCEDURE sp_SearchDeliveries
    @SearchTerm NVARCHAR(200),
    @PageNumber INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    -- Use CONTAINS for full-text search
    SELECT
        d.Id,
        d.Status,
        d.PickupAddress,
        d.DropAddress,
        d.PickupContactName,
        d.DropContactName,
        d.CreatedAt,
        d.EstimatedPrice,
        -- Relevance score
        KEY_TBL.RANK AS SearchRank
    FROM Deliveries d
    INNER JOIN CONTAINSTABLE(Deliveries, (PickupAddress, DropAddress, PickupContactName, DropContactName, PackageDescription), @SearchTerm) AS KEY_TBL
        ON d.Id = KEY_TBL.[KEY]
    ORDER BY KEY_TBL.RANK DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    -- Return total count
    SELECT COUNT(*) AS TotalCount
    FROM CONTAINSTABLE(Deliveries, (PickupAddress, DropAddress, PickupContactName, DropContactName, PackageDescription), @SearchTerm);
END
GO

PRINT 'Created sp_SearchDeliveries procedure';

-- =====================================================
-- SEARCH PROCEDURE: Search users
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_SearchUsers')
    DROP PROCEDURE sp_SearchUsers;
GO

CREATE PROCEDURE sp_SearchUsers
    @SearchTerm NVARCHAR(200),
    @Role NVARCHAR(20) = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    SELECT
        u.Id,
        u.FullName,
        u.Phone,
        u.Email,
        u.Role,
        u.IsActive,
        u.CreatedAt,
        KEY_TBL.RANK AS SearchRank
    FROM Users u
    INNER JOIN CONTAINSTABLE(Users, (FullName, Email), @SearchTerm) AS KEY_TBL
        ON u.Id = KEY_TBL.[KEY]
    WHERE (@Role IS NULL OR u.Role = @Role)
    ORDER BY KEY_TBL.RANK DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

PRINT 'Created sp_SearchUsers procedure';

-- =====================================================
-- SEARCH PROCEDURE: Search addresses
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_SearchAddresses')
    DROP PROCEDURE sp_SearchAddresses;
GO

CREATE PROCEDURE sp_SearchAddresses
    @SearchTerm NVARCHAR(200),
    @UserId UNIQUEIDENTIFIER = NULL,
    @PageSize INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    -- Search in SavedAddresses
    SELECT TOP (@PageSize)
        'SavedAddress' AS Source,
        sa.Id,
        sa.AddressLabel AS Name,
        CONCAT(sa.AddressLine1, ', ', sa.City, ', ', sa.Pincode) AS FullAddress,
        sa.Latitude,
        sa.Longitude,
        KEY_TBL.RANK AS SearchRank
    FROM SavedAddresses sa
    INNER JOIN CONTAINSTABLE(SavedAddresses, (AddressLine1, AddressLine2, City, Landmark), @SearchTerm) AS KEY_TBL
        ON sa.Id = KEY_TBL.[KEY]
    WHERE (@UserId IS NULL OR sa.UserId = @UserId)
      AND sa.IsActive = 1
    ORDER BY KEY_TBL.RANK DESC;
END
GO

PRINT 'Created sp_SearchAddresses procedure';

-- =====================================================
-- VIEW: Full-text index status
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'v_FullTextIndexStatus')
    DROP VIEW v_FullTextIndexStatus;
GO

CREATE VIEW v_FullTextIndexStatus AS
SELECT
    OBJECT_NAME(fti.object_id) AS TableName,
    fc.name AS CatalogName,
    fti.is_enabled AS IsEnabled,
    fti.change_tracking_state_desc AS ChangeTracking,
    fti.crawl_type_desc AS LastCrawlType,
    fti.crawl_start_date AS LastCrawlStart,
    fti.crawl_end_date AS LastCrawlEnd,
    OBJECTPROPERTY(fti.object_id, 'TableFullTextItemCount') AS IndexedRowCount
FROM sys.fulltext_indexes fti
INNER JOIN sys.fulltext_catalogs fc ON fti.fulltext_catalog_id = fc.fulltext_catalog_id;
GO

PRINT 'Created v_FullTextIndexStatus view';

PRINT 'Phase 3 - Full-Text Search: COMPLETE';
