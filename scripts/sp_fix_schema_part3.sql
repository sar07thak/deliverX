-- =====================================================
-- SCHEMA FIX SCRIPT - PART 3
-- Fixes: Complaint module (TrackingCode doesn't exist - use Id)
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- =====================================================
-- FIX: usp_Complaint_Create
-- Deliveries has no TrackingCode, use Id instead
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Complaint_Create')
    DROP PROCEDURE usp_Complaint_Create;
GO

CREATE PROCEDURE usp_Complaint_Create
    @DeliveryId UNIQUEIDENTIFIER,
    @RaisedById UNIQUEIDENTIFIER,
    @RaisedByType NVARCHAR(10),
    @AgainstId UNIQUEIDENTIFIER = NULL,
    @AgainstType NVARCHAR(10) = NULL,
    @Category NVARCHAR(50),
    @Severity NVARCHAR(20) = 'MEDIUM',
    @Subject NVARCHAR(255),
    @Description NVARCHAR(MAX),
    @NewComplaintId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM Deliveries WHERE Id = @DeliveryId)
        BEGIN
            RAISERROR('Delivery not found', 16, 1);
            RETURN;
        END

        IF EXISTS (
            SELECT 1 FROM Complaints
            WHERE DeliveryId = @DeliveryId
              AND RaisedById = @RaisedById
              AND Category = @Category
              AND Status NOT IN ('CLOSED', 'REJECTED')
        )
        BEGIN
            RAISERROR('An active complaint already exists for this delivery', 16, 1);
            RETURN;
        END

        SET @NewComplaintId = NEWID();

        -- Generate complaint number manually since function has issues
        DECLARE @ComplaintNumber NVARCHAR(20) = 'CMP-' + FORMAT(GETUTCDATE(), 'yyyyMMdd') + '-' + RIGHT('00000' + CAST(ABS(CHECKSUM(NEWID())) % 100000 AS NVARCHAR), 5);

        INSERT INTO Complaints (
            Id, ComplaintNumber, DeliveryId, RaisedById, RaisedByType,
            AgainstId, AgainstType, Category, Severity,
            Subject, Description, Status,
            CreatedAt, UpdatedAt
        )
        VALUES (
            @NewComplaintId, @ComplaintNumber, @DeliveryId, @RaisedById, @RaisedByType,
            @AgainstId, @AgainstType, @Category, @Severity,
            @Subject, @Description, 'OPEN',
            GETUTCDATE(), GETUTCDATE()
        );

        SELECT
            c.Id, c.ComplaintNumber, c.DeliveryId, c.RaisedById, c.RaisedByType,
            c.AgainstId, c.AgainstType, c.Category, c.Severity,
            c.Subject, c.Description, c.Status,
            c.CreatedAt,
            CAST(d.Id AS NVARCHAR(50)) AS DeliveryReference,
            rb.FullName AS RaisedByName, rb.Phone AS RaisedByPhone
        FROM Complaints c
        INNER JOIN Deliveries d ON d.Id = c.DeliveryId
        INNER JOIN Users rb ON rb.Id = c.RaisedById
        WHERE c.Id = @NewComplaintId;

    END TRY
    BEGIN CATCH
        EXEC usp_LogError @ErrorContext = 'usp_Complaint_Create', @UserId = @RaisedById;
        THROW;
    END CATCH
END
GO

PRINT 'Fixed usp_Complaint_Create';

-- =====================================================
-- FIX: usp_Complaint_GetById
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Complaint_GetById')
    DROP PROCEDURE usp_Complaint_GetById;
GO

CREATE PROCEDURE usp_Complaint_GetById
    @ComplaintId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    -- Main complaint info
    SELECT
        c.Id, c.ComplaintNumber, c.DeliveryId, c.RaisedById, c.RaisedByType,
        c.AgainstId, c.AgainstType, c.Category, c.Severity,
        c.Subject, c.Description, c.Status,
        c.Resolution, c.ResolutionNotes,
        c.AssignedToId, c.AssignedAt, c.ResolvedAt, c.ClosedAt,
        c.CreatedAt, c.UpdatedAt,
        CAST(d.Id AS NVARCHAR(50)) AS DeliveryReference,
        d.Status AS DeliveryStatus,
        rb.FullName AS RaisedByName, rb.Phone AS RaisedByPhone, rb.Email AS RaisedByEmail,
        ag.FullName AS AgainstName, ag.Phone AS AgainstPhone,
        ast.FullName AS AssignedToName
    FROM Complaints c
    INNER JOIN Deliveries d ON d.Id = c.DeliveryId
    INNER JOIN Users rb ON rb.Id = c.RaisedById
    LEFT JOIN Users ag ON ag.Id = c.AgainstId
    LEFT JOIN Users ast ON ast.Id = c.AssignedToId
    WHERE c.Id = @ComplaintId;

    -- Evidence
    SELECT
        e.Id, e.Type, e.FileName, e.FileUrl, e.Description,
        e.UploadedById, e.UploadedAt,
        u.FullName AS UploadedByName
    FROM ComplaintEvidences e
    INNER JOIN Users u ON u.Id = e.UploadedById
    WHERE e.ComplaintId = @ComplaintId
    ORDER BY e.UploadedAt DESC;

    -- Comments
    SELECT
        cm.Id, cm.AuthorId, cm.Content, cm.IsInternal, cm.CreatedAt,
        u.FullName AS AuthorName, u.Role AS AuthorRole
    FROM ComplaintComments cm
    INNER JOIN Users u ON u.Id = cm.AuthorId
    WHERE cm.ComplaintId = @ComplaintId
    ORDER BY cm.CreatedAt ASC;
