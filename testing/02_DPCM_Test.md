# DPCM (Delivery Partner Channel Manager) Role Testing Guide

## Role: DPCM (Channel Manager)
**Hierarchy Level: 2**
**Description:** Manages a network of Delivery Partners, tracks their performance, and earns commission

---

## Pre-requisites
- API running on `http://localhost:5205`
- UI running on `http://localhost:3000`
- Admin account created (for reference)
- Fresh database recommended for clean testing

---

## What is DPCM?

A **Delivery Partner Channel Manager (DPCM)** is a middle-tier role that:
- Recruits and manages multiple Delivery Partners (DPs)
- Monitors DP performance and ratings
- Earns commission from deliveries completed by their DPs
- Handles DP-related complaints
- Requests settlements for accumulated earnings

---

## Method 1: Register via UI (Recommended)

### Step 1: Open Login Page
1. Open browser and go to `http://localhost:3000`
2. You'll see the login page with role selection

### Step 2: Expand Admin/Manager Login Section
1. Scroll down and click **"Admin / Manager Login"** link
2. This expands to show two additional options:
   - **DPCM (Channel Manager)** - Select this one
   - Super Admin

### Step 3: Select DPCM Role
1. Click on **"DPCM (Channel Manager)"** option (with tie icon)
2. You'll see a warning: "Admin/Manager accounts require authorization"
3. The option will be highlighted with a purple border

### Step 4: Enter Phone Number
1. Enter phone number: `8888800001` (without +91)
2. Click **"Send OTP"** button

### Step 5: Verify OTP
1. A success message will appear with the OTP (e.g., "OTP: 123456")
2. Enter the 6-digit OTP shown in the message
3. Click **"Verify & Login"**

### Step 6: Automatic Redirect to DPCM Dashboard
- You will be automatically redirected to `/dpcm` (DPCM Dashboard)
- The navbar will show:
  - **"DeliverX Manager"** in the logo area
  - Purple **"MANAGER"** badge next to your phone number
  - DPCM-specific navigation links

---

## Method 2: Register via API (For Testing)

### Step 1: Send OTP
```bash
curl -X POST http://localhost:5205/api/v1/auth/otp/send \
  -H "Content-Type: application/json" \
  -d "{\"phone\": \"+918888800001\", \"purpose\": \"REGISTRATION\"}"
```

**Response:**
```json
{
  "success": true,
  "data": {
    "status": "OTP_SENT",
    "expiresIn": 300,
    "message": "OTP sent successfully. OTP: 123456 (expires in 5 minutes)"
  }
}
```

### Step 2: Verify OTP with DPCM Role
```bash
curl -X POST http://localhost:5205/api/v1/auth/otp/verify \
  -H "Content-Type: application/json" \
  -d "{\"phone\": \"+918888800001\", \"otp\": \"123456\", \"role\": \"DPCM\", \"deviceId\": \"dpcm-device-001\"}"
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
      "id": "abc12345-...",
      "role": "DPCM",
      "phone": "+918888800001",
      "profileComplete": true
    }
  }
}
```

### Step 3: Save Token for API Testing
```bash
# Save the token (replace with your actual token)
DPCM_TOKEN="eyJhbGciOiJIUzI1NiIs..."
```

---

## DPCM Dashboard Features (UI Testing)

### Overview Tab (Default)
After login, you'll see the **Overview** tab with:

**Stats Cards:**
| Card | Description |
|------|-------------|
| Total DPs | Count of DPs managed by you |
| Active DPs | DPs currently online/available |
| Pending KYC | Your DPs with pending KYC |
| Today's Deliveries | Deliveries by your DPs today |
| Total Deliveries | All deliveries by your DPs |
| Total Commission | Your earned commission |
| Pending Settlement | Amount awaiting settlement |

**Quick Actions:**
- View Delivery Partners
- Track Deliveries
- Request Settlement

**Recent Activity:** Feed of latest DP activities

