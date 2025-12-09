# DeliveryDost Seed Data & Testing Plan

## Database Discovery Summary

### Tables: 87 total
### Foreign Keys: 99 relationships
### Stored Procedures: 75
### Functions: 5

---

## 1. DUMMY DATA STRATEGY

### Seeding Order (FK Dependency Chain)

```
Level 0 (Master/Lookup - No Dependencies):
├── MasterVehicleTypes (existing: 5)
├── MasterLanguages
├── MasterPackageTypes
├── MasterCautionTypes
├── MasterDeliveryStatuses
├── MasterBusinessCategories
├── SubscriptionPlans
├── Charities
├── Permissions
├── ComplaintSLAConfigs
└── PlatformFeeConfigs

Level 1 (Users - Core Identity):
└── Users (existing: 5, need ~200 more)

Level 2 (User-Dependent Profiles):
├── DPCManagers (→ Users)
├── DeliveryPartnerProfiles (→ Users, DPCManagers)
├── BusinessConsumerProfiles (→ Users, SubscriptionPlans)
├── Inspectors (→ Users)
├── Wallets (→ Users)
├── SavedAddresses (→ Users)
├── KYCRequests (→ Users)
└── UserSubscriptions (→ Users, SubscriptionPlans)

Level 3 (Operations):
├── Deliveries (→ Users)
├── PincodeDPCMMappings (→ DPCManagers, Users)
├── DPPricingConfigs (→ Users)
└── ServiceAreas (→ Users)

Level 4 (Delivery-Dependent):
├── DeliveryEvents (→ Deliveries, Users)
├── DeliveryPackages (→ Deliveries, MasterPackageTypes)
├── DeliveryAddresses (→ Deliveries, SavedAddresses)
├── DeliveryRoutes (→ Deliveries)
├── DeliveryBids (→ Deliveries, Users)
├── ProofOfDeliveries (→ Deliveries, Users)
├── Payments (→ Deliveries, Users)
├── CommissionRecords (→ Deliveries, Users)
└── Ratings (→ Deliveries, Users)

Level 5 (Complaints & Settlements):
├── Complaints (→ Deliveries, Users)
├── ComplaintComments (→ Complaints, Users)
├── ComplaintEvidences (→ Complaints, Users)
├── Settlements (→ Users)
└── SettlementItems (→ Settlements, Deliveries)

Level 6 (Wallet Transactions):
└── WalletTransactions (→ Wallets)
```

### Data Volume Plan

| Entity | Count | Notes |
|--------|-------|-------|
| MasterVehicleTypes | 5 (existing) | Keep |
| MasterLanguages | 10 | Hindi, English, Telugu, Tamil, etc. |
| MasterPackageTypes | 8 | Document, Parcel, Food, etc. |
| MasterCautionTypes | 6 | Fragile, Liquid, Heavy, etc. |
| MasterDeliveryStatuses | 12 | Full status flow |
| SubscriptionPlans | 6 | Free, Basic, Pro (BC & EC) |
| Users | 200+ | Mixed roles |
| DPCManagers | 15 | 10 active, 5 pending |
| DeliveryPartnerProfiles | 80 | 60 active, 20 inactive |
| BusinessConsumerProfiles | 50 | Various subscription tiers |
| Inspectors | 15 | 12 active, 3 unavailable |
| Wallets | 200+ | One per user |
| Deliveries | 300 | Mixed statuses |
| DeliveryEvents | 1000+ | 3-5 per delivery |
| Complaints | 50 | Mixed statuses |
| WalletTransactions | 500+ | Credits/Debits |

### Geographic Distribution (Indian Cities)

| City | State | Lat/Lng Center | Pincodes |
|------|-------|----------------|----------|
| Jaipur | Rajasthan | 26.9124, 75.7873 | 302001-302033 |
| Delhi | Delhi | 28.6139, 77.2090 | 110001-110096 |
| Mumbai | Maharashtra | 19.0760, 72.8777 | 400001-400104 |

### Realistic Value Ranges

**Pricing:**
- PerKmRate: ₹8-15 per km
- PerKgRate: ₹2-5 per kg
- MinCharge: ₹30-80
- Package Value: ₹100-50,000

**Distances:**
- Within city: 2-25 km
- Inter-city: 50-500 km
- MaxDistanceKm for DP: 30-100 km

**Timing:**
- CreatedAt: Last 90 days
- Deliveries: 60% within last 30 days

---

## 2. TEST COVERAGE MATRIX

### CRUD Tests per Entity

| Entity | Insert | Update | GetById | List | Delete | Notes |
|--------|--------|--------|---------|------|--------|-------|
| User | ✓ | ✓ | ✓ | ✓ | Soft | usp_User_* |
| DeliveryPartner | ✓ | ✓ | ✓ | ✓ | Soft | usp_DeliveryPartner_* |
| DPCM | ✓ | ✓ | ✓ | ✓ | Soft | usp_DPCM_* |
| Delivery | ✓ | ✓ | ✓ | ✓ | Cancel | usp_Delivery_* |
| Wallet | ✓ | - | ✓ | ✓ | - | usp_Wallet_* |
| Complaint | ✓ | ✓ | ✓ | ✓ | Close | usp_Complaint_* |
| Inspector | ✓ | - | - | ✓ | - | usp_Inspector_* |

### Workflow Tests

1. **Delivery Lifecycle**
   - Create → MatchCandidates → Assign → PickedUp → Delivered
   - Create → MatchCandidates → NoMatch → Cancel
   - Create → Assign → Cancel (by customer)

2. **Wallet Flow**
   - Create wallet → Credit → Debit → Check balance
   - Hold → Release → Verify
   - Idempotency key check

3. **Complaint Flow**
   - Create → Assign Inspector → Investigate → Resolve
   - Create → Reject
   - Add evidence, add comments

