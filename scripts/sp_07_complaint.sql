-- =====================================================
-- DELIVERYDOST STORED PROCEDURES - COMPLAINT MODULE
-- =====================================================

USE DeliveryDost_Dev;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- =====================================================
-- SEQUENCE: Inspector Codes
-- =====================================================

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'seq_InspectorCode')
BEGIN
    CREATE SEQUENCE seq_InspectorCode
        AS INT
        START WITH 1001
        INCREMENT BY 1
        NO CACHE;
    PRINT 'Created seq_InspectorCode';
END
GO

-- =====================================================
-- PROCEDURE: Create Complaint
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Complaint_Create')
    DROP PROCEDURE usp_Complaint_Create;
GO

CREATE PROCEDURE usp_Complaint_Create
    @DeliveryId UNIQUEIDENTIFIER,
    @RaisedById UNIQUEIDENTIFIER,
    @RaisedByType NVARCHAR(10), -- EC, BC, DP
    @AgainstId UNIQUEIDENTIFIER = NULL,
    @AgainstType NVARCHAR(10) = NULL,
    @Category NVARCHAR(50), -- DAMAGE, THEFT, DELAY, BEHAVIOR, FRAUD, OTHER
    @Severity NVARCHAR(20) = 'MEDIUM', -- LOW, MEDIUM, HIGH, CRITICAL
    @Subject NVARCHAR(255),
    @Description NVARCHAR(MAX),
    @NewComplaintId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        -- Validate delivery exists
        IF NOT EXISTS (SELECT 1 FROM Deliveries WHERE Id = @DeliveryId)
        BEGIN
            RAISERROR('Delivery not found', 16, 1);
            RETURN;
        END

        -- Check for duplicate complaint
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
        DECLARE @ComplaintNumber NVARCHAR(20) = dbo.ufn_GenerateComplaintNumber();

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

        -- Return created complaint
        SELECT
            c.Id, c.ComplaintNumber, c.DeliveryId, c.RaisedById, c.RaisedByType,
            c.AgainstId, c.AgainstType, c.Category, c.Severity,
            c.Subject, c.Description, c.Status,
            c.CreatedAt,
            d.TrackingCode AS DeliveryTrackingCode,
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

PRINT 'Created usp_Complaint_Create';

-- =====================================================
-- PROCEDURE: Get Complaint By ID
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
        d.TrackingCode AS DeliveryTrackingCode,
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

PRINT 'Created usp_Complaint_GetById';

-- =====================================================
-- PROCEDURE: List Complaints (with filters and pagination)
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

    -- Total count
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

    -- Paged results
    SELECT
        c.Id, c.ComplaintNumber, c.DeliveryId, c.RaisedById, c.RaisedByType,
        c.AgainstId, c.AgainstType, c.Category, c.Severity,
        c.Subject, c.Status, c.AssignedToId,
        c.CreatedAt, c.UpdatedAt, c.ResolvedAt,
        rb.FullName AS RaisedByName,
        ag.FullName AS AgainstName,
        ast.FullName AS AssignedToName,
        d.TrackingCode AS DeliveryTrackingCode,
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

PRINT 'Created usp_Complaint_List';

-- =====================================================
-- PROCEDURE: Assign Complaint to Inspector/Admin
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

        -- Update inspector active cases
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

        -- Add internal comment
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

PRINT 'Created usp_Complaint_Assign';

-- =====================================================
-- PROCEDURE: Update Complaint Status
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Complaint_UpdateStatus')
    DROP PROCEDURE usp_Complaint_UpdateStatus;
GO

