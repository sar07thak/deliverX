-- =============================================
-- DeliverX Database Schema for SQL Server
-- Generated for SSMS compatibility
-- =============================================

-- Drop existing tables (in reverse dependency order)
-- Uncomment if you want to recreate
/*
DROP TABLE IF EXISTS WalletTransactions;
DROP TABLE IF EXISTS Wallets;
DROP TABLE IF EXISTS ProofOfDeliveries;
DROP TABLE IF EXISTS DeliveryEvents;
DROP TABLE IF EXISTS DeliveryMatchingHistories;
DROP TABLE IF EXISTS DeliveryPricings;
DROP TABLE IF EXISTS Deliveries;
DROP TABLE IF EXISTS DPAvailabilities;
DROP TABLE IF EXISTS Ratings;
DROP TABLE IF EXISTS ComplaintEvidences;
DROP TABLE IF EXISTS ComplaintComments;
DROP TABLE IF EXISTS Complaints;
DROP TABLE IF EXISTS SubscriptionInvoices;
DROP TABLE IF EXISTS UserSubscriptions;
DROP TABLE IF EXISTS SubscriptionPlans;
DROP TABLE IF EXISTS PromoCodeUsages;
DROP TABLE IF EXISTS PromoCodes;
DROP TABLE IF EXISTS Referrals;
DROP TABLE IF EXISTS ReferralCodes;
DROP TABLE IF EXISTS Donations;
DROP TABLE IF EXISTS DonationPreferences;
DROP TABLE IF EXISTS Charities;
DROP TABLE IF EXISTS SettlementItems;
DROP TABLE IF EXISTS Settlements;
DROP TABLE IF EXISTS CommissionRecords;
DROP TABLE IF EXISTS Payments;
DROP TABLE IF EXISTS DPPricingConfigs;
DROP TABLE IF EXISTS DPCMCommissionConfigs;
DROP TABLE IF EXISTS PlatformFeeConfigs;
DROP TABLE IF EXISTS ServiceAreas;
DROP TABLE IF EXISTS BusinessConsumerProfiles;
DROP TABLE IF EXISTS DeliveryPartnerProfiles;
DROP TABLE IF EXISTS DPCManagers;
DROP TABLE IF EXISTS VehicleLicenseVerifications;
DROP TABLE IF EXISTS PoliceVerifications;
DROP TABLE IF EXISTS BankVerifications;
DROP TABLE IF EXISTS PANVerifications;
DROP TABLE IF EXISTS AadhaarVerifications;
DROP TABLE IF EXISTS KYCRequests;
DROP TABLE IF EXISTS AdminAuditLogs;
DROP TABLE IF EXISTS BehaviorIndexes;
DROP TABLE IF EXISTS ComplaintSLAConfigs;
DROP TABLE IF EXISTS SystemConfigs;
DROP TABLE IF EXISTS Inspectors;
DROP TABLE IF EXISTS AuthAuditLogs;
DROP TABLE IF EXISTS UserSessions;
DROP TABLE IF EXISTS RolePermissions;
DROP TABLE IF EXISTS Permissions;
DROP TABLE IF EXISTS OTPVerifications;
DROP TABLE IF EXISTS Users;
*/

-- =============================================
-- CORE TABLES
-- =============================================

-- Users Table
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Phone NVARCHAR(15) NULL,
    Email NVARCHAR(255) NULL,
    PasswordHash NVARCHAR(255) NULL,
    Role NVARCHAR(20) NOT NULL,
    Is2FAEnabled BIT NOT NULL DEFAULT 0,
    TotpSecret NVARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    IsEmailVerified BIT NOT NULL DEFAULT 0,
    IsPhoneVerified BIT NOT NULL DEFAULT 0,
    LastLoginAt DATETIME2 NULL,
    PasswordChangedAt DATETIME2 NULL,
    FailedLoginAttempts INT NOT NULL DEFAULT 0,
    LockedUntil DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT CK_Users_ContactMethod CHECK (Phone IS NOT NULL OR Email IS NOT NULL)
);

