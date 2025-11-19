# Feature PRD: Registration & KYC (Know Your Customer)

**Feature ID:** F-02
**Version:** 1.0
**Priority:** P0 (Critical - MVP Core)
**Estimated Effort:** 4-5 weeks
**Dependencies:** F-01 (IAM)

---

## 1. Feature Overview

### Purpose
Enable secure and compliant onboarding of all user types (Delivery Partners, DPCMs, DBCs, End Consumers, Inspectors) with identity verification through Aadhaar eKYC, PAN validation, Police verification, and bank account verification to ensure trust and regulatory compliance.

### Success Criteria
- KYC completion rate > 85% for DP within 7 days
- Aadhaar verification success rate > 95%
- Zero duplicate registrations (same Aadhaar/PAN/Phone)
- Fraud detection rate < 0.1%
- Average KYC completion time < 10 minutes

### Business Value
- Regulatory compliance (DPDP Act, IT Act, PMLA)
- Trust building in the ecosystem
- Fraud prevention
- Enables safe financial transactions

---

## 2. User Stories

### US-KYC-001: DP Self-Registration
**As a** prospective Delivery Partner
**I want to** register and complete KYC through my mobile app
**So that** I can start accepting deliveries and earning income

**Acceptance Criteria:**
- Profile creation with basic info (name, DOB, photo)
- Phone verification via OTP
- Aadhaar eKYC integration (UIDAI or DigiLocker)
- PAN verification
- Bank account verification (micro-deposit or NPCI)
- Police verification consent
- Service area setup
- Pricing configuration (perKM, perKG, minCharge)
- Vehicle details and license upload
- Complete flow in < 15 minutes

### US-KYC-002: DPCM Bulk DP Onboarding
**As a** Delivery Partner Channel Manager
**I want to** onboard multiple DPs under my organization via CSV upload
**So that** I can quickly scale my delivery network

**Acceptance Criteria:**
- CSV template download
- Bulk upload with validation
- Automatic phone OTP to each DP for consent
- Track KYC status for each DP in dashboard
- Ability to assign service areas and pricing in bulk
- Automatic duplicate detection across phone/Aadhaar/PAN

### US-KYC-003: DBC Registration
**As a** Business owner
**I want to** register my business with GSTIN and bank details
**So that** I can create delivery orders and get GST invoices

**Acceptance Criteria:**
- Business name, contact person, email, phone
- GSTIN validation (optional but recommended)
- PAN verification
- Bank account for refunds/settlements
- API key generation for enterprise integration
- Subscription plan selection

### US-KYC-004: Aadhaar eKYC Integration
**As a** System Administrator
**I want** automated Aadhaar verification via UIDAI/DigiLocker
**So that** we ensure genuine identity verification at scale

**Acceptance Criteria:**
- Integration with UIDAI eKYC API or DigiLocker
- Fetch and store: Name, DOB, Gender, Address (masked Aadhaar)
- Store only Aadhaar reference ID, never full Aadhaar number
- Fallback to manual verification if API fails
- Compliance with UIDAI data storage guidelines

### US-KYC-005: Duplicate Detection
**As a** System Administrator
**I want** to prevent duplicate registrations
**So that** users cannot create multiple accounts to game the system

**Acceptance Criteria:**
- Check phone uniqueness across all roles
- Check Aadhaar hash uniqueness (one person, one DP account)
- Check PAN uniqueness within role
- Check bank account uniqueness for payout accounts
- Alert admin for suspicious patterns (same device, IP)

### US-KYC-006: Manual KYC Review Queue
**As a** KYC Admin
**I want** to manually review flagged KYC submissions
**So that** I can approve/reject cases where automated verification fails

**Acceptance Criteria:**
- Queue of pending KYC reviews
- View uploaded documents (Aadhaar, PAN, License, Bank proof)
- Approve/Reject with reason
- Request additional documents
- Notify user of status change
- Audit trail of all decisions

---

## 3. Detailed Requirements

### 3.1 Registration Fields by User Type

#### Delivery Partner (DP)
**Personal Information:**
- Full Name (as per Aadhaar)
- Phone Number (unique, verified via OTP)
- Email (optional)
- Date of Birth
- Gender
- Profile Photo
- Current Address (city, state, pincode)

**Identity Documents:**
- Aadhaar Number (for eKYC, stored as hash)
- PAN Number
- Driving License / Vehicle RC (if applicable)

**Financial:**
- Bank Account Number
- IFSC Code
- Account Holder Name
- Bank Verification (Penny drop or NPCI)

**Service Details:**
- Vehicle Type (Bicycle, Two-wheeler, Four-wheeler, On-foot)
- Service Area (center lat/lng + radius)
- Languages spoken
- Availability (24x7, Part-time, Weekends)

**Pricing:**
- Per KM rate
- Per KG rate
- Minimum charge
- Maximum delivery distance (km)

**Verification Consents:**
- Police verification consent (checkbox)
- Terms & Conditions
- Privacy Policy
- Independent contractor agreement

#### DPCM (Delivery Partner Channel Manager)
- Organization Name
- Contact Person Name, Phone, Email
- PAN (organization or proprietor)
- Aadhaar (of contact person)
- Bank Account (for settlements)
- Service Regions (multi-select: cities/states)
- Commission Rate (% or flat per delivery)
- Registration Certificate / Business Proof

#### DBC (Delivery Business Consumer)
- Business Name
- Contact Person Name, Phone, Email
- GSTIN (optional but validated if provided)
- PAN
- Business Address
- Bank Account
- Business Category (E-commerce, Food, Grocery, Pharma, etc.)
- Subscription Plan selection

#### End Consumer (EC)
- Name
- Phone (OTP verified)
- Email (optional)
- Saved Addresses (multiple)

