# DPCM (Delivery Partner Channel Manager) Testing Guide

## Account Details

### New Account (Test Registration)
- **Phone**: 9100000001
- **Profile Status**: Needs Registration
- **Use For**: Testing registration flow

### Complete Account (Test Dashboard)
- **Phone**: 9100000002
- **Profile Status**: Complete
- **Organization**: Test DPCM Organization
- **Wallet Balance**: 10,000 INR
- **Use For**: Testing dashboard features

## Registration Flow Test (Use 9100000001)

### Step 1: Personal Information
1. Go to http://localhost:5300
2. Enter phone: `9100000001`
3. Complete OTP verification
4. Redirected to DPCM Registration Step 1
5. Fill in:
   - Organization Name: `My DPCM Business`
   - Contact Person Name: `Test Manager`
   - Email: `dpcm@test.com`
   - Service Regions: Select applicable states
6. Click "Next"

### Step 2: Bank Details
1. Fill in:
   - Account Holder Name: `Test Manager`
   - Account Number: `1234567890123456`
   - Confirm Account Number: `1234567890123456`
   - IFSC Code: `SBIN0001234` (auto-fetches bank name)
2. Click "Next"

### Step 3: KYC Documents
1. Fill in:
   - PAN Number: `ABCDE1234F`
   - Name as per PAN: `Test Manager`
   - Aadhaar Last 4 Digits: `1234` (optional)
2. Click "Complete Registration"
3. Should redirect to DPCM Dashboard

## Dashboard Features Test (Use 9100000002)

### 1. Dashboard
- [ ] View dashboard statistics
- [ ] Check total DPs onboarded
- [ ] View earnings summary
- [ ] View commission earned

### 2. Referral Code
- [ ] View unique referral code
- [ ] Copy referral code
- [ ] Share referral link

### 3. DP Management
- [ ] View list of DPs under this DPCM
- [ ] View DP status (Active/Inactive/Pending)
- [ ] View DP performance metrics
- [ ] Filter DPs by status

### 4. Earnings & Commissions
- [ ] View total commission earned
- [ ] View commission breakdown by DP
- [ ] View pending settlements
- [ ] Download commission reports

### 5. Wallet
- [ ] View wallet balance
- [ ] View transaction history
- [ ] Request withdrawal

### 6. Profile Management
- [ ] View profile details
- [ ] Update contact information
- [ ] Change bank details

## Test Scenarios

### Scenario 1: Complete Registration
1. Login with 9100000001
2. Complete all 3 steps
3. Verify redirect to dashboard
4. Check referral code is generated

### Scenario 2: Onboard New DP
1. Login with 9100000002
2. Copy referral code
3. Share with new DP (9200000001)
4. DP registers using referral code
5. Verify DP appears in DPCM's DP list

### Scenario 3: Track Commissions
1. Login with 9100000002
2. Go to Earnings section
3. View commission history
4. Filter by date range

### Scenario 4: Request Withdrawal
1. Login with 9100000002
2. Go to Wallet
3. Click "Withdraw"
4. Enter amount
5. Confirm withdrawal request