CREATE UNIQUE INDEX IX_Users_Phone ON Users(Phone) WHERE Phone IS NOT NULL;
CREATE UNIQUE INDEX IX_Users_Email ON Users(Email) WHERE Email IS NOT NULL;
CREATE INDEX IX_Users_Role ON Users(Role);

-- OTP Verifications
CREATE TABLE OTPVerifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Phone NVARCHAR(15) NOT NULL,
    OTPHash NVARCHAR(255) NOT NULL,
    Attempts INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2 NOT NULL,
    IsVerified BIT NOT NULL DEFAULT 0
);

CREATE INDEX IX_OTPVerifications_Phone_ExpiresAt ON OTPVerifications(Phone, ExpiresAt);

-- Permissions
CREATE TABLE Permissions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code NVARCHAR(100) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500) NULL,
    Category NVARCHAR(100) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE UNIQUE INDEX IX_Permissions_Code ON Permissions(Code);

-- Role Permissions
CREATE TABLE RolePermissions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Role NVARCHAR(20) NOT NULL,
    PermissionId UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY (PermissionId) REFERENCES Permissions(Id) ON DELETE CASCADE
);

CREATE INDEX IX_RolePermissions_Role ON RolePermissions(Role);
CREATE INDEX IX_RolePermissions_PermissionId ON RolePermissions(PermissionId);
CREATE UNIQUE INDEX IX_RolePermissions_Role_PermissionId ON RolePermissions(Role, PermissionId);

-- User Sessions
CREATE TABLE UserSessions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    RefreshTokenHash NVARCHAR(255) NOT NULL,
    DeviceType NVARCHAR(50) NULL,
    DeviceId NVARCHAR(100) NULL,
    IpAddress NVARCHAR(50) NULL,
    UserAgent NVARCHAR(500) NULL,
    Location NVARCHAR(200) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastActiveAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2 NOT NULL,
    IsRevoked BIT NOT NULL DEFAULT 0,
    RevokedAt DATETIME2 NULL,
    CONSTRAINT FK_UserSessions_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_UserSessions_UserId ON UserSessions(UserId);
CREATE INDEX IX_UserSessions_RefreshTokenHash ON UserSessions(RefreshTokenHash);
CREATE INDEX IX_UserSessions_ExpiresAt ON UserSessions(ExpiresAt);

-- Auth Audit Logs
CREATE TABLE AuthAuditLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NULL,
    EventType NVARCHAR(50) NOT NULL,
    Phone NVARCHAR(15) NULL,
    Email NVARCHAR(255) NULL,
    IpAddress NVARCHAR(50) NULL,
    UserAgent NVARCHAR(500) NULL,
    Details NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_AuthAuditLogs_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
);

CREATE INDEX IX_AuthAuditLogs_UserId ON AuthAuditLogs(UserId);
CREATE INDEX IX_AuthAuditLogs_EventType ON AuthAuditLogs(EventType);
CREATE INDEX IX_AuthAuditLogs_CreatedAt ON AuthAuditLogs(CreatedAt);

-- =============================================
-- KYC TABLES
-- =============================================

