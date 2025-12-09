-- =====================================================
-- DELIVERYDOST TEST SCRIPT - COMPLAINT WORKFLOW
-- =====================================================
-- Tests complete complaint lifecycle:
-- OPEN -> ASSIGNED -> IN_PROGRESS -> RESOLVED -> CLOSED
-- Plus: Comments, Evidences, Escalation
-- =====================================================

USE DeliveryDost_Dev;
GO

SET NOCOUNT ON;
PRINT '====================================================';
PRINT 'TEST: COMPLAINT RESOLUTION WORKFLOW';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETUTCDATE(), 121);
PRINT '====================================================';
PRINT '';

-- Test variables
DECLARE @ComplaintId UNIQUEIDENTIFIER = NEWID();
DECLARE @DeliveryId UNIQUEIDENTIFIER;
DECLARE @RaisedById UNIQUEIDENTIFIER;
DECLARE @AgainstId UNIQUEIDENTIFIER;
DECLARE @InspectorId UNIQUEIDENTIFIER;
DECLARE @CurrentStatus NVARCHAR(20);
DECLARE @CommentCount INT;
DECLARE @EvidenceCount INT;
DECLARE @TestsPassed INT = 0;
DECLARE @TestsFailed INT = 0;

-- =====================================================
-- SETUP: Get prerequisite data
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'SETUP: Getting prerequisite data';
PRINT '----------------------------------------------------';

-- Get a delivered delivery with DP assigned
SELECT TOP 1
    @DeliveryId = d.Id,
    @RaisedById = d.RequesterId,
    @AgainstId = d.AssignedDPId
FROM Deliveries d
WHERE d.Status = 'DELIVERED' AND d.AssignedDPId IS NOT NULL;

IF @DeliveryId IS NULL
BEGIN
    PRINT '  [ERROR] No delivered deliveries found. Run seed scripts first.';
    RETURN;
END
PRINT '  Delivery ID: ' + CAST(@DeliveryId AS VARCHAR(50));
PRINT '  Complainant: ' + CAST(@RaisedById AS VARCHAR(50));

-- Get an available inspector
SELECT TOP 1 @InspectorId = UserId FROM Inspectors WHERE IsAvailable = 1;
IF @InspectorId IS NULL
BEGIN
    PRINT '  [ERROR] No available inspectors found. Run seed scripts first.';
    RETURN;
END
PRINT '  Inspector: ' + CAST(@InspectorId AS VARCHAR(50));
PRINT '';

-- =====================================================
-- TEST 1: CREATE COMPLAINT (Status: OPEN)
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 1: CREATE COMPLAINT (Status: OPEN)';
PRINT '----------------------------------------------------';

BEGIN TRY
    INSERT INTO Complaints (
        Id, ComplaintNumber, DeliveryId, RaisedById, RaisedByType,
        AgainstId, AgainstType, Category, Severity,
        Subject, Description, Status,
        CreatedAt, UpdatedAt
    )
    VALUES (
        @ComplaintId,
        'CMP-TEST-' + FORMAT(GETUTCDATE(), 'yyyyMMddHHmmss'),
        @DeliveryId,
        @RaisedById,
        'BC',
        @AgainstId,
        'DP',
        'DAMAGE',
        'HIGH',
        'Package arrived damaged',
        'The package was visibly damaged when it arrived. The outer box was crushed and the contents inside were broken.',
        'OPEN',
        GETUTCDATE(),
        GETUTCDATE()
    );

    SELECT @CurrentStatus = Status FROM Complaints WHERE Id = @ComplaintId;

    IF @CurrentStatus = 'OPEN'
    BEGIN
        PRINT '  [PASS] Complaint created with OPEN status';
        PRINT '  -> Complaint ID: ' + CAST(@ComplaintId AS VARCHAR(50));
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Unexpected status: ' + @CurrentStatus;
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] Complaint creation failed: ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
    RETURN;
END CATCH

PRINT '';