### Delivery Partners Tab
Click **"Delivery Partners"** tab to manage your DPs:

1. **Filter Options:** All Partners, Active, Inactive, Pending KYC
2. **DP List Table:**
   | Column | Description |
   |--------|-------------|
   | Name | DP's full name |
   | Phone | Contact number |
   | Status | ACTIVE / INACTIVE |
   | KYC | APPROVED / PENDING / REJECTED |
   | Deliveries | Total completed deliveries |
   | Rating | Average rating (stars) |
   | Earnings | Total earnings |
   | Actions | Activate/Deactivate button |

3. **Actions:**
   - Click **"Deactivate"** to suspend a DP
   - Click **"Activate"** to enable a DP

### Deliveries Tab
Click **"Deliveries"** tab to track deliveries by your DPs:

1. **Filter by Status:** All, Created, Picked Up, In Transit, Delivered
2. **Delivery List Table:**
   | Column | Description |
   |--------|-------------|
   | Tracking ID | Unique delivery ID |
   | Delivery Partner | DP handling the delivery |
   | Status | Current delivery status |
   | Route | Pickup → Drop location |
   | Amount | Delivery charge |
   | Commission | Your commission from this delivery |

### Commission Tab
Click **"Commission"** tab to configure and view earnings:

**Commission Configuration:**
- **Commission Type:** Percentage or Fixed Amount
- **Commission Value:** Percentage (e.g., 10%) or Fixed (e.g., ₹50)
- **Min Commission:** Minimum amount per delivery
- **Max Commission:** Maximum amount per delivery
- **Save Configuration** button

**Commission Summary:**
- This Month's Earnings
- Pending Settlement
- Last Settlement Amount

### Service Areas Tab
Click **"Service Areas"** tab to view operational areas:

- List of service areas where your DPs can operate
- Each area shows: Name, Center coordinates, Radius, Active status

### Settlements Tab
Click **"Settlements"** tab to manage payouts:

**Settlement Summary:**
- Available for Settlement (green)
- Total Settled This Month (blue)
- Bank Account (masked)

**Actions:**
- **Request Settlement** button (enabled when balance >= ₹100)

**Settlement History Table:**
| Column | Description |
|--------|-------------|
| Date | Settlement date |
| Amount | Settlement amount |
| Status | Completed / Pending / Failed |
| Reference | Transaction reference ID |

---

## DPCM Navigation Menu

**Main Navigation Bar:**
| Link | Destination |
|------|-------------|
| Dashboard | /dpcm (DPCM Dashboard) |
| All Deliveries | /deliveries |
| Wallet | /wallet |
| My Partners | /dpcm |

**More Menu (Dropdown):**
| Link | Description |
|------|-------------|
| DPCM Dashboard | Full DPCM dashboard |
| Service Areas | View service areas |
| Profile | User profile page |

---

## API Testing (With Token)

### Test DPCM Dashboard
```bash
curl -X GET http://localhost:5205/api/v1/dpcm/dashboard \
  -H "Authorization: Bearer $DPCM_TOKEN"
```

**Expected Response:**
```json
{
  "stats": {
    "totalManagedDPs": 0,
    "activeDPs": 0,
    "inactiveDPs": 0,
    "pendingOnboarding": 0,
    "totalDeliveries": 0,
    "deliveriesToday": 0,
    "openComplaints": 0,
    "avgDPRating": 0
  },
  "managedDPs": [],
  "pendingActions": [],
  "earnings": {
    "totalEarnings": 0,
    "earningsThisMonth": 0,
    "pendingSettlement": 0,
    "commissionRate": 0
  }
}
```

### Test Service Areas
```bash
curl -X GET http://localhost:5205/api/v1/service-area \
  -H "Authorization: Bearer $DPCM_TOKEN"
```

### Test Wallet Balance
```bash
curl -X GET http://localhost:5205/api/v1/wallet/balance \
  -H "Authorization: Bearer $DPCM_TOKEN"
```

