CREATE TABLE IF NOT EXISTS "__EFMigrationsLock" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK___EFMigrationsLock" PRIMARY KEY,
    "Timestamp" TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS "OTPVerifications" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_OTPVerifications" PRIMARY KEY,
    "Phone" TEXT NOT NULL,
    "OTPHash" TEXT NOT NULL,
    "Attempts" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    "ExpiresAt" TEXT NOT NULL,
    "IsVerified" INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS "Permissions" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Permissions" PRIMARY KEY,
    "Code" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Category" TEXT NULL,
    "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now'))
);
CREATE TABLE IF NOT EXISTS "Users" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY,
    "Phone" TEXT NULL,
    "Email" TEXT NULL,
    "PasswordHash" TEXT NULL,
    "Role" TEXT NOT NULL,
    "Is2FAEnabled" INTEGER NOT NULL,
    "TotpSecret" TEXT NULL,
    "IsActive" INTEGER NOT NULL,
    "IsEmailVerified" INTEGER NOT NULL,
    "IsPhoneVerified" INTEGER NOT NULL,
    "LastLoginAt" TEXT NULL,
    "PasswordChangedAt" TEXT NULL,
    "FailedLoginAttempts" INTEGER NOT NULL,
    "LockedUntil" TEXT NULL,
    "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    "UpdatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    CONSTRAINT "CK_Users_ContactMethod" CHECK ([Phone] IS NOT NULL OR [Email] IS NOT NULL)
);
CREATE TABLE IF NOT EXISTS "RolePermissions" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_RolePermissions" PRIMARY KEY,
    "Role" TEXT NOT NULL,
    "PermissionId" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    CONSTRAINT "FK_RolePermissions_Permissions_PermissionId" FOREIGN KEY ("PermissionId") REFERENCES "Permissions" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "AuthAuditLogs" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AuthAuditLogs" PRIMARY KEY,
    "UserId" TEXT NULL,
    "EventType" TEXT NOT NULL,
    "Phone" TEXT NULL,
    "Email" TEXT NULL,
    "IpAddress" TEXT NULL,
    "UserAgent" TEXT NULL,
    "Details" TEXT NULL,
    "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    CONSTRAINT "FK_AuthAuditLogs_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
);
CREATE TABLE IF NOT EXISTS "UserSessions" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_UserSessions" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "RefreshTokenHash" TEXT NOT NULL,
    "DeviceType" TEXT NULL,
    "DeviceId" TEXT NULL,
    "IpAddress" TEXT NULL,
    "UserAgent" TEXT NULL,
    "Location" TEXT NULL,
    "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    "LastActiveAt" TEXT NOT NULL DEFAULT (datetime('now')),
    "ExpiresAt" TEXT NOT NULL,
    "IsRevoked" INTEGER NOT NULL,
    "RevokedAt" TEXT NULL,
    CONSTRAINT "FK_UserSessions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE INDEX "IX_AuthAuditLogs_CreatedAt" ON "AuthAuditLogs" ("CreatedAt");