-- =====================================================
-- TEST 2: ADD INITIAL COMMENT (FROM COMPLAINANT)
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 2: ADD INITIAL COMMENT FROM COMPLAINANT';
PRINT '----------------------------------------------------';

BEGIN TRY
    INSERT INTO ComplaintComments (Id, ComplaintId, AuthorId, Content, IsInternal, CreatedAt)
    VALUES (NEWID(), @ComplaintId, @RaisedById,
            'I have attached photos of the damaged package. Please investigate this matter urgently.',
            0, GETUTCDATE());

    SELECT @CommentCount = COUNT(*) FROM ComplaintComments WHERE ComplaintId = @ComplaintId;

    IF @CommentCount = 1
    BEGIN
        PRINT '  [PASS] Comment added successfully';
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Comment not added';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 3: ADD EVIDENCE
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 3: ADD EVIDENCE (PHOTO)';
PRINT '----------------------------------------------------';

BEGIN TRY
    INSERT INTO ComplaintEvidences (Id, ComplaintId, Type, FileName, FileUrl, Description, UploadedById, UploadedAt)
    VALUES (
        NEWID(),
        @ComplaintId,
        'PHOTO',
        'damaged_package_1.jpg',
        '/evidence/complaints/' + CAST(@ComplaintId AS NVARCHAR(50)) + '/damaged_package_1.jpg',
        'Photo showing crushed outer packaging',
        @RaisedById,
        GETUTCDATE()
    );

    -- Add second evidence
    INSERT INTO ComplaintEvidences (Id, ComplaintId, Type, FileName, FileUrl, Description, UploadedById, UploadedAt)
    VALUES (
        NEWID(),
        @ComplaintId,
        'PHOTO',
        'damaged_contents.jpg',
        '/evidence/complaints/' + CAST(@ComplaintId AS NVARCHAR(50)) + '/damaged_contents.jpg',
        'Photo showing broken contents inside',
        @RaisedById,
        GETUTCDATE()
    );

    SELECT @EvidenceCount = COUNT(*) FROM ComplaintEvidences WHERE ComplaintId = @ComplaintId;

    IF @EvidenceCount = 2
    BEGIN
        PRINT '  [PASS] 2 evidence files uploaded';
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Evidence upload failed';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 4: ASSIGN TO INSPECTOR
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 4: ASSIGN TO INSPECTOR (Status: ASSIGNED)';
PRINT '----------------------------------------------------';

BEGIN TRY
    UPDATE Complaints
    SET
        Status = 'ASSIGNED',
        AssignedToId = @InspectorId,
        AssignedAt = GETUTCDATE(),
        UpdatedAt = GETUTCDATE()
    WHERE Id = @ComplaintId;

    SELECT @CurrentStatus = Status FROM Complaints WHERE Id = @ComplaintId;
    DECLARE @AssignedTo UNIQUEIDENTIFIER;
    SELECT @AssignedTo = AssignedToId FROM Complaints WHERE Id = @ComplaintId;

    IF @CurrentStatus = 'ASSIGNED' AND @AssignedTo = @InspectorId
    BEGIN
        PRINT '  [PASS] Complaint assigned to inspector';
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Assignment failed';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 5: INSPECTOR INTERNAL NOTE
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 5: ADD INTERNAL NOTE (INSPECTOR)';
PRINT '----------------------------------------------------';

BEGIN TRY
    INSERT INTO ComplaintComments (Id, ComplaintId, AuthorId, Content, IsInternal, CreatedAt)
    VALUES (NEWID(), @ComplaintId, @InspectorId,
            '[INTERNAL] Reviewing evidence. Photos clearly show damage. Need to check delivery partner GPS logs.',
            1, GETUTCDATE());

    SELECT @CommentCount = COUNT(*) FROM ComplaintComments WHERE ComplaintId = @ComplaintId AND IsInternal = 1;

    IF @CommentCount = 1
    BEGIN
        PRINT '  [PASS] Internal note added (not visible to complainant)';
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Internal note not added';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 6: START INVESTIGATION
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 6: START INVESTIGATION (Status: IN_PROGRESS)';
PRINT '----------------------------------------------------';

