# Admin (SuperAdmin) Testing Guide

## Role: SuperAdmin
**Phone Number:** 9999999999

---

## Step 1: Login as Admin

1. Open http://localhost:5300/Account/Login
2. Select **"Admin"** role (shield icon)
3. Enter phone: `9999999999`
4. Click **Send OTP**
5. Enter the OTP shown on screen
6. Click **Verify & Continue**

**Expected:** Redirected to Admin Dashboard

---

## Step 2: Verify Admin Dashboard

After login, you should see:

### Dashboard Stats
- Total Users count
- Active Deliveries
- Pending KYC requests
- Open Complaints

### Navigation Sidebar
- Dashboard
- **Management Section:**
  - Users
  - KYC Requests
  - Complaints
- **Account Section:**
  - Wallet
  - Profile

---

## Step 3: Test User Management

1. Click **Users** in sidebar
2. Verify the Users page loads

### Features to Test:
- [ ] Filter by Role (dropdown: All, EC, BC, DP, DPCM, SuperAdmin)
- [ ] Filter by Status (Active/Inactive)
- [ ] Clear Filters button
- [ ] User list table with columns: User, Role, Status, Joined, Actions
- [ ] Pagination (if many users)

### Actions:
- **Deactivate User:**
  1. Find an active user
  2. Click **Deactivate** button
  3. Enter reason in modal
  4. Click **Deactivate**
  5. Verify status changes to Inactive

- **Activate User:**
  1. Find an inactive user
  2. Click **Activate** button
  3. Verify status changes to Active

---

## Step 4: Test KYC Requests

1. Click **KYC Requests** in sidebar
2. Verify the KYC page loads

### Features to Test:
- [ ] Filter by Status (Pending, Approved, Rejected)
- [ ] KYC list table with columns: User, Document Type, Status, Submitted, Actions

### Actions (for Pending requests):
- **Approve KYC:**
  1. Find a pending request
  2. Click **Approve** button
  3. Verify status changes to Approved

- **Reject KYC:**
  1. Find a pending request
  2. Click **Reject** button
  3. Enter rejection reason
  4. Click **Reject**
  5. Verify status changes to Rejected

**Note:** KYC requests will appear after DPs/DPCMs complete registration.

---

## Step 5: Test Complaints Management

1. Click **Complaints** in sidebar
2. Verify the Complaints page loads

### Features to Test:
- [ ] Filter by Status (Open, In Progress, Resolved, Closed)
- [ ] Complaints table with: Ticket, Category, Priority, Status, Filed By, Date
- [ ] View button to see complaint details

**Note:** Complaints will appear after users file them.

---

## Step 6: Test Profile & Logout

### Profile:
1. Click **Profile** in sidebar
2. Verify profile page shows:
   - Phone number
   - Role: SuperAdmin
   - Account status

### Logout:
1. Click dropdown (top-right user icon)
2. Click **Logout**
3. Verify redirected to login page

---

## Test Checklist

- [ ] Login as SuperAdmin (9999999999)
- [ ] Dashboard loads with stats
- [ ] Users page loads and displays
- [ ] Role filter works
- [ ] Status filter works
- [ ] User activate/deactivate works
- [ ] KYC Requests page loads
- [ ] Complaints page loads
- [ ] Profile page shows correct info
- [ ] Logout works

---

## Expected Results

| Page | URL | Expected |
|------|-----|----------|
| Dashboard | /Dashboard | Shows admin stats |
| Users | /Admin/Users | User list with filters |
| KYC | /Admin/KycRequests | KYC list (may be empty) |
| Complaints | /Admin/Complaints | Complaints list (may be empty) |
| Profile | /Account/Profile | User info |

---

## Troubleshooting

### Dashboard shows zeros
- This is normal for fresh database
- Stats will populate as users register and make deliveries

### KYC/Complaints empty
- These populate when DPs register and users file complaints
- Continue with DPCM and DP testing first

### Access Denied error
- Ensure you selected "Admin" role at login
- Logout and login again with correct role

---

## Next Step

Proceed to **02_DPCM_Test.md** to register a DPCM (Manager) account.
