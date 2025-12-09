-- =====================================================
-- PHASE 3: CHANGE DATA CAPTURE (CDC) FOR AUDIT
-- Tracks all changes to critical tables
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- =====================================================
-- NOTE: SQL Server Express does NOT support CDC.
-- This script implements trigger-based change tracking instead.
-- For SQL Server Enterprise, enable native CDC.
-- =====================================================

PRINT 'Creating trigger-based change tracking system...';

-- =====================================================
-- CHANGE TRACKING TABLE
-- =====================================================

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ChangeTrackingLog')
BEGIN
    CREATE TABLE ChangeTrackingLog (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        TableName NVARCHAR(128) NOT NULL,
        OperationType NVARCHAR(10) NOT NULL, -- INSERT, UPDATE, DELETE
        PrimaryKeyValue NVARCHAR(255) NOT NULL,
        ChangedColumns NVARCHAR(MAX) NULL, -- JSON array of changed column names
        OldValues NVARCHAR(MAX) NULL, -- JSON object
        NewValues NVARCHAR(MAX) NULL, -- JSON object
        ChangedBy NVARCHAR(128) NULL,
        ChangedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        SessionId INT NULL,
        HostName NVARCHAR(128) NULL
    );
    PRINT 'Created ChangeTrackingLog table';
END
GO

-- Indexes for change tracking queries
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChangeTrackingLog_Table' AND object_id = OBJECT_ID('ChangeTrackingLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ChangeTrackingLog_Table ON ChangeTrackingLog(TableName, ChangedAt DESC);
    PRINT 'Created IX_ChangeTrackingLog_Table';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChangeTrackingLog_PK' AND object_id = OBJECT_ID('ChangeTrackingLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ChangeTrackingLog_PK ON ChangeTrackingLog(PrimaryKeyValue, TableName);
    PRINT 'Created IX_ChangeTrackingLog_PK';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChangeTrackingLog_Date' AND object_id = OBJECT_ID('ChangeTrackingLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ChangeTrackingLog_Date ON ChangeTrackingLog(ChangedAt DESC) INCLUDE (TableName, OperationType);
    PRINT 'Created IX_ChangeTrackingLog_Date';
END
GO

-- =====================================================
-- TRIGGER: Users table change tracking
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_Users_ChangeTracking')
    DROP TRIGGER TR_Users_ChangeTracking;
GO