CREATE PROCEDURE usp_Complaint_UpdateStatus
    @ComplaintId UNIQUEIDENTIFIER,
    @NewStatus NVARCHAR(20), -- OPEN, ASSIGNED, IN_PROGRESS, RESOLVED, CLOSED, REJECTED
    @UpdatedBy UNIQUEIDENTIFIER,
    @Notes NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @OldStatus NVARCHAR(20);
        SELECT @OldStatus = Status FROM Complaints WHERE Id = @ComplaintId;

        -- Validate status transition
        IF @OldStatus = 'CLOSED' OR @OldStatus = 'REJECTED'
        BEGIN
            RAISERROR('Cannot update closed or rejected complaint', 16, 1);
            RETURN;
        END

        UPDATE Complaints
        SET
            Status = @NewStatus,
            UpdatedAt = GETUTCDATE()
        WHERE Id = @ComplaintId;

        -- Add comment for status change
        IF @Notes IS NOT NULL
        BEGIN
            INSERT INTO ComplaintComments (Id, ComplaintId, AuthorId, Content, IsInternal, CreatedAt)
            VALUES (NEWID(), @ComplaintId, @UpdatedBy, @Notes, 1, GETUTCDATE());
        END

        COMMIT;

        SELECT 1 AS Success, 'Status updated to ' + @NewStatus AS Message;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        EXEC usp_LogError @ErrorContext = 'usp_Complaint_UpdateStatus';
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_Complaint_UpdateStatus';

-- =====================================================
-- PROCEDURE: Resolve Complaint
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Complaint_Resolve')
    DROP PROCEDURE usp_Complaint_Resolve;
GO

CREATE PROCEDURE usp_Complaint_Resolve
    @ComplaintId UNIQUEIDENTIFIER,
    @Resolution NVARCHAR(100), -- REFUND, COMPENSATION, NO_ACTION, PENALTY, OTHER
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

        -- Update inspector stats
        IF @AssignedToId IS NOT NULL AND EXISTS (SELECT 1 FROM Inspectors WHERE UserId = @AssignedToId)
        BEGIN
            UPDATE Inspectors
            SET
                ActiveCases = CASE WHEN ActiveCases > 0 THEN ActiveCases - 1 ELSE 0 END,
                TotalCasesHandled = TotalCasesHandled + 1
            WHERE UserId = @AssignedToId;
        END

        -- Add resolution comment
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

PRINT 'Created usp_Complaint_Resolve';

-- =====================================================
-- PROCEDURE: Close Complaint
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Complaint_Close')
    DROP PROCEDURE usp_Complaint_Close;
GO

CREATE PROCEDURE usp_Complaint_Close
    @ComplaintId UNIQUEIDENTIFIER,
    @ClosedBy UNIQUEIDENTIFIER,
    @ClosingNotes NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE Complaints
        SET
            Status = 'CLOSED',
            ClosedAt = GETUTCDATE(),
            UpdatedAt = GETUTCDATE()
        WHERE Id = @ComplaintId;

        IF @ClosingNotes IS NOT NULL
        BEGIN
            INSERT INTO ComplaintComments (Id, ComplaintId, AuthorId, Content, IsInternal, CreatedAt)
            VALUES (NEWID(), @ComplaintId, @ClosedBy, @ClosingNotes, 1, GETUTCDATE());
        END

        COMMIT;

        SELECT 1 AS Success, 'Complaint closed' AS Message;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        EXEC usp_LogError @ErrorContext = 'usp_Complaint_Close';
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_Complaint_Close';

-- =====================================================
-- PROCEDURE: Add Evidence to Complaint
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Complaint_AddEvidence')
    DROP PROCEDURE usp_Complaint_AddEvidence;
GO

CREATE PROCEDURE usp_Complaint_AddEvidence
    @ComplaintId UNIQUEIDENTIFIER,
    @Type NVARCHAR(20), -- PHOTO, VIDEO, DOCUMENT, AUDIO
    @FileName NVARCHAR(255),
    @FileUrl NVARCHAR(500),
    @Description NVARCHAR(500) = NULL,
    @UploadedById UNIQUEIDENTIFIER,
    @NewEvidenceId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Check complaint exists and not closed
        IF NOT EXISTS (SELECT 1 FROM Complaints WHERE Id = @ComplaintId AND Status NOT IN ('CLOSED', 'REJECTED'))
        BEGIN
            RAISERROR('Complaint not found or closed', 16, 1);
            RETURN;
        END

        SET @NewEvidenceId = NEWID();

        INSERT INTO ComplaintEvidences (
            Id, ComplaintId, Type, FileName, FileUrl,
            Description, UploadedById, UploadedAt
        )
        VALUES (
            @NewEvidenceId, @ComplaintId, @Type, @FileName, @FileUrl,
            @Description, @UploadedById, GETUTCDATE()
        );

        SELECT
            e.*, u.FullName AS UploadedByName
        FROM ComplaintEvidences e
        INNER JOIN Users u ON u.Id = e.UploadedById
        WHERE e.Id = @NewEvidenceId;

    END TRY
    BEGIN CATCH
        EXEC usp_LogError @ErrorContext = 'usp_Complaint_AddEvidence', @UserId = @UploadedById;
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_Complaint_AddEvidence';

