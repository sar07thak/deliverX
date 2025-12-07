# DP (Delivery Partner) Testing Guide

## Role: DP (Delivery Partner)
**Phone Number:** 7878798797

**Pre-requisite:** Complete DPCM registration first (02_DPCM_Test.md) to get referral code.

---

## Step 1: Login as DP

1. Open http://localhost:5300/Account/Login
2. Select **"Delivery"** role (bicycle icon)
3. Enter phone: `7878798797`
4. Click **Send OTP**
5. Enter the OTP shown on screen
6. Click **Verify & Continue**

**Expected:** Redirected to DP Registration page (5-step form)

---

## Step 2: Complete DP Registration (5 Steps)

### Step 1 of 5: Personal Information
Fill in the form:
- **Full Name:** `Test Delivery Partner`
- **Date of Birth:** `1995-01-15`
- **Gender:** `Male`
- **Address:** `123 Test Street, Test City`

Click **Next Step**

### Step 2 of 5: DPCM Referral
- **Referral Code:** Enter the DPCM code from Step 02 (e.g., `DPCM-XXXXXX`)
- Or click **Skip** if testing without DPCM

Click **Next Step**

### Step 3 of 5: Vehicle Information
Fill in vehicle details:
- **Vehicle Type:** `Bike` (or Two-Wheeler)
- **Vehicle Number:** `DL01AB1234`
- **License Number:** `DL-0420110012345`

Click **Next Step**

### Step 4 of 5: KYC Documents
Upload or provide:
- **Aadhaar Number:** `123456789012`
- **PAN Number:** `ABCDE1234F`
- (Document uploads may be optional in dev mode)

Click **Next Step**

### Step 5 of 5: Bank Details
Fill in banking information:
- **Account Holder Name:** `Test Delivery Partner`
- **Account Number:** `9876543210`
- **IFSC Code:** `SBIN0001234`
- **Bank Name:** `State Bank of India`

Click **Complete Registration**

**Expected:** Success message, redirected to DP Dashboard

---

## Step 3: Verify DP Dashboard

After registration, you should see:

### Dashboard Stats
- Today's Deliveries: 0
- Total Earnings: ₹0
- Rating: No ratings yet
- Status: Offline

### Quick Actions
- Go Online button
- View Available Deliveries
- View My Deliveries

### Availability Toggle
Toggle between **Online** and **Offline** status.

---

## Step 4: Go Online

1. On Dashboard, click **Go Online** button
2. Allow location access if prompted
3. Status should change to **Online** (green)

**Note:** You must be online to receive delivery requests.

---

## Step 5: Test Available Deliveries

1. Click **Available Deliveries** in sidebar
2. Verify the page loads

### Features to Test:
- [ ] List of pending delivery requests (may be empty)
- [ ] Distance/location info for each delivery
- [ ] Estimated earnings display
- [ ] Accept/Reject buttons

**Note:** Deliveries will appear after BC/EC users create them (see 04_BC_Test.md, 05_EC_Test.md)

---

## Step 6: Test Active Delivery

1. Click **Active Delivery** in sidebar
2. Shows current delivery being handled (if any)

### Delivery Flow (when you have an active delivery):
1. **Accept** delivery from Available list
2. **Navigate to Pickup** location
3. Click **Picked Up** when collected
4. **Navigate to Drop** location
5. Click **Delivered** when completed
6. Enter OTP from recipient (if required)

---

## Step 7: Test Delivery History

1. Click **History** in sidebar
2. View completed deliveries

### Features:
- [ ] List of past deliveries
- [ ] Status (Delivered, Cancelled)
- [ ] Earnings per delivery
- [ ] Date/time

---

## Step 8: Test Service Area

1. Click **Service Area** in sidebar
2. Configure your operating area

### Settings:
- **Center Location:** Latitude/Longitude
- **Radius:** Operating radius in km
- **Save** configuration

---

## Step 9: Test Wallet

1. Click **Wallet** in sidebar
2. View earnings and transactions

### Features to Test:
- [ ] Balance display
- [ ] Earnings breakdown
- [ ] Transaction history
- [ ] Pending settlements

---

## Test Checklist

- [ ] Login as DP (7878798797)
- [ ] Complete 5-step registration
  - [ ] Step 1: Personal Info
  - [ ] Step 2: DPCM Referral (or skip)
  - [ ] Step 3: Vehicle Info
  - [ ] Step 4: KYC Documents
  - [ ] Step 5: Bank Details
- [ ] Dashboard loads with stats
- [ ] Go Online/Offline toggle works
- [ ] Available Deliveries page loads
- [ ] Active Delivery page loads
- [ ] History page loads
- [ ] Service Area page loads
- [ ] Wallet page loads
- [ ] Profile shows correct info
- [ ] Logout works

---

## Expected Results

| Page | URL | Expected |
|------|-----|----------|
| Dashboard | /Dashboard | DP stats, online toggle |
| Available | /Dp/Available | Pending deliveries list |
| Active | /Dp/Active | Current delivery (if any) |
| History | /Dp/History | Completed deliveries |
| Service Area | /ServiceArea | Map and radius config |
| Wallet | /Wallet | Balance and transactions |
| Profile | /Account/Profile | DP user info |

---

## DP Registration Form Fields

### Step 1 - Personal Info
| Field | Required | Example |
|-------|----------|---------|
| Full Name | Yes | Test Delivery Partner |
| Date of Birth | Yes | 1995-01-15 |
| Gender | Yes | Male |
| Address | Yes | 123 Test Street |

### Step 2 - DPCM Referral
| Field | Required | Example |
|-------|----------|---------|
| Referral Code | No | DPCM-XXXXXX |

### Step 3 - Vehicle Info
| Field | Required | Example |
|-------|----------|---------|
| Vehicle Type | Yes | Bike |
| Vehicle Number | Yes | DL01AB1234 |
| License Number | Yes | DL-0420110012345 |

### Step 4 - KYC Documents
| Field | Required | Example |
|-------|----------|---------|
| Aadhaar | Yes | 123456789012 |
| PAN | Yes | ABCDE1234F |

### Step 5 - Bank Details
| Field | Required | Example |
|-------|----------|---------|
| Account Holder | Yes | Test Delivery Partner |
| Account Number | Yes | 9876543210 |
| IFSC Code | Yes | SBIN0001234 |
| Bank Name | Yes | State Bank of India |

---

## Delivery Status Flow

```
CREATED → ASSIGNED → PICKED_UP → IN_TRANSIT → DELIVERED
                ↓
           CANCELLED (if rejected/cancelled)
```

### DP Actions:
1. **Accept** - CREATED → ASSIGNED
2. **Picked Up** - ASSIGNED → PICKED_UP
3. **In Transit** - PICKED_UP → IN_TRANSIT
4. **Delivered** - IN_TRANSIT → DELIVERED

---

## Troubleshooting

### Registration form not showing
- Profile is already complete
- Go directly to Dashboard

### Can't go online
- Allow location access in browser
- Check if service area is configured

### No available deliveries
- BC/EC users need to create deliveries first
- Ensure you're Online and in service area

### DPCM referral invalid
- Check the code is correct
- DPCM must be registered first

### KYC rejected
- Contact admin for approval
- Check uploaded documents

---

## What Happens Next

After DP registration:
1. You can go Online to receive deliveries
2. Available deliveries appear in your list
3. Accept deliveries and complete them
4. Earn money per delivery
5. Request settlements from wallet

---

## Next Step

Proceed to **04_BC_Test.md** to create delivery requests as a Business Consumer.

Then return here to accept and complete those deliveries!
