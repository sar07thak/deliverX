-- =====================================================
-- DELIVERYDOST SEED DATA - STEP 6: COMPLAINTS
-- =====================================================
-- Run AFTER seed_05_transactions.sql
-- Creates: Complaints, ComplaintComments, ComplaintEvidences
-- =====================================================

USE DeliveryDost_Dev;
GO

SET NOCOUNT ON;
PRINT '====================================';
PRINT 'STEP 6: SEEDING COMPLAINTS';
PRINT '====================================';

-- =====================================================
-- 6.1 COMPLAINTS (50)
-- =====================================================
PRINT 'Creating Complaints...';

DECLARE @ComplaintCount INT = 0;
DECLARE @i INT = 1;
DECLARE @ComplaintId UNIQUEIDENTIFIER;
DECLARE @DeliveryId UNIQUEIDENTIFIER;
DECLARE @RaisedById UNIQUEIDENTIFIER;
DECLARE @RaisedByType NVARCHAR(10);
DECLARE @AgainstId UNIQUEIDENTIFIER;
DECLARE @Category NVARCHAR(20);
DECLARE @Severity NVARCHAR(10);
DECLARE @Status NVARCHAR(20);
DECLARE @InspectorId UNIQUEIDENTIFIER;

WHILE @i <= 50
BEGIN
    -- Get a random delivered or failed delivery
    SELECT TOP 1
        @DeliveryId = d.Id,
        @RaisedById = d.RequesterId,
        @RaisedByType = d.RequesterType,
        @AgainstId = d.AssignedDPId
    FROM Deliveries d
    WHERE d.Status IN ('DELIVERED', 'FAILED', 'CANCELLED')
      AND d.AssignedDPId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM Complaints c WHERE c.DeliveryId = d.Id AND c.RaisedById = d.RequesterId)
    ORDER BY NEWID();

    IF @DeliveryId IS NOT NULL
    BEGIN
        -- Determine complaint details
        SET @Category = CASE @i % 6
            WHEN 0 THEN 'DAMAGE' WHEN 1 THEN 'DELAY' WHEN 2 THEN 'BEHAVIOR'
            WHEN 3 THEN 'THEFT' WHEN 4 THEN 'FRAUD' ELSE 'OTHER'
        END;

        SET @Severity = CASE @i % 4
            WHEN 0 THEN 'LOW' WHEN 1 THEN 'MEDIUM' WHEN 2 THEN 'HIGH' ELSE 'CRITICAL'
        END;

        SET @Status = CASE
            WHEN @i <= 10 THEN 'OPEN'
            WHEN @i <= 15 THEN 'ASSIGNED'
            WHEN @i <= 20 THEN 'IN_PROGRESS'
            WHEN @i <= 40 THEN 'RESOLVED'
            WHEN @i <= 45 THEN 'CLOSED'
            ELSE 'REJECTED'
        END;

        -- Get inspector for assigned/in-progress complaints
        IF @Status IN ('ASSIGNED', 'IN_PROGRESS', 'RESOLVED', 'CLOSED')
        BEGIN
            SELECT TOP 1 @InspectorId = UserId FROM Inspectors WHERE IsAvailable = 1 ORDER BY NEWID();
        END
        ELSE
            SET @InspectorId = NULL;

        SET @ComplaintId = NEWID();

        INSERT INTO Complaints (
            Id, ComplaintNumber, DeliveryId, RaisedById, RaisedByType,
            AgainstId, AgainstType, Category, Severity, Subject, Description, Status,
            Resolution, ResolutionNotes, AssignedToId, AssignedAt, ResolvedAt, ClosedAt,
            CreatedAt, UpdatedAt
        )
        VALUES (
            @ComplaintId,
            'CMP-' + FORMAT(GETUTCDATE(), 'yyyyMMdd') + '-' + RIGHT('00000' + CAST(@i AS VARCHAR), 5),
            @DeliveryId,
            @RaisedById,
            @RaisedByType,
            @AgainstId,
            'DP',
            @Category,
            @Severity,
            CASE @Category
                WHEN 'DAMAGE' THEN 'Package damaged during delivery'
                WHEN 'DELAY' THEN 'Significant delay in delivery'
                WHEN 'BEHAVIOR' THEN 'Unprofessional behavior by delivery partner'
                WHEN 'THEFT' THEN 'Items missing from package'
                WHEN 'FRAUD' THEN 'Fraudulent activity reported'
                ELSE 'General complaint'
            END,
            CASE @Category
                WHEN 'DAMAGE' THEN 'The package was delivered in damaged condition. Contents were affected.'
                WHEN 'DELAY' THEN 'The delivery was delayed by more than 2 hours without any communication.'
                WHEN 'BEHAVIOR' THEN 'The delivery partner was rude and unprofessional during delivery.'
                WHEN 'THEFT' THEN 'Some items from the package appear to be missing after delivery.'
                WHEN 'FRAUD' THEN 'Suspected fraudulent activity during delivery process.'
                ELSE 'General issue with the delivery service that needs attention.'
            END,
            @Status,
            CASE WHEN @Status IN ('RESOLVED', 'CLOSED') THEN
                CASE @i % 4 WHEN 0 THEN 'REFUND' WHEN 1 THEN 'COMPENSATION' WHEN 2 THEN 'NO_ACTION' ELSE 'PENALTY' END
            ELSE NULL END,
            CASE WHEN @Status IN ('RESOLVED', 'CLOSED') THEN 'Investigation completed. Action taken as per policy.' ELSE NULL END,
            @InspectorId,
            CASE WHEN @InspectorId IS NOT NULL THEN DATEADD(HOUR, -CAST(RAND() * 48 + 1 AS INT), GETUTCDATE()) ELSE NULL END,
            CASE WHEN @Status IN ('RESOLVED', 'CLOSED') THEN DATEADD(HOUR, -CAST(RAND() * 24 AS INT), GETUTCDATE()) ELSE NULL END,
            CASE WHEN @Status = 'CLOSED' THEN DATEADD(HOUR, -CAST(RAND() * 12 AS INT), GETUTCDATE()) ELSE NULL END,
            DATEADD(DAY, -CAST(RAND() * 30 AS INT), GETUTCDATE()),
            GETUTCDATE()
        );

        SET @ComplaintCount = @ComplaintCount + 1;
    END

    SET @i = @i + 1;
