-- =====================================================
-- PHASE 3: ROW-LEVEL SECURITY (RLS)
-- Restricts data access based on user context
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- =====================================================
-- NOTE: Row-Level Security requires SQL Server 2016+
-- This creates security policies for multi-tenant isolation
-- =====================================================

PRINT 'Creating row-level security policies...';

-- =====================================================
-- SECURITY CONTEXT TABLE
-- Stores current user context for RLS
-- =====================================================

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SecurityContext')
BEGIN
    CREATE TABLE SecurityContext (
        SessionId INT PRIMARY KEY DEFAULT @@SPID,
        UserId UNIQUEIDENTIFIER NULL,
        UserRole NVARCHAR(20) NULL,
        DPCMId UNIQUEIDENTIFIER NULL, -- For DPCM scope
        SetAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    PRINT 'Created SecurityContext table';
END
GO

-- =====================================================
-- PROCEDURE: Set security context
-- Call this at the start of each request/session
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_SetSecurityContext')
    DROP PROCEDURE sp_SetSecurityContext;
GO

CREATE PROCEDURE sp_SetSecurityContext
    @UserId UNIQUEIDENTIFIER,
    @UserRole NVARCHAR(20),
    @DPCMId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Remove old context for this session
    DELETE FROM SecurityContext WHERE SessionId = @@SPID;

    -- Set new context
    INSERT INTO SecurityContext (SessionId, UserId, UserRole, DPCMId)
    VALUES (@@SPID, @UserId, @UserRole, @DPCMId);
END
GO

PRINT 'Created sp_SetSecurityContext procedure';

-- =====================================================
-- PROCEDURE: Clear security context
-- Call this at end of request/logout
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_ClearSecurityContext')
    DROP PROCEDURE sp_ClearSecurityContext;
GO

CREATE PROCEDURE sp_ClearSecurityContext
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM SecurityContext WHERE SessionId = @@SPID;
END
GO

PRINT 'Created sp_ClearSecurityContext procedure';

-- =====================================================
-- FUNCTION: Get current user ID
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'fn_GetCurrentUserId' AND type = 'FN')
    DROP FUNCTION fn_GetCurrentUserId;
GO

CREATE FUNCTION fn_GetCurrentUserId()
RETURNS UNIQUEIDENTIFIER
WITH SCHEMABINDING
AS
BEGIN
    RETURN (SELECT TOP 1 UserId FROM dbo.SecurityContext WHERE SessionId = @@SPID);
END
GO

PRINT 'Created fn_GetCurrentUserId function';

-- =====================================================
-- FUNCTION: Get current user role
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'fn_GetCurrentUserRole' AND type = 'FN')
    DROP FUNCTION fn_GetCurrentUserRole;
GO

CREATE FUNCTION fn_GetCurrentUserRole()
RETURNS NVARCHAR(20)
WITH SCHEMABINDING
AS
BEGIN
    RETURN (SELECT TOP 1 UserRole FROM dbo.SecurityContext WHERE SessionId = @@SPID);
END
GO

PRINT 'Created fn_GetCurrentUserRole function';

-- =====================================================
-- FUNCTION: Get current DPCM ID
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'fn_GetCurrentDPCMId' AND type = 'FN')
    DROP FUNCTION fn_GetCurrentDPCMId;
GO

CREATE FUNCTION fn_GetCurrentDPCMId()
RETURNS UNIQUEIDENTIFIER
WITH SCHEMABINDING
AS
BEGIN
    RETURN (SELECT TOP 1 DPCMId FROM dbo.SecurityContext WHERE SessionId = @@SPID);
END
GO

PRINT 'Created fn_GetCurrentDPCMId function';

-- =====================================================
-- SECURITY PREDICATE: Deliveries access
-- Users can only see their own deliveries
-- DPCMs can see deliveries of their DPs
-- Admins can see all
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'fn_DeliveriesSecurityPredicate' AND type = 'IF')
    DROP FUNCTION fn_DeliveriesSecurityPredicate;
GO

CREATE FUNCTION fn_DeliveriesSecurityPredicate(@RequesterId UNIQUEIDENTIFIER, @AssignedDPId UNIQUEIDENTIFIER)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
    SELECT 1 AS AccessGranted
    WHERE
        -- No context set = full access (for background jobs, migrations)
        dbo.fn_GetCurrentUserId() IS NULL
        -- SuperAdmin sees all
        OR dbo.fn_GetCurrentUserRole() = 'SuperAdmin'
        -- Requester sees their own
        OR @RequesterId = dbo.fn_GetCurrentUserId()
        -- Assigned DP sees their own
        OR @AssignedDPId = dbo.fn_GetCurrentUserId()
        -- DPCM sees their DPs' deliveries
        OR (dbo.fn_GetCurrentUserRole() = 'DPCM' AND EXISTS (
            SELECT 1 FROM dbo.DeliveryPartnerProfiles dp
            WHERE dp.UserId = @AssignedDPId
              AND dp.DPCMId = dbo.fn_GetCurrentDPCMId()
        ));
