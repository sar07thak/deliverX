# BC (Business Consumer) Testing Guide

## Account Details

### New Account (Test Registration)
- **Phone**: 9300000001
- **Profile Status**: Needs Registration
- **Use For**: Testing registration flow

### Complete Account (Test Dashboard)
- **Phone**: 9300000002
- **Profile Status**: Complete
- **Business Name**: Test Business Pvt Ltd
- **Category**: E-commerce
- **Wallet Balance**: 25,000 INR
- **Use For**: Testing dashboard and booking features

## Registration Flow Test (Use 9300000001)

### Step 1: Login
1. Go to http://localhost:5300
2. Enter phone: `9300000001`
3. Complete OTP verification
4. Redirected to BC Registration

### Step 2: Business Information
1. Fill in:
   - Business Name: `My E-commerce Store`
   - Contact Person Name: `Store Owner`
   - Business Email: `bc@test.com`
   - Business Category: Select (E-commerce, Retail, Food, etc.)
   - GSTIN (optional): `29AABCT1234F1Z5`
2. Click "Next"

### Step 3: Address Details
1. Fill in:
   - Address Line 1: `123 Business Park`
   - Address Line 2: `Sector 5`
   - City: `Mumbai`
   - State: `Maharashtra`
   - Pincode: `400001`
2. Click "Next"

### Step 4: Bank Details
1. Fill in:
   - Account Holder Name: `My E-commerce Store`
   - Account Number: `1122334455667788`
   - Confirm Account Number: `1122334455667788`
   - IFSC Code: `ICIC0001234`
2. Click "Next"

### Step 5: KYC Documents
1. Fill in:
   - PAN Number: `LMNOP9876Q`
2. Click "Complete Registration"

## Dashboard Features Test (Use 9300000002)

### 1. Dashboard
- [ ] View dashboard overview
- [ ] Check pending deliveries
- [ ] View completed deliveries
- [ ] See spending summary

### 2. Book Delivery
- [ ] Create new delivery request
- [ ] Enter pickup details
- [ ] Enter drop details
- [ ] Select package type/size
- [ ] Get price estimate
- [ ] Confirm booking

### 3. Active Deliveries
- [ ] View ongoing deliveries
- [ ] Track delivery in real-time
- [ ] View DP details
- [ ] Contact DP

### 4. Delivery History
- [ ] View past deliveries
- [ ] Filter by date/status
- [ ] Download delivery receipts
- [ ] Rate completed deliveries

### 5. Saved Addresses
- [ ] Add pickup location
- [ ] Add frequent drop locations
- [ ] Edit saved addresses
- [ ] Delete addresses

### 6. Wallet
- [ ] View balance
- [ ] Add money
- [ ] View transactions
- [ ] Set up auto-recharge

### 7. Pricing & Subscriptions
- [ ] View pricing plans
- [ ] Subscribe to a plan
- [ ] View subscription benefits
- [ ] Manage subscription

### 8. Reports
- [ ] View delivery reports
- [ ] Export data
- [ ] View spending analytics

### 9. Profile
- [ ] Update business details
- [ ] Update contact info
- [ ] Update bank details

## Test Scenarios

### Scenario 1: Complete Registration
1. Login with 9300000001
2. Complete all registration steps
3. Verify redirect to dashboard
4. Add initial wallet balance

### Scenario 2: Book a Delivery
1. Login with 9300000002
2. Click "Book Delivery"
3. Enter pickup location:
   - Address: 123 Business Park
   - Contact: 9300000002
4. Enter drop location:
   - Address: 456 Customer Home
   - Contact: 9876543210
5. Select package: Small (up to 5kg)
6. Review price
7. Confirm booking
8. Track delivery

### Scenario 3: Add Money to Wallet
1. Login with 9300000002
2. Go to Wallet
3. Click "Add Money"
4. Enter amount: 5000
5. Complete payment
6. Verify balance updated

### Scenario 4: Rate a Delivery
1. Login with 9300000002
2. Go to Delivery History
3. Find completed delivery
4. Click "Rate"
5. Give star rating
6. Add comment
7. Submit

### Scenario 5: Subscribe to Plan
1. Login with 9300000002
2. Go to Subscriptions
3. View available plans
4. Select a plan
5. Make payment
6. Verify subscription active