4. **DPCM Operations**
   - Activate DPCM → Assign pincodes → Add DPs
   - Commission calculation

---

## 3. FILES TO CREATE

```
scripts/testing/
├── seed_00_plan.md (this file)
├── seed_01_master_data.sql
├── seed_02_users.sql
├── seed_03_profiles.sql
├── seed_04_deliveries.sql
├── seed_05_transactions.sql
├── seed_06_complaints.sql
├── seed_all.sql (master runner)
├── test_01_user_crud.sql
├── test_02_dp_crud.sql
├── test_03_delivery_workflow.sql
├── test_04_wallet_workflow.sql
├── test_05_complaint_workflow.sql
├── test_06_edge_cases.sql
└── test_all.sql (master runner)
```

---

## 4. EXECUTION COMMANDS

```bash
# Seed all data
sqlcmd -S localhost -d DeliveryDost_Dev -E -i seed_all.sql

# Run all tests
sqlcmd -S localhost -d DeliveryDost_Dev -E -i test_all.sql

# Individual tests
sqlcmd -S localhost -d DeliveryDost_Dev -E -i test_03_delivery_workflow.sql
```

---

## 5. FILES CREATED (COMPLETE)

### SQL Seed Scripts

| File | Description | Records Created |
|------|-------------|-----------------|
| `seed_01_master_data.sql` | Languages, Package Types, Caution Types, Statuses, Plans, Charities, SLA Configs, Pincodes | ~80 records |
| `seed_02_users.sql` | Admin, DPCM, DP, BC, EC, Inspector users | 225+ users |
| `seed_03_profiles.sql` | DPCManagers, DP Profiles, BC Profiles, Inspectors, Wallets, Addresses | 200+ records |
| `seed_04_deliveries.sql` | Deliveries with Events, PODs, Ratings | 300 deliveries |
| `seed_05_transactions.sql` | Wallet Transactions, Payments, Commissions, Settlements | 500+ records |
| `seed_06_complaints.sql` | Complaints with Comments and Evidences | 50+ records |
| `seed_all.sql` | Master runner with quick seed + instructions | - |

### SQL Test Scripts

| File | Description | Tests |
|------|-------------|-------|
| `test_01_user_crud.sql` | User CRUD operations | 11 tests |
| `test_02_dp_crud.sql` | Delivery Partner CRUD | 10 tests |
| `test_03_delivery_workflow.sql` | Complete delivery lifecycle | 10 tests |
| `test_04_wallet_workflow.sql` | Wallet operations (Credit, Debit, Hold, Release) | 10 tests |
| `test_05_complaint_workflow.sql` | Complaint resolution workflow | 10 tests |
| `test_06_edge_cases.sql` | Boundary conditions, constraints, validation | 15+ tests |
| `test_all.sql` | Master runner with smoke tests | 5 smoke tests |

### C# Integration Tests

| File | Description | Tests |
|------|-------------|-------|
| `UserRepositoryTests.cs` | User entity tests | 9 tests |
| `DeliveryWorkflowTests.cs` | Delivery lifecycle tests | 8 tests |
| `WalletOperationsTests.cs` | Wallet operations tests | 9 tests |
| `DeliveryDost.Tests.csproj` | xUnit test project | - |

---

## 6. DATA DISTRIBUTION SUMMARY

### Users by Role (Target)
- **Admin**: 5 users
- **DPCM**: 15 users
- **DP**: 80 users
- **BC**: 50 users
- **EC**: 60 users
- **Inspector**: 15 users

### Deliveries by Status (Target)
- **PENDING**: 50
- **MATCHING**: 10
- **ASSIGNED**: 30
- **IN_PROGRESS**: 40
- **DELIVERED**: 150
- **CANCELLED**: 20
- **FAILED**: 10

### Geographic Distribution
- **Jaipur**: 40% of data
- **Delhi**: 35% of data
- **Mumbai**: 25% of data

---

## 7. RUNNING THE TESTS

### SQL Server (SSMS)
```sql
-- 1. Open SSMS, connect to DeliveryDost_Dev
-- 2. Open and run each seed file in order:
--    seed_01 → seed_02 → seed_03 → seed_04 → seed_05 → seed_06
-- 3. Run test_all.sql for smoke tests
-- 4. Run individual test files for detailed testing
```

### Command Line (sqlcmd)
```bash
cd C:\Users\HP\Desktop\finnidTech\scripts\testing

# Seed data (run in order)
sqlcmd -S localhost -d DeliveryDost_Dev -E -i seed_01_master_data.sql
sqlcmd -S localhost -d DeliveryDost_Dev -E -i seed_02_users.sql
sqlcmd -S localhost -d DeliveryDost_Dev -E -i seed_03_profiles.sql
sqlcmd -S localhost -d DeliveryDost_Dev -E -i seed_04_deliveries.sql
sqlcmd -S localhost -d DeliveryDost_Dev -E -i seed_05_transactions.sql
sqlcmd -S localhost -d DeliveryDost_Dev -E -i seed_06_complaints.sql

# Run all tests
sqlcmd -S localhost -d DeliveryDost_Dev -E -i test_all.sql
```

### C# Tests (dotnet)
```bash
cd C:\Users\HP\Desktop\finnidTech\src\DeliveryDost.Tests
dotnet test
```

---

## 8. SUCCESS CRITERIA

✅ All smoke tests pass in test_all.sql
✅ Individual CRUD tests pass for each entity
✅ Workflow tests complete full lifecycle
✅ Edge case tests identify constraint violations
✅ No orphaned records (FK integrity)
✅ No negative balances in wallets
✅ Delivery status timestamps are consistent

---

**Created**: December 2024
**Status**: COMPLETE
