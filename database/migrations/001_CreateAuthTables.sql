-- =============================================
-- DeliveryDost Authentication Tables
-- Database: DeliveryDost_Dev
-- Created: 2024-12-07
-- Note: Using MD5 hash for passwords (as requested)
-- =============================================

-- =============================================
-- 1. ADMIN TABLE (Separate from regular users)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Admins')
BEGIN
    CREATE TABLE [dbo].[Admins] (
        [Id]                    UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
        [Username]              NVARCHAR(100)       NOT NULL,
        [Email]                 NVARCHAR(256)       NOT NULL,
        [PasswordHash]          NVARCHAR(32)        NOT NULL,  -- MD5 produces 32 char hex string
        [FullName]              NVARCHAR(200)       NULL,
        [Role]                  NVARCHAR(50)        NOT NULL DEFAULT 'Admin',  -- SuperAdmin, Admin, Support
        [IsActive]              BIT                 NOT NULL DEFAULT 1,
        [IsTwoFactorEnabled]    BIT                 NOT NULL DEFAULT 0,
        [TwoFactorSecret]       NVARCHAR(256)       NULL,
        [LastLoginAt]           DATETIME2           NULL,
        [LastLoginIP]           NVARCHAR(50)        NULL,
        [FailedLoginAttempts]   INT                 NOT NULL DEFAULT 0,
        [LockedUntil]           DATETIME2           NULL,
        [PasswordChangedAt]     DATETIME2           NULL,
        [CreatedAt]             DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]             DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]             UNIQUEIDENTIFIER    NULL,

        CONSTRAINT [PK_Admins] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_Admins_Username] UNIQUE ([Username]),
        CONSTRAINT [UQ_Admins_Email] UNIQUE ([Email])
    );

    PRINT 'Table [Admins] created successfully.';
END
ELSE
    PRINT 'Table [Admins] already exists.';
GO

-- Index for faster login queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Admins_Username_IsActive')
    CREATE NONCLUSTERED INDEX [IX_Admins_Username_IsActive] ON [dbo].[Admins] ([Username], [IsActive]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Admins_Email_IsActive')
    CREATE NONCLUSTERED INDEX [IX_Admins_Email_IsActive] ON [dbo].[Admins] ([Email], [IsActive]);
GO


-- =============================================
-- 2. USERS TABLE (For DP, DPCM, EC, DBC, etc.)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id]                    UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
        [Phone]                 NVARCHAR(20)        NULL,
        [Email]                 NVARCHAR(256)       NULL,
        [FullName]              NVARCHAR(200)       NULL,
        [PasswordHash]          NVARCHAR(32)        NULL,      -- MD5 produces 32 char hex string
        [Role]                  NVARCHAR(50)        NOT NULL,  -- DP, DPCM, DBC, EC, Inspector
        [Is2FAEnabled]          BIT                 NOT NULL DEFAULT 0,
        [TotpSecret]            NVARCHAR(256)       NULL,
        [IsActive]              BIT                 NOT NULL DEFAULT 1,
        [IsEmailVerified]       BIT                 NOT NULL DEFAULT 0,
        [IsPhoneVerified]       BIT                 NOT NULL DEFAULT 0,
        [LastLoginAt]           DATETIME2           NULL,
        [LastLoginIP]           NVARCHAR(50)        NULL,
        [PasswordChangedAt]     DATETIME2           NULL,
        [FailedLoginAttempts]   INT                 NOT NULL DEFAULT 0,
        [LockedUntil]           DATETIME2           NULL,
        [CreatedAt]             DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]             DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id])
    );

    PRINT 'Table [Users] created successfully.';
END
ELSE
    PRINT 'Table [Users] already exists.';
GO

-- Unique constraint on Phone (when not null)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Users_Phone')
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_Users_Phone] ON [dbo].[Users] ([Phone]) WHERE [Phone] IS NOT NULL;
GO

-- Unique constraint on Email (when not null)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Users_Email')
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_Users_Email] ON [dbo].[Users] ([Email]) WHERE [Email] IS NOT NULL;
GO

-- Index for faster login queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Phone_IsActive')
    CREATE NONCLUSTERED INDEX [IX_Users_Phone_IsActive] ON [dbo].[Users] ([Phone], [IsActive]) WHERE [Phone] IS NOT NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Email_IsActive')
    CREATE NONCLUSTERED INDEX [IX_Users_Email_IsActive] ON [dbo].[Users] ([Email], [IsActive]) WHERE [Email] IS NOT NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Role')
    CREATE NONCLUSTERED INDEX [IX_Users_Role] ON [dbo].[Users] ([Role]);
GO


-- =============================================
-- 3. USER SESSIONS TABLE (For JWT refresh tokens)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserSessions')
BEGIN
    CREATE TABLE [dbo].[UserSessions] (
        [Id]                    UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
        [UserId]                UNIQUEIDENTIFIER    NOT NULL,
        [RefreshTokenHash]      NVARCHAR(64)        NOT NULL,  -- SHA256 of refresh token
        [DeviceType]            NVARCHAR(50)        NULL,      -- iOS, Android, Web
        [DeviceId]              NVARCHAR(256)       NULL,
        [IpAddress]             NVARCHAR(50)        NULL,
        [UserAgent]             NVARCHAR(500)       NULL,
        [Location]              NVARCHAR(200)       NULL,
        [CreatedAt]             DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        [LastActiveAt]          DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        [ExpiresAt]             DATETIME2           NOT NULL,
        [IsRevoked]             BIT                 NOT NULL DEFAULT 0,
        [RevokedAt]             DATETIME2           NULL,

        CONSTRAINT [PK_UserSessions] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_UserSessions_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
    );

    PRINT 'Table [UserSessions] created successfully.';
