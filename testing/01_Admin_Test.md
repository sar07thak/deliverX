# Admin (SuperAdmin) Role Testing Guide

## Role: ADMIN (SuperAdmin)
**Hierarchy Level: 1 (Highest)**
**Description:** Platform administrator with full system access

---

## Pre-requisites
- API running on `http://localhost:5205`
- UI running on `http://localhost:3000`
- Fresh database recommended for clean testing

---

## Method 1: Register via UI (Recommended)

### Step 1: Open Login Page
1. Open browser and go to `http://localhost:3000`
2. You'll see the login page with role selection

### Step 2: Expand Admin Login Section
1. Scroll down and click **"Admin / Manager Login"** link
2. This expands to show two additional options:
   - DPCM (Channel Manager)
   - Super Admin

### Step 3: Select Super Admin Role
1. Click on **"Super Admin"** option (with shield icon)
2. You'll see a warning: "Admin/Manager accounts require authorization"
3. The option will be highlighted with a purple border

### Step 4: Enter Phone Number
1. Enter phone number: `9999900001` (without +91)
2. Click **"Send OTP"** button

### Step 5: Verify OTP
1. A success message will appear with the OTP (e.g., "OTP: 123456")
2. Enter the 6-digit OTP shown in the message
3. Click **"Verify & Login"**

### Step 6: Automatic Redirect to Admin Dashboard
- You will be automatically redirected to `/admin` (Admin Dashboard)
- The navbar will show:
  - **"DeliverX Admin"** in the logo area
  - Red **"ADMIN"** badge next to your phone number
  - Admin-specific navigation links

---

## Method 2: Register via API (For Testing)

### Step 1: Send OTP
```bash
curl -X POST http://localhost:5205/api/v1/auth/otp/send \
  -H "Content-Type: application/json" \
  -d "{\"phone\": \"+919999900001\", \"purpose\": \"REGISTRATION\"}"
```

**Response:**
```json
{
  "success": true,
  "data": {
    "status": "OTP_SENT",
    "expiresIn": 300,
    "message": "OTP sent successfully. OTP: 766959 (expires in 5 minutes)"
  }
}
```

### Step 2: Verify OTP with SA Role
```bash
curl -X POST http://localhost:5205/api/v1/auth/otp/verify \
  -H "Content-Type: application/json" \
  -d "{\"phone\": \"+919999900001\", \"otp\": \"766959\", \"role\": \"SA\", \"deviceId\": \"admin-device-001\"}"
```

**Response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "...",
    "expiresIn": 604800,
    "user": {
      "id": "7c263752-d8fb-4526-8538-1bc9b7f0a2df",
      "role": "SuperAdmin",
      "phone": "+919999900001",
      "profileComplete": true
    }
  }
}
```

### Step 3: Save Token for API Testing
```bash
# Save the token (replace with your actual token)
TOKEN="eyJhbGciOiJIUzI1NiIs..."
```

---

## Admin Dashboard Features (UI Testing)

### Overview Tab (Default)
After login, you'll see the **Overview** tab with:

**Stats Cards:**
| Card | Description |
|------|-------------|
| Total Users | Count of all registered users |
| Total Deliveries | Count of all deliveries |
| Active DPs | Delivery Partners currently online |
| Pending KYC | KYC requests awaiting approval |

**Revenue Overview Section:**
- Today's Revenue
- This Week's Revenue
- This Month's Revenue
- Total Platform Fees

**System Alerts:** Shows warnings like pending KYC or open complaints

**Recent Activity:** Feed of latest platform activities

### Users Tab
Click **"Users"** tab to access user management:

1. **Filter by Role:** Use dropdown to filter (EC, BC, DP, DPCM, SA)
2. **User List:** Shows phone, role, status, join date
3. **Actions:**
   - Click **"Deactivate"** (red button) to disable a user
   - Click **"Activate"** (green button) to enable a user
4. **Pagination:** Navigate through pages at the bottom

### KYC Requests Tab
Click **"KYC Requests"** tab to manage KYC:

1. **Filter by Status:** Pending, Approved, Rejected
2. **Request List:** Shows request ID, user, document type, status, date
3. **Actions (for Pending requests):**
   - Click **"Approve"** (green) to approve KYC
   - Click **"Reject"** (red) to reject - a prompt will ask for reason

### Audit Logs Tab
Click **"Audit Logs"** tab to view system logs:

1. **Log Entries:** Timestamp, User, Action, Entity, IP Address
2. **Pagination:** Navigate through log pages
3. Actions logged include: User status changes, KYC approvals/rejections, Config changes

---

## Admin Navigation Menu

**Main Navigation Bar:**
| Link | Destination |
|------|-------------|
| Dashboard | /admin (Admin Dashboard) |
| Users | /admin (Users Tab) |
| KYC | /kyc (KYC Management) |

**More Menu (Dropdown):**
| Link | Description |
|------|-------------|
| Admin Dashboard | Full admin dashboard |
| KYC Approvals | KYC management page |
| Service Areas | Manage service areas |
| Subscriptions | View subscription plans |
| Ratings | View all ratings |
| All Complaints | View all complaints |
| Profile | User profile page |

---

## API Testing (With Token)

### Test Dashboard Endpoint
```bash
curl -X GET http://localhost:5205/api/v1/admin/dashboard \
  -H "Authorization: Bearer $TOKEN"
