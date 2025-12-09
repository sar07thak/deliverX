# DP (Delivery Partner) Testing Guide

## Account Details

### New Accounts (Test Registration)
- **Phone**: 9200000001
- **Phone**: 9200000002
- **Profile Status**: Needs Registration
- **Use For**: Testing registration flow with/without DPCM referral

### Complete Account (Test Dashboard)
- **Phone**: 9200000003
- **Profile Status**: Complete
- **Name**: Active Delivery Partner
- **Vehicle Type**: BIKE
- **Wallet Balance**: 5,000 INR
- **Use For**: Testing dashboard and delivery features

## Registration Flow Test (Use 9200000001)

### Step 1: Login & Role Detection
1. Go to http://localhost:5300
2. Enter phone: `9200000001`
3. Complete OTP verification
4. Redirected to DP Registration

### Step 2: Personal Information
1. Fill in:
   - Full Name: `Test Delivery Partner`
   - Date of Birth: Select date (must be 18+)
   - Gender: Select
   - Email: `dp@test.com`
2. Click "Next"

### Step 3: Vehicle Information
1. Fill in:
   - Vehicle Type: BIKE / SCOOTER / CAR
   - Vehicle Number: `MH01AB1234`
   - Driving License Number: `MH0120200012345`
2. Click "Next"

### Step 4: Bank Details
1. Fill in:
   - Account Holder Name: `Test Delivery Partner`
   - Account Number: `9876543210123456`
   - Confirm Account Number: `9876543210123456`
   - IFSC Code: `HDFC0001234`
2. Click "Next"

### Step 5: KYC Documents
1. Fill in:
   - PAN Number: `FGHIJ5678K`
   - Aadhaar Last 4 Digits: `5678`
2. Click "Complete Registration"

### Step 6 (Optional): DPCM Referral
1. If user has DPCM referral code, enter it
2. This links DP to the DPCM
3. DPCM will earn commission on this DP's deliveries

## Registration with DPCM Referral

### Test Linking to DPCM
1. First, login as DPCM (9100000002) and copy referral code
2. Then, register DP (9200000002) with the referral code
3. Verify DP is linked to DPCM

## Dashboard Features Test (Use 9200000003)

### 1. Dashboard
- [ ] View earnings summary
- [ ] Check today's deliveries count
- [ ] View weekly/monthly stats
- [ ] See online/offline toggle

### 2. Go Online/Offline
- [ ] Toggle online status
- [ ] Set availability
- [ ] Update current location

### 3. Active Deliveries
- [ ] View current delivery
- [ ] View delivery details
- [ ] Update delivery status
- [ ] Navigate to pickup/drop location

### 4. Delivery History
- [ ] View past deliveries
- [ ] Filter by date
- [ ] View delivery details
- [ ] Check earnings per delivery

### 5. Earnings
- [ ] View total earnings
- [ ] View daily breakdown
- [ ] View pending payments
- [ ] Request withdrawal

### 6. Wallet
- [ ] View wallet balance
- [ ] View transactions
- [ ] Add money (if applicable)
- [ ] Withdraw funds

### 7. Ratings & Reviews
- [ ] View average rating
- [ ] View individual reviews
- [ ] Track rating history

### 8. Profile
- [ ] View profile details
- [ ] Update phone/email
- [ ] Update vehicle details
- [ ] Update bank details

## Test Scenarios

### Scenario 1: Complete Registration
1. Login with 9200000001
2. Complete all registration steps
3. Verify redirect to dashboard
4. Check initial wallet balance

### Scenario 2: Register with DPCM Code
1. Get referral code from DPCM (9100000002)
2. Login with 9200000002
3. During registration, enter DPCM referral code
4. Complete registration
5. Verify linking to DPCM

### Scenario 3: Accept and Complete Delivery
1. Login with 9200000003
2. Go online
3. Wait for delivery request (or simulate)
4. Accept delivery
5. Navigate to pickup
6. Mark picked up
7. Navigate to drop
8. Complete delivery
9. Verify earnings credited

### Scenario 4: Update Availability
1. Login with 9200000003
2. Go to Profile > Availability
3. Set availability schedule
4. Save changes

### Scenario 5: Request Payout
1. Login with 9200000003
2. Go to Wallet
3. Click Withdraw
4. Enter amount
5. Confirm
6. Check withdrawal request status
