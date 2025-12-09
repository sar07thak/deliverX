-- =====================================================
-- DELIVERYDOST STORED PROCEDURES - CONVENTIONS
-- =====================================================
-- Version: 1.0
-- Date: 2025-12-08
-- =====================================================

/*
===============================================================================
1. NAMING CONVENTIONS
===============================================================================

1.1. Stored Procedures:
    - CRUD Operations: usp_{Entity}_{Action}
      Examples:
        usp_User_Insert
        usp_User_Update
        usp_User_GetById
        usp_User_List
        usp_User_Delete (soft delete)

    - Business Workflows: usp_{Module}_{UseCase}
      Examples:
        usp_Delivery_CreateAndMatch
        usp_Delivery_AssignPartner
        usp_Wallet_ApplyTransaction
        usp_Complaint_ResolveByAdmin

    - Reports/Analytics: usp_Report_{Name}
      Examples:
        usp_Report_DailyDeliveries
        usp_Report_PartnerEarnings

1.2. User-Defined Functions:
    - Scalar Functions: ufn_{Purpose}
      Examples:
        ufn_CalculateDeliveryPrice
        ufn_GetDistanceKm
        ufn_GenerateTrackingCode

    - Table-Valued Functions: ufn_{Entity}_{Purpose}
      Examples:
        ufn_DeliveryPartner_FindInRadius
        ufn_Delivery_GetCandidates

1.3. Parameter Naming:
    - Use PascalCase with @ prefix
    - Use meaningful, full names
    - Examples:
        @UserId, @DeliveryPartnerId, @FromDate, @ToDate
        @PageNumber, @PageSize, @SearchTerm
        @Latitude, @Longitude, @RadiusKm

===============================================================================
2. STANDARD PATTERNS
===============================================================================

2.1. All Procedures Start With:
    SET NOCOUNT ON;
    SET XACT_ABORT ON; -- For procedures with transactions

2.2. Error Handling:
    - Use TRY...CATCH for multi-statement operations
    - Log errors to ErrorLog table
    - Return structured error info

2.3. Pagination Pattern:
    - Input: @PageNumber INT = 1, @PageSize INT = 20
    - Use OFFSET/FETCH
    - Return TotalCount via second result set or OUTPUT param

2.4. Soft Delete:
    - Use IsActive = 0 or IsDeleted = 1
    - Never hard delete unless specifically required

2.5. Audit Fields:
    - Always set UpdatedAt = GETUTCDATE() on updates
    - CreatedAt set only on insert

===============================================================================
3. RETURN PATTERNS
===============================================================================

3.1. Insert Procedures:
    - Return new entity ID
    - Return full entity or key fields

3.2. Update Procedures:
    - Return affected row count
    - Optionally return updated entity

3.3. List Procedures:
    - Result Set 1: Paged data
    - Result Set 2: SELECT @TotalCount AS TotalCount

3.4. Error Returns:
    - Use OUTPUT parameters for success/error info
    - Or throw errors for application to catch

===============================================================================
4. MODULE BREAKDOWN
===============================================================================

MODULE: Users/Authentication
    - usp_User_Insert
    - usp_User_Update
    - usp_User_GetById
    - usp_User_GetByPhone
    - usp_User_GetByEmail
    - usp_User_List
    - usp_User_Deactivate
    - usp_User_UpdateLoginStatus
    - usp_User_RecordFailedLogin

MODULE: DeliveryPartner
    - usp_DeliveryPartner_Insert
    - usp_DeliveryPartner_Update
    - usp_DeliveryPartner_GetById
    - usp_DeliveryPartner_GetByUserId
    - usp_DeliveryPartner_List
    - usp_DeliveryPartner_SetOnlineStatus
    - usp_DeliveryPartner_UpdateLocation
    - usp_DeliveryPartner_UpdatePricing
    - usp_DeliveryPartner_UpdateServiceArea
    - usp_DeliveryPartner_GetAvailable (matching)

MODULE: DPCM (Channel Manager)
    - usp_DPCM_Insert
    - usp_DPCM_Update
    - usp_DPCM_GetById
    - usp_DPCM_List
    - usp_DPCM_GetDeliveryPartners
    - usp_DPCM_UpdateSecurityDeposit

MODULE: BusinessConsumer
    - usp_BusinessConsumer_Insert
    - usp_BusinessConsumer_Update
    - usp_BusinessConsumer_GetById
    - usp_BusinessConsumer_List
    - usp_BusinessConsumer_AddPickupLocation
    - usp_BusinessConsumer_GetPickupLocations

MODULE: Delivery
    - usp_Delivery_Create
    - usp_Delivery_Update
    - usp_Delivery_GetById
    - usp_Delivery_List
    - usp_Delivery_GetByRequester
    - usp_Delivery_GetByDP
    - usp_Delivery_MatchCandidates
    - usp_Delivery_AssignPartner
    - usp_Delivery_UpdateStatus
    - usp_Delivery_Cancel
    - usp_Delivery_RecordEvent
    - usp_Delivery_GetTimeline

MODULE: Wallet
    - usp_Wallet_Create
    - usp_Wallet_GetByUserId
    - usp_Wallet_ApplyTransaction
    - usp_Wallet_GetTransactions
    - usp_Wallet_HoldBalance
    - usp_Wallet_ReleaseHold

MODULE: Complaint
    - usp_Complaint_Create
    - usp_Complaint_Update
    - usp_Complaint_GetById
    - usp_Complaint_List
    - usp_Complaint_AssignToAdmin
    - usp_Complaint_Resolve
    - usp_Complaint_Close
    - usp_Complaint_AddComment
    - usp_Complaint_AddEvidence

MODULE: KYC
    - usp_KYC_CreateRequest
    - usp_KYC_UpdateStatus
    - usp_KYC_GetByUserId
    - usp_KYC_GetPending
    - usp_KYC_Approve
    - usp_KYC_Reject

MODULE: Subscription
    - usp_Subscription_GetPlans
    - usp_Subscription_Subscribe
    - usp_Subscription_Cancel
    - usp_Subscription_GetByUser

MODULE: Reports/Analytics
    - usp_Report_DashboardStats
    - usp_Report_DeliveryMetrics
    - usp_Report_PartnerPerformance
    - usp_Report_WalletSummary

===============================================================================
5. HELPER FUNCTIONS
===============================================================================

SCALAR FUNCTIONS:
    - ufn_CalculateDistance(@Lat1, @Lng1, @Lat2, @Lng2) -> FLOAT (km)
    - ufn_CalculateDeliveryPrice(@DistanceKm, @WeightKg, @PerKmRate, @PerKgRate, @MinCharge) -> DECIMAL
    - ufn_GenerateComplaintNumber() -> NVARCHAR(20)
    - ufn_GenerateDeliveryCode() -> NVARCHAR(20)
    - ufn_IsWithinRadius(@CenterLat, @CenterLng, @PointLat, @PointLng, @RadiusKm) -> BIT

TABLE-VALUED FUNCTIONS:
    - ufn_DeliveryPartner_FindInRadius(@Lat, @Lng, @RadiusKm) -> TABLE
    - ufn_DeliveryPartner_GetCandidatesForDelivery(@DeliveryId) -> TABLE
    - ufn_User_GetPermissions(@UserId) -> TABLE

*/

PRINT 'Conventions document loaded';