CREATE INDEX "IX_AuthAuditLogs_EventType" ON "AuthAuditLogs" ("EventType");
CREATE INDEX "IX_AuthAuditLogs_UserId" ON "AuthAuditLogs" ("UserId");
CREATE INDEX "IX_OTPVerifications_Phone_ExpiresAt" ON "OTPVerifications" ("Phone", "ExpiresAt");
CREATE UNIQUE INDEX "IX_Permissions_Code" ON "Permissions" ("Code");
CREATE INDEX "IX_RolePermissions_PermissionId" ON "RolePermissions" ("PermissionId");
CREATE INDEX "IX_RolePermissions_Role" ON "RolePermissions" ("Role");
CREATE UNIQUE INDEX "IX_RolePermissions_Role_PermissionId" ON "RolePermissions" ("Role", "PermissionId");
CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email") WHERE [Email] IS NOT NULL;
CREATE UNIQUE INDEX "IX_Users_Phone" ON "Users" ("Phone") WHERE [Phone] IS NOT NULL;
CREATE INDEX "IX_Users_Role" ON "Users" ("Role");
CREATE INDEX "IX_UserSessions_ExpiresAt" ON "UserSessions" ("ExpiresAt");
CREATE INDEX "IX_UserSessions_RefreshTokenHash" ON "UserSessions" ("RefreshTokenHash");
CREATE INDEX "IX_UserSessions_UserId" ON "UserSessions" ("UserId");
CREATE TABLE IF NOT EXISTS "AadhaarVerifications" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AadhaarVerifications" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "AadhaarHash" TEXT NOT NULL,
    "AadhaarReferenceId" TEXT NULL,
    "NameAsPerAadhaar" TEXT NOT NULL,
    "DOB" TEXT NOT NULL,
    "Gender" TEXT NULL,
    "AddressEncrypted" TEXT NULL,
    "VerificationMethod" TEXT NULL,
    "VerifiedAt" TEXT NULL,
    "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    CONSTRAINT "FK_AadhaarVerifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "BankVerifications" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_BankVerifications" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "AccountNumberEncrypted" TEXT NOT NULL,
    "AccountNumberHash" TEXT NOT NULL,
    "IFSCCode" TEXT NOT NULL,
    "AccountHolderName" TEXT NOT NULL,
    "BankName" TEXT NULL,
    "BranchName" TEXT NULL,
    "VerificationMethod" TEXT NULL,
    "TransactionId" TEXT NULL,
    "NameMatchScore" INTEGER NULL,
    "VerifiedAt" TEXT NULL,
    "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    CONSTRAINT "FK_BankVerifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "BusinessConsumerProfiles" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_BusinessConsumerProfiles" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "BusinessName" TEXT NOT NULL,
    "ContactPersonName" TEXT NOT NULL,
    "GSTIN" TEXT NULL,
    "PAN" TEXT NOT NULL,
    "BusinessCategory" TEXT NULL,
    "BusinessAddress" TEXT NULL,
    "BankAccountEncrypted" TEXT NULL,
    "SubscriptionPlanId" TEXT NULL,
    "IsActive" INTEGER NOT NULL DEFAULT 0,
    "ActivatedAt" TEXT NULL,
    "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    "UpdatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    CONSTRAINT "FK_BusinessConsumerProfiles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "DPCManagers" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_DPCManagers" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "OrganizationName" TEXT NOT NULL,
    "ContactPersonName" TEXT NOT NULL,
    "PAN" TEXT NOT NULL,
    "RegistrationCertificateUrl" TEXT NULL,
    "ServiceRegions" TEXT NULL,
    "CommissionType" TEXT NULL,
    "CommissionValue" TEXT NULL,
    "BankAccountEncrypted" TEXT NULL,
    "IsActive" INTEGER NOT NULL DEFAULT 0,
    "ActivatedAt" TEXT NULL,
    "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    "UpdatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    CONSTRAINT "FK_DPCManagers_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "KYCRequests" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_KYCRequests" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "VerificationType" TEXT NOT NULL,
    "Status" TEXT NOT NULL DEFAULT 'PENDING',
    "Method" TEXT NULL,
    "RequestData" TEXT NULL,
    "ResponseData" TEXT NULL,
    "DocumentUrls" TEXT NULL,
    "VerifiedBy" TEXT NULL,
    "RejectionReason" TEXT NULL,
    "InitiatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    "CompletedAt" TEXT NULL,
    "ExpiresAt" TEXT NULL,
    "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    "UpdatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    CONSTRAINT "FK_KYCRequests_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_KYCRequests_Users_VerifiedBy" FOREIGN KEY ("VerifiedBy") REFERENCES "Users" ("Id") ON DELETE SET NULL
);
CREATE TABLE IF NOT EXISTS "PANVerifications" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_PANVerifications" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "PAN" TEXT NOT NULL,
    "NameAsPerPAN" TEXT NOT NULL,
    "DOB" TEXT NULL,
    "PANStatus" TEXT NULL,
    "NameMatchScore" INTEGER NULL,
    "VerifiedAt" TEXT NULL,
    "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    CONSTRAINT "FK_PANVerifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "PoliceVerifications" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_PoliceVerifications" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "VerificationAgency" TEXT NULL,
    "AddressForVerification" TEXT NULL,
    "RequestDocumentUrl" TEXT NULL,
    "ClearanceDocumentUrl" TEXT NULL,
    "Status" TEXT NOT NULL DEFAULT 'PENDING',
    "InitiatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    "CompletedAt" TEXT NULL,
    "Remarks" TEXT NULL,
    "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    CONSTRAINT "FK_PoliceVerifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "VehicleLicenseVerifications" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_VehicleLicenseVerifications" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "LicenseNumber" TEXT NULL,
    "LicenseDocumentUrl" TEXT NULL,
    "LicenseValidUpto" TEXT NULL,
    "VehicleNumber" TEXT NULL,
    "VehicleRCDocumentUrl" TEXT NULL,
    "VehicleType" TEXT NULL,
    "VehicleOwnerName" TEXT NULL,
    "VerifiedAt" TEXT NULL,
    "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    CONSTRAINT "FK_VehicleLicenseVerifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE UNIQUE INDEX "IX_AadhaarVerifications_AadhaarHash" ON "AadhaarVerifications" ("AadhaarHash");
CREATE UNIQUE INDEX "IX_AadhaarVerifications_UserId" ON "AadhaarVerifications" ("UserId");
CREATE INDEX "IX_BankVerifications_AccountNumberHash" ON "BankVerifications" ("AccountNumberHash");
CREATE INDEX "IX_BankVerifications_UserId" ON "BankVerifications" ("UserId");
CREATE UNIQUE INDEX "IX_BusinessConsumerProfiles_UserId" ON "BusinessConsumerProfiles" ("UserId");
CREATE UNIQUE INDEX "IX_DPCManagers_UserId" ON "DPCManagers" ("UserId");
CREATE INDEX "IX_KYCRequests_Status" ON "KYCRequests" ("Status");
CREATE INDEX "IX_KYCRequests_UserId" ON "KYCRequests" ("UserId");
CREATE INDEX "IX_KYCRequests_VerificationType_Status" ON "KYCRequests" ("VerificationType", "Status");
CREATE INDEX "IX_KYCRequests_VerifiedBy" ON "KYCRequests" ("VerifiedBy");
CREATE INDEX "IX_PANVerifications_PAN" ON "PANVerifications" ("PAN");
CREATE INDEX "IX_PANVerifications_UserId" ON "PANVerifications" ("UserId");
CREATE INDEX "IX_PoliceVerifications_Status" ON "PoliceVerifications" ("Status");
CREATE INDEX "IX_PoliceVerifications_UserId" ON "PoliceVerifications" ("UserId");
CREATE INDEX "IX_VehicleLicenseVerifications_UserId" ON "VehicleLicenseVerifications" ("UserId");
CREATE TABLE IF NOT EXISTS "ServiceAreas" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_ServiceAreas" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "UserRole" TEXT NOT NULL DEFAULT 'DP',
    "AreaType" TEXT NOT NULL DEFAULT 'CIRCLE',
    "CenterLat" TEXT NOT NULL,
    "CenterLng" TEXT NOT NULL,
    "RadiusKm" TEXT NOT NULL,
    "AreaName" TEXT NULL,
    "IsActive" INTEGER NOT NULL DEFAULT 1,
    "AllowDropOutsideArea" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    "UpdatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    CONSTRAINT "FK_ServiceAreas_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE INDEX "IX_ServiceAreas_CenterLat_CenterLng" ON "ServiceAreas" ("CenterLat", "CenterLng");
