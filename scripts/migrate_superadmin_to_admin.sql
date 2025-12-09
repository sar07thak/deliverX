-- ============================================
-- DeliveryDost - Migrate SuperAdmin to Admin
-- Run this script to update all SuperAdmin users to Admin role
-- ============================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

PRINT '============================================'
PRINT 'MIGRATING SuperAdmin USERS TO Admin'
PRINT '============================================'

-- Count existing SuperAdmin users
DECLARE @SuperAdminCount INT;
SELECT @SuperAdminCount = COUNT(*) FROM Users WHERE Role = 'SuperAdmin';
PRINT 'Found ' + CAST(@SuperAdminCount AS VARCHAR) + ' users with SuperAdmin role'

-- Update all SuperAdmin users to Admin
IF @SuperAdminCount > 0
BEGIN
    UPDATE Users
    SET Role = 'Admin', UpdatedAt = GETUTCDATE()
    WHERE Role = 'SuperAdmin';

    PRINT 'Successfully updated ' + CAST(@SuperAdminCount AS VARCHAR) + ' users from SuperAdmin to Admin'
END
ELSE
BEGIN
    PRINT 'No SuperAdmin users found to update'
END

-- Verify the update
PRINT ''
PRINT '============================================'
PRINT 'VERIFICATION - Users by Role:'
PRINT '============================================'
SELECT Role, COUNT(*) AS UserCount
FROM Users
GROUP BY Role
ORDER BY Role;

PRINT ''
PRINT 'Migration complete!'