```

**Expected Response:**
```json
{
  "platformStats": {
    "totalUsers": 2,
    "activeUsers": 2,
    "totalDPs": 0,
    "activeDPs": 0,
    "totalDeliveries": 0,
    "pendingKYC": 0
  },
  "revenueStats": {
    "totalRevenue": 0,
    "revenueToday": 0,
    "revenueThisWeek": 0,
    "revenueThisMonth": 0
  },
  "dailyMetrics": [],
  "topDPs": [],
  "alerts": []
}
```

### Test Users Endpoint
```bash
curl -X GET "http://localhost:5205/api/v1/admin/users?page=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"
```

**Expected Response:**
```json
{
  "items": [
    {
      "id": "7c263752-d8fb-4526-8538-1bc9b7f0a2df",
      "name": "+919999900001",
      "phone": "+919999900001",
      "role": "SuperAdmin",
      "status": "ACTIVE",
      "createdAt": "2025-12-04T05:06:16.668927"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

### Test KYC Endpoint
```bash
curl -X GET "http://localhost:5205/api/v1/admin/kyc?page=1&pageSize=10&status=PENDING" \
  -H "Authorization: Bearer $TOKEN"
```

### Test Audit Logs Endpoint
```bash
curl -X GET "http://localhost:5205/api/v1/admin/audit-logs?page=1&pageSize=20" \
  -H "Authorization: Bearer $TOKEN"
```

### Update User Status
```bash
curl -X PUT "http://localhost:5205/api/v1/admin/users/{userId}/status" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"status\": \"INACTIVE\", \"reason\": \"Testing\"}"
```

---

## Role Badge Colors (UI Reference)

| Role | Badge Color | Display Name |
|------|-------------|--------------|
| SuperAdmin | Red (#dc3545) | ADMIN |
| DPCM | Purple (#6f42c1) | MANAGER |
| DP | Yellow (#ffc107) | PARTNER |
| BC | Blue (#17a2b8) | BUSINESS |
| EC | Green (#28a745) | CONSUMER |

---

## Admin Capabilities Summary

| Feature | Access Level | UI Location |
|---------|--------------|-------------|
| View Platform Stats | Full | /admin (Overview) |
| User Management | Full CRUD | /admin (Users Tab) |
| Activate/Deactivate Users | Yes | /admin (Users Tab) |
| KYC Approval/Rejection | Yes | /admin (KYC Tab) |
| View Audit Logs | Yes | /admin (Audit Tab) |
| Service Area Management | Full | /service-area |
| Subscription Plans | View/Create | /subscriptions |
| Promo Codes | Create/Edit | API only |
| View All Complaints | Yes | /complaints |
| Platform Configuration | Yes | /admin |

---

## Troubleshooting

### Issue: "Admin / Manager Login" option not visible
**Solution:** Scroll down below the three role options (EC, BC, DP) and click the link

### Issue: Can't see admin dashboard after login
**Solution:**
1. Clear browser localStorage: `localStorage.clear()`
2. Login again with SA role
3. Check browser console for errors

### Issue: "Failed to load dashboard" error
**Solution:**
1. Ensure API is running on port 5205
2. Check that you're logged in with SuperAdmin role
3. Verify token is valid (not expired)

### Issue: API returns 403 Forbidden
**Solution:** The user role in token must be "SuperAdmin". Re-login with `role: "SA"`

### Issue: Users/KYC/Logs not loading
**Solution:** These tabs load data when clicked. Wait for API response or check network tab for errors.

---

## Test Checklist

- [ ] Login as SuperAdmin via UI
- [ ] Verify redirect to /admin dashboard
- [ ] Check Overview tab shows stats
- [ ] Switch to Users tab and verify user list
- [ ] Test user activate/deactivate
- [ ] Switch to KYC tab (may be empty)
- [ ] Switch to Audit Logs tab
- [ ] Test More dropdown menu links
- [ ] Verify logout works

---

## Next Steps
After completing Admin testing, say **"yes"** to proceed with **DPCM (Delivery Partner Channel Manager)** role testing.