CREATE INDEX "IX_ServiceAreas_IsActive" ON "ServiceAreas" ("IsActive");
CREATE INDEX "IX_ServiceAreas_UserId" ON "ServiceAreas" ("UserId");
CREATE INDEX "IX_ServiceAreas_UserId_IsActive" ON "ServiceAreas" ("UserId", "IsActive");
CREATE TABLE IF NOT EXISTS "DPCMCommissionConfigs" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_DPCMCommissionConfigs" PRIMARY KEY,
    "DPCMId" TEXT NOT NULL,
    "CommissionType" TEXT NOT NULL,
    "CommissionValue" TEXT NOT NULL,
    "MinCommissionAmount" TEXT NOT NULL DEFAULT '0.0',
    "MaxCommissionAmount" TEXT NULL,
    "EffectiveFrom" TEXT NOT NULL,
    "EffectiveTo" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_DPCMCommissionConfigs_DPCManagers_DPCMId" FOREIGN KEY ("DPCMId") REFERENCES "DPCManagers" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "PlatformFeeConfigs" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_PlatformFeeConfigs" PRIMARY KEY,
    "FeeType" TEXT NOT NULL,
    "FeeCalculationType" TEXT NOT NULL,
    "FeeValue" TEXT NOT NULL,
    "ApplicableRoles" TEXT NULL,
    "Conditions" TEXT NULL,
    "EffectiveFrom" TEXT NOT NULL,
    "EffectiveTo" TEXT NULL,
    "CreatedAt" TEXT NOT NULL
);
CREATE INDEX "IX_DPCMCommissionConfigs_DPCMId" ON "DPCMCommissionConfigs" ("DPCMId");
CREATE INDEX "IX_PlatformFeeConfigs_EffectiveFrom_EffectiveTo" ON "PlatformFeeConfigs" ("EffectiveFrom", "EffectiveTo");
CREATE TABLE IF NOT EXISTS "DeliveryPricings" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_DeliveryPricings" PRIMARY KEY,
    "CalculatedAt" TEXT NOT NULL,
    "Currency" TEXT NOT NULL DEFAULT 'INR',
    "DPCMCommission" TEXT NOT NULL,
    "DPEarning" TEXT NOT NULL,
    "DPId" TEXT NOT NULL,
    "DeliveryId" TEXT NULL,
    "DistanceKm" TEXT NOT NULL,
    "GSTAmount" TEXT NOT NULL,
    "GSTPercentage" TEXT NOT NULL,
    "MinCharge" TEXT NOT NULL,
    "PerKgRate" TEXT NOT NULL,
    "PerKmRate" TEXT NOT NULL,
    "PlatformFee" TEXT NOT NULL,
    "Subtotal" TEXT NOT NULL,
    "Surcharges" TEXT NULL,
    "TotalAmount" TEXT NOT NULL,
    "WeightKg" TEXT NOT NULL,
    CONSTRAINT "FK_DeliveryPricings_Users_DPId" FOREIGN KEY ("DPId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);
CREATE TABLE IF NOT EXISTS "DPPricingConfigs" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_DPPricingConfigs" PRIMARY KEY,
    "AcceptsPriorityDelivery" INTEGER NOT NULL DEFAULT 1,
    "CreatedAt" TEXT NOT NULL,
    "Currency" TEXT NOT NULL DEFAULT 'INR',
    "DPId" TEXT NOT NULL,
    "EffectiveFrom" TEXT NOT NULL,
    "EffectiveTo" TEXT NULL,
    "MaxDistanceKm" TEXT NOT NULL DEFAULT '20.0',
    "MinCharge" TEXT NOT NULL,
    "PeakHourSurcharge" TEXT NOT NULL DEFAULT '0.0',
    "PerKgRate" TEXT NOT NULL,
    "PerKmRate" TEXT NOT NULL,
    "PrioritySurcharge" TEXT NOT NULL DEFAULT '0.0',
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_DPPricingConfigs_Users_DPId" FOREIGN KEY ("DPId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "DeliveryPartnerProfiles" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_DeliveryPartnerProfiles" PRIMARY KEY,
    "ActivatedAt" TEXT NULL,
    "Address" TEXT NULL,
    "Availability" TEXT NULL,
    "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    "DOB" TEXT NOT NULL,
    "DPCMId" TEXT NULL,
    "FullName" TEXT NOT NULL,
    "Gender" TEXT NULL,
    "IsActive" INTEGER NOT NULL DEFAULT 0,
    "Languages" TEXT NULL,
    "MaxDistanceKm" TEXT NULL,
    "MinCharge" TEXT NULL,
    "PerKgRate" TEXT NULL,
    "PerKmRate" TEXT NULL,
    "ProfilePhotoUrl" TEXT NULL,
    "ServiceAreaCenterLat" TEXT NULL,
    "ServiceAreaCenterLng" TEXT NULL,
    "ServiceAreaRadiusKm" TEXT NULL,
    "UpdatedAt" TEXT NOT NULL DEFAULT (datetime('now')),
    "UserId" TEXT NOT NULL,
    "VehicleType" TEXT NULL,
    CONSTRAINT "FK_DeliveryPartnerProfiles_DPCManagers_DPCMId" FOREIGN KEY ("DPCMId") REFERENCES "DPCManagers" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_DeliveryPartnerProfiles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE INDEX "IX_DeliveryPricings_DeliveryId" ON "DeliveryPricings" ("DeliveryId");
CREATE INDEX "IX_DeliveryPricings_DPId" ON "DeliveryPricings" ("DPId");
CREATE INDEX "IX_DPPricingConfigs_DPId" ON "DPPricingConfigs" ("DPId");
CREATE INDEX "IX_DPPricingConfigs_EffectiveFrom_EffectiveTo" ON "DPPricingConfigs" ("EffectiveFrom", "EffectiveTo");
CREATE INDEX "IX_DeliveryPartnerProfiles_DPCMId" ON "DeliveryPartnerProfiles" ("DPCMId");
CREATE INDEX "IX_DeliveryPartnerProfiles_IsActive" ON "DeliveryPartnerProfiles" ("IsActive");
CREATE UNIQUE INDEX "IX_DeliveryPartnerProfiles_UserId" ON "DeliveryPartnerProfiles" ("UserId");
CREATE TABLE IF NOT EXISTS "Deliveries" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Deliveries" PRIMARY KEY,
    "RequesterId" TEXT NOT NULL,
    "RequesterType" TEXT NOT NULL,
    "AssignedDPId" TEXT NULL,
    "AssignedAt" TEXT NULL,
    "PickupLat" TEXT NOT NULL,
    "PickupLng" TEXT NOT NULL,
    "PickupAddress" TEXT NOT NULL,
    "PickupContactName" TEXT NULL,
    "PickupContactPhone" TEXT NULL,
    "PickupInstructions" TEXT NULL,
    "DropLat" TEXT NOT NULL,
    "DropLng" TEXT NOT NULL,
    "DropAddress" TEXT NOT NULL,
    "DropContactName" TEXT NULL,
    "DropContactPhone" TEXT NULL,
    "DropInstructions" TEXT NULL,
    "WeightKg" TEXT NOT NULL,
    "PackageType" TEXT NOT NULL,
    "PackageDimensions" TEXT NULL,
    "PackageValue" TEXT NULL,
    "PackageDescription" TEXT NULL,
    "Priority" TEXT NOT NULL DEFAULT 'ASAP',
    "ScheduledAt" TEXT NULL,
    "Status" TEXT NOT NULL DEFAULT 'CREATED',
    "EstimatedPrice" TEXT NULL,
    "FinalPrice" TEXT NULL,
    "SpecialInstructions" TEXT NULL,
    "PreferredDPId" TEXT NULL,
    "DistanceKm" TEXT NULL,
    "EstimatedDurationMinutes" INTEGER NULL,
    "MatchingAttempts" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    "CancelledAt" TEXT NULL,
    "CancellationReason" TEXT NULL,
    CONSTRAINT "FK_Deliveries_Users_AssignedDPId" FOREIGN KEY ("AssignedDPId") REFERENCES "Users" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Deliveries_Users_RequesterId" FOREIGN KEY ("RequesterId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);