END

PRINT '  -> Created ' + CAST(@ComplaintCount AS VARCHAR) + ' Complaints';
GO

-- =====================================================
-- 6.2 COMPLAINT COMMENTS
-- =====================================================
PRINT 'Creating ComplaintComments...';

DECLARE @CommentCount INT = 0;

-- Initial comment from complainant
INSERT INTO ComplaintComments (Id, ComplaintId, AuthorId, Content, IsInternal, CreatedAt)
SELECT
    NEWID(),
    c.Id,
    c.RaisedById,
    'I am reporting this issue because ' +
    CASE c.Category
        WHEN 'DAMAGE' THEN 'the package was visibly damaged when I received it.'
        WHEN 'DELAY' THEN 'the delivery took much longer than expected.'
        WHEN 'BEHAVIOR' THEN 'the delivery person was very unprofessional.'
        WHEN 'THEFT' THEN 'some items were missing from my order.'
        WHEN 'FRAUD' THEN 'there seems to be fraudulent activity.'
        ELSE 'I had a bad experience.'
    END,
    0,
    DATEADD(MINUTE, 5, c.CreatedAt)
FROM Complaints c
WHERE NOT EXISTS (SELECT 1 FROM ComplaintComments cc WHERE cc.ComplaintId = c.Id AND cc.AuthorId = c.RaisedById);

SET @CommentCount = @CommentCount + @@ROWCOUNT;

-- Internal notes from assigned inspectors
INSERT INTO ComplaintComments (Id, ComplaintId, AuthorId, Content, IsInternal, CreatedAt)
SELECT
    NEWID(),
    c.Id,
    c.AssignedToId,
    CASE ROW_NUMBER() OVER (ORDER BY c.Id) % 4
        WHEN 0 THEN 'Started investigation. Contacted delivery partner for statement.'
        WHEN 1 THEN 'Reviewed delivery photos and GPS logs. Evidence collected.'
        WHEN 2 THEN 'Spoke with both parties. Waiting for additional documentation.'
        ELSE 'Investigation ongoing. Will provide update within 24 hours.'
    END,
    1,
    DATEADD(HOUR, 2, c.AssignedAt)
FROM Complaints c
WHERE c.AssignedToId IS NOT NULL
  AND c.Status IN ('ASSIGNED', 'IN_PROGRESS', 'RESOLVED', 'CLOSED')
  AND NOT EXISTS (SELECT 1 FROM ComplaintComments cc WHERE cc.ComplaintId = c.Id AND cc.AuthorId = c.AssignedToId AND cc.IsInternal = 1);

SET @CommentCount = @CommentCount + @@ROWCOUNT;

-- Resolution comments
INSERT INTO ComplaintComments (Id, ComplaintId, AuthorId, Content, IsInternal, CreatedAt)
SELECT
    NEWID(),
    c.Id,
    c.AssignedToId,
    'Case resolved with resolution: ' + ISNULL(c.Resolution, 'N/A') + '. ' + ISNULL(c.ResolutionNotes, ''),
    0,
    c.ResolvedAt
FROM Complaints c
WHERE c.Status IN ('RESOLVED', 'CLOSED')
  AND c.ResolvedAt IS NOT NULL
  AND c.AssignedToId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM ComplaintComments cc WHERE cc.ComplaintId = c.Id AND cc.Content LIKE 'Case resolved%');

