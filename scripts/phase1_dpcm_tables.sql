-- =====================================================
-- PHASE 1: DPCM - NORMALIZED TABLES
-- =====================================================

USE DeliveryDost_Dev;
GO

-- DPCMServiceRegions - Normalized from ServiceRegions JSON
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DPCMServiceRegions')
BEGIN
    CREATE TABLE DPCMServiceRegions (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DPCMId UNIQUEIDENTIFIER NOT NULL,

        RegionType NVARCHAR(20) NOT NULL,
        StateName NVARCHAR(100) NULL,
        DistrictName NVARCHAR(100) NULL,
        Pincode NVARCHAR(6) NULL,
        CustomRegionName NVARCHAR(100) NULL,

        IsActive BIT NOT NULL DEFAULT 1,
        AssignedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_DPCMServiceRegions_DPCManagers FOREIGN KEY (DPCMId) REFERENCES DPCManagers(Id) ON DELETE CASCADE
    );
    PRINT 'Created DPCMServiceRegions table';
END
GO

-- DPCMSecurityDeposits - Security deposit history
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DPCMSecurityDeposits')
BEGIN
    CREATE TABLE DPCMSecurityDeposits (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DPCMId UNIQUEIDENTIFIER NOT NULL,

        Amount DECIMAL(10,2) NOT NULL,
        TransactionType NVARCHAR(20) NOT NULL,
        TransactionRef NVARCHAR(100) NULL,
        Status NVARCHAR(20) NOT NULL,

        ReceivedAt DATETIME2 NULL,
        RefundedAt DATETIME2 NULL,
        Remarks NVARCHAR(500) NULL,

        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_DPCMSecurityDeposits_DPCManagers FOREIGN KEY (DPCMId) REFERENCES DPCManagers(Id)
    );
    PRINT 'Created DPCMSecurityDeposits table';
END
GO

PRINT 'Phase 1 - DPCM Tables: COMPLETE';