CREATE TABLE IF NOT EXISTS "DeliveryEvents" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_DeliveryEvents" PRIMARY KEY,
    "DeliveryId" TEXT NOT NULL,
    "EventType" TEXT NOT NULL,
    "FromStatus" TEXT NULL,
    "ToStatus" TEXT NULL,
    "ActorId" TEXT NULL,
    "ActorType" TEXT NULL,
    "Metadata" TEXT NULL,
    "Timestamp" TEXT NOT NULL,
    CONSTRAINT "FK_DeliveryEvents_Deliveries_DeliveryId" FOREIGN KEY ("DeliveryId") REFERENCES "Deliveries" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_DeliveryEvents_Users_ActorId" FOREIGN KEY ("ActorId") REFERENCES "Users" ("Id") ON DELETE SET NULL
);
CREATE TABLE IF NOT EXISTS "DeliveryMatchingHistories" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_DeliveryMatchingHistories" PRIMARY KEY,
    "DeliveryId" TEXT NOT NULL,
    "DPId" TEXT NOT NULL,
    "MatchingAttempt" INTEGER NOT NULL DEFAULT 1,
    "NotifiedAt" TEXT NOT NULL,
    "ResponseType" TEXT NULL,
    "RespondedAt" TEXT NULL,
    "RejectionReason" TEXT NULL,
    CONSTRAINT "FK_DeliveryMatchingHistories_Deliveries_DeliveryId" FOREIGN KEY ("DeliveryId") REFERENCES "Deliveries" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_DeliveryMatchingHistories_Users_DPId" FOREIGN KEY ("DPId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "DPAvailabilities" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_DPAvailabilities" PRIMARY KEY,
    "DPId" TEXT NOT NULL,
    "Status" TEXT NOT NULL DEFAULT 'OFFLINE',
    "CurrentDeliveryId" TEXT NULL,
    "LastLocationLat" TEXT NULL,
    "LastLocationLng" TEXT NULL,
    "LastLocationUpdatedAt" TEXT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_DPAvailabilities_Deliveries_CurrentDeliveryId" FOREIGN KEY ("CurrentDeliveryId") REFERENCES "Deliveries" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_DPAvailabilities_Users_DPId" FOREIGN KEY ("DPId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE INDEX "IX_Deliveries_AssignedDPId" ON "Deliveries" ("AssignedDPId");
CREATE INDEX "IX_Deliveries_CreatedAt" ON "Deliveries" ("CreatedAt");
CREATE INDEX "IX_Deliveries_Priority_Status" ON "Deliveries" ("Priority", "Status");
CREATE INDEX "IX_Deliveries_RequesterId" ON "Deliveries" ("RequesterId");
CREATE INDEX "IX_Deliveries_Status" ON "Deliveries" ("Status");
CREATE INDEX "IX_DeliveryEvents_ActorId" ON "DeliveryEvents" ("ActorId");
CREATE INDEX "IX_DeliveryEvents_DeliveryId" ON "DeliveryEvents" ("DeliveryId");
CREATE INDEX "IX_DeliveryEvents_Timestamp" ON "DeliveryEvents" ("Timestamp");
CREATE INDEX "IX_DeliveryMatchingHistories_DeliveryId" ON "DeliveryMatchingHistories" ("DeliveryId");
CREATE INDEX "IX_DeliveryMatchingHistories_DPId" ON "DeliveryMatchingHistories" ("DPId");
CREATE INDEX "IX_DPAvailabilities_CurrentDeliveryId" ON "DPAvailabilities" ("CurrentDeliveryId");
CREATE UNIQUE INDEX "IX_DPAvailabilities_DPId" ON "DPAvailabilities" ("DPId");
CREATE INDEX "IX_DPAvailabilities_Status" ON "DPAvailabilities" ("Status");
CREATE TABLE IF NOT EXISTS "ProofOfDeliveries" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_ProofOfDeliveries" PRIMARY KEY,
    "DeliveryId" TEXT NOT NULL,
    "RecipientName" TEXT NULL,
    "RecipientRelation" TEXT NULL,
    "RecipientOTP" TEXT NULL,
    "OTPVerified" INTEGER NOT NULL,
    "OTPSentAt" TEXT NULL,
    "OTPVerifiedAt" TEXT NULL,
    "PODPhotoUrl" TEXT NULL,
    "PackagePhotoUrl" TEXT NULL,
    "SignatureUrl" TEXT NULL,
    "DeliveredLat" TEXT NULL,
    "DeliveredLng" TEXT NULL,
    "DistanceFromDropLocation" TEXT NULL,
    "PickedUpAt" TEXT NULL,
    "InTransitAt" TEXT NULL,
    "DeliveredAt" TEXT NULL,
    "ClosedAt" TEXT NULL,
    "Notes" TEXT NULL,
    "DeliveryCondition" TEXT NULL,
    "VerifiedBy" TEXT NULL,
    "VerifiedAt" TEXT NULL,
    "CreatedAt" TEXT NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_ProofOfDeliveries_Deliveries_DeliveryId" FOREIGN KEY ("DeliveryId") REFERENCES "Deliveries" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ProofOfDeliveries_Users_VerifiedBy" FOREIGN KEY ("VerifiedBy") REFERENCES "Users" ("Id") ON DELETE SET NULL
);
CREATE UNIQUE INDEX "IX_ProofOfDeliveries_DeliveryId" ON "ProofOfDeliveries" ("DeliveryId");
CREATE INDEX "IX_ProofOfDeliveries_VerifiedBy" ON "ProofOfDeliveries" ("VerifiedBy");
CREATE TABLE IF NOT EXISTS "BehaviorIndexes" (
    "UserId" TEXT NOT NULL CONSTRAINT "PK_BehaviorIndexes" PRIMARY KEY,
    "AverageRating" TEXT NOT NULL,
    "TotalRatings" INTEGER NOT NULL,
    "CompletionRate" TEXT NOT NULL,
    "PunctualityRate" TEXT NOT NULL,
    "ComplaintFreeRate" TEXT NOT NULL,
    "BehaviorScore" TEXT NOT NULL,
    "LastCalculatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_BehaviorIndexes_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "Ratings" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Ratings" PRIMARY KEY,
    "DeliveryId" TEXT NOT NULL,
    "RaterId" TEXT NOT NULL,
    "RaterType" TEXT NOT NULL,
    "TargetId" TEXT NOT NULL,
    "TargetType" TEXT NOT NULL,
    "Score" INTEGER NOT NULL,
    "Tags" TEXT NULL,
    "Comment" TEXT NULL,
    "IsAnonymous" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Ratings_Deliveries_DeliveryId" FOREIGN KEY ("DeliveryId") REFERENCES "Deliveries" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Ratings_Users_RaterId" FOREIGN KEY ("RaterId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Ratings_Users_TargetId" FOREIGN KEY ("TargetId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE INDEX "IX_Ratings_CreatedAt" ON "Ratings" ("CreatedAt");
CREATE UNIQUE INDEX "IX_Ratings_DeliveryId_RaterId_TargetId" ON "Ratings" ("DeliveryId", "RaterId", "TargetId");
CREATE INDEX "IX_Ratings_RaterId" ON "Ratings" ("RaterId");
CREATE INDEX "IX_Ratings_TargetId" ON "Ratings" ("TargetId");
CREATE TABLE IF NOT EXISTS "Complaints" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Complaints" PRIMARY KEY,
    "ComplaintNumber" TEXT NOT NULL,
    "DeliveryId" TEXT NOT NULL,
    "RaisedById" TEXT NOT NULL,
    "RaisedByType" TEXT NOT NULL,
    "AgainstId" TEXT NULL,
    "AgainstType" TEXT NULL,
    "Category" TEXT NOT NULL,
    "Severity" TEXT NOT NULL,
    "Subject" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "Resolution" TEXT NULL,
    "ResolutionNotes" TEXT NULL,
    "AssignedToId" TEXT NULL,
    "AssignedAt" TEXT NULL,
    "ResolvedAt" TEXT NULL,
    "ClosedAt" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Complaints_Deliveries_DeliveryId" FOREIGN KEY ("DeliveryId") REFERENCES "Deliveries" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Complaints_Users_AgainstId" FOREIGN KEY ("AgainstId") REFERENCES "Users" ("Id"),
    CONSTRAINT "FK_Complaints_Users_AssignedToId" FOREIGN KEY ("AssignedToId") REFERENCES "Users" ("Id"),
    CONSTRAINT "FK_Complaints_Users_RaisedById" FOREIGN KEY ("RaisedById") REFERENCES "Users" ("Id")
);
CREATE TABLE IF NOT EXISTS "ComplaintSLAConfigs" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_ComplaintSLAConfigs" PRIMARY KEY,
    "Category" TEXT NOT NULL,
    "Severity" TEXT NOT NULL,
    "ResponseTimeHours" INTEGER NOT NULL,
    "ResolutionTimeHours" INTEGER NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS "Inspectors" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Inspectors" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "InspectorCode" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "Phone" TEXT NOT NULL,
    "Zone" TEXT NULL,
    "ActiveCases" INTEGER NOT NULL,
    "TotalCasesHandled" INTEGER NOT NULL,
    "ResolutionRate" TEXT NOT NULL,
    "AverageResolutionTimeHours" TEXT NOT NULL,
    "IsAvailable" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Inspectors_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "ComplaintComments" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_ComplaintComments" PRIMARY KEY,
    "ComplaintId" TEXT NOT NULL,
    "AuthorId" TEXT NOT NULL,
    "Content" TEXT NOT NULL,
    "IsInternal" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_ComplaintComments_Complaints_ComplaintId" FOREIGN KEY ("ComplaintId") REFERENCES "Complaints" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ComplaintComments_Users_AuthorId" FOREIGN KEY ("AuthorId") REFERENCES "Users" ("Id")
);
CREATE TABLE IF NOT EXISTS "ComplaintEvidences" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_ComplaintEvidences" PRIMARY KEY,
    "ComplaintId" TEXT NOT NULL,
    "Type" TEXT NOT NULL,
    "FileName" TEXT NOT NULL,
    "FileUrl" TEXT NOT NULL,
    "Description" TEXT NULL,
    "UploadedById" TEXT NOT NULL,
    "UploadedAt" TEXT NOT NULL,
    CONSTRAINT "FK_ComplaintEvidences_Complaints_ComplaintId" FOREIGN KEY ("ComplaintId") REFERENCES "Complaints" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ComplaintEvidences_Users_UploadedById" FOREIGN KEY ("UploadedById") REFERENCES "Users" ("Id")
);
CREATE INDEX "IX_ComplaintComments_AuthorId" ON "ComplaintComments" ("AuthorId");
CREATE INDEX "IX_ComplaintComments_ComplaintId" ON "ComplaintComments" ("ComplaintId");
CREATE INDEX "IX_ComplaintEvidences_ComplaintId" ON "ComplaintEvidences" ("ComplaintId");
CREATE INDEX "IX_ComplaintEvidences_UploadedById" ON "ComplaintEvidences" ("UploadedById");
CREATE INDEX "IX_Complaints_AgainstId" ON "Complaints" ("AgainstId");
CREATE INDEX "IX_Complaints_AssignedToId" ON "Complaints" ("AssignedToId");
CREATE INDEX "IX_Complaints_Category" ON "Complaints" ("Category");
CREATE UNIQUE INDEX "IX_Complaints_ComplaintNumber" ON "Complaints" ("ComplaintNumber");
CREATE INDEX "IX_Complaints_CreatedAt" ON "Complaints" ("CreatedAt");
CREATE INDEX "IX_Complaints_DeliveryId" ON "Complaints" ("DeliveryId");
CREATE INDEX "IX_Complaints_RaisedById" ON "Complaints" ("RaisedById");
CREATE INDEX "IX_Complaints_Status" ON "Complaints" ("Status");
CREATE UNIQUE INDEX "IX_ComplaintSLAConfigs_Category_Severity" ON "ComplaintSLAConfigs" ("Category", "Severity");
CREATE UNIQUE INDEX "IX_Inspectors_InspectorCode" ON "Inspectors" ("InspectorCode");
CREATE UNIQUE INDEX "IX_Inspectors_UserId" ON "Inspectors" ("UserId");
CREATE TABLE IF NOT EXISTS "CommissionRecords" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_CommissionRecords" PRIMARY KEY,
    "DeliveryId" TEXT NOT NULL,
    "DPId" TEXT NOT NULL,
    "DPCMId" TEXT NULL,
    "DeliveryAmount" TEXT NOT NULL,
    "DPEarning" TEXT NOT NULL,
    "DPCMCommission" TEXT NOT NULL,
    "PlatformFee" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_CommissionRecords_Deliveries_DeliveryId" FOREIGN KEY ("DeliveryId") REFERENCES "Deliveries" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CommissionRecords_Users_DPCMId" FOREIGN KEY ("DPCMId") REFERENCES "Users" ("Id"),
    CONSTRAINT "FK_CommissionRecords_Users_DPId" FOREIGN KEY ("DPId") REFERENCES "Users" ("Id")
);
CREATE TABLE IF NOT EXISTS "Payments" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Payments" PRIMARY KEY,
    "PaymentNumber" TEXT NOT NULL,
    "UserId" TEXT NOT NULL,
    "DeliveryId" TEXT NULL,
    "PaymentType" TEXT NOT NULL,
    "Amount" TEXT NOT NULL,
    "PlatformFee" TEXT NULL,
    "Tax" TEXT NULL,
    "TotalAmount" TEXT NOT NULL,
    "PaymentMethod" TEXT NOT NULL,
    "PaymentGateway" TEXT NULL,
    "GatewayTransactionId" TEXT NULL,
    "GatewayOrderId" TEXT NULL,
    "Status" TEXT NOT NULL,
    "FailureReason" TEXT NULL,
    "CompletedAt" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Payments_Deliveries_DeliveryId" FOREIGN KEY ("DeliveryId") REFERENCES "Deliveries" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Payments_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "Settlements" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Settlements" PRIMARY KEY,
    "SettlementNumber" TEXT NOT NULL,
    "BeneficiaryId" TEXT NOT NULL,
    "BeneficiaryType" TEXT NOT NULL,
    "GrossAmount" TEXT NOT NULL,
    "TdsAmount" TEXT NOT NULL,
    "NetAmount" TEXT NOT NULL,
    "BankAccountNumber" TEXT NULL,
    "BankIfscCode" TEXT NULL,
    "UpiId" TEXT NULL,
    "PayoutMethod" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "PayoutReference" TEXT NULL,
    "FailureReason" TEXT NULL,
    "ProcessedAt" TEXT NULL,
    "SettlementDate" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Settlements_Users_BeneficiaryId" FOREIGN KEY ("BeneficiaryId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "Wallets" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Wallets" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "WalletType" TEXT NOT NULL,
    "Balance" TEXT NOT NULL,
    "HoldBalance" TEXT NOT NULL,
    "Currency" TEXT NOT NULL DEFAULT 'INR',
    "IsActive" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Wallets_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "SettlementItems" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_SettlementItems" PRIMARY KEY,
    "SettlementId" TEXT NOT NULL,
    "DeliveryId" TEXT NOT NULL,
    "EarningAmount" TEXT NOT NULL,
    "CommissionAmount" TEXT NOT NULL,
    "NetAmount" TEXT NOT NULL,
    "EarnedAt" TEXT NOT NULL,
    CONSTRAINT "FK_SettlementItems_Deliveries_DeliveryId" FOREIGN KEY ("DeliveryId") REFERENCES "Deliveries" ("Id"),
    CONSTRAINT "FK_SettlementItems_Settlements_SettlementId" FOREIGN KEY ("SettlementId") REFERENCES "Settlements" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "WalletTransactions" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_WalletTransactions" PRIMARY KEY,
    "WalletId" TEXT NOT NULL,
    "TransactionType" TEXT NOT NULL,
    "Category" TEXT NOT NULL,
    "Amount" TEXT NOT NULL,
    "BalanceBefore" TEXT NOT NULL,
    "BalanceAfter" TEXT NOT NULL,
    "ReferenceId" TEXT NULL,
    "ReferenceType" TEXT NULL,
    "Description" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_WalletTransactions_Wallets_WalletId" FOREIGN KEY ("WalletId") REFERENCES "Wallets" ("Id") ON DELETE CASCADE
);
CREATE UNIQUE INDEX "IX_CommissionRecords_DeliveryId" ON "CommissionRecords" ("DeliveryId");
CREATE INDEX "IX_CommissionRecords_DPCMId" ON "CommissionRecords" ("DPCMId");
CREATE INDEX "IX_CommissionRecords_DPId" ON "CommissionRecords" ("DPId");
CREATE INDEX "IX_CommissionRecords_Status" ON "CommissionRecords" ("Status");
CREATE INDEX "IX_Payments_CreatedAt" ON "Payments" ("CreatedAt");
CREATE INDEX "IX_Payments_DeliveryId" ON "Payments" ("DeliveryId");
CREATE UNIQUE INDEX "IX_Payments_PaymentNumber" ON "Payments" ("PaymentNumber");
CREATE INDEX "IX_Payments_Status" ON "Payments" ("Status");
CREATE INDEX "IX_Payments_UserId" ON "Payments" ("UserId");
CREATE INDEX "IX_SettlementItems_DeliveryId" ON "SettlementItems" ("DeliveryId");
CREATE INDEX "IX_SettlementItems_SettlementId" ON "SettlementItems" ("SettlementId");
CREATE INDEX "IX_Settlements_BeneficiaryId" ON "Settlements" ("BeneficiaryId");
CREATE INDEX "IX_Settlements_SettlementDate" ON "Settlements" ("SettlementDate");
CREATE UNIQUE INDEX "IX_Settlements_SettlementNumber" ON "Settlements" ("SettlementNumber");
CREATE INDEX "IX_Settlements_Status" ON "Settlements" ("Status");
CREATE UNIQUE INDEX "IX_Wallets_UserId" ON "Wallets" ("UserId");
CREATE INDEX "IX_WalletTransactions_CreatedAt" ON "WalletTransactions" ("CreatedAt");
CREATE INDEX "IX_WalletTransactions_ReferenceId_ReferenceType" ON "WalletTransactions" ("ReferenceId", "ReferenceType");
CREATE INDEX "IX_WalletTransactions_WalletId" ON "WalletTransactions" ("WalletId");
CREATE TABLE IF NOT EXISTS "PromoCodes" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_PromoCodes" PRIMARY KEY,
    "Code" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "DiscountType" TEXT NOT NULL,
    "DiscountValue" TEXT NOT NULL,
    "MaxDiscountAmount" TEXT NULL,
    "MinOrderAmount" TEXT NULL,
    "ApplicableTo" TEXT NOT NULL,
    "MaxUsage" INTEGER NULL,
    "CurrentUsage" INTEGER NOT NULL,
    "MaxUsagePerUser" INTEGER NULL,
    "ValidFrom" TEXT NULL,
    "ValidTo" TEXT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS "SubscriptionPlans" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_SubscriptionPlans" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "PlanType" TEXT NOT NULL,
    "BillingCycle" TEXT NOT NULL,
    "Price" TEXT NOT NULL,
    "DiscountedPrice" TEXT NULL,
    "DeliveryQuota" INTEGER NOT NULL,
    "PerDeliveryDiscount" TEXT NULL,
    "PrioritySupport" INTEGER NOT NULL,
    "AdvancedAnalytics" INTEGER NOT NULL,
    "Features" TEXT NULL,
    "IsActive" INTEGER NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS "PromoCodeUsages" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_PromoCodeUsages" PRIMARY KEY,
    "PromoCodeId" TEXT NOT NULL,
    "UserId" TEXT NOT NULL,
    "UsedFor" TEXT NOT NULL,
    "ReferenceId" TEXT NULL,
    "DiscountApplied" TEXT NOT NULL,
    "UsedAt" TEXT NOT NULL,
    CONSTRAINT "FK_PromoCodeUsages_PromoCodes_PromoCodeId" FOREIGN KEY ("PromoCodeId") REFERENCES "PromoCodes" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PromoCodeUsages_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "UserSubscriptions" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_UserSubscriptions" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "PlanId" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "StartDate" TEXT NOT NULL,
    "EndDate" TEXT NOT NULL,
    "NextBillingDate" TEXT NULL,
    "AutoRenew" INTEGER NOT NULL,
    "DeliveriesUsed" INTEGER NOT NULL,
    "DeliveryQuota" INTEGER NOT NULL,
    "AmountPaid" TEXT NOT NULL,
    "PaymentMethod" TEXT NOT NULL,
    "CancellationReason" TEXT NULL,
    "CancelledAt" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_UserSubscriptions_SubscriptionPlans_PlanId" FOREIGN KEY ("PlanId") REFERENCES "SubscriptionPlans" ("Id"),
    CONSTRAINT "FK_UserSubscriptions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "SubscriptionInvoices" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_SubscriptionInvoices" PRIMARY KEY,
    "InvoiceNumber" TEXT NOT NULL,
    "SubscriptionId" TEXT NOT NULL,
    "UserId" TEXT NOT NULL,
    "BillingPeriod" TEXT NOT NULL,
    "Subtotal" TEXT NOT NULL,
    "Discount" TEXT NULL,
    "TaxAmount" TEXT NOT NULL,
    "TotalAmount" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "DueDate" TEXT NOT NULL,
    "PaidAt" TEXT NULL,
    "PaymentId" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_SubscriptionInvoices_UserSubscriptions_SubscriptionId" FOREIGN KEY ("SubscriptionId") REFERENCES "UserSubscriptions" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_SubscriptionInvoices_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id")
);
CREATE UNIQUE INDEX "IX_PromoCodes_Code" ON "PromoCodes" ("Code");
CREATE INDEX "IX_PromoCodes_IsActive" ON "PromoCodes" ("IsActive");
CREATE INDEX "IX_PromoCodeUsages_PromoCodeId_UserId" ON "PromoCodeUsages" ("PromoCodeId", "UserId");
CREATE INDEX "IX_PromoCodeUsages_UserId" ON "PromoCodeUsages" ("UserId");
CREATE UNIQUE INDEX "IX_SubscriptionInvoices_InvoiceNumber" ON "SubscriptionInvoices" ("InvoiceNumber");
CREATE INDEX "IX_SubscriptionInvoices_Status" ON "SubscriptionInvoices" ("Status");
CREATE INDEX "IX_SubscriptionInvoices_SubscriptionId" ON "SubscriptionInvoices" ("SubscriptionId");
CREATE INDEX "IX_SubscriptionInvoices_UserId" ON "SubscriptionInvoices" ("UserId");
CREATE INDEX "IX_SubscriptionPlans_PlanType_IsActive" ON "SubscriptionPlans" ("PlanType", "IsActive");
CREATE INDEX "IX_UserSubscriptions_EndDate" ON "UserSubscriptions" ("EndDate");
CREATE INDEX "IX_UserSubscriptions_PlanId" ON "UserSubscriptions" ("PlanId");
CREATE INDEX "IX_UserSubscriptions_Status" ON "UserSubscriptions" ("Status");
CREATE INDEX "IX_UserSubscriptions_UserId" ON "UserSubscriptions" ("UserId");
CREATE TABLE IF NOT EXISTS "Charities" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Charities" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "Category" TEXT NOT NULL,
    "LogoUrl" TEXT NULL,
    "WebsiteUrl" TEXT NULL,
    "RegistrationNumber" TEXT NOT NULL,
    "TotalReceived" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS "ReferralCodes" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_ReferralCodes" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "Code" TEXT NOT NULL,
    "ReferrerReward" TEXT NOT NULL,
    "RefereeReward" TEXT NOT NULL,
    "TotalReferrals" INTEGER NOT NULL,
    "SuccessfulReferrals" INTEGER NOT NULL,
    "TotalEarnings" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_ReferralCodes_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "Referrals" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Referrals" PRIMARY KEY,
    "ReferrerId" TEXT NOT NULL,
    "RefereeId" TEXT NOT NULL,
    "ReferralCode" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "ReferrerReward" TEXT NULL,
    "RefereeReward" TEXT NULL,
    "ReferrerRewarded" INTEGER NOT NULL,
    "RefereeRewarded" INTEGER NOT NULL,
    "CompletedAt" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Referrals_Users_RefereeId" FOREIGN KEY ("RefereeId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Referrals_Users_ReferrerId" FOREIGN KEY ("ReferrerId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);