#### Inspector
- Full Name
- Phone, Email
- Aadhaar eKYC
- PAN
- Police Clearance Certificate
- Bank Account
- Assigned Regions

### 3.2 KYC Verification Workflows

#### A. Aadhaar eKYC (UIDAI / DigiLocker)

**Option 1: UIDAI eKYC (Offline)**
```
Flow:
1. User enters Aadhaar number (last 4 digits shown)
2. System generates Aadhaar hash (SHA-256)
3. User uploads Aadhaar XML (downloaded from UIDAI portal) OR
4. System calls UIDAI eKYC API with user consent
5. Extract: Name, DOB, Gender, Address
6. Store only: AadhaarReferenceId (not full number)
7. Mark Aadhaar as verified
8. Delete XML file after extraction
```

**Option 2: DigiLocker Integration**
```
Flow:
1. User redirects to DigiLocker
2. User authorizes document sharing
3. DigiLocker returns signed Aadhaar data
4. System extracts and validates
5. Store reference ID
```

**Compliance:**
- NEVER store full 12-digit Aadhaar number
- Store only SHA-256 hash for duplicate detection
- Store UIDAI reference ID for audit
- Encrypt extracted PII (name, address) at rest

#### B. PAN Verification (NSDL/Income Tax)

```
API Integration:
1. User enters PAN (ABCDE1234F)
2. Call PAN verification API (NSDL or Income Tax Dept)
3. Response: Name, DOB, Status (Active/Inactive)
4. Cross-check Name with Aadhaar name (fuzzy match, 80% threshold)
5. If mismatch, flag for manual review
6. Store PAN with encrypted name
```

**Fallback:** Manual upload of PAN card image + OCR + manual review

#### C. Bank Account Verification

**Method 1: Penny Drop (Micro-deposit)**
```
Flow:
1. User enters Account Number, IFSC, Name
2. System deposits ₹1 to account via payment gateway API
3. Verify account holder name matches user name
4. If match, mark verified
5. Reverse ₹1 or keep for wallet credit
```

**Method 2: NPCI Account Validation API**
```
Flow:
1. Call NPCI Validation API with Account + IFSC
2. Get account status and name
3. Validate name match
4. Mark verified
```

#### D. Police Verification (Third-party or Government Portal)

**MVP Approach: Consent + Manual**
```
Flow:
1. User consents to police verification
2. System creates verification request with:
   - User details
   - Aadhaar reference
   - Address
3. Admin manually initiates verification via:
   - Local police station
   - Third-party verification agency (e.g., AuthBridge, SpringVerify)
4. Police clearance uploaded
5. Admin approves KYC
```

**Future: API Integration**
- Integrate with state-level police verification portals
- Automated background check APIs

#### E. Vehicle & License Verification (for DPs)

```
Flow:
1. DP uploads Driving License (front + back)
2. OCR extracts: License Number, Name, DOB, Validity
3. Cross-check with Aadhaar name and DOB
4. Optional: Validate license number via DigiLocker or Parivahan API
5. Upload Vehicle RC (Registration Certificate)
6. Extract: Vehicle Number, Owner Name, Type
7. Cross-verify owner name with DP name
```

### 3.3 Duplicate Detection Rules

**Rule 1: Phone Uniqueness**
- One phone number = One User ID across all roles
- Exception: Same phone can be DP and EC (different accounts)

**Rule 2: Aadhaar Uniqueness**
- One Aadhaar hash = One DP account
- One Aadhaar hash = One DPCM account
- Cannot be both DP and DPCM with same Aadhaar

**Rule 3: PAN Uniqueness**
- One PAN per role (can have same PAN for DP and DBC if proprietor)

**Rule 4: Bank Account Uniqueness**
- Same bank account can be used by max 1 DP (prevent payout fraud)
- Flag for review if same account used by multiple users

**Rule 5: Device Fingerprint**
- Track device ID during registration
- Flag if > 5 accounts created from same device in 24 hours

---

## 4. API Specifications

### 4.1 DP Registration - Initiate
```http
POST /api/v1/registration/dp/initiate
Content-Type: application/json

Request:
{
  "phone": "9876543210",
  "referralCode": "DPCM12345" // Optional: if onboarded by DPCM
}

Response (200):
{
  "userId": "user-uuid",
  "status": "OTP_SENT",
  "message": "OTP sent to +91-9876543210"
}

Response (409 - Conflict):
{
  "code": "PHONE_EXISTS",
  "message": "This phone number is already registered as DP"
}
```

### 4.2 DP Registration - Complete Profile
```http
POST /api/v1/registration/dp/profile
Authorization: Bearer <access-token>
Content-Type: application/json

Request:
{
  "fullName": "Ravi Kumar",
  "email": "ravi@example.com",
  "dob": "1995-05-15",
  "gender": "Male",
  "profilePhotoUrl": "https://storage.../photo.jpg",
  "address": {
    "line1": "123 Main St",
    "city": "Jaipur",
    "state": "Rajasthan",
    "pincode": "302001"
  },
  "vehicleType": "Two-wheeler",
  "languages": ["Hindi", "English"],
  "availability": "Part-time",
  "serviceArea": {
    "centerLat": 26.9124,
    "centerLng": 75.7873,
    "radiusKm": 5
  },
  "pricing": {
    "perKmRate": 10,
    "perKgRate": 5,
    "minCharge": 30,
    "maxDistanceKm": 20
  }
}

Response (200):
{
  "userId": "user-uuid",
  "status": "PROFILE_COMPLETED",
  "nextStep": "KYC_VERIFICATION"
}
```

