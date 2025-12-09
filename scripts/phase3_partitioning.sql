-- =====================================================
-- PHASE 3: TABLE PARTITIONING
-- Partitions large tables by date for performance
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- =====================================================
-- NOTE: SQL Server Express does NOT support partitioning.
-- This script creates partition-ready archive tables instead.
-- For SQL Server Standard/Enterprise, use actual partitioning.
-- =====================================================

PRINT 'Creating archive tables for historical data management...';

-- =====================================================
-- DELIVERIES ARCHIVE TABLE
-- =====================================================

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DeliveriesArchive')
BEGIN
    CREATE TABLE DeliveriesArchive (
        Id UNIQUEIDENTIFIER NOT NULL,
        RequesterId UNIQUEIDENTIFIER NOT NULL,
        RequesterType NVARCHAR(20) NULL,
        AssignedDPId UNIQUEIDENTIFIER NULL,
        AssignedAt DATETIME2 NULL,
        PickupLat DECIMAL(10,8) NULL,
        PickupLng DECIMAL(11,8) NULL,
        PickupAddress NVARCHAR(500) NULL,
        PickupContactName NVARCHAR(255) NULL,
        PickupContactPhone NVARCHAR(15) NULL,
        DropLat DECIMAL(10,8) NULL,
        DropLng DECIMAL(11,8) NULL,
        DropAddress NVARCHAR(500) NULL,
        DropContactName NVARCHAR(255) NULL,
        DropContactPhone NVARCHAR(15) NULL,
        WeightKg DECIMAL(8,3) NULL,
        PackageType NVARCHAR(50) NULL,
        Status NVARCHAR(50) NOT NULL,
        EstimatedPrice DECIMAL(10,2) NULL,
        FinalPrice DECIMAL(10,2) NULL,
        DistanceKm DECIMAL(10,3) NULL,
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NULL,
        CancelledAt DATETIME2 NULL,
        CancellationReason NVARCHAR(500) NULL,
        -- Archive metadata
        ArchivedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ArchiveYear INT NOT NULL,
        ArchiveMonth INT NOT NULL,

        CONSTRAINT PK_DeliveriesArchive PRIMARY KEY (Id, ArchiveYear, ArchiveMonth)
    );
    PRINT 'Created DeliveriesArchive table';
END
GO

-- Index for archive queries
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DeliveriesArchive_Date' AND object_id = OBJECT_ID('DeliveriesArchive'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DeliveriesArchive_Date ON DeliveriesArchive(ArchiveYear, ArchiveMonth, CreatedAt);
    PRINT 'Created IX_DeliveriesArchive_Date';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DeliveriesArchive_Requester' AND object_id = OBJECT_ID('DeliveriesArchive'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DeliveriesArchive_Requester ON DeliveriesArchive(RequesterId, ArchiveYear);
    PRINT 'Created IX_DeliveriesArchive_Requester';
END
GO

-- =====================================================
-- DELIVERY EVENTS ARCHIVE TABLE
-- =====================================================

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DeliveryEventsArchive')
BEGIN
    CREATE TABLE DeliveryEventsArchive (
        Id UNIQUEIDENTIFIER NOT NULL,
        DeliveryId UNIQUEIDENTIFIER NOT NULL,
        EventType NVARCHAR(50) NOT NULL,
        EventData NVARCHAR(MAX) NULL,
        Latitude DECIMAL(10,8) NULL,
        Longitude DECIMAL(11,8) NULL,
        CreatedAt DATETIME2 NOT NULL,
        CreatedBy UNIQUEIDENTIFIER NULL,
        -- Archive metadata
        ArchivedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ArchiveYear INT NOT NULL,
        ArchiveMonth INT NOT NULL,

        CONSTRAINT PK_DeliveryEventsArchive PRIMARY KEY (Id, ArchiveYear, ArchiveMonth)
    );
    PRINT 'Created DeliveryEventsArchive table';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DeliveryEventsArchive_Delivery' AND object_id = OBJECT_ID('DeliveryEventsArchive'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DeliveryEventsArchive_Delivery ON DeliveryEventsArchive(DeliveryId, ArchiveYear);
    PRINT 'Created IX_DeliveryEventsArchive_Delivery';
END
GO

-- =====================================================
-- AUDIT LOGS ARCHIVE TABLE
-- =====================================================

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AuditLogsArchive')
BEGIN
    CREATE TABLE AuditLogsArchive (
        Id UNIQUEIDENTIFIER NOT NULL,
        LogType NVARCHAR(20) NOT NULL, -- AUTH, ADMIN
        UserId UNIQUEIDENTIFIER NULL,
        Action NVARCHAR(100) NOT NULL,
        EntityType NVARCHAR(100) NULL,
        EntityId NVARCHAR(100) NULL,
        OldValues NVARCHAR(MAX) NULL,
        NewValues NVARCHAR(MAX) NULL,
        IpAddress NVARCHAR(50) NULL,
        UserAgent NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL,
        -- Archive metadata
        ArchivedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ArchiveYear INT NOT NULL,
        ArchiveMonth INT NOT NULL,

        CONSTRAINT PK_AuditLogsArchive PRIMARY KEY (Id, ArchiveYear, ArchiveMonth)
    );
    PRINT 'Created AuditLogsArchive table';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLogsArchive_Date' AND object_id = OBJECT_ID('AuditLogsArchive'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditLogsArchive_Date ON AuditLogsArchive(ArchiveYear, ArchiveMonth, CreatedAt);
    PRINT 'Created IX_AuditLogsArchive_Date';
END
GO

-- =====================================================
-- ARCHIVE PROCEDURE: Archive old deliveries
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_ArchiveOldDeliveries')
    DROP PROCEDURE sp_ArchiveOldDeliveries;
GO