CREATE TABLE IF NOT EXISTS "DonationPreferences" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_DonationPreferences" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "EnableRoundUp" INTEGER NOT NULL,
    "PreferredCharityId" TEXT NULL,
    "MonthlyLimit" TEXT NULL,
    "CurrentMonthTotal" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_DonationPreferences_Charities_PreferredCharityId" FOREIGN KEY ("PreferredCharityId") REFERENCES "Charities" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_DonationPreferences_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "Donations" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Donations" PRIMARY KEY,
    "DonationNumber" TEXT NOT NULL,
    "DonorId" TEXT NOT NULL,
    "CharityId" TEXT NULL,
    "CharityName" TEXT NOT NULL,
    "Amount" TEXT NOT NULL,
    "Source" TEXT NOT NULL,
    "DeliveryId" TEXT NULL,
    "IsAnonymous" INTEGER NOT NULL,
    "Message" TEXT NULL,
    "Status" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Donations_Charities_CharityId" FOREIGN KEY ("CharityId") REFERENCES "Charities" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Donations_Deliveries_DeliveryId" FOREIGN KEY ("DeliveryId") REFERENCES "Deliveries" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Donations_Users_DonorId" FOREIGN KEY ("DonorId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE INDEX "IX_Charities_IsActive" ON "Charities" ("IsActive");