---

## Role Badge Colors (UI Reference)

| Role | Badge Color | Display Name |
|------|-------------|--------------|
| SuperAdmin | Red (#dc3545) | ADMIN |
| **DPCM** | **Purple (#6f42c1)** | **MANAGER** |
| DP | Yellow (#ffc107) | PARTNER |
| BC | Blue (#17a2b8) | BUSINESS |
| EC | Green (#28a745) | CONSUMER |

---

## DPCM Capabilities Summary

| Feature | Access Level | UI Location |
|---------|--------------|-------------|
| View DPCM Stats | Full | /dpcm (Overview) |
| Manage DPs | Activate/Deactivate | /dpcm (Partners Tab) |
| Track DP Deliveries | View Only | /dpcm (Deliveries Tab) |
| Configure Commission | Edit Own | /dpcm (Commission Tab) |
| View Service Areas | View Only | /service-area |
| Request Settlement | Yes | /dpcm (Settlements Tab) |
| View Wallet | Full | /wallet |
| View Own Profile | Full | /profile |

---

## DPCM vs Admin Comparison

| Feature | DPCM | Admin |
|---------|------|-------|
| Manage ALL users | No | Yes |
| Manage own DPs | Yes | N/A |
| Approve KYC | No | Yes |
| View Audit Logs | No | Yes |
| Configure Platform Fees | No | Yes |
| Create Subscription Plans | No | Yes |
| View All Complaints | No | Yes |
| Request Settlement | Yes | N/A |
| Earn Commission | Yes | No |

---

## Testing Workflow: DPCM with DPs

To fully test DPCM functionality, you need to:

### 1. Create DPCM Account
Follow steps above to register as DPCM

### 2. Create Delivery Partner Accounts
Register 2-3 DPs that will be managed by this DPCM:
```bash
# Register DP 1
curl -X POST http://localhost:5205/api/v1/auth/otp/send \
  -H "Content-Type: application/json" \
  -d "{\"phone\": \"+917777700001\", \"purpose\": \"REGISTRATION\"}"

# Verify with role: "DP"
curl -X POST http://localhost:5205/api/v1/auth/otp/verify \
  -H "Content-Type: application/json" \
  -d "{\"phone\": \"+917777700001\", \"otp\": \"123456\", \"role\": \"DP\", \"deviceId\": \"dp-device-001\"}"
```

### 3. Link DPs to DPCM
(This is typically done during DP registration or via admin)

### 4. Test DPCM Dashboard
- View managed DPs
- Check delivery stats
- Monitor commission

---

## Troubleshooting

### Issue: DPCM Dashboard shows all zeros
**Solution:** This is expected for a new DPCM with no managed DPs. Create and link DPs first.

### Issue: Can't see "Admin / Manager Login"
**Solution:** Scroll down below the three basic role options and click the link

### Issue: Redirected to /dashboard instead of /dpcm
**Solution:**
1. Clear localStorage: `localStorage.clear()`
2. Login again with DPCM role
3. Check that role in response is "DPCM"

### Issue: "Request Settlement" button disabled
**Solution:** Minimum settlement amount is ₹100. Earn more commission first.

### Issue: No DPs showing in Partners tab
**Solution:** DPs need to be registered and linked to this DPCM account

---

## Test Checklist

- [ ] Login as DPCM via UI
- [ ] Verify redirect to /dpcm dashboard
- [ ] Check Overview tab shows stats (may be zeros)
- [ ] Switch to Delivery Partners tab
- [ ] Switch to Deliveries tab
- [ ] Switch to Commission tab
- [ ] Try saving commission configuration
- [ ] Switch to Service Areas tab
- [ ] Switch to Settlements tab
- [ ] Check settlement history
- [ ] Test More dropdown menu links
- [ ] Verify logout works

---

## Next Steps
After completing DPCM testing, say **"yes"** to proceed with **DP (Delivery Partner)** role testing.