-- =====================================================
-- PROCEDURE: Add Comment to Complaint
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Complaint_AddComment')
    DROP PROCEDURE usp_Complaint_AddComment;
GO

CREATE PROCEDURE usp_Complaint_AddComment
    @ComplaintId UNIQUEIDENTIFIER,
    @AuthorId UNIQUEIDENTIFIER,
    @Content NVARCHAR(MAX),
    @IsInternal BIT = 0,
    @NewCommentId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Check complaint exists
        IF NOT EXISTS (SELECT 1 FROM Complaints WHERE Id = @ComplaintId)
        BEGIN
            RAISERROR('Complaint not found', 16, 1);
            RETURN;
        END

        SET @NewCommentId = NEWID();

        INSERT INTO ComplaintComments (
            Id, ComplaintId, AuthorId, Content, IsInternal, CreatedAt
        )
        VALUES (
            @NewCommentId, @ComplaintId, @AuthorId, @Content, @IsInternal, GETUTCDATE()
        );

        -- Update complaint UpdatedAt
        UPDATE Complaints SET UpdatedAt = GETUTCDATE() WHERE Id = @ComplaintId;

        SELECT
            cm.*, u.FullName AS AuthorName, u.Role AS AuthorRole
        FROM ComplaintComments cm
        INNER JOIN Users u ON u.Id = cm.AuthorId
        WHERE cm.Id = @NewCommentId;

    END TRY
    BEGIN CATCH
        EXEC usp_LogError @ErrorContext = 'usp_Complaint_AddComment', @UserId = @AuthorId;
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_Complaint_AddComment';

-- =====================================================
-- PROCEDURE: Create Inspector
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Inspector_Create')
    DROP PROCEDURE usp_Inspector_Create;
GO

CREATE PROCEDURE usp_Inspector_Create
    @UserId UNIQUEIDENTIFIER,
    @Name NVARCHAR(255),
    @Email NVARCHAR(255),
    @Phone NVARCHAR(20),
    @Zone NVARCHAR(100) = NULL,
    @NewInspectorId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF EXISTS (SELECT 1 FROM Inspectors WHERE UserId = @UserId)
        BEGIN
            RAISERROR('Inspector profile already exists', 16, 1);
            RETURN;
        END

        SET @NewInspectorId = NEWID();
        DECLARE @InspectorCode NVARCHAR(10) = 'INS-' + RIGHT('0000' + CAST(NEXT VALUE FOR seq_InspectorCode AS NVARCHAR), 4);

        INSERT INTO Inspectors (
            Id, UserId, InspectorCode, Name, Email, Phone,
            Zone, ActiveCases, TotalCasesHandled,
            ResolutionRate, AverageResolutionTimeHours,
            IsAvailable, CreatedAt, UpdatedAt
        )
        VALUES (
            @NewInspectorId, @UserId, @InspectorCode, @Name, @Email, @Phone,
            @Zone, 0, 0,
            0, 0,
            1, GETUTCDATE(), GETUTCDATE()
        );

        SELECT * FROM Inspectors WHERE Id = @NewInspectorId;

    END TRY
    BEGIN CATCH
        EXEC usp_LogError @ErrorContext = 'usp_Inspector_Create', @UserId = @UserId;
        THROW;
    END CATCH
END
GO

PRINT 'Created usp_Inspector_Create';

-- =====================================================
-- PROCEDURE: Get Available Inspectors
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Inspector_GetAvailable')
    DROP PROCEDURE usp_Inspector_GetAvailable;
GO