### 4.3 Aadhaar eKYC - Initiate
```http
POST /api/v1/kyc/aadhaar/initiate
Authorization: Bearer <access-token>
Content-Type: application/json

Request (Option 1: Manual Upload):
{
  "userId": "user-uuid",
  "aadhaarLast4": "1234",
  "method": "MANUAL_UPLOAD",
  "documentUrl": "https://storage.../aadhaar-masked.jpg"
}

Request (Option 2: DigiLocker):
{
  "userId": "user-uuid",
  "method": "DIGILOCKER",
  "redirectUrl": "https://app.deliverx.com/kyc/callback"
}

Response (200 - Manual):
{
  "kycId": "kyc-uuid",
  "status": "PENDING_REVIEW",
  "message": "Document uploaded. Manual verification in progress."
}

Response (200 - DigiLocker):
{
  "kycId": "kyc-uuid",
  "digilockerAuthUrl": "https://digilocker.gov.in/...",
  "message": "Redirect user to DigiLocker for authorization"
}
```

### 4.4 Aadhaar eKYC - Verify (Callback)
```http
POST /api/v1/kyc/aadhaar/verify
Content-Type: application/json

Request (from DigiLocker callback):
{
  "userId": "user-uuid",
  "kycId": "kyc-uuid",
  "digilockerToken": "encrypted-token",
  "aadhaarXml": "base64-encoded-xml"
}

Response (200):
{
  "kycId": "kyc-uuid",
  "status": "VERIFIED",
  "extractedData": {
    "name": "Ravi Kumar",
    "dob": "1995-05-15",
    "gender": "Male",
    "address": "*** *** *** Jaipur, Rajasthan - 302001" // Masked
  },
  "aadhaarReferenceId": "ref-12345",
  "verifiedAt": "2025-11-14T10:30:00Z"
}

Response (409 - Duplicate):
{
  "code": "AADHAAR_ALREADY_USED",
  "message": "This Aadhaar is already registered in the system"
}
```

### 4.5 PAN Verification
```http
POST /api/v1/kyc/pan/verify
Authorization: Bearer <access-token>
Content-Type: application/json

Request:
{
  "userId": "user-uuid",
  "pan": "ABCDE1234F",
  "nameAsPerPan": "Ravi Kumar" // Optional, for cross-check
}

Response (200):
{
  "kycId": "kyc-uuid",
  "status": "VERIFIED",
  "panDetails": {
    "pan": "ABCDE1234F",
    "name": "RAVI KUMAR",
    "dob": "15/05/1995",
    "status": "ACTIVE"
  },
  "nameMatchScore": 95, // % match with Aadhaar name
  "verifiedAt": "2025-11-14T10:35:00Z"
}

Response (400 - Invalid PAN):
{
  "code": "INVALID_PAN",
  "message": "PAN is invalid or inactive"
}

Response (422 - Name Mismatch):
{
  "code": "NAME_MISMATCH",
  "message": "Name on PAN does not match Aadhaar. Manual review required.",
  "nameMatchScore": 45
}
```

### 4.6 Bank Verification - Penny Drop
```http
POST /api/v1/kyc/bank/verify
Authorization: Bearer <access-token>
Content-Type: application/json

Request:
{
  "userId": "user-uuid",
  "accountNumber": "1234567890",
  "ifscCode": "SBIN0001234",
  "accountHolderName": "Ravi Kumar",
  "method": "PENNY_DROP" // or "NPCI"
}

Response (200):
{
  "kycId": "kyc-uuid",
  "status": "VERIFICATION_INITIATED",
  "message": "₹1 deposited to your account. Verification in progress.",
  "transactionId": "txn-uuid",
  "expectedTime": "2-5 minutes"
}
```

### 4.7 Bank Verification - Confirm
```http
GET /api/v1/kyc/bank/{kycId}/status
Authorization: Bearer <access-token>

Response (200 - Success):
{
  "kycId": "kyc-uuid",
  "status": "VERIFIED",
  "bankDetails": {
    "accountNumber": "******7890",
    "ifsc": "SBIN0001234",
    "accountHolderName": "RAVI KUMAR",
    "bankName": "State Bank of India"
  },
  "nameMatchScore": 90,
  "verifiedAt": "2025-11-14T10:40:00Z"
}
```

### 4.8 Police Verification - Initiate
```http
POST /api/v1/kyc/police/initiate
Authorization: Bearer <access-token>
Content-Type: application/json

Request:
{
  "userId": "user-uuid",
  "consent": true,
  "addressForVerification": {
    "line1": "123 Main St",
    "city": "Jaipur",
    "state": "Rajasthan",
    "pincode": "302001"
  }
}

Response (200):
{
  "kycId": "kyc-uuid",
  "status": "PENDING_VERIFICATION",
  "message": "Police verification request created. Typically takes 7-15 days.",
  "estimatedCompletionDays": 15
}
```

### 4.9 DPCM Bulk DP Upload
```http
POST /api/v1/registration/dpcm/{dpcmId}/bulk-upload
Authorization: Bearer <access-token>
Content-Type: multipart/form-data

Request:
{
  "file": <CSV file>,
  "autoSendOtp": true,
  "defaultServiceArea": {
    "centerLat": 26.9124,
    "centerLng": 75.7873,
    "radiusKm": 10
  },
  "defaultPricing": {
    "perKmRate": 12,
    "perKgRate": 6,
    "minCharge": 35
  }
}

CSV Format:
fullName,phone,email,dob,vehicleType,aadhaarLast4,pan
Ravi Kumar,9876543210,ravi@example.com,1995-05-15,Two-wheeler,1234,ABCDE1234F

Response (200):
{
  "batchId": "batch-uuid",
  "totalRecords": 150,
  "successCount": 145,
  "failureCount": 5,
  "failures": [
    {
      "row": 23,
      "phone": "9876543210",
      "error": "Phone already registered"
    }
  ],
  "status": "PROCESSING",
  "message": "OTP sent to all valid phone numbers. DPs need to verify OTP to proceed."
}
```