END
GO

PRINT 'Fixed usp_Complaint_GetById';

-- =====================================================
-- FIX: usp_Complaint_List
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Complaint_List')
    DROP PROCEDURE usp_Complaint_List;
GO

CREATE PROCEDURE usp_Complaint_List
    @PageNumber INT = 1,
    @PageSize INT = 20,
    @Status NVARCHAR(20) = NULL,
    @Category NVARCHAR(50) = NULL,
    @Severity NVARCHAR(20) = NULL,
    @RaisedById UNIQUEIDENTIFIER = NULL,
    @AgainstId UNIQUEIDENTIFIER = NULL,
    @AssignedToId UNIQUEIDENTIFIER = NULL,
    @SearchTerm NVARCHAR(100) = NULL,
    @FromDate DATETIME2 = NULL,
    @ToDate DATETIME2 = NULL,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    SELECT @TotalCount = COUNT(*)
    FROM Complaints c
    WHERE (@Status IS NULL OR c.Status = @Status)
      AND (@Category IS NULL OR c.Category = @Category)
      AND (@Severity IS NULL OR c.Severity = @Severity)
      AND (@RaisedById IS NULL OR c.RaisedById = @RaisedById)
      AND (@AgainstId IS NULL OR c.AgainstId = @AgainstId)
      AND (@AssignedToId IS NULL OR c.AssignedToId = @AssignedToId)
      AND (@SearchTerm IS NULL OR
           c.ComplaintNumber LIKE '%' + @SearchTerm + '%' OR
           c.Subject LIKE '%' + @SearchTerm + '%')
      AND (@FromDate IS NULL OR c.CreatedAt >= @FromDate)
      AND (@ToDate IS NULL OR c.CreatedAt <= @ToDate);

    SELECT
        c.Id, c.ComplaintNumber, c.DeliveryId, c.RaisedById, c.RaisedByType,
        c.AgainstId, c.AgainstType, c.Category, c.Severity,
        c.Subject, c.Status, c.AssignedToId,
        c.CreatedAt, c.UpdatedAt, c.ResolvedAt,
        rb.FullName AS RaisedByName,
        ag.FullName AS AgainstName,
        ast.FullName AS AssignedToName,
        CAST(d.Id AS NVARCHAR(50)) AS DeliveryReference,
        (SELECT COUNT(*) FROM ComplaintEvidences WHERE ComplaintId = c.Id) AS EvidenceCount,
        (SELECT COUNT(*) FROM ComplaintComments WHERE ComplaintId = c.Id) AS CommentCount
    FROM Complaints c
    INNER JOIN Users rb ON rb.Id = c.RaisedById
    INNER JOIN Deliveries d ON d.Id = c.DeliveryId
    LEFT JOIN Users ag ON ag.Id = c.AgainstId
    LEFT JOIN Users ast ON ast.Id = c.AssignedToId
    WHERE (@Status IS NULL OR c.Status = @Status)
      AND (@Category IS NULL OR c.Category = @Category)
      AND (@Severity IS NULL OR c.Severity = @Severity)
      AND (@RaisedById IS NULL OR c.RaisedById = @RaisedById)
      AND (@AgainstId IS NULL OR c.AgainstId = @AgainstId)
      AND (@AssignedToId IS NULL OR c.AssignedToId = @AssignedToId)
      AND (@SearchTerm IS NULL OR
           c.ComplaintNumber LIKE '%' + @SearchTerm + '%' OR
           c.Subject LIKE '%' + @SearchTerm + '%')
      AND (@FromDate IS NULL OR c.CreatedAt >= @FromDate)
      AND (@ToDate IS NULL OR c.CreatedAt <= @ToDate)
    ORDER BY
        CASE c.Severity WHEN 'CRITICAL' THEN 1 WHEN 'HIGH' THEN 2 WHEN 'MEDIUM' THEN 3 ELSE 4 END,
        c.CreatedAt DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    SELECT @TotalCount AS TotalCount;