CREATE PROCEDURE sp_ArchiveOldDeliveries
    @MonthsOld INT = 6,
    @BatchSize INT = 1000
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CutoffDate DATETIME2 = DATEADD(MONTH, -@MonthsOld, GETUTCDATE());
    DECLARE @ArchivedCount INT = 0;
    DECLARE @TotalArchived INT = 0;

    PRINT 'Archiving deliveries older than ' + CONVERT(VARCHAR, @CutoffDate, 120);

    WHILE 1 = 1
    BEGIN
        BEGIN TRANSACTION;

        -- Archive batch of completed/cancelled deliveries
        INSERT INTO DeliveriesArchive (
            Id, RequesterId, RequesterType, AssignedDPId, AssignedAt,
            PickupLat, PickupLng, PickupAddress, PickupContactName, PickupContactPhone,
            DropLat, DropLng, DropAddress, DropContactName, DropContactPhone,
            WeightKg, PackageType, Status, EstimatedPrice, FinalPrice, DistanceKm,
            CreatedAt, UpdatedAt, CancelledAt, CancellationReason,
            ArchiveYear, ArchiveMonth
        )
        SELECT TOP (@BatchSize)
            d.Id, d.RequesterId, d.RequesterType, d.AssignedDPId, d.AssignedAt,
            d.PickupLat, d.PickupLng, d.PickupAddress, d.PickupContactName, d.PickupContactPhone,
            d.DropLat, d.DropLng, d.DropAddress, d.DropContactName, d.DropContactPhone,
            d.WeightKg, d.PackageType, d.Status, d.EstimatedPrice, d.FinalPrice, d.DistanceKm,
            d.CreatedAt, d.UpdatedAt, d.CancelledAt, d.CancellationReason,
            YEAR(d.CreatedAt), MONTH(d.CreatedAt)
        FROM Deliveries d
        WHERE d.CreatedAt < @CutoffDate
          AND d.Status IN ('DELIVERED', 'CANCELLED', 'RETURNED')
          AND NOT EXISTS (SELECT 1 FROM DeliveriesArchive da WHERE da.Id = d.Id);

        SET @ArchivedCount = @@ROWCOUNT;
        SET @TotalArchived = @TotalArchived + @ArchivedCount;

        IF @ArchivedCount = 0
        BEGIN
            COMMIT;
            BREAK;
        END

        -- Delete archived records from main table
        DELETE d
        FROM Deliveries d
        WHERE d.Id IN (
            SELECT TOP (@BatchSize) da.Id
            FROM DeliveriesArchive da
            WHERE da.ArchivedAt >= DATEADD(MINUTE, -5, GETUTCDATE())
        );

        COMMIT;

        PRINT 'Archived batch: ' + CAST(@ArchivedCount AS VARCHAR) + ' deliveries';

        -- Prevent blocking
        WAITFOR DELAY '00:00:01';
    END

    PRINT 'Total archived: ' + CAST(@TotalArchived AS VARCHAR) + ' deliveries';

    RETURN @TotalArchived;
END
GO

PRINT 'Created sp_ArchiveOldDeliveries procedure';

-- =====================================================
-- ARCHIVE PROCEDURE: Archive old audit logs
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_ArchiveOldAuditLogs')
    DROP PROCEDURE sp_ArchiveOldAuditLogs;
GO

CREATE PROCEDURE sp_ArchiveOldAuditLogs
    @MonthsOld INT = 12,
    @BatchSize INT = 5000
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CutoffDate DATETIME2 = DATEADD(MONTH, -@MonthsOld, GETUTCDATE());
    DECLARE @TotalArchived INT = 0;

    PRINT 'Archiving audit logs older than ' + CONVERT(VARCHAR, @CutoffDate, 120);

    -- Archive AuthAuditLogs
    INSERT INTO AuditLogsArchive (Id, LogType, UserId, Action, EntityType, EntityId, IpAddress, UserAgent, CreatedAt, ArchiveYear, ArchiveMonth)
    SELECT Id, 'AUTH', UserId, Action, NULL, NULL, IpAddress, UserAgent, CreatedAt, YEAR(CreatedAt), MONTH(CreatedAt)
    FROM AuthAuditLogs
    WHERE CreatedAt < @CutoffDate
      AND NOT EXISTS (SELECT 1 FROM AuditLogsArchive a WHERE a.Id = AuthAuditLogs.Id);

    SET @TotalArchived = @TotalArchived + @@ROWCOUNT;

    -- Archive AdminAuditLogs
    INSERT INTO AuditLogsArchive (Id, LogType, UserId, Action, EntityType, EntityId, OldValues, NewValues, IpAddress, CreatedAt, ArchiveYear, ArchiveMonth)
    SELECT Id, 'ADMIN', AdminId, Action, EntityType, EntityId, OldValues, NewValues, IpAddress, CreatedAt, YEAR(CreatedAt), MONTH(CreatedAt)
    FROM AdminAuditLogs
    WHERE CreatedAt < @CutoffDate
      AND NOT EXISTS (SELECT 1 FROM AuditLogsArchive a WHERE a.Id = AdminAuditLogs.Id);

    SET @TotalArchived = @TotalArchived + @@ROWCOUNT;

    PRINT 'Total archived: ' + CAST(@TotalArchived AS VARCHAR) + ' audit logs';

    RETURN @TotalArchived;
END
GO

PRINT 'Created sp_ArchiveOldAuditLogs procedure';

-- =====================================================
-- SCHEDULED JOB: Archive maintenance (run monthly)
-- =====================================================

-- Note: Create a SQL Server Agent Job to run these procedures monthly
-- Or use application-level scheduling (Hangfire, etc.)

PRINT 'Phase 3 - Partitioning/Archiving: COMPLETE';