SET @CommentCount = @CommentCount + @@ROWCOUNT;

PRINT '  -> Created ' + CAST(@CommentCount AS VARCHAR) + ' ComplaintComments';
GO

-- =====================================================
-- 6.3 COMPLAINT EVIDENCES
-- =====================================================
PRINT 'Creating ComplaintEvidences...';

DECLARE @EvidenceCount INT = 0;

INSERT INTO ComplaintEvidences (Id, ComplaintId, Type, FileName, FileUrl, Description, UploadedById, UploadedAt)
SELECT
    NEWID(),
    c.Id,
    CASE ROW_NUMBER() OVER (ORDER BY c.Id) % 4 WHEN 0 THEN 'PHOTO' WHEN 1 THEN 'VIDEO' WHEN 2 THEN 'DOCUMENT' ELSE 'PHOTO' END,
    CASE ROW_NUMBER() OVER (ORDER BY c.Id) % 4
        WHEN 0 THEN 'damage_photo_' + CAST(ROW_NUMBER() OVER (ORDER BY c.Id) AS NVARCHAR) + '.jpg'
        WHEN 1 THEN 'incident_video_' + CAST(ROW_NUMBER() OVER (ORDER BY c.Id) AS NVARCHAR) + '.mp4'
        WHEN 2 THEN 'invoice_' + CAST(ROW_NUMBER() OVER (ORDER BY c.Id) AS NVARCHAR) + '.pdf'
        ELSE 'evidence_photo_' + CAST(ROW_NUMBER() OVER (ORDER BY c.Id) AS NVARCHAR) + '.jpg'
    END,
    '/evidence/complaints/' + CAST(c.Id AS NVARCHAR(50)) + '/' +
    CASE ROW_NUMBER() OVER (ORDER BY c.Id) % 4
        WHEN 0 THEN 'damage_photo.jpg'
        WHEN 1 THEN 'incident_video.mp4'
        WHEN 2 THEN 'invoice.pdf'
        ELSE 'evidence_photo.jpg'
    END,
    CASE ROW_NUMBER() OVER (ORDER BY c.Id) % 4
        WHEN 0 THEN 'Photo showing the damage to the package'
        WHEN 1 THEN 'Video recording of the incident'
        WHEN 2 THEN 'Invoice/receipt for the order'
        ELSE 'Additional evidence photo'
    END,
    c.RaisedById,
    DATEADD(MINUTE, 10, c.CreatedAt)
FROM Complaints c
WHERE c.Category IN ('DAMAGE', 'THEFT', 'FRAUD')
  AND NOT EXISTS (SELECT 1 FROM ComplaintEvidences ce WHERE ce.ComplaintId = c.Id);

SET @EvidenceCount = @@ROWCOUNT;

-- Add second evidence for some complaints
INSERT INTO ComplaintEvidences (Id, ComplaintId, Type, FileName, FileUrl, Description, UploadedById, UploadedAt)
SELECT
    NEWID(),
    c.Id,
    'PHOTO',
    'additional_evidence_' + CAST(ROW_NUMBER() OVER (ORDER BY c.Id) AS NVARCHAR) + '.jpg',
    '/evidence/complaints/' + CAST(c.Id AS NVARCHAR(50)) + '/additional.jpg',
    'Additional photo evidence',
    c.RaisedById,
    DATEADD(MINUTE, 15, c.CreatedAt)
FROM Complaints c
WHERE c.Severity IN ('HIGH', 'CRITICAL')
  AND (SELECT COUNT(*) FROM ComplaintEvidences ce WHERE ce.ComplaintId = c.Id) < 2;

SET @EvidenceCount = @EvidenceCount + @@ROWCOUNT;

PRINT '  -> Created ' + CAST(@EvidenceCount AS VARCHAR) + ' ComplaintEvidences';
GO

-- =====================================================
-- SUMMARY
-- =====================================================
PRINT '';
PRINT '====================================';
PRINT 'STEP 6 COMPLETE: Complaints Summary';
PRINT '====================================';

SELECT Status, COUNT(*) AS Count
FROM Complaints
GROUP BY Status
ORDER BY
    CASE Status WHEN 'OPEN' THEN 1 WHEN 'ASSIGNED' THEN 2 WHEN 'IN_PROGRESS' THEN 3
    WHEN 'RESOLVED' THEN 4 WHEN 'CLOSED' THEN 5 ELSE 6 END;

SELECT Category, Severity, COUNT(*) AS Count
FROM Complaints
GROUP BY Category, Severity
ORDER BY Category, Severity;

SELECT 'ComplaintComments' AS Entity, COUNT(*) AS Count FROM ComplaintComments
UNION ALL SELECT 'ComplaintEvidences', COUNT(*) FROM ComplaintEvidences;
GO