### 4.10 KYC Status Check
```http
GET /api/v1/kyc/{userId}/status
Authorization: Bearer <access-token>

Response (200):
{
  "userId": "user-uuid",
  "overallStatus": "PARTIALLY_VERIFIED", // PENDING, PARTIALLY_VERIFIED, FULLY_VERIFIED, REJECTED
  "verifications": {
    "phone": {
      "status": "VERIFIED",
      "verifiedAt": "2025-11-14T09:00:00Z"
    },
    "aadhaar": {
      "status": "VERIFIED",
      "verifiedAt": "2025-11-14T10:30:00Z",
      "referenceId": "ref-12345"
    },
    "pan": {
      "status": "VERIFIED",
      "verifiedAt": "2025-11-14T10:35:00Z"
    },
    "bank": {
      "status": "VERIFIED",
      "verifiedAt": "2025-11-14T10:40:00Z"
    },
    "police": {
      "status": "PENDING",
      "initiatedAt": "2025-11-14T10:45:00Z",
      "estimatedCompletionDate": "2025-11-29"
    }
  },
  "canActivate": true, // If all mandatory verifications done
  "pendingVerifications": ["police"],
  "nextStep": "Wait for police verification or start accepting deliveries (if allowed)"
}
```

---

## 5. Database Schema

```sql
-- KYC Requests (master table for all verification types)
CREATE TABLE KYCRequests (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Users(Id),
    VerificationType NVARCHAR(50) NOT NULL, -- AADHAAR, PAN, BANK, POLICE, VEHICLE_LICENSE
    Status NVARCHAR(50) NOT NULL DEFAULT 'PENDING', -- PENDING, IN_PROGRESS, VERIFIED, REJECTED, EXPIRED
    Method NVARCHAR(50), -- MANUAL_UPLOAD, DIGILOCKER, API, PENNY_DROP
    RequestData NVARCHAR(MAX), -- JSON: submitted data
    ResponseData NVARCHAR(MAX), -- JSON: verification response
    DocumentUrls NVARCHAR(MAX), -- JSON array of uploaded documents
    VerifiedBy UNIQUEIDENTIFIER NULL FOREIGN KEY REFERENCES Users(Id), -- Admin who verified manually
    RejectionReason NVARCHAR(500) NULL,
    InitiatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CompletedAt DATETIME2 NULL,
    ExpiresAt DATETIME2 NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),

    INDEX IX_KYCRequests_UserId (UserId),
    INDEX IX_KYCRequests_Status (Status),
    INDEX IX_KYCRequests_Type_Status (VerificationType, Status)
);

-- Aadhaar Verification Details
CREATE TABLE AadhaarVerifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL UNIQUE FOREIGN KEY REFERENCES Users(Id),
    AadhaarHash NVARCHAR(64) NOT NULL UNIQUE, -- SHA-256 hash of Aadhaar number
    AadhaarReferenceId NVARCHAR(255), -- UIDAI reference ID
    NameAsPerAadhaar NVARCHAR(255) NOT NULL, -- Encrypted
    DOB DATE NOT NULL,
    Gender NVARCHAR(10),
    AddressEncrypted NVARCHAR(MAX), -- Encrypted full address
    VerificationMethod NVARCHAR(50), -- DIGILOCKER, MANUAL, OFFLINE_EKYC
    VerifiedAt DATETIME2 NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),

    INDEX IX_AadhaarVerifications_Hash (AadhaarHash)
);

-- PAN Verification Details
CREATE TABLE PANVerifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Users(Id),
    PAN NVARCHAR(10) NOT NULL,
    NameAsPerPAN NVARCHAR(255) NOT NULL, -- Encrypted
    DOB DATE NULL,
    PANStatus NVARCHAR(20), -- ACTIVE, INACTIVE
    NameMatchScore INT, -- % match with Aadhaar name
    VerifiedAt DATETIME2 NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),

    INDEX IX_PANVerifications_PAN (PAN),
    INDEX IX_PANVerifications_UserId (UserId)
);

-- Bank Verification Details
CREATE TABLE BankVerifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Users(Id),
    AccountNumberEncrypted NVARCHAR(255) NOT NULL, -- Encrypted
    AccountNumberHash NVARCHAR(64) NOT NULL, -- For duplicate detection
    IFSCCode NVARCHAR(11) NOT NULL,
    AccountHolderName NVARCHAR(255) NOT NULL, -- Encrypted
    BankName NVARCHAR(255),
    BranchName NVARCHAR(255),
    VerificationMethod NVARCHAR(50), -- PENNY_DROP, NPCI
    TransactionId NVARCHAR(255), -- Penny drop transaction ID
    NameMatchScore INT,
    VerifiedAt DATETIME2 NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),

    INDEX IX_BankVerifications_UserId (UserId),
    INDEX IX_BankVerifications_AccountHash (AccountNumberHash)
);

-- Police Verification Details
CREATE TABLE PoliceVerifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Users(Id),
    VerificationAgency NVARCHAR(255), -- Local police / Third-party agency name
    AddressForVerification NVARCHAR(MAX), -- JSON
    RequestDocumentUrl NVARCHAR(500),
    ClearanceDocumentUrl NVARCHAR(500),
    Status NVARCHAR(50) DEFAULT 'PENDING', -- PENDING, IN_PROGRESS, CLEARED, REJECTED
    InitiatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CompletedAt DATETIME2 NULL,
    Remarks NVARCHAR(MAX),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),

    INDEX IX_PoliceVerifications_UserId (UserId),
    INDEX IX_PoliceVerifications_Status (Status)
);

-- Vehicle & License Verification (for DPs)
CREATE TABLE VehicleLicenseVerifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Users(Id),
    LicenseNumber NVARCHAR(20),
    LicenseDocumentUrl NVARCHAR(500),
    LicenseValidUpto DATE,
    VehicleNumber NVARCHAR(20),
    VehicleRCDocumentUrl NVARCHAR(500),
    VehicleType NVARCHAR(50), -- Two-wheeler, Four-wheeler, etc.
    VehicleOwnerName NVARCHAR(255),
    VerifiedAt DATETIME2 NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),

    INDEX IX_VehicleLicense_UserId (UserId)
);

-- Delivery Partner Profiles (extends Users table)
CREATE TABLE DeliveryPartnerProfiles (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL UNIQUE FOREIGN KEY REFERENCES Users(Id),
    DPCMId UNIQUEIDENTIFIER NULL FOREIGN KEY REFERENCES DPCManagers(Id), -- If onboarded by DPCM
    FullName NVARCHAR(255) NOT NULL,
    ProfilePhotoUrl NVARCHAR(500),
    DOB DATE NOT NULL,
    Gender NVARCHAR(10),
    Address NVARCHAR(MAX), -- JSON
    VehicleType NVARCHAR(50),
    Languages NVARCHAR(MAX), -- JSON array
    Availability NVARCHAR(50), -- 24x7, Part-time, Weekends
    ServiceAreaCenterLat DECIMAL(10, 8),
    ServiceAreaCenterLng DECIMAL(11, 8),
    ServiceAreaRadiusKm DECIMAL(5, 2),
    PerKmRate DECIMAL(10, 2),
    PerKgRate DECIMAL(10, 2),
    MinCharge DECIMAL(10, 2),
    MaxDistanceKm DECIMAL(5, 2),
    IsActive BIT DEFAULT 0, -- Activated only after KYC
    ActivatedAt DATETIME2 NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),

    INDEX IX_DPProfiles_DPCMId (DPCMId),
    INDEX IX_DPProfiles_IsActive (IsActive)
);

-- DPCM Profiles
CREATE TABLE DPCManagers (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL UNIQUE FOREIGN KEY REFERENCES Users(Id),
    OrganizationName NVARCHAR(255) NOT NULL,
    ContactPersonName NVARCHAR(255) NOT NULL,
    PAN NVARCHAR(10) NOT NULL,
    RegistrationCertificateUrl NVARCHAR(500),
    ServiceRegions NVARCHAR(MAX), -- JSON array: ["Jaipur", "Delhi"]
    CommissionType NVARCHAR(20), -- PERCENTAGE, FLAT
    CommissionValue DECIMAL(10, 2),
    BankAccountEncrypted NVARCHAR(MAX), -- JSON, encrypted
    IsActive BIT DEFAULT 0,
    ActivatedAt DATETIME2 NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- DBC Profiles
CREATE TABLE BusinessConsumerProfiles (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL UNIQUE FOREIGN KEY REFERENCES Users(Id),
    BusinessName NVARCHAR(255) NOT NULL,
    ContactPersonName NVARCHAR(255) NOT NULL,
    GSTIN NVARCHAR(15) NULL,
    PAN NVARCHAR(10) NOT NULL,
    BusinessCategory NVARCHAR(100), -- E-commerce, Food, Pharma, etc.
    BusinessAddress NVARCHAR(MAX), -- JSON
    BankAccountEncrypted NVARCHAR(MAX), -- JSON, encrypted
    SubscriptionPlanId UNIQUEIDENTIFIER NULL,
    IsActive BIT DEFAULT 0,
    ActivatedAt DATETIME2 NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Bulk Upload Batches (for DPCM)
CREATE TABLE BulkUploadBatches (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DPCMId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES DPCManagers(Id),
    UploadedBy UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Users(Id),
    FileName NVARCHAR(255),
    FileUrl NVARCHAR(500),
    TotalRecords INT NOT NULL,
    SuccessCount INT DEFAULT 0,
    FailureCount INT DEFAULT 0,
    Status NVARCHAR(50) DEFAULT 'PROCESSING', -- PROCESSING, COMPLETED, FAILED
    ErrorLog NVARCHAR(MAX), -- JSON array of errors
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CompletedAt DATETIME2 NULL,

    INDEX IX_BulkUpload_DPCMId (DPCMId),
    INDEX IX_BulkUpload_Status (Status)
);
```