CREATE TRIGGER TR_Users_ChangeTracking
ON Users
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Operation NVARCHAR(10);

    IF EXISTS(SELECT 1 FROM inserted) AND EXISTS(SELECT 1 FROM deleted)
        SET @Operation = 'UPDATE';
    ELSE IF EXISTS(SELECT 1 FROM inserted)
        SET @Operation = 'INSERT';
    ELSE
        SET @Operation = 'DELETE';

    -- Log INSERT
    IF @Operation = 'INSERT'
    BEGIN
        INSERT INTO ChangeTrackingLog (TableName, OperationType, PrimaryKeyValue, NewValues, ChangedBy, SessionId, HostName)
        SELECT
            'Users',
            'INSERT',
            CAST(i.Id AS NVARCHAR(255)),
            (SELECT i.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            SYSTEM_USER,
            @@SPID,
            HOST_NAME()
        FROM inserted i;
    END

    -- Log UPDATE
    IF @Operation = 'UPDATE'
    BEGIN
        INSERT INTO ChangeTrackingLog (TableName, OperationType, PrimaryKeyValue, OldValues, NewValues, ChangedBy, SessionId, HostName)
        SELECT
            'Users',
            'UPDATE',
            CAST(i.Id AS NVARCHAR(255)),
            (SELECT d.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            (SELECT i.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            SYSTEM_USER,
            @@SPID,
            HOST_NAME()
        FROM inserted i
        INNER JOIN deleted d ON i.Id = d.Id;
    END

    -- Log DELETE
    IF @Operation = 'DELETE'
    BEGIN
        INSERT INTO ChangeTrackingLog (TableName, OperationType, PrimaryKeyValue, OldValues, ChangedBy, SessionId, HostName)
        SELECT
            'Users',
            'DELETE',
            CAST(d.Id AS NVARCHAR(255)),
            (SELECT d.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            SYSTEM_USER,
            @@SPID,
            HOST_NAME()
        FROM deleted d;
    END
END
GO

PRINT 'Created TR_Users_ChangeTracking trigger';

-- =====================================================
-- TRIGGER: Deliveries table change tracking
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_Deliveries_ChangeTracking')
    DROP TRIGGER TR_Deliveries_ChangeTracking;
GO

CREATE TRIGGER TR_Deliveries_ChangeTracking
ON Deliveries
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Operation NVARCHAR(10);

    IF EXISTS(SELECT 1 FROM inserted) AND EXISTS(SELECT 1 FROM deleted)
        SET @Operation = 'UPDATE';
    ELSE IF EXISTS(SELECT 1 FROM inserted)
        SET @Operation = 'INSERT';
    ELSE
        SET @Operation = 'DELETE';

    IF @Operation = 'INSERT'
    BEGIN
        INSERT INTO ChangeTrackingLog (TableName, OperationType, PrimaryKeyValue, NewValues, ChangedBy, SessionId, HostName)
        SELECT 'Deliveries', 'INSERT', CAST(i.Id AS NVARCHAR(255)),
            (SELECT i.Id, i.RequesterId, i.Status, i.AssignedDPId, i.EstimatedPrice, i.FinalPrice FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            SYSTEM_USER, @@SPID, HOST_NAME()
        FROM inserted i;
    END

    IF @Operation = 'UPDATE'
    BEGIN
        INSERT INTO ChangeTrackingLog (TableName, OperationType, PrimaryKeyValue, OldValues, NewValues, ChangedBy, SessionId, HostName)
        SELECT 'Deliveries', 'UPDATE', CAST(i.Id AS NVARCHAR(255)),
            (SELECT d.Id, d.Status, d.AssignedDPId, d.FinalPrice FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            (SELECT i.Id, i.Status, i.AssignedDPId, i.FinalPrice FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            SYSTEM_USER, @@SPID, HOST_NAME()
        FROM inserted i
        INNER JOIN deleted d ON i.Id = d.Id;
    END

    IF @Operation = 'DELETE'
    BEGIN
        INSERT INTO ChangeTrackingLog (TableName, OperationType, PrimaryKeyValue, OldValues, ChangedBy, SessionId, HostName)
        SELECT 'Deliveries', 'DELETE', CAST(d.Id AS NVARCHAR(255)),
            (SELECT d.Id, d.RequesterId, d.Status FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            SYSTEM_USER, @@SPID, HOST_NAME()
        FROM deleted d;
    END
END
GO

PRINT 'Created TR_Deliveries_ChangeTracking trigger';

-- =====================================================
-- TRIGGER: Wallets table change tracking (financial)
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_Wallets_ChangeTracking')
    DROP TRIGGER TR_Wallets_ChangeTracking;
GO

CREATE TRIGGER TR_Wallets_ChangeTracking
ON Wallets
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Operation NVARCHAR(10);

    IF EXISTS(SELECT 1 FROM inserted) AND EXISTS(SELECT 1 FROM deleted)
        SET @Operation = 'UPDATE';
    ELSE IF EXISTS(SELECT 1 FROM inserted)
        SET @Operation = 'INSERT';
    ELSE
        SET @Operation = 'DELETE';

    IF @Operation = 'INSERT'
    BEGIN
        INSERT INTO ChangeTrackingLog (TableName, OperationType, PrimaryKeyValue, NewValues, ChangedBy, SessionId, HostName)
        SELECT 'Wallets', 'INSERT', CAST(i.Id AS NVARCHAR(255)),
            (SELECT i.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            SYSTEM_USER, @@SPID, HOST_NAME()
        FROM inserted i;
    END

    IF @Operation = 'UPDATE'
    BEGIN
        INSERT INTO ChangeTrackingLog (TableName, OperationType, PrimaryKeyValue, OldValues, NewValues, ChangedBy, SessionId, HostName)
        SELECT 'Wallets', 'UPDATE', CAST(i.Id AS NVARCHAR(255)),
            (SELECT d.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            (SELECT i.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            SYSTEM_USER, @@SPID, HOST_NAME()
        FROM inserted i
        INNER JOIN deleted d ON i.Id = d.Id;
    END

    IF @Operation = 'DELETE'
    BEGIN
        INSERT INTO ChangeTrackingLog (TableName, OperationType, PrimaryKeyValue, OldValues, ChangedBy, SessionId, HostName)
        SELECT 'Wallets', 'DELETE', CAST(d.Id AS NVARCHAR(255)),
            (SELECT d.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            SYSTEM_USER, @@SPID, HOST_NAME()
        FROM deleted d;
    END
END
GO

PRINT 'Created TR_Wallets_ChangeTracking trigger';

-- =====================================================
-- TRIGGER: WalletTransactions change tracking
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_WalletTransactions_ChangeTracking')
    DROP TRIGGER TR_WalletTransactions_ChangeTracking;
GO

CREATE TRIGGER TR_WalletTransactions_ChangeTracking
ON WalletTransactions
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Operation NVARCHAR(10);

    IF EXISTS(SELECT 1 FROM inserted) AND EXISTS(SELECT 1 FROM deleted)
        SET @Operation = 'UPDATE';
    ELSE IF EXISTS(SELECT 1 FROM inserted)
        SET @Operation = 'INSERT';
    ELSE
        SET @Operation = 'DELETE';

    IF @Operation = 'INSERT'
    BEGIN
        INSERT INTO ChangeTrackingLog (TableName, OperationType, PrimaryKeyValue, NewValues, ChangedBy, SessionId, HostName)
        SELECT 'WalletTransactions', 'INSERT', CAST(i.Id AS NVARCHAR(255)),
            (SELECT i.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            SYSTEM_USER, @@SPID, HOST_NAME()
        FROM inserted i;
    END

    IF @Operation = 'UPDATE'
    BEGIN
        INSERT INTO ChangeTrackingLog (TableName, OperationType, PrimaryKeyValue, OldValues, NewValues, ChangedBy, SessionId, HostName)
        SELECT 'WalletTransactions', 'UPDATE', CAST(i.Id AS NVARCHAR(255)),
            (SELECT d.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            (SELECT i.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            SYSTEM_USER, @@SPID, HOST_NAME()
        FROM inserted i
        INNER JOIN deleted d ON i.Id = d.Id;
    END

    IF @Operation = 'DELETE'
    BEGIN
        INSERT INTO ChangeTrackingLog (TableName, OperationType, PrimaryKeyValue, OldValues, ChangedBy, SessionId, HostName)
        SELECT 'WalletTransactions', 'DELETE', CAST(d.Id AS NVARCHAR(255)),
            (SELECT d.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            SYSTEM_USER, @@SPID, HOST_NAME()
        FROM deleted d;
    END
END
GO

PRINT 'Created TR_WalletTransactions_ChangeTracking trigger';

-- =====================================================
-- VIEW: Recent changes
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'v_RecentChanges')
    DROP VIEW v_RecentChanges;
GO

CREATE VIEW v_RecentChanges AS
SELECT TOP 1000
    Id,
    TableName,
    OperationType,
    PrimaryKeyValue,
    ChangedBy,
    ChangedAt,
    HostName,
    OldValues,
    NewValues
FROM ChangeTrackingLog
ORDER BY ChangedAt DESC;
GO

PRINT 'Created v_RecentChanges view';

-- =====================================================
-- PROCEDURE: Get entity change history
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_GetEntityHistory')
    DROP PROCEDURE sp_GetEntityHistory;
GO

CREATE PROCEDURE sp_GetEntityHistory
    @TableName NVARCHAR(128),
    @EntityId NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        OperationType,
        ChangedBy,
        ChangedAt,
        HostName,
        OldValues,
        NewValues
    FROM ChangeTrackingLog
    WHERE TableName = @TableName
      AND PrimaryKeyValue = @EntityId
    ORDER BY ChangedAt DESC;
END
GO

PRINT 'Created sp_GetEntityHistory procedure';

PRINT 'Phase 3 - CDC/Audit: COMPLETE';
