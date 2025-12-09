# EC (Enterprise Client) Testing Guide

## Account Details

### New Account (Test Registration)
- **Phone**: 9400000001
- **Profile Status**: Needs Registration
- **Use For**: Testing enterprise registration flow

## Registration Flow Test (Use 9400000001)

### Step 1: Login
1. Go to http://localhost:5300
2. Enter phone: `9400000001`
3. Complete OTP verification
4. Redirected to EC Registration

### Step 2: Enterprise Information
1. Fill in:
   - Company Name: `Big Enterprise Corp`
   - Contact Person Name: `Enterprise Manager`
   - Designation: `Operations Head`
   - Official Email: `enterprise@bigcorp.com`
   - Company Website: `https://bigcorp.com`
2. Click "Next"

### Step 3: Business Details
1. Fill in:
   - Business Category: Select
   - Estimated Monthly Deliveries: `1000+`
   - CIN Number: `U12345MH2020PTC123456`
   - GSTIN: `27AABCE1234F1Z5`
2. Click "Next"

### Step 4: Address Details
1. Fill in:
   - Registered Office Address
   - City, State, Pincode
   - Operational Areas (multiple)
2. Click "Next"

### Step 5: Bank & Payment Details
1. Fill in:
   - Account Holder Name: `Big Enterprise Corp`
   - Account Number: `9988776655443322`
   - IFSC Code: `SBIN0005678`
   - Preferred Payment Terms: Monthly/Weekly
2. Click "Next"

### Step 6: KYC & Documents
1. Fill in:
   - PAN Number: `AABCE1234F`
   - Upload documents (if required)
2. Click "Submit Application"

### After Registration
- Enterprise accounts typically require manual approval
- Admin/SuperAdmin will review and approve
- Once approved, dashboard access is granted

## Dashboard Features (After Approval)

### 1. Dashboard
- [ ] View enterprise dashboard
- [ ] Check delivery volume metrics
- [ ] View cost analytics
- [ ] Monitor SLA compliance

### 2. Bulk Delivery Booking
- [ ] Upload CSV for bulk orders
- [ ] API integration status
- [ ] Bulk booking interface

### 3. Dedicated Support
- [ ] Access dedicated account manager
- [ ] Priority support tickets

### 4. Custom Pricing
- [ ] View negotiated rates
- [ ] Volume discounts
- [ ] Contract details

### 5. Reports & Analytics
- [ ] Detailed delivery reports
- [ ] Cost analysis
- [ ] Performance metrics
- [ ] Custom report builder

### 6. API Access
- [ ] View API keys
- [ ] API documentation
- [ ] Webhook configuration

### 7. Invoice & Billing
- [ ] View invoices
- [ ] Download statements
- [ ] Payment history

## Test Scenarios

### Scenario 1: Submit Enterprise Application
1. Login with 9400000001
2. Complete all registration steps
3. Submit application
4. Note: Application goes to admin for review

### Scenario 2: Admin Approval Flow
1. Login as SuperAdmin (9999999999)
2. Go to Enterprise Applications
3. Find pending application
4. Review details
5. Approve/Reject

### Scenario 3: Bulk Order Upload
1. Login as approved EC
2. Go to Bulk Orders
3. Download CSV template
4. Fill with delivery data
5. Upload CSV
6. Review and confirm
7. Track bulk order status