---

## 6. Development Implementation Guide

### 6.1 Technology Stack
- **Backend:** ASP.NET Core 8.0 Web API
- **KYC Integrations:**
  - Aadhaar: DigiLocker API / NSDL eKYC
  - PAN: NSDL PAN API / Income Tax e-Filing API
  - Bank: Razorpay Fund Account Validation / Cashfree Verification API
  - Police: Manual (MVP) / Third-party APIs (AuthBridge, SpringVerify)
- **Document Storage:** Azure Blob Storage / AWS S3
- **OCR:** Azure Computer Vision / Tesseract
- **Encryption:** AES-256 (PII), Azure Key Vault (keys)
- **Queue:** Azure Service Bus (async verification)

### 6.2 Project Structure
```
DeliverX.KYC/
├── Controllers/
│   ├── RegistrationController.cs
│   ├── KYCController.cs
│   └── AdminKYCController.cs
├── Services/
│   ├── Registration/
│   │   ├── DPRegistrationService.cs
│   │   ├── DPCMRegistrationService.cs
│   │   └── DBCRegistrationService.cs
│   ├── KYC/
│   │   ├── AadhaarVerificationService.cs
│   │   ├── PANVerificationService.cs
│   │   ├── BankVerificationService.cs
│   │   ├── PoliceVerificationService.cs
│   │   └── DuplicateDetectionService.cs
│   ├── Integrations/
│   │   ├── DigiLockerClient.cs
│   │   ├── NSDLPANClient.cs
│   │   ├── RazorpayVerificationClient.cs
│   │   └── OCRService.cs
│   └── BulkUploadService.cs
├── Models/
│   ├── DTOs/
│   ├── Entities/
│   └── Enums/
├── Validators/
│   └── KYCValidators.cs
├── BackgroundJobs/
│   ├── KYCVerificationWorker.cs
│   └── PoliceVerificationWorker.cs
└── Utilities/
    ├── EncryptionHelper.cs
    ├── HashHelper.cs
    └── NameMatchHelper.cs
```