END
GO

PRINT 'Fixed usp_Complaint_List';

-- =====================================================
-- FIX: usp_Complaint_Assign
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Complaint_Assign')
    DROP PROCEDURE usp_Complaint_Assign;
GO

CREATE PROCEDURE usp_Complaint_Assign
    @ComplaintId UNIQUEIDENTIFIER,
    @AssignedToId UNIQUEIDENTIFIER,
    @AssignedBy UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @OldAssignedTo UNIQUEIDENTIFIER;
        DECLARE @OldStatus NVARCHAR(20);

        SELECT @OldAssignedTo = AssignedToId, @OldStatus = Status
        FROM Complaints WHERE Id = @ComplaintId;

        IF @OldStatus IN ('CLOSED', 'REJECTED')
        BEGIN
            RAISERROR('Cannot assign closed or rejected complaint', 16, 1);
            RETURN;
        END

        UPDATE Complaints
        SET
            AssignedToId = @AssignedToId,
            AssignedAt = GETUTCDATE(),
            Status = CASE WHEN Status = 'OPEN' THEN 'ASSIGNED' ELSE Status END,
            UpdatedAt = GETUTCDATE()
        WHERE Id = @ComplaintId;

        IF EXISTS (SELECT 1 FROM Inspectors WHERE UserId = @AssignedToId)
        BEGIN
            UPDATE Inspectors
            SET ActiveCases = ActiveCases + 1
            WHERE UserId = @AssignedToId;

            IF @OldAssignedTo IS NOT NULL AND EXISTS (SELECT 1 FROM Inspectors WHERE UserId = @OldAssignedTo)
            BEGIN
                UPDATE Inspectors
                SET ActiveCases = CASE WHEN ActiveCases > 0 THEN ActiveCases - 1 ELSE 0 END
                WHERE UserId = @OldAssignedTo;
            END
        END

        INSERT INTO ComplaintComments (Id, ComplaintId, AuthorId, Content, IsInternal, CreatedAt)
        VALUES (
            NEWID(), @ComplaintId, @AssignedBy,
            'Complaint assigned to inspector',
            1, GETUTCDATE()
        );

        COMMIT;

        EXEC usp_Complaint_GetById @ComplaintId = @ComplaintId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        EXEC usp_LogError @ErrorContext = 'usp_Complaint_Assign';
        THROW;
    END CATCH
END
GO

PRINT 'Fixed usp_Complaint_Assign';

-- =====================================================
-- FIX: usp_Complaint_Resolve
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Complaint_Resolve')
    DROP PROCEDURE usp_Complaint_Resolve;
GO

CREATE PROCEDURE usp_Complaint_Resolve
    @ComplaintId UNIQUEIDENTIFIER,
    @Resolution NVARCHAR(100),
    @ResolutionNotes NVARCHAR(MAX),
    @ResolvedBy UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @Status NVARCHAR(20);
        DECLARE @AssignedToId UNIQUEIDENTIFIER;

        SELECT @Status = Status, @AssignedToId = AssignedToId
        FROM Complaints WHERE Id = @ComplaintId;

        IF @Status IN ('CLOSED', 'REJECTED', 'RESOLVED')
        BEGIN
            RAISERROR('Complaint already resolved or closed', 16, 1);
            RETURN;
        END

        UPDATE Complaints
        SET
            Status = 'RESOLVED',
            Resolution = @Resolution,
            ResolutionNotes = @ResolutionNotes,
            ResolvedAt = GETUTCDATE(),
            UpdatedAt = GETUTCDATE()
        WHERE Id = @ComplaintId;

        IF @AssignedToId IS NOT NULL AND EXISTS (SELECT 1 FROM Inspectors WHERE UserId = @AssignedToId)
        BEGIN
            UPDATE Inspectors
            SET
                ActiveCases = CASE WHEN ActiveCases > 0 THEN ActiveCases - 1 ELSE 0 END,
                TotalCasesHandled = TotalCasesHandled + 1
            WHERE UserId = @AssignedToId;
        END

        INSERT INTO ComplaintComments (Id, ComplaintId, AuthorId, Content, IsInternal, CreatedAt)
        VALUES (
            NEWID(), @ComplaintId, @ResolvedBy,
            'Complaint resolved: ' + @Resolution + '. ' + @ResolutionNotes,
            0, GETUTCDATE()
        );

        COMMIT;

        EXEC usp_Complaint_GetById @ComplaintId = @ComplaintId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        EXEC usp_LogError @ErrorContext = 'usp_Complaint_Resolve';
        THROW;
    END CATCH
END
GO

PRINT 'Fixed usp_Complaint_Resolve';

PRINT 'Schema fixes Part 3: COMPLETE';