CREATE PROCEDURE usp_Inspector_GetAvailable
    @Zone NVARCHAR(100) = NULL,
    @MaxActiveCases INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        i.Id, i.UserId, i.InspectorCode, i.Name, i.Email, i.Phone,
        i.Zone, i.ActiveCases, i.TotalCasesHandled,
        i.ResolutionRate, i.AverageResolutionTimeHours,
        i.IsAvailable
    FROM Inspectors i
    WHERE i.IsAvailable = 1
      AND i.ActiveCases < @MaxActiveCases
      AND (@Zone IS NULL OR i.Zone = @Zone OR i.Zone IS NULL)
    ORDER BY i.ActiveCases ASC, i.ResolutionRate DESC;
END
GO

PRINT 'Created usp_Inspector_GetAvailable';

-- =====================================================
-- PROCEDURE: Get Complaint Statistics (Dashboard)
-- =====================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'usp_Complaint_GetStatistics')
    DROP PROCEDURE usp_Complaint_GetStatistics;
GO

CREATE PROCEDURE usp_Complaint_GetStatistics
    @FromDate DATE = NULL,
    @ToDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromDate IS NULL SET @FromDate = DATEADD(DAY, -30, GETUTCDATE());
    IF @ToDate IS NULL SET @ToDate = GETUTCDATE();

    -- Overall stats
    SELECT
        COUNT(*) AS TotalComplaints,
        SUM(CASE WHEN Status = 'OPEN' THEN 1 ELSE 0 END) AS OpenCount,
        SUM(CASE WHEN Status = 'ASSIGNED' THEN 1 ELSE 0 END) AS AssignedCount,
        SUM(CASE WHEN Status = 'IN_PROGRESS' THEN 1 ELSE 0 END) AS InProgressCount,
        SUM(CASE WHEN Status = 'RESOLVED' THEN 1 ELSE 0 END) AS ResolvedCount,
        SUM(CASE WHEN Status = 'CLOSED' THEN 1 ELSE 0 END) AS ClosedCount,
        SUM(CASE WHEN Status = 'REJECTED' THEN 1 ELSE 0 END) AS RejectedCount
    FROM Complaints
    WHERE CreatedAt >= @FromDate AND CreatedAt < DATEADD(DAY, 1, @ToDate);

    -- By severity
    SELECT
        Severity,
        COUNT(*) AS Count,
        SUM(CASE WHEN Status NOT IN ('RESOLVED', 'CLOSED', 'REJECTED') THEN 1 ELSE 0 END) AS PendingCount
    FROM Complaints
    WHERE CreatedAt >= @FromDate AND CreatedAt < DATEADD(DAY, 1, @ToDate)
    GROUP BY Severity
    ORDER BY
        CASE Severity WHEN 'CRITICAL' THEN 1 WHEN 'HIGH' THEN 2 WHEN 'MEDIUM' THEN 3 ELSE 4 END;

    -- By category
    SELECT
        Category,
        COUNT(*) AS Count
    FROM Complaints
    WHERE CreatedAt >= @FromDate AND CreatedAt < DATEADD(DAY, 1, @ToDate)
    GROUP BY Category
    ORDER BY Count DESC;

    -- Resolution metrics
    SELECT
        AVG(DATEDIFF(HOUR, CreatedAt, ResolvedAt)) AS AvgResolutionTimeHours,
        MIN(DATEDIFF(HOUR, CreatedAt, ResolvedAt)) AS MinResolutionTimeHours,
        MAX(DATEDIFF(HOUR, CreatedAt, ResolvedAt)) AS MaxResolutionTimeHours
    FROM Complaints
    WHERE ResolvedAt IS NOT NULL
      AND CreatedAt >= @FromDate AND CreatedAt < DATEADD(DAY, 1, @ToDate);

    -- Daily trend
    SELECT
        CAST(CreatedAt AS DATE) AS Date,
        COUNT(*) AS NewComplaints,
        SUM(CASE WHEN Status IN ('RESOLVED', 'CLOSED') THEN 1 ELSE 0 END) AS ResolvedComplaints
    FROM Complaints
    WHERE CreatedAt >= @FromDate AND CreatedAt < DATEADD(DAY, 1, @ToDate)
    GROUP BY CAST(CreatedAt AS DATE)
    ORDER BY Date;
END
GO

PRINT 'Created usp_Complaint_GetStatistics';

PRINT 'Complaint module: COMPLETE';
