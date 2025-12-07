# DPCM (Manager) Testing Guide

## Role: DPCM (Delivery Partner Channel Manager)
**Phone Number:** 8888800001

---

## Step 1: Login as DPCM

1. Open http://localhost:5300/Account/Login
2. Select **"Manager"** role (person-badge icon)
3. Enter phone: `8888800001`
4. Click **Send OTP**
5. Enter the OTP shown on screen
6. Click **Verify & Continue**

**Expected:** Redirected to DPCM Registration page (if first time)

---

## Step 2: Complete DPCM Registration

After first login, you'll see the **3-Step Registration Form**:

### Step 1 of 3: Organization Info
Fill in the form:
- **Organization Name:** `Test Delivery Network`
- **Contact Person Name:** `Test Manager`
- **PAN Number:** `ABCDE1234F`

Click **Next Step**

### Step 2 of 3: Service Areas
- View the map or area configuration
- **Center Latitude:** `28.6139` (Delhi example)
- **Center Longitude:** `77.2090`
- **Radius (km):** `10`

Click **Next Step**

### Step 3 of 3: Bank Details
Fill in banking information:
- **Account Holder Name:** `Test Manager`
- **Account Number:** `1234567890`
- **IFSC Code:** `HDFC0001234`
- **Bank Name:** `HDFC Bank`

Click **Complete Registration**

**Expected:** Success message, redirected to DPCM Dashboard

---

## Step 3: Verify DPCM Dashboard

After registration, you should see:

### Dashboard Stats
- Total DPs: 0 (no DPs registered yet)
- Active DPs: 0
- Total Deliveries: 0
- Commission Earned: ₹0

### Quick Actions
- Share Referral Code
- View My DPs
- Request Settlement

### Referral Code
**Important:** Note your referral code! DPs will use this to register under you.

Example: `DPCM-XXXXXX`

---

## Step 4: Test My DPs Page

1. Click **My DPs** in sidebar
2. Verify the page loads

### Features to Test:
- [ ] Stats cards (Total, Active, Inactive DPs)
- [ ] Referral code display with copy button
- [ ] DP list table (will be empty until DPs register)
- [ ] Filter buttons (All, Active, Inactive)

**Note:** This page will populate after you complete DP registration (03_DP_Test.md)

---

## Step 5: Test Service Area

1. Click **Service Area** in sidebar
2. Verify your service area configuration

### Features:
- [ ] Map display (if implemented)
- [ ] Area details (center, radius)
- [ ] Edit service area option

---

## Step 6: Test Wallet

1. Click **Wallet** in sidebar
2. Verify wallet page loads

### Features to Test:
- [ ] Balance display (₹0 initially)
- [ ] Commission earnings section
- [ ] Settlement requests
- [ ] Transaction history (empty initially)

---

## Step 7: Copy Referral Code for DP Testing

**IMPORTANT:** Before proceeding to DP testing, copy your referral code:

1. Go to Dashboard or My DPs page
2. Find the **Referral Code** section
3. Click **Copy** button or note the code

You'll need this code in **03_DP_Test.md** to register DPs under this DPCM.

---

## Test Checklist

- [ ] Login as DPCM (8888800001)
- [ ] Complete 3-step registration
  - [ ] Step 1: Organization Info
  - [ ] Step 2: Service Area
  - [ ] Step 3: Bank Details
- [ ] Dashboard loads with stats
- [ ] My DPs page loads
- [ ] Service Area page loads
- [ ] Wallet page loads
- [ ] Referral code is visible and can be copied
- [ ] Profile shows correct info
- [ ] Logout works

---

## Expected Results

| Page | URL | Expected |
|------|-----|----------|
| Dashboard | /Dashboard | DPCM stats and quick actions |
| My DPs | /Dpcm/MyDPs | DP list (empty initially) |
| Service Area | /ServiceArea | Map and area config |
| Wallet | /Wallet | Balance and transactions |
| Profile | /Account/Profile | DPCM user info |

---

## DPCM Registration Form Fields

### Step 1 - Organization Info
| Field | Required | Example |
|-------|----------|---------|
| Organization Name | Yes | Test Delivery Network |
| Contact Person | Yes | Test Manager |
| PAN | Yes | ABCDE1234F |

### Step 2 - Service Area
| Field | Required | Example |
|-------|----------|---------|
| Latitude | Yes | 28.6139 |
| Longitude | Yes | 77.2090 |
| Radius (km) | Yes | 10 |

### Step 3 - Bank Details
| Field | Required | Example |
|-------|----------|---------|
| Account Holder | Yes | Test Manager |
| Account Number | Yes | 1234567890 |
| IFSC Code | Yes | HDFC0001234 |
| Bank Name | Yes | HDFC Bank |

---

## Troubleshooting

### Registration form not showing
- This means profile is already complete
- You should be on the dashboard directly

### Service area not saving
- Ensure latitude/longitude are valid numbers
- Radius should be between 1-50 km

### Referral code not visible
- Complete registration first
- Refresh the dashboard page

### Dashboard shows errors
- Check if registration was completed successfully
- Logout and login again

---

## What Happens Next

After DPCM registration:
1. You get a unique referral code
2. DPs can register using your code
3. DPs who register under you appear in "My DPs"
4. You earn commission from their deliveries
5. You can request settlements when balance > ₹100

---

## Next Step

Proceed to **03_DP_Test.md** to register a Delivery Partner using your DPCM referral code.

**Remember:** Keep note of your referral code!