END
ELSE
    PRINT 'Table [UserSessions] already exists.';
GO

-- Index for faster session lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserSessions_UserId')
    CREATE NONCLUSTERED INDEX [IX_UserSessions_UserId] ON [dbo].[UserSessions] ([UserId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserSessions_RefreshTokenHash')
    CREATE NONCLUSTERED INDEX [IX_UserSessions_RefreshTokenHash] ON [dbo].[UserSessions] ([RefreshTokenHash]);
GO


-- =============================================
-- 4. ADMIN SESSIONS TABLE (Separate for security)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AdminSessions')
BEGIN
    CREATE TABLE [dbo].[AdminSessions] (
        [Id]                    UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
        [AdminId]               UNIQUEIDENTIFIER    NOT NULL,
        [RefreshTokenHash]      NVARCHAR(64)        NOT NULL,
        [IpAddress]             NVARCHAR(50)        NULL,
        [UserAgent]             NVARCHAR(500)       NULL,
        [CreatedAt]             DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        [LastActiveAt]          DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        [ExpiresAt]             DATETIME2           NOT NULL,
        [IsRevoked]             BIT                 NOT NULL DEFAULT 0,
        [RevokedAt]             DATETIME2           NULL,

        CONSTRAINT [PK_AdminSessions] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_AdminSessions_Admins] FOREIGN KEY ([AdminId]) REFERENCES [dbo].[Admins]([Id]) ON DELETE CASCADE
    );

    PRINT 'Table [AdminSessions] created successfully.';
END
ELSE
    PRINT 'Table [AdminSessions] already exists.';
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AdminSessions_AdminId')
    CREATE NONCLUSTERED INDEX [IX_AdminSessions_AdminId] ON [dbo].[AdminSessions] ([AdminId]);
GO


-- =============================================
-- 5. OTP VERIFICATIONS TABLE
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OTPVerifications')
BEGIN
    CREATE TABLE [dbo].[OTPVerifications] (
        [Id]                    UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
        [Phone]                 NVARCHAR(20)        NULL,
        [Email]                 NVARCHAR(256)       NULL,
        [OtpHash]               NVARCHAR(32)        NOT NULL,  -- MD5 hash of OTP
        [Purpose]               NVARCHAR(50)        NOT NULL,  -- Login, Signup, PasswordReset, PhoneVerify
        [Attempts]              INT                 NOT NULL DEFAULT 0,
        [MaxAttempts]           INT                 NOT NULL DEFAULT 3,
        [IsUsed]                BIT                 NOT NULL DEFAULT 0,
        [ExpiresAt]             DATETIME2           NOT NULL,
        [CreatedAt]             DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        [UsedAt]                DATETIME2           NULL,

        CONSTRAINT [PK_OTPVerifications] PRIMARY KEY CLUSTERED ([Id])
    );

    PRINT 'Table [OTPVerifications] created successfully.';
END
ELSE
    PRINT 'Table [OTPVerifications] already exists.';
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OTPVerifications_Phone_Purpose')
    CREATE NONCLUSTERED INDEX [IX_OTPVerifications_Phone_Purpose] ON [dbo].[OTPVerifications] ([Phone], [Purpose]) WHERE [Phone] IS NOT NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OTPVerifications_Email_Purpose')
    CREATE NONCLUSTERED INDEX [IX_OTPVerifications_Email_Purpose] ON [dbo].[OTPVerifications] ([Email], [Purpose]) WHERE [Email] IS NOT NULL;
GO


-- =============================================
-- 6. AUTH AUDIT LOG TABLE
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuthAuditLogs')
BEGIN
    CREATE TABLE [dbo].[AuthAuditLogs] (
        [Id]                    UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
        [UserId]                UNIQUEIDENTIFIER    NULL,
        [AdminId]               UNIQUEIDENTIFIER    NULL,
        [Action]                NVARCHAR(100)       NOT NULL,  -- Login, Logout, FailedLogin, PasswordChange, etc.
        [IpAddress]             NVARCHAR(50)        NULL,
        [UserAgent]             NVARCHAR(500)       NULL,
        [Details]               NVARCHAR(MAX)       NULL,      -- JSON with additional details
        [IsSuccess]             BIT                 NOT NULL DEFAULT 1,
        [CreatedAt]             DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT [PK_AuthAuditLogs] PRIMARY KEY CLUSTERED ([Id])
    );

    PRINT 'Table [AuthAuditLogs] created successfully.';
END
ELSE
    PRINT 'Table [AuthAuditLogs] already exists.';
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuthAuditLogs_UserId')
    CREATE NONCLUSTERED INDEX [IX_AuthAuditLogs_UserId] ON [dbo].[AuthAuditLogs] ([UserId]) WHERE [UserId] IS NOT NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuthAuditLogs_AdminId')
    CREATE NONCLUSTERED INDEX [IX_AuthAuditLogs_AdminId] ON [dbo].[AuthAuditLogs] ([AdminId]) WHERE [AdminId] IS NOT NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuthAuditLogs_Action_CreatedAt')
    CREATE NONCLUSTERED INDEX [IX_AuthAuditLogs_Action_CreatedAt] ON [dbo].[AuthAuditLogs] ([Action], [CreatedAt]);
GO


PRINT '=============================================';
PRINT 'All authentication tables created successfully!';
PRINT '=============================================';
GO