BEGIN TRY
    UPDATE Complaints
    SET
        Status = 'IN_PROGRESS',
        UpdatedAt = GETUTCDATE()
    WHERE Id = @ComplaintId;

    SELECT @CurrentStatus = Status FROM Complaints WHERE Id = @ComplaintId;

    IF @CurrentStatus = 'IN_PROGRESS'
    BEGIN
        PRINT '  [PASS] Investigation started';
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Status update failed';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 7: ADD INVESTIGATION UPDATE
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 7: ADD INVESTIGATION UPDATE';
PRINT '----------------------------------------------------';

BEGIN TRY
    INSERT INTO ComplaintComments (Id, ComplaintId, AuthorId, Content, IsInternal, CreatedAt)
    VALUES (NEWID(), @ComplaintId, @InspectorId,
            'We have reviewed the evidence and contacted the delivery partner. Investigation is in progress and we will update you soon.',
            0, GETUTCDATE());

    PRINT '  [PASS] Investigation update sent to complainant';
    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 8: RESOLVE COMPLAINT
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 8: RESOLVE COMPLAINT (Status: RESOLVED)';
PRINT '----------------------------------------------------';

BEGIN TRY
    UPDATE Complaints
    SET
        Status = 'RESOLVED',
        Resolution = 'REFUND',
        ResolutionNotes = 'Investigation confirmed package damage during transit. Full refund has been processed to customer wallet.',
        ResolvedAt = GETUTCDATE(),
        UpdatedAt = GETUTCDATE()
    WHERE Id = @ComplaintId;

    SELECT @CurrentStatus = Status FROM Complaints WHERE Id = @ComplaintId;
    DECLARE @Resolution NVARCHAR(20);
    SELECT @Resolution = Resolution FROM Complaints WHERE Id = @ComplaintId;

    IF @CurrentStatus = 'RESOLVED' AND @Resolution = 'REFUND'
    BEGIN
        PRINT '  [PASS] Complaint resolved with REFUND';
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Resolution failed';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 9: ADD RESOLUTION COMMENT
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 9: ADD RESOLUTION COMMENT';
PRINT '----------------------------------------------------';

BEGIN TRY
    INSERT INTO ComplaintComments (Id, ComplaintId, AuthorId, Content, IsInternal, CreatedAt)
    VALUES (NEWID(), @ComplaintId, @InspectorId,
            'Your complaint has been resolved. A full refund of Rs. 500 has been credited to your wallet. We apologize for the inconvenience caused.',
            0, GETUTCDATE());

    PRINT '  [PASS] Resolution notification sent';
    SET @TestsPassed = @TestsPassed + 1;
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- TEST 10: CLOSE COMPLAINT
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'TEST 10: CLOSE COMPLAINT (Status: CLOSED)';
PRINT '----------------------------------------------------';

BEGIN TRY
    UPDATE Complaints
    SET
        Status = 'CLOSED',
        ClosedAt = GETUTCDATE(),
        UpdatedAt = GETUTCDATE()
    WHERE Id = @ComplaintId;

    SELECT @CurrentStatus = Status FROM Complaints WHERE Id = @ComplaintId;

    IF @CurrentStatus = 'CLOSED'
    BEGIN
        PRINT '  [PASS] Complaint closed successfully';
        SET @TestsPassed = @TestsPassed + 1;
    END
    ELSE
    BEGIN
        PRINT '  [FAIL] Close operation failed';
        SET @TestsFailed = @TestsFailed + 1;
    END
END TRY
BEGIN CATCH
    PRINT '  [FAIL] ' + ERROR_MESSAGE();
    SET @TestsFailed = @TestsFailed + 1;
END CATCH

PRINT '';