### 6.3 Core Service Implementation

#### DuplicateDetectionService.cs
```csharp
public class DuplicateDetectionService : IDuplicateDetectionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DuplicateDetectionService> _logger;

    public async Task<DuplicateCheckResult> CheckDuplicatesAsync(
        string phone,
        string aadhaarHash,
        string pan,
        string bankAccountHash,
        CancellationToken ct)
    {
        var result = new DuplicateCheckResult();

        // Check phone
        var phoneExists = await _context.Users
            .AnyAsync(u => u.Phone == phone, ct);
        if (phoneExists)
        {
            result.IsDuplicate = true;
            result.DuplicateFields.Add("Phone");
            result.ExistingUserId = await _context.Users
                .Where(u => u.Phone == phone)
                .Select(u => u.Id)
                .FirstOrDefaultAsync(ct);
        }

        // Check Aadhaar
        if (!string.IsNullOrEmpty(aadhaarHash))
        {
            var aadhaarExists = await _context.AadhaarVerifications
                .AnyAsync(a => a.AadhaarHash == aadhaarHash, ct);
            if (aadhaarExists)
            {
                result.IsDuplicate = true;
                result.DuplicateFields.Add("Aadhaar");
            }
        }

        // Check PAN (allow same PAN for different roles)
        if (!string.IsNullOrEmpty(pan))
        {
            var panExists = await _context.PANVerifications
                .Include(p => p.User)
                .AnyAsync(p => p.PAN == pan && p.User.Role == "DP", ct);
            if (panExists)
            {
                result.IsDuplicate = true;
                result.DuplicateFields.Add("PAN");
            }
        }

        // Check bank account (max 1 DP per account)
        if (!string.IsNullOrEmpty(bankAccountHash))
        {
            var bankExists = await _context.BankVerifications
                .Include(b => b.User)
                .CountAsync(b => b.AccountNumberHash == bankAccountHash && b.User.Role == "DP", ct);
            if (bankExists >= 1)
            {
                result.Warnings.Add("Bank account already used by another DP. Flagged for review.");
            }
        }

        return result;
    }
}
```

#### AadhaarVerificationService.cs
```csharp
public class AadhaarVerificationService : IAadhaarVerificationService
{
    private readonly IDigiLockerClient _digiLockerClient;
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionHelper _encryption;
    private readonly IDuplicateDetectionService _duplicateDetection;

    public async Task<VerificationResult> InitiateDigiLockerVerificationAsync(
        Guid userId,
        string redirectUrl,
        CancellationToken ct)
    {
        // 1. Generate DigiLocker auth URL
        var authUrl = await _digiLockerClient.GetAuthorizationUrlAsync(
            userId.ToString(),
            redirectUrl);

        // 2. Create KYC request
        var kycRequest = new KYCRequest
        {
            UserId = userId,
            VerificationType = "AADHAAR",
            Method = "DIGILOCKER",
            Status = "IN_PROGRESS"
        };
        _context.KYCRequests.Add(kycRequest);
        await _context.SaveChangesAsync(ct);

        return new VerificationResult
        {
            IsSuccess = true,
            KYCId = kycRequest.Id,
            RedirectUrl = authUrl
        };
    }

    public async Task<VerificationResult> CompleteDigiLockerVerificationAsync(
        Guid userId,
        string digilockerCode,
        CancellationToken ct)
    {
        // 1. Exchange code for Aadhaar data
        var aadhaarData = await _digiLockerClient.GetAadhaarDataAsync(digilockerCode);

        // 2. Generate Aadhaar hash
        var aadhaarHash = HashHelper.SHA256(aadhaarData.AadhaarNumber);

        // 3. Check duplicates
        var duplicateCheck = await _duplicateDetection.CheckDuplicatesAsync(
            phone: null,
            aadhaarHash: aadhaarHash,
            pan: null,
            bankAccountHash: null,
            ct);

        if (duplicateCheck.IsDuplicate && duplicateCheck.DuplicateFields.Contains("Aadhaar"))
        {
            return new VerificationResult
            {
                IsSuccess = false,
                ErrorCode = "AADHAAR_ALREADY_USED",
                ErrorMessage = "This Aadhaar is already registered"
            };
        }

        // 4. Store verification data (encrypted)
        var verification = new AadhaarVerification
        {
            UserId = userId,
            AadhaarHash = aadhaarHash,
            AadhaarReferenceId = aadhaarData.ReferenceId,
            NameAsPerAadhaar = _encryption.Encrypt(aadhaarData.Name),
            DOB = aadhaarData.DOB,
            Gender = aadhaarData.Gender,
            AddressEncrypted = _encryption.Encrypt(JsonSerializer.Serialize(aadhaarData.Address)),
            VerificationMethod = "DIGILOCKER",
            VerifiedAt = DateTime.UtcNow
        };
        _context.AadhaarVerifications.Add(verification);

        // 5. Update KYC request status
        var kycRequest = await _context.KYCRequests
            .FirstOrDefaultAsync(k => k.UserId == userId && k.VerificationType == "AADHAAR", ct);
        if (kycRequest != null)
        {
            kycRequest.Status = "VERIFIED";
            kycRequest.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);

        // 6. Check if all KYC complete → activate DP
        await CheckAndActivateDPAsync(userId, ct);

        return new VerificationResult
        {
            IsSuccess = true,
            VerifiedData = new
            {
                Name = aadhaarData.Name,
                DOB = aadhaarData.DOB,
                Gender = aadhaarData.Gender
            }
        };
    }

    private async Task CheckAndActivateDPAsync(Guid userId, CancellationToken ct)
    {
        var allVerifications = await _context.KYCRequests
            .Where(k => k.UserId == userId)
            .ToListAsync(ct);

        // Mandatory: Aadhaar, PAN, Bank
        var mandatoryVerified = allVerifications
            .Where(k => new[] { "AADHAAR", "PAN", "BANK" }.Contains(k.VerificationType))
            .All(k => k.Status == "VERIFIED");

        if (mandatoryVerified)
        {
            var dpProfile = await _context.DeliveryPartnerProfiles
                .FirstOrDefaultAsync(dp => dp.UserId == userId, ct);

            if (dpProfile != null && !dpProfile.IsActive)
            {
                dpProfile.IsActive = true;
                dpProfile.ActivatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);

                // Send activation notification
                await _notificationService.SendDPActivationNotificationAsync(userId);
            }
        }
    }
}
```

