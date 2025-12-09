# Admin Testing Guide

## Account Details
- **Phone**: 9999999998
- **Role**: Admin
- **Profile Status**: Complete
- **Wallet Balance**: 50,000 INR

## Login Steps
1. Go to http://localhost:5300
2. Enter phone: `9999999998`
3. Click "Send OTP"
4. Enter OTP (check console logs for development OTP)
5. Should redirect to Admin Dashboard

## Features to Test

### 1. Dashboard
- [ ] View dashboard statistics
- [ ] Check user metrics
- [ ] View delivery metrics

### 2. User Support
- [ ] View user complaints
- [ ] Respond to queries
- [ ] Update ticket status

### 3. DPCM Review
- [ ] View DPCM applications
- [ ] Review KYC documents
- [ ] Make recommendations

### 4. DP Verification
- [ ] View DP KYC submissions
- [ ] Verify documents
- [ ] Approve/Reject KYC

### 5. Delivery Monitoring
- [ ] View active deliveries
- [ ] Track delivery status
- [ ] Handle issues

### 6. Reports
- [ ] View daily reports
- [ ] Export data

## Test Scenarios

### Scenario 1: Handle Customer Complaint
1. Login as Admin
2. Go to Complaints section
3. View pending complaints
4. Add comment
5. Update status

### Scenario 2: Review DP KYC
1. Login as Admin
2. Go to KYC Requests
3. Select pending DP KYC
4. Verify documents
5. Approve or request re-upload
