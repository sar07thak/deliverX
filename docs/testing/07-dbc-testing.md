# DBC (Delivery Business Consumer) Testing Guide

## Account Details

### New Account (Test Registration)
- **Phone**: 9500000001
- **Profile Status**: Needs Registration
- **Use For**: Testing DBC registration flow

## What is DBC?
DBC (Delivery Business Consumer) is a role for businesses that primarily need delivery services for their daily operations. This includes:
- Restaurants with delivery
- Local stores with home delivery
- Small e-commerce businesses
- Service providers needing pickup/drop services

## Registration Flow Test (Use 9500000001)

### Step 1: Login
1. Go to http://localhost:5300
2. Enter phone: `9500000001`
3. Complete OTP verification
4. Redirected to DBC Registration

### Step 2: Business Information
1. Fill in:
   - Business Name: `Local Delivery Store`
   - Owner Name: `Store Owner`
   - Business Type: Restaurant/Retail/Service
   - Email: `dbc@test.com`
   - Daily Delivery Volume: 10-50
2. Click "Next"

### Step 3: Store/Business Location
1. Fill in:
   - Store Address: `Shop 12, Market Complex`
   - Landmark: `Near Bus Stand`
   - City: `Pune`
   - State: `Maharashtra`
   - Pincode: `411001`
   - Operating Hours: 9 AM - 9 PM
2. Click "Next"

### Step 4: Bank Details
1. Fill in:
   - Account Holder Name: `Local Delivery Store`
   - Account Number: `5566778899001122`
   - Confirm Account Number: `5566778899001122`
   - IFSC Code: `BARB0PUNE01`
2. Click "Next"

### Step 5: KYC
1. Fill in:
   - PAN Number: `QRSTU4567V`
   - FSSAI License (if food business): Optional
2. Click "Complete Registration"

## Dashboard Features (After Registration)

### 1. Dashboard
- [ ] View daily delivery summary
- [ ] Quick booking widget
- [ ] Active deliveries count
- [ ] Today's spending

### 2. Quick Delivery Booking
- [ ] One-tap booking for frequent routes
- [ ] Saved customer addresses
- [ ] Quick repeat order

### 3. Active Deliveries
- [ ] View ongoing deliveries
- [ ] Track DP location
- [ ] Contact DP
- [ ] Mark issues

### 4. Delivery History
- [ ] View past deliveries
- [ ] Filter by date
- [ ] Customer-wise history

### 5. Customer Management
- [ ] Add frequent customers
- [ ] Save customer addresses
- [ ] Customer delivery history

### 6. Wallet
- [ ] View balance
- [ ] Recharge wallet
- [ ] Transaction history
- [ ] Set low balance alerts

### 7. Pricing
- [ ] View delivery rates
- [ ] Distance-based pricing
- [ ] Peak hour rates

### 8. Profile & Settings
- [ ] Update business hours
- [ ] Update address
- [ ] Notification settings

## Test Scenarios

### Scenario 1: Complete Registration
1. Login with 9500000001
2. Complete all registration steps
3. Verify redirect to dashboard
4. Add wallet balance

### Scenario 2: Book Quick Delivery
1. Login as DBC
2. Click "New Delivery"
3. Enter/Select customer address
4. Enter package details
5. Confirm booking
6. Track delivery

### Scenario 3: Save Frequent Customer
1. Login as DBC
2. Go to Customers
3. Add new customer:
   - Name: Regular Customer
   - Phone: 9876543210
   - Address: 123 Customer Lane
4. Save
5. Use for quick booking

### Scenario 4: View Daily Report
1. Login as DBC
2. Go to Reports
3. Select today's date
4. View:
   - Total deliveries
   - Total spent
   - Average delivery time
5. Export report

### Scenario 5: Handle Delivery Issue
1. Login as DBC
2. Go to Active Deliveries
3. Select a delivery
4. Report issue
5. Track resolution