#### PANVerificationService.cs
```csharp
public class PANVerificationService : IPANVerificationService
{
    private readonly INSDLPANClient _nsdlClient;
    private readonly ApplicationDbContext _context;
    private readonly INameMatchHelper _nameMatch;

    public async Task<VerificationResult> VerifyPANAsync(
        Guid userId,
        string pan,
        CancellationToken ct)
    {
        // 1. Validate PAN format
        if (!Regex.IsMatch(pan, @"^[A-Z]{5}[0-9]{4}[A-Z]{1}$"))
        {
            return VerificationResult.Failure("INVALID_PAN_FORMAT", "Invalid PAN format");
        }

        // 2. Call NSDL API
        var panDetails = await _nsdlClient.VerifyPANAsync(pan);
        if (panDetails == null || panDetails.Status != "ACTIVE")
        {
            return VerificationResult.Failure("INVALID_PAN", "PAN is invalid or inactive");
        }

        // 3. Get Aadhaar name for cross-check
        var aadhaarVerification = await _context.AadhaarVerifications
            .FirstOrDefaultAsync(a => a.UserId == userId, ct);

        int nameMatchScore = 0;
        if (aadhaarVerification != null)
        {
            var aadhaarName = _encryption.Decrypt(aadhaarVerification.NameAsPerAadhaar);
            nameMatchScore = _nameMatch.CalculateSimilarity(aadhaarName, panDetails.Name);

            if (nameMatchScore < 70) // Threshold
            {
                // Flag for manual review but don't reject
                await _auditService.LogAsync("PAN_NAME_MISMATCH",
                    userId: userId,
                    details: $"Aadhaar: {aadhaarName}, PAN: {panDetails.Name}, Score: {nameMatchScore}");
            }
        }

        // 4. Store verification
        var verification = new PANVerification
        {
            UserId = userId,
            PAN = pan,
            NameAsPerPAN = _encryption.Encrypt(panDetails.Name),
            DOB = panDetails.DOB,
            PANStatus = panDetails.Status,
            NameMatchScore = nameMatchScore,
            VerifiedAt = DateTime.UtcNow
        };
        _context.PANVerifications.Add(verification);

        // 5. Update KYC request
        var kycRequest = await _context.KYCRequests
            .FirstOrDefaultAsync(k => k.UserId == userId && k.VerificationType == "PAN", ct);
        if (kycRequest == null)
        {
            kycRequest = new KYCRequest
            {
                UserId = userId,
                VerificationType = "PAN",
                Method = "API"
            };
            _context.KYCRequests.Add(kycRequest);
        }
        kycRequest.Status = "VERIFIED";
        kycRequest.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return VerificationResult.Success(new
        {
            PAN = pan,
            Name = panDetails.Name,
            NameMatchScore = nameMatchScore
        });
    }
}
```

#### BulkUploadService.cs
```csharp
public class BulkUploadService : IBulkUploadService
{
    private readonly ApplicationDbContext _context;
    private readonly IDPRegistrationService _dpRegistration;
    private readonly IOtpService _otpService;

    public async Task<BulkUploadResult> ProcessBulkUploadAsync(
        Guid dpcmId,
        IFormFile csvFile,
        BulkUploadOptions options,
        CancellationToken ct)
    {
        // 1. Create batch record
        var batch = new BulkUploadBatch
        {
            DPCMId = dpcmId,
            UploadedBy = dpcmId,
            FileName = csvFile.FileName,
            Status = "PROCESSING"
        };
        _context.BulkUploadBatches.Add(batch);
        await _context.SaveChangesAsync(ct);

        // 2. Parse CSV
        var records = new List<DPBulkRecord>();
        using (var reader = new StreamReader(csvFile.OpenReadStream()))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            records = csv.GetRecords<DPBulkRecord>().ToList();
        }

        batch.TotalRecords = records.Count;
        await _context.SaveChangesAsync(ct);

        // 3. Process each record
        var successCount = 0;
        var failures = new List<BulkUploadError>();

        foreach (var (record, index) in records.Select((r, i) => (r, i)))
        {
            try
            {
                // Validate
                if (string.IsNullOrEmpty(record.Phone) || !PhoneValidator.IsValid(record.Phone))
                {
                    failures.Add(new BulkUploadError
                    {
                        Row = index + 2, // +2 for header + 0-index
                        Phone = record.Phone,
                        Error = "Invalid phone number"
                    });
                    continue;
                }

                // Check duplicate
                var duplicateCheck = await _duplicateDetection.CheckDuplicatesAsync(
                    record.Phone, null, null, null, ct);
                if (duplicateCheck.IsDuplicate)
                {
                    failures.Add(new BulkUploadError
                    {
                        Row = index + 2,
                        Phone = record.Phone,
                        Error = $"Duplicate: {string.Join(", ", duplicateCheck.DuplicateFields)}"
                    });
                    continue;
                }

                // Create user
                var user = new User
                {
                    Phone = record.Phone,
                    Email = record.Email,
                    Role = "DP",
                    IsPhoneVerified = false // Will verify via OTP
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync(ct);

                // Create DP profile
                var dpProfile = new DeliveryPartnerProfile
                {
                    UserId = user.Id,
                    DPCMId = dpcmId,
                    FullName = record.FullName,
                    DOB = record.DOB,
                    VehicleType = record.VehicleType,
                    ServiceAreaCenterLat = options.DefaultServiceArea.CenterLat,
                    ServiceAreaCenterLng = options.DefaultServiceArea.CenterLng,
                    ServiceAreaRadiusKm = options.DefaultServiceArea.RadiusKm,
                    PerKmRate = options.DefaultPricing.PerKmRate,
                    PerKgRate = options.DefaultPricing.PerKgRate,
                    MinCharge = options.DefaultPricing.MinCharge,
                    IsActive = false
                };
                _context.DeliveryPartnerProfiles.Add(dpProfile);
                await _context.SaveChangesAsync(ct);

                // Send OTP for verification
                if (options.AutoSendOtp)
                {
                    await _otpService.SendOtpAsync(record.Phone, ct);
                }

                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing row {Row}", index + 2);
                failures.Add(new BulkUploadError
                {
                    Row = index + 2,
                    Phone = record.Phone,
                    Error = ex.Message
                });
            }
        }

        // 4. Update batch
        batch.SuccessCount = successCount;
        batch.FailureCount = failures.Count;
        batch.Status = "COMPLETED";
        batch.ErrorLog = JsonSerializer.Serialize(failures);
        batch.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return new BulkUploadResult
        {
            BatchId = batch.Id,
            TotalRecords = batch.TotalRecords,
            SuccessCount = successCount,
            FailureCount = failures.Count,
            Failures = failures
        };
    }
}
```

