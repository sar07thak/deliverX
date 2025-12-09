# SuperAdmin Testing Guide

## Account Details
- **Phone**: 9999999999
- **Role**: SuperAdmin
- **Profile Status**: Complete
- **Wallet Balance**: 100,000 INR

## Login Steps
1. Go to http://localhost:5300
2. Enter phone: `9999999999`
3. Click "Send OTP"
4. Enter OTP (check console logs for development OTP)
5. Should redirect to SuperAdmin Dashboard

## Features to Test

### 1. Dashboard
- [ ] View system overview statistics
- [ ] Check total users count
- [ ] Check total deliveries count
- [ ] View revenue metrics

### 2. User Management
- [ ] View all users list
- [ ] Filter users by role
- [ ] Search users by phone/email
- [ ] Activate/Deactivate users
- [ ] View user details

### 3. DPCM Management
- [ ] View all DPCM list
- [ ] Approve pending DPCM applications
- [ ] View DPCM performance metrics
- [ ] Manage DPCM commission rates

### 4. DP Management
- [ ] View all Delivery Partners
- [ ] Filter by DPCM
- [ ] View DP verification status
- [ ] Manage DP status

### 5. BC/EC Management
- [ ] View Business Consumers
- [ ] View Enterprise Clients
- [ ] Manage subscriptions

### 6. Delivery Management
- [ ] View all deliveries
- [ ] Filter by status
- [ ] View delivery details
- [ ] Handle escalations

### 7. Financial Management
- [ ] View wallet transactions
- [ ] Process settlements
- [ ] View commission records
- [ ] Generate reports

### 8. System Settings
- [ ] Configure pricing
- [ ] Manage promo codes
- [ ] View audit logs
- [ ] System configuration

## Test Scenarios

### Scenario 1: Approve New DPCM
1. Login as SuperAdmin
2. Go to DPCM Management
3. Find pending DPCM (9100000001 after registration)
4. Review details
5. Approve/Reject

### Scenario 2: Handle Complaint Escalation
1. Login as SuperAdmin
2. Go to Complaints
3. Filter by "Escalated"
4. Review and resolve

### Scenario 3: Generate Financial Report
1. Login as SuperAdmin
2. Go to Reports
3. Select date range
4. Generate commission report