GO

PRINT 'Created fn_DeliveriesSecurityPredicate function';

-- =====================================================
-- SECURITY PREDICATE: DeliveryPartnerProfiles access
-- DPs see only their own profile
-- DPCMs see their assigned DPs
-- Admins see all
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'fn_DPProfilesSecurityPredicate' AND type = 'IF')
    DROP FUNCTION fn_DPProfilesSecurityPredicate;
GO

CREATE FUNCTION fn_DPProfilesSecurityPredicate(@UserId UNIQUEIDENTIFIER, @DPCMId UNIQUEIDENTIFIER)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
    SELECT 1 AS AccessGranted
    WHERE
        dbo.fn_GetCurrentUserId() IS NULL
        OR dbo.fn_GetCurrentUserRole() = 'SuperAdmin'
        OR @UserId = dbo.fn_GetCurrentUserId()
        OR (dbo.fn_GetCurrentUserRole() = 'DPCM' AND @DPCMId = dbo.fn_GetCurrentDPCMId());
GO

PRINT 'Created fn_DPProfilesSecurityPredicate function';

-- =====================================================
-- SECURITY PREDICATE: Wallets access
-- Users see only their own wallet
-- Admins see all
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'fn_WalletsSecurityPredicate' AND type = 'IF')
    DROP FUNCTION fn_WalletsSecurityPredicate;
GO

CREATE FUNCTION fn_WalletsSecurityPredicate(@UserId UNIQUEIDENTIFIER)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
    SELECT 1 AS AccessGranted
    WHERE
        dbo.fn_GetCurrentUserId() IS NULL
        OR dbo.fn_GetCurrentUserRole() = 'SuperAdmin'
        OR @UserId = dbo.fn_GetCurrentUserId();
GO

PRINT 'Created fn_WalletsSecurityPredicate function';

-- =====================================================
-- CREATE SECURITY POLICIES
-- Note: Uncomment to enable RLS (test thoroughly first!)
-- =====================================================

/*
-- Drop existing policies
IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = 'DeliveriesSecurityPolicy')
    DROP SECURITY POLICY DeliveriesSecurityPolicy;

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = 'DPProfilesSecurityPolicy')
    DROP SECURITY POLICY DPProfilesSecurityPolicy;

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = 'WalletsSecurityPolicy')
    DROP SECURITY POLICY WalletsSecurityPolicy;

-- Create policies (FILTER limits SELECT, BLOCK limits INSERT/UPDATE/DELETE)
CREATE SECURITY POLICY DeliveriesSecurityPolicy
    ADD FILTER PREDICATE dbo.fn_DeliveriesSecurityPredicate(RequesterId, AssignedDPId) ON dbo.Deliveries
    WITH (STATE = ON);

CREATE SECURITY POLICY DPProfilesSecurityPolicy
    ADD FILTER PREDICATE dbo.fn_DPProfilesSecurityPredicate(UserId, DPCMId) ON dbo.DeliveryPartnerProfiles
    WITH (STATE = ON);

CREATE SECURITY POLICY WalletsSecurityPolicy
    ADD FILTER PREDICATE dbo.fn_WalletsSecurityPredicate(UserId) ON dbo.Wallets
    WITH (STATE = ON);

PRINT 'Security policies ENABLED';
*/

PRINT 'Security policies created but NOT ENABLED (uncomment to enable)';

-- =====================================================
-- SECURE VIEWS: Alternative to RLS
-- Use these views instead of direct table access
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'v_MyDeliveries')
    DROP VIEW v_MyDeliveries;
GO

CREATE VIEW v_MyDeliveries AS
SELECT d.*
FROM Deliveries d
WHERE
    dbo.fn_GetCurrentUserId() IS NULL
    OR dbo.fn_GetCurrentUserRole() = 'SuperAdmin'
    OR d.RequesterId = dbo.fn_GetCurrentUserId()
    OR d.AssignedDPId = dbo.fn_GetCurrentUserId();
GO

PRINT 'Created v_MyDeliveries secure view';

IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'v_MyWallet')
    DROP VIEW v_MyWallet;
GO

CREATE VIEW v_MyWallet AS
SELECT w.*
FROM Wallets w
WHERE
    dbo.fn_GetCurrentUserId() IS NULL
    OR dbo.fn_GetCurrentUserRole() = 'SuperAdmin'
    OR w.UserId = dbo.fn_GetCurrentUserId();
GO

PRINT 'Created v_MyWallet secure view';

PRINT 'Phase 3 - Row-Level Security: COMPLETE';