CREATE UNIQUE INDEX "IX_Charities_RegistrationNumber" ON "Charities" ("RegistrationNumber");
CREATE INDEX "IX_DonationPreferences_PreferredCharityId" ON "DonationPreferences" ("PreferredCharityId");
CREATE UNIQUE INDEX "IX_DonationPreferences_UserId" ON "DonationPreferences" ("UserId");
CREATE INDEX "IX_Donations_CharityId" ON "Donations" ("CharityId");
CREATE INDEX "IX_Donations_DeliveryId" ON "Donations" ("DeliveryId");
CREATE UNIQUE INDEX "IX_Donations_DonationNumber" ON "Donations" ("DonationNumber");
CREATE INDEX "IX_Donations_DonorId" ON "Donations" ("DonorId");
CREATE UNIQUE INDEX "IX_ReferralCodes_Code" ON "ReferralCodes" ("Code");
CREATE INDEX "IX_ReferralCodes_UserId" ON "ReferralCodes" ("UserId");
CREATE INDEX "IX_Referrals_RefereeId" ON "Referrals" ("RefereeId");
CREATE INDEX "IX_Referrals_ReferralCode" ON "Referrals" ("ReferralCode");
CREATE INDEX "IX_Referrals_ReferrerId" ON "Referrals" ("ReferrerId");
CREATE TABLE IF NOT EXISTS "AdminAuditLogs" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AdminAuditLogs" PRIMARY KEY,
    "UserId" TEXT NULL,
    "Action" TEXT NOT NULL,
    "EntityType" TEXT NULL,
    "EntityId" TEXT NULL,
    "OldValue" TEXT NULL,
    "NewValue" TEXT NULL,
    "IpAddress" TEXT NULL,
    "UserAgent" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_AdminAuditLogs_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
);
CREATE TABLE IF NOT EXISTS "SystemConfigs" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_SystemConfigs" PRIMARY KEY,
    "Key" TEXT NOT NULL,
    "Value" TEXT NOT NULL,
    "Category" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "DataType" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    "UpdatedBy" TEXT NULL
);
CREATE INDEX "IX_AdminAuditLogs_Action" ON "AdminAuditLogs" ("Action");
CREATE INDEX "IX_AdminAuditLogs_CreatedAt" ON "AdminAuditLogs" ("CreatedAt");
CREATE INDEX "IX_AdminAuditLogs_UserId" ON "AdminAuditLogs" ("UserId");
CREATE INDEX "IX_SystemConfigs_Category" ON "SystemConfigs" ("Category");
CREATE UNIQUE INDEX "IX_SystemConfigs_Key" ON "SystemConfigs" ("Key");