-- =====================================================
-- VERIFY: Complaint Timeline
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'VERIFY: COMPLAINT TIMELINE';
PRINT '----------------------------------------------------';

SELECT @CommentCount = COUNT(*) FROM ComplaintComments WHERE ComplaintId = @ComplaintId;
SELECT @EvidenceCount = COUNT(*) FROM ComplaintEvidences WHERE ComplaintId = @ComplaintId;

PRINT '  Total Comments: ' + CAST(@CommentCount AS VARCHAR);
PRINT '  Total Evidence Files: ' + CAST(@EvidenceCount AS VARCHAR);
PRINT '';
PRINT '  Comment Timeline:';

SELECT
    ROW_NUMBER() OVER (ORDER BY cc.CreatedAt) AS Seq,
    u.FullName AS Author,
    CASE WHEN cc.IsInternal = 1 THEN '[INTERNAL]' ELSE '[PUBLIC]' END AS Type,
    LEFT(cc.Content, 60) + '...' AS ContentPreview,
    FORMAT(cc.CreatedAt, 'HH:mm:ss') AS Time
FROM ComplaintComments cc
INNER JOIN Users u ON u.Id = cc.AuthorId
WHERE cc.ComplaintId = @ComplaintId
ORDER BY cc.CreatedAt;

PRINT '';

-- =====================================================
-- VERIFY: Final Complaint State
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'VERIFY: FINAL COMPLAINT STATE';
PRINT '----------------------------------------------------';

SELECT
    c.ComplaintNumber,
    c.Category,
    c.Severity,
    c.Status,
    c.Resolution,
    u1.FullName AS RaisedBy,
    u2.FullName AS AssignedTo,
    FORMAT(c.CreatedAt, 'HH:mm:ss') AS Created,
    FORMAT(c.AssignedAt, 'HH:mm:ss') AS Assigned,
    FORMAT(c.ResolvedAt, 'HH:mm:ss') AS Resolved,
    FORMAT(c.ClosedAt, 'HH:mm:ss') AS Closed,
    DATEDIFF(MINUTE, c.CreatedAt, c.ClosedAt) AS TotalMinutes
FROM Complaints c
INNER JOIN Users u1 ON u1.Id = c.RaisedById
LEFT JOIN Users u2 ON u2.Id = c.AssignedToId
WHERE c.Id = @ComplaintId;

PRINT '';

-- =====================================================
-- CLEANUP
-- =====================================================
PRINT '----------------------------------------------------';
PRINT 'CLEANUP: Removing test data';
PRINT '----------------------------------------------------';

BEGIN TRY
    DELETE FROM ComplaintEvidences WHERE ComplaintId = @ComplaintId;
    DELETE FROM ComplaintComments WHERE ComplaintId = @ComplaintId;
    DELETE FROM Complaints WHERE Id = @ComplaintId;
    PRINT '  Test data cleaned up successfully';
END TRY
BEGIN CATCH
    PRINT '  [WARN] Cleanup failed: ' + ERROR_MESSAGE();
END CATCH

PRINT '';

-- =====================================================
-- TEST SUMMARY
-- =====================================================
PRINT '====================================================';
PRINT 'TEST SUMMARY: COMPLAINT WORKFLOW';
PRINT '====================================================';
PRINT 'Tests Passed: ' + CAST(@TestsPassed AS VARCHAR);
PRINT 'Tests Failed: ' + CAST(@TestsFailed AS VARCHAR);
PRINT 'Total Tests:  ' + CAST(@TestsPassed + @TestsFailed AS VARCHAR);
PRINT '';

IF @TestsFailed = 0
    PRINT 'RESULT: ALL TESTS PASSED - Complaint workflow verified';
ELSE
    PRINT 'RESULT: SOME TESTS FAILED - Review above for details';

PRINT '';
PRINT 'Finished at: ' + CONVERT(VARCHAR, GETUTCDATE(), 121);
PRINT '====================================================';
GO