---

## 7. Testing Strategy

### 7.1 Unit Tests
```csharp
[Fact]
public async Task DuplicateDetection_PhoneExists_ReturnsDuplicate()
{
    // Arrange
    var service = CreateDuplicateDetectionService();
    await SeedUserWithPhone("9876543210");

    // Act
    var result = await service.CheckDuplicatesAsync("9876543210", null, null, null, CancellationToken.None);

    // Assert
    Assert.True(result.IsDuplicate);
    Assert.Contains("Phone", result.DuplicateFields);
}

[Fact]
public async Task AadhaarVerification_ValidData_StoresEncrypted()
{
    // Test implementation
}

[Fact]
public async Task NameMatch_SimilarNames_ReturnsHighScore()
{
    var helper = new NameMatchHelper();
    var score = helper.CalculateSimilarity("RAVI KUMAR", "Ravi Kumar");
    Assert.True(score >= 90);
}
```

### 7.2 Integration Tests
- End-to-end DP registration flow
- DigiLocker integration (mock)
- PAN API integration (mock)
- Bulk upload with CSV
- Duplicate detection across all fields

### 7.3 Security Tests
- Verify Aadhaar number is never stored in plain text
- Verify PII is encrypted at rest
- Test for SQL injection in search fields
- Test file upload restrictions (CSV only, size limits)

### 7.4 Load Tests
- 1000 concurrent registrations
- 10,000 duplicate checks per minute
- Bulk upload with 10,000 records

---

## 8. Compliance & Security

### 8.1 UIDAI Compliance
- Never store 12-digit Aadhaar number
- Store only SHA-256 hash + reference ID
- Encrypt extracted name and address
- Delete Aadhaar XML/image after extraction
- Audit all Aadhaar access

### 8.2 Data Retention
- KYC documents: 7 years (as per PMLA)
- Failed KYC attempts: 90 days
- Audit logs: 5 years

### 8.3 Encryption
- PII fields: AES-256-GCM
- Keys stored in Azure Key Vault
- Rotate keys annually

---

## 9. Monitoring & Metrics

### Key Metrics:
- KYC completion rate (%)
- Aadhaar verification success rate (target: >95%)
- PAN verification success rate (target: >98%)
- Bank verification success rate (target: >90%)
- Average KYC completion time (target: <10 min)
- Manual review queue length
- Duplicate detection rate

### Alerts:
- Spike in KYC failures (API down)
- High duplicate attempts (fraud)
- Manual review queue > 100
- Encryption key access anomalies

---

## 10. Deployment Checklist

- [ ] DigiLocker integration configured
- [ ] NSDL PAN API credentials set
- [ ] Bank verification API (Razorpay/Cashfree) configured
- [ ] Azure Key Vault for encryption keys
- [ ] Azure Blob Storage for documents
- [ ] Background workers for async verification
- [ ] Admin KYC review dashboard deployed
- [ ] CSV bulk upload template published
- [ ] Data retention policies configured
- [ ] Security audit completed
- [ ] UIDAI compliance verified

---

## 11. Future Enhancements

- Video KYC for high-risk cases
- AI-based document fraud detection
- Automated police verification API integration
- Real-time face match (photo vs Aadhaar photo)
- Blockchain-based KYC sharing (with consent)
- Multi-language support for KYC forms

---

## 12. Dependencies & Integration

### Depends On:
- F-01 (IAM) - for authentication

### Used By:
- F-05 (Delivery Creation) - DP must be KYC-verified
- F-09 (Wallet & Payments) - Bank verification required for payouts

### External Integrations:
- DigiLocker API
- NSDL PAN Verification API
- Razorpay/Cashfree Bank Verification
- Third-party Police Verification (AuthBridge/SpringVerify)
- Azure Computer Vision (OCR)

---

**Status:** Ready for Development
**Next Steps:** Sprint 2-3 (4 weeks) - Aadhaar/PAN/Bank integrations + Bulk upload