-- KYC Requests
CREATE TABLE KYCRequests (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    VerificationType NVARCHAR(50) NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'PENDING',
    Method NVARCHAR(50) NULL,
    RequestData NVARCHAR(MAX) NULL,
    ResponseData NVARCHAR(MAX) NULL,
    DocumentUrls NVARCHAR(MAX) NULL,
    RejectionReason NVARCHAR(500) NULL,
    VerifiedBy UNIQUEIDENTIFIER NULL,
    InitiatedAt DATETIME2 NULL,
    CompletedAt DATETIME2 NULL,
    ExpiresAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_KYCRequests_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_KYCRequests_UserId ON KYCRequests(UserId);
CREATE INDEX IX_KYCRequests_Status ON KYCRequests(Status);

-- Aadhaar Verifications
CREATE TABLE AadhaarVerifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    AadhaarHash NVARCHAR(255) NOT NULL,
    AadhaarReferenceId NVARCHAR(100) NULL,
    NameAsPerAadhaar NVARCHAR(200) NOT NULL,
    DOB DATE NOT NULL,
    Gender NVARCHAR(10) NULL,
    Address NVARCHAR(500) NULL,
    PhotoUrl NVARCHAR(500) NULL,
    IsVerified BIT NOT NULL DEFAULT 0,
    VerifiedAt DATETIME2 NULL,
    VerificationMethod NVARCHAR(50) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_AadhaarVerifications_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_AadhaarVerifications_UserId ON AadhaarVerifications(UserId);

-- PAN Verifications
CREATE TABLE PANVerifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    PANHash NVARCHAR(255) NOT NULL,
    NameAsPerPAN NVARCHAR(200) NOT NULL,
    PANType NVARCHAR(20) NULL,
    IsVerified BIT NOT NULL DEFAULT 0,
    VerifiedAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_PANVerifications_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_PANVerifications_UserId ON PANVerifications(UserId);

-- Bank Verifications
CREATE TABLE BankVerifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    AccountNumberHash NVARCHAR(255) NOT NULL,
    IFSCCode NVARCHAR(20) NOT NULL,
    BankName NVARCHAR(200) NOT NULL,
    BranchName NVARCHAR(200) NULL,
    AccountHolderName NVARCHAR(200) NOT NULL,
    AccountType NVARCHAR(50) NULL,
    IsVerified BIT NOT NULL DEFAULT 0,
    VerifiedAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_BankVerifications_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_BankVerifications_UserId ON BankVerifications(UserId);

-- Vehicle License Verifications
CREATE TABLE VehicleLicenseVerifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    LicenseNumber NVARCHAR(50) NOT NULL,
    LicenseType NVARCHAR(50) NOT NULL,
    IssuingAuthority NVARCHAR(100) NULL,
    ValidFrom DATE NOT NULL,
    ValidUntil DATE NOT NULL,
    VehicleCategories NVARCHAR(200) NULL,
    IsVerified BIT NOT NULL DEFAULT 0,
    VerifiedAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_VehicleLicenseVerifications_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_VehicleLicenseVerifications_UserId ON VehicleLicenseVerifications(UserId);

-- Police Verifications
CREATE TABLE PoliceVerifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    VerificationStatus NVARCHAR(50) NOT NULL DEFAULT 'PENDING',
    PoliceStation NVARCHAR(200) NULL,
    CertificateNumber NVARCHAR(100) NULL,
    CertificateUrl NVARCHAR(500) NULL,
    ValidUntil DATE NULL,
    VerifiedAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_PoliceVerifications_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_PoliceVerifications_UserId ON PoliceVerifications(UserId);

-- =============================================
-- PROFILE TABLES
-- =============================================

-- DPCM Managers
CREATE TABLE DPCManagers (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    FullName NVARCHAR(200) NOT NULL,
    Email NVARCHAR(255) NULL,
    CommissionPercentage DECIMAL(5,2) NOT NULL DEFAULT 5.00,
    TotalEarnings DECIMAL(18,2) NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    ActivatedAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_DPCManagers_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_DPCManagers_UserId ON DPCManagers(UserId);

-- Delivery Partner Profiles
CREATE TABLE DeliveryPartnerProfiles (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    DPCMId UNIQUEIDENTIFIER NULL,
    FullName NVARCHAR(200) NOT NULL,
    DOB DATE NOT NULL,
    Gender NVARCHAR(10) NULL,
    Address NVARCHAR(500) NULL,
    ProfilePhotoUrl NVARCHAR(500) NULL,
    VehicleType NVARCHAR(50) NULL,
    Languages NVARCHAR(500) NULL,
    Availability NVARCHAR(50) NULL,
    ServiceAreaCenterLat DECIMAL(10,7) NULL,
    ServiceAreaCenterLng DECIMAL(10,7) NULL,
    ServiceAreaRadiusKm DECIMAL(5,2) NULL,
    PerKmRate DECIMAL(10,2) NULL,
    PerKgRate DECIMAL(10,2) NULL,
    MinCharge DECIMAL(10,2) NULL,
    MaxDistanceKm DECIMAL(5,2) NULL,
    IsActive BIT NOT NULL DEFAULT 0,
    ActivatedAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_DeliveryPartnerProfiles_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_DeliveryPartnerProfiles_DPCManagers FOREIGN KEY (DPCMId) REFERENCES DPCManagers(Id) ON DELETE SET NULL
);

CREATE UNIQUE INDEX IX_DeliveryPartnerProfiles_UserId ON DeliveryPartnerProfiles(UserId);
CREATE INDEX IX_DeliveryPartnerProfiles_DPCMId ON DeliveryPartnerProfiles(DPCMId);
CREATE INDEX IX_DeliveryPartnerProfiles_IsActive ON DeliveryPartnerProfiles(IsActive);

-- Business Consumer Profiles
CREATE TABLE BusinessConsumerProfiles (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    BusinessName NVARCHAR(200) NOT NULL,
    GSTIN NVARCHAR(20) NULL,
    BusinessType NVARCHAR(100) NULL,
    Address NVARCHAR(500) NULL,
    ContactPerson NVARCHAR(200) NULL,
    IsVerified BIT NOT NULL DEFAULT 0,
    VerifiedAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_BusinessConsumerProfiles_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_BusinessConsumerProfiles_UserId ON BusinessConsumerProfiles(UserId);

-- =============================================
-- SERVICE & PRICING TABLES
-- =============================================

-- Service Areas
CREATE TABLE ServiceAreas (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    CenterLat DECIMAL(10,7) NOT NULL,
    CenterLng DECIMAL(10,7) NOT NULL,
    RadiusKm DECIMAL(5,2) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Platform Fee Configs
CREATE TABLE PlatformFeeConfigs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FeeType NVARCHAR(50) NOT NULL,
    FeePercentage DECIMAL(5,2) NULL,
    FeeFixedAmount DECIMAL(10,2) NULL,
    MinFee DECIMAL(10,2) NULL,
    MaxFee DECIMAL(10,2) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    EffectiveFrom DATETIME2 NOT NULL,
    EffectiveTo DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- DPCM Commission Configs
CREATE TABLE DPCMCommissionConfigs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DPCMId UNIQUEIDENTIFIER NOT NULL,
    CommissionType NVARCHAR(50) NOT NULL DEFAULT 'PERCENTAGE',
    CommissionValue DECIMAL(10,2) NOT NULL,
    MinCommission DECIMAL(10,2) NULL,
    MaxCommission DECIMAL(10,2) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    EffectiveFrom DATETIME2 NOT NULL,
    EffectiveTo DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_DPCMCommissionConfigs_DPCManagers FOREIGN KEY (DPCMId) REFERENCES DPCManagers(Id) ON DELETE CASCADE
);

CREATE INDEX IX_DPCMCommissionConfigs_DPCMId ON DPCMCommissionConfigs(DPCMId);

-- DP Pricing Configs
CREATE TABLE DPPricingConfigs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DPId UNIQUEIDENTIFIER NOT NULL,
    PerKmRate DECIMAL(10,2) NOT NULL,
    PerKgRate DECIMAL(10,2) NOT NULL,
    MinCharge DECIMAL(10,2) NOT NULL,
    MaxDistanceKm DECIMAL(5,2) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    EffectiveFrom DATETIME2 NOT NULL,
    EffectiveTo DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_DPPricingConfigs_Users FOREIGN KEY (DPId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_DPPricingConfigs_DPId ON DPPricingConfigs(DPId);

-- =============================================
-- DELIVERY TABLES
-- =============================================

-- Deliveries
CREATE TABLE Deliveries (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    RequesterId UNIQUEIDENTIFIER NOT NULL,
    RequesterType NVARCHAR(10) NOT NULL,
    AssignedDPId UNIQUEIDENTIFIER NULL,
    AssignedAt DATETIME2 NULL,
    PickupLat DECIMAL(10,7) NOT NULL,
    PickupLng DECIMAL(10,7) NOT NULL,
    PickupAddress NVARCHAR(500) NOT NULL,
    PickupContactName NVARCHAR(200) NULL,
    PickupContactPhone NVARCHAR(15) NULL,
    PickupInstructions NVARCHAR(500) NULL,
    DropLat DECIMAL(10,7) NOT NULL,
    DropLng DECIMAL(10,7) NOT NULL,
    DropAddress NVARCHAR(500) NOT NULL,
    DropContactName NVARCHAR(200) NULL,
    DropContactPhone NVARCHAR(15) NULL,
    DropInstructions NVARCHAR(500) NULL,
    WeightKg DECIMAL(10,2) NOT NULL,
    PackageType NVARCHAR(50) NOT NULL,
    PackageDimensions NVARCHAR(100) NULL,
    PackageValue DECIMAL(18,2) NULL,
    PackageDescription NVARCHAR(500) NULL,
    Priority NVARCHAR(20) NOT NULL DEFAULT 'ASAP',
    ScheduledAt DATETIME2 NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'CREATED',
    EstimatedPrice DECIMAL(18,2) NULL,
    FinalPrice DECIMAL(18,2) NULL,
    SpecialInstructions NVARCHAR(500) NULL,
    PreferredDPId UNIQUEIDENTIFIER NULL,
    DistanceKm DECIMAL(10,2) NULL,
    EstimatedDurationMinutes INT NULL,
    MatchingAttempts INT NOT NULL DEFAULT 0,
    CancelledAt DATETIME2 NULL,
    CancellationReason NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Deliveries_Requester FOREIGN KEY (RequesterId) REFERENCES Users(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Deliveries_AssignedDP FOREIGN KEY (AssignedDPId) REFERENCES Users(Id) ON DELETE SET NULL
);

CREATE INDEX IX_Deliveries_RequesterId ON Deliveries(RequesterId);
CREATE INDEX IX_Deliveries_AssignedDPId ON Deliveries(AssignedDPId);
CREATE INDEX IX_Deliveries_Status ON Deliveries(Status);
CREATE INDEX IX_Deliveries_CreatedAt ON Deliveries(CreatedAt);
CREATE INDEX IX_Deliveries_Priority_Status ON Deliveries(Priority, Status);

-- Delivery Events
CREATE TABLE DeliveryEvents (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeliveryId UNIQUEIDENTIFIER NOT NULL,
    EventType NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500) NULL,
    Latitude DECIMAL(10,7) NULL,
    Longitude DECIMAL(10,7) NULL,
    PerformedBy UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_DeliveryEvents_Deliveries FOREIGN KEY (DeliveryId) REFERENCES Deliveries(Id) ON DELETE CASCADE
);

CREATE INDEX IX_DeliveryEvents_DeliveryId ON DeliveryEvents(DeliveryId);
CREATE INDEX IX_DeliveryEvents_EventType ON DeliveryEvents(EventType);

-- Delivery Matching History
CREATE TABLE DeliveryMatchingHistories (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeliveryId UNIQUEIDENTIFIER NOT NULL,
    DPId UNIQUEIDENTIFIER NOT NULL,
    Status NVARCHAR(20) NOT NULL,
    ResponseTime INT NULL,
    RejectionReason NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_DeliveryMatchingHistories_Deliveries FOREIGN KEY (DeliveryId) REFERENCES Deliveries(Id) ON DELETE CASCADE,
    CONSTRAINT FK_DeliveryMatchingHistories_Users FOREIGN KEY (DPId) REFERENCES Users(Id) ON DELETE NO ACTION
);

CREATE INDEX IX_DeliveryMatchingHistories_DeliveryId ON DeliveryMatchingHistories(DeliveryId);
CREATE INDEX IX_DeliveryMatchingHistories_DPId ON DeliveryMatchingHistories(DPId);

-- Delivery Pricing
CREATE TABLE DeliveryPricings (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeliveryId UNIQUEIDENTIFIER NOT NULL,
    BasePrice DECIMAL(18,2) NOT NULL,
    DistanceCharge DECIMAL(18,2) NOT NULL,
    WeightCharge DECIMAL(18,2) NOT NULL,
    PriorityCharge DECIMAL(18,2) NOT NULL DEFAULT 0,
    DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalPrice DECIMAL(18,2) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_DeliveryPricings_Deliveries FOREIGN KEY (DeliveryId) REFERENCES Deliveries(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_DeliveryPricings_DeliveryId ON DeliveryPricings(DeliveryId);

-- DP Availability
CREATE TABLE DPAvailabilities (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DPId UNIQUEIDENTIFIER NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'OFFLINE',
    CurrentLat DECIMAL(10,7) NULL,
    CurrentLng DECIMAL(10,7) NULL,
    VehicleType NVARCHAR(50) NULL,
    LastUpdated DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_DPAvailabilities_Users FOREIGN KEY (DPId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_DPAvailabilities_DPId ON DPAvailabilities(DPId);
CREATE INDEX IX_DPAvailabilities_Status ON DPAvailabilities(Status);

-- Proof of Delivery
CREATE TABLE ProofOfDeliveries (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeliveryId UNIQUEIDENTIFIER NOT NULL,
    RecipientName NVARCHAR(200) NULL,
    SignatureImageUrl NVARCHAR(500) NULL,
    PhotoUrl NVARCHAR(500) NULL,
    VerificationMethod NVARCHAR(50) NOT NULL,
    OTPVerified BIT NOT NULL DEFAULT 0,
    Notes NVARCHAR(500) NULL,
    Latitude DECIMAL(10,7) NULL,
    Longitude DECIMAL(10,7) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_ProofOfDeliveries_Deliveries FOREIGN KEY (DeliveryId) REFERENCES Deliveries(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_ProofOfDeliveries_DeliveryId ON ProofOfDeliveries(DeliveryId);

-- =============================================
-- RATING & COMPLAINT TABLES
-- =============================================

-- Ratings
CREATE TABLE Ratings (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeliveryId UNIQUEIDENTIFIER NOT NULL,
    RaterId UNIQUEIDENTIFIER NOT NULL,
    RatedUserId UNIQUEIDENTIFIER NOT NULL,
    RaterType NVARCHAR(20) NOT NULL,
    RatedType NVARCHAR(20) NOT NULL,
    Score INT NOT NULL,
    Comment NVARCHAR(500) NULL,
    Tags NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Ratings_Deliveries FOREIGN KEY (DeliveryId) REFERENCES Deliveries(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Ratings_Rater FOREIGN KEY (RaterId) REFERENCES Users(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Ratings_RatedUser FOREIGN KEY (RatedUserId) REFERENCES Users(Id) ON DELETE NO ACTION,
    CONSTRAINT CK_Ratings_Score CHECK (Score >= 1 AND Score <= 5)
);

CREATE INDEX IX_Ratings_DeliveryId ON Ratings(DeliveryId);
CREATE INDEX IX_Ratings_RatedUserId ON Ratings(RatedUserId);

-- Behavior Indexes
CREATE TABLE BehaviorIndexes (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    Score DECIMAL(5,2) NOT NULL DEFAULT 100,
    TotalRatings INT NOT NULL DEFAULT 0,
    AverageRating DECIMAL(3,2) NOT NULL DEFAULT 0,
    CompletionRate DECIMAL(5,2) NOT NULL DEFAULT 100,
    CancellationRate DECIMAL(5,2) NOT NULL DEFAULT 0,
    LastCalculated DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_BehaviorIndexes_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_BehaviorIndexes_UserId ON BehaviorIndexes(UserId);

-- Complaints
CREATE TABLE Complaints (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeliveryId UNIQUEIDENTIFIER NULL,
    ComplainantId UNIQUEIDENTIFIER NOT NULL,
    AgainstId UNIQUEIDENTIFIER NULL,
    Category NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    Priority NVARCHAR(20) NOT NULL DEFAULT 'MEDIUM',
    Status NVARCHAR(20) NOT NULL DEFAULT 'OPEN',
    Resolution NVARCHAR(MAX) NULL,
    ResolvedBy UNIQUEIDENTIFIER NULL,
    ResolvedAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Complaints_Deliveries FOREIGN KEY (DeliveryId) REFERENCES Deliveries(Id) ON DELETE SET NULL,
    CONSTRAINT FK_Complaints_Complainant FOREIGN KEY (ComplainantId) REFERENCES Users(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Complaints_Against FOREIGN KEY (AgainstId) REFERENCES Users(Id) ON DELETE NO ACTION
);

CREATE INDEX IX_Complaints_DeliveryId ON Complaints(DeliveryId);
CREATE INDEX IX_Complaints_ComplainantId ON Complaints(ComplainantId);
CREATE INDEX IX_Complaints_Status ON Complaints(Status);

-- =============================================
-- WALLET & PAYMENT TABLES
-- =============================================

-- Wallets
CREATE TABLE Wallets (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    Balance DECIMAL(18,2) NOT NULL DEFAULT 0,
    HoldAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Currency NVARCHAR(3) NOT NULL DEFAULT 'INR',
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Wallets_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_Wallets_UserId ON Wallets(UserId);

-- Wallet Transactions
CREATE TABLE WalletTransactions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    WalletId UNIQUEIDENTIFIER NOT NULL,
    Type NVARCHAR(20) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    BalanceAfter DECIMAL(18,2) NOT NULL,
    Description NVARCHAR(500) NULL,
    ReferenceId NVARCHAR(100) NULL,
    ReferenceType NVARCHAR(50) NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'COMPLETED',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_WalletTransactions_Wallets FOREIGN KEY (WalletId) REFERENCES Wallets(Id) ON DELETE CASCADE
);

CREATE INDEX IX_WalletTransactions_WalletId ON WalletTransactions(WalletId);
CREATE INDEX IX_WalletTransactions_CreatedAt ON WalletTransactions(CreatedAt);

-- Payments
CREATE TABLE Payments (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    DeliveryId UNIQUEIDENTIFIER NULL,
    Amount DECIMAL(18,2) NOT NULL,
    PaymentMethod NVARCHAR(50) NOT NULL,
    PaymentGateway NVARCHAR(50) NULL,
    GatewayTransactionId NVARCHAR(100) NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'PENDING',
    FailureReason NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Payments_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Payments_Deliveries FOREIGN KEY (DeliveryId) REFERENCES Deliveries(Id) ON DELETE SET NULL
);

CREATE INDEX IX_Payments_UserId ON Payments(UserId);
CREATE INDEX IX_Payments_Status ON Payments(Status);

-- Commission Records
CREATE TABLE CommissionRecords (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeliveryId UNIQUEIDENTIFIER NOT NULL,
    DPId UNIQUEIDENTIFIER NOT NULL,
    DPCMId UNIQUEIDENTIFIER NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    DPShare DECIMAL(18,2) NOT NULL,
    DPCMCommission DECIMAL(18,2) NOT NULL DEFAULT 0,
    PlatformFee DECIMAL(18,2) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_CommissionRecords_Deliveries FOREIGN KEY (DeliveryId) REFERENCES Deliveries(Id) ON DELETE CASCADE,
    CONSTRAINT FK_CommissionRecords_DP FOREIGN KEY (DPId) REFERENCES Users(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_CommissionRecords_DPCM FOREIGN KEY (DPCMId) REFERENCES DPCManagers(Id) ON DELETE SET NULL
);

CREATE INDEX IX_CommissionRecords_DeliveryId ON CommissionRecords(DeliveryId);
CREATE INDEX IX_CommissionRecords_DPId ON CommissionRecords(DPId);

-- Settlements
CREATE TABLE Settlements (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    TaxDeducted DECIMAL(18,2) NOT NULL DEFAULT 0,
    NetAmount DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'PENDING',
    BankTransactionId NVARCHAR(100) NULL,
    ProcessedAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Settlements_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE NO ACTION
);

CREATE INDEX IX_Settlements_UserId ON Settlements(UserId);
CREATE INDEX IX_Settlements_Status ON Settlements(Status);

-- =============================================
-- SUBSCRIPTION TABLES
-- =============================================

-- Subscription Plans
CREATE TABLE SubscriptionPlans (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    PlanType NVARCHAR(50) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    DurationDays INT NOT NULL,
    DeliveriesIncluded INT NULL,
    DiscountPercentage DECIMAL(5,2) NOT NULL DEFAULT 0,
    Features NVARCHAR(MAX) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- User Subscriptions
CREATE TABLE UserSubscriptions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    PlanId UNIQUEIDENTIFIER NOT NULL,
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    DeliveriesUsed INT NOT NULL DEFAULT 0,
    Status NVARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    AutoRenew BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_UserSubscriptions_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserSubscriptions_Plans FOREIGN KEY (PlanId) REFERENCES SubscriptionPlans(Id) ON DELETE NO ACTION
);

CREATE INDEX IX_UserSubscriptions_UserId ON UserSubscriptions(UserId);
CREATE INDEX IX_UserSubscriptions_Status ON UserSubscriptions(Status);

-- Promo Codes
CREATE TABLE PromoCodes (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code NVARCHAR(50) NOT NULL,
    DiscountType NVARCHAR(20) NOT NULL,
    DiscountValue DECIMAL(18,2) NOT NULL,
    MaxUses INT NULL,
    UsedCount INT NOT NULL DEFAULT 0,
    MinOrderAmount DECIMAL(18,2) NULL,
    MaxDiscountAmount DECIMAL(18,2) NULL,
    ValidFrom DATETIME2 NOT NULL,
    ValidUntil DATETIME2 NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE UNIQUE INDEX IX_PromoCodes_Code ON PromoCodes(Code);

-- =============================================
-- REFERRAL TABLES
-- =============================================

-- Referral Codes
CREATE TABLE ReferralCodes (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    Code NVARCHAR(20) NOT NULL,
    TotalReferrals INT NOT NULL DEFAULT 0,
    TotalEarnings DECIMAL(18,2) NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_ReferralCodes_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_ReferralCodes_UserId ON ReferralCodes(UserId);
CREATE UNIQUE INDEX IX_ReferralCodes_Code ON ReferralCodes(Code);

-- Referrals
CREATE TABLE Referrals (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ReferrerId UNIQUEIDENTIFIER NOT NULL,
    ReferredUserId UNIQUEIDENTIFIER NOT NULL,
    ReferralCodeId UNIQUEIDENTIFIER NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'PENDING',
    RewardAmount DECIMAL(18,2) NULL,
    RewardPaidAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Referrals_Referrer FOREIGN KEY (ReferrerId) REFERENCES Users(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Referrals_ReferredUser FOREIGN KEY (ReferredUserId) REFERENCES Users(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Referrals_ReferralCode FOREIGN KEY (ReferralCodeId) REFERENCES ReferralCodes(Id) ON DELETE NO ACTION
);

CREATE INDEX IX_Referrals_ReferrerId ON Referrals(ReferrerId);
CREATE INDEX IX_Referrals_ReferredUserId ON Referrals(ReferredUserId);

-- =============================================
-- ADMIN & SYSTEM TABLES
-- =============================================

-- Admin Audit Logs
CREATE TABLE AdminAuditLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NULL,
    Action NVARCHAR(100) NOT NULL,
    EntityType NVARCHAR(100) NULL,
    EntityId NVARCHAR(100) NULL,
    OldValue NVARCHAR(MAX) NULL,
    NewValue NVARCHAR(MAX) NULL,
    IpAddress NVARCHAR(50) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_AdminAuditLogs_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
);

CREATE INDEX IX_AdminAuditLogs_UserId ON AdminAuditLogs(UserId);
CREATE INDEX IX_AdminAuditLogs_Action ON AdminAuditLogs(Action);
CREATE INDEX IX_AdminAuditLogs_CreatedAt ON AdminAuditLogs(CreatedAt);

-- System Configs
CREATE TABLE SystemConfigs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ConfigKey NVARCHAR(100) NOT NULL,
    ConfigValue NVARCHAR(MAX) NOT NULL,
    Description NVARCHAR(500) NULL,
    DataType NVARCHAR(50) NOT NULL DEFAULT 'STRING',
    IsEditable BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE UNIQUE INDEX IX_SystemConfigs_ConfigKey ON SystemConfigs(ConfigKey);

-- =============================================
-- EF MIGRATIONS TRACKING (Optional)
-- =============================================

CREATE TABLE __EFMigrationsHistory (
    MigrationId NVARCHAR(150) NOT NULL PRIMARY KEY,
    ProductVersion NVARCHAR(32) NOT NULL
);

PRINT 'DeliverX Database Schema Created Successfully!';
GO
