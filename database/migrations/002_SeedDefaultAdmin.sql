-- =============================================
-- Seed Default Admin User
-- Database: DeliveryDost_Dev
-- Created: 2024-12-07
-- =============================================

-- Default Admin Credentials:
-- Username: superadmin
-- Email: admin@deliverydost.com
-- Password: Admin@123 (MD5 hashed)
-- =============================================

-- Helper function to generate MD5 hash (if not exists)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fn_MD5Hash]') AND type = N'FN')
BEGIN
    EXEC('
    CREATE FUNCTION [dbo].[fn_MD5Hash](@input NVARCHAR(MAX))
    RETURNS NVARCHAR(32)
    AS
    BEGIN
        RETURN LOWER(CONVERT(NVARCHAR(32), HASHBYTES(''MD5'', @input), 2));
    END
    ');
    PRINT 'Function [fn_MD5Hash] created successfully.';
END
GO

-- Insert Default SuperAdmin
IF NOT EXISTS (SELECT 1 FROM [dbo].[Admins] WHERE [Username] = 'superadmin')
BEGIN
    INSERT INTO [dbo].[Admins] (
        [Id],
        [Username],
        [Email],
        [PasswordHash],
        [FullName],
        [Role],
        [IsActive],
        [CreatedAt],
        [UpdatedAt]
    )
    VALUES (
        NEWID(),
        'superadmin',
        'admin@deliverydost.com',
        LOWER(CONVERT(NVARCHAR(32), HASHBYTES('MD5', 'Admin@123'), 2)),
        'Super Administrator',
        'SuperAdmin',
        1,
        GETUTCDATE(),
        GETUTCDATE()
    );

    PRINT 'Default SuperAdmin user created successfully.';
    PRINT 'Username: superadmin';
    PRINT 'Password: Admin@123';
END
ELSE
    PRINT 'SuperAdmin user already exists.';
GO

-- Insert Default Admin
IF NOT EXISTS (SELECT 1 FROM [dbo].[Admins] WHERE [Username] = 'admin')
BEGIN
    INSERT INTO [dbo].[Admins] (
        [Id],
        [Username],
        [Email],
        [PasswordHash],
        [FullName],
        [Role],
        [IsActive],
        [CreatedAt],
        [UpdatedAt]
    )
    VALUES (
        NEWID(),
        'admin',
        'support@deliverydost.com',
        LOWER(CONVERT(NVARCHAR(32), HASHBYTES('MD5', 'Admin@123'), 2)),
        'System Administrator',
        'Admin',
        1,
        GETUTCDATE(),
        GETUTCDATE()
    );

    PRINT 'Default Admin user created successfully.';
    PRINT 'Username: admin';
    PRINT 'Password: Admin@123';
END
ELSE
    PRINT 'Admin user already exists.';
GO

-- Insert sample test users for each role (for development only)
-- Remove this section in production!

-- Sample Delivery Partner (DP)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Phone] = '9999900001')
BEGIN
    INSERT INTO [dbo].[Users] (
        [Id], [Phone], [Email], [FullName], [PasswordHash], [Role],
        [IsActive], [IsPhoneVerified], [CreatedAt], [UpdatedAt]
    )
    VALUES (
        NEWID(),
        '9999900001',
        'dp.test@deliverydost.com',
        'Test Delivery Partner',
        LOWER(CONVERT(NVARCHAR(32), HASHBYTES('MD5', 'Test@123'), 2)),
        'DP',
        1, 1, GETUTCDATE(), GETUTCDATE()
    );
    PRINT 'Sample DP user created (Phone: 9999900001, Password: Test@123)';
END
GO

-- Sample DP Collection Manager (DPCM)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Phone] = '9999900002')
BEGIN
    INSERT INTO [dbo].[Users] (
        [Id], [Phone], [Email], [FullName], [PasswordHash], [Role],
        [IsActive], [IsPhoneVerified], [CreatedAt], [UpdatedAt]
    )
    VALUES (
        NEWID(),
        '9999900002',
        'dpcm.test@deliverydost.com',
        'Test Collection Manager',
        LOWER(CONVERT(NVARCHAR(32), HASHBYTES('MD5', 'Test@123'), 2)),
        'DPCM',
        1, 1, GETUTCDATE(), GETUTCDATE()
    );
    PRINT 'Sample DPCM user created (Phone: 9999900002, Password: Test@123)';
END
GO

-- Sample E-Commerce Client (EC)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Phone] = '9999900003')
BEGIN
    INSERT INTO [dbo].[Users] (
        [Id], [Phone], [Email], [FullName], [PasswordHash], [Role],
        [IsActive], [IsPhoneVerified], [CreatedAt], [UpdatedAt]
    )
    VALUES (
        NEWID(),
        '9999900003',
        'ec.test@deliverydost.com',
        'Test E-Commerce Client',
        LOWER(CONVERT(NVARCHAR(32), HASHBYTES('MD5', 'Test@123'), 2)),
        'EC',
        1, 1, GETUTCDATE(), GETUTCDATE()
    );
    PRINT 'Sample EC user created (Phone: 9999900003, Password: Test@123)';
END
GO

-- Sample Direct Business Consumer (DBC)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Phone] = '9999900004')
BEGIN
    INSERT INTO [dbo].[Users] (
        [Id], [Phone], [Email], [FullName], [PasswordHash], [Role],
        [IsActive], [IsPhoneVerified], [CreatedAt], [UpdatedAt]
    )
    VALUES (
        NEWID(),
        '9999900004',
        'dbc.test@deliverydost.com',
        'Test Business Consumer',
        LOWER(CONVERT(NVARCHAR(32), HASHBYTES('MD5', 'Test@123'), 2)),
        'DBC',
        1, 1, GETUTCDATE(), GETUTCDATE()
    );
    PRINT 'Sample DBC user created (Phone: 9999900004, Password: Test@123)';
END
GO

PRINT '=============================================';
PRINT 'Seed data inserted successfully!';
PRINT '=============================================';
PRINT 'ADMIN CREDENTIALS:';
PRINT '  SuperAdmin - Username: superadmin, Password: Admin@123';
PRINT '  Admin      - Username: admin, Password: Admin@123';
PRINT '';
PRINT 'TEST USER CREDENTIALS (All passwords: Test@123):';
PRINT '  DP   - Phone: 9999900001';
PRINT '  DPCM - Phone: 9999900002';
PRINT '  EC   - Phone: 9999900003';
PRINT '  DBC  - Phone: 9999900004';
PRINT '=============================================';
GO
