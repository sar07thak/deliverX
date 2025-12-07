# BC (Business Consumer) Testing Guide

## Role: BC (Business Consumer)
**Phone Number:** 7772223333

---

## Step 1: Login as BC

1. Open http://localhost:5300/Account/Login
2. Select **"Business"** role (building icon)
3. Enter phone: `7772223333`
4. Click **Send OTP**
5. Enter the OTP shown on screen
6. Click **Verify & Continue**

**Expected:** Redirected directly to Dashboard (no registration required for BC)

---

## Step 2: Verify BC Dashboard

After login, you should see:

### Dashboard Stats
- Total Deliveries: 0
- Active Deliveries: 0
- Wallet Balance: ₹0
- Completed: 0

### Quick Actions
- New Delivery button
- View My Deliveries
- Recharge Wallet

### Navigation Sidebar
- Dashboard
- **Deliveries Section:**
  - New Delivery
  - My Deliveries
- **Support Section:**
  - My Complaints
- **Account Section:**
  - Wallet
  - Profile

---

## Step 3: Add Wallet Balance

Before creating deliveries, add funds to wallet:

1. Click **Wallet** in sidebar
2. Click **Add Money** button
3. Enter amount: `1000`
4. Select payment method (UPI/Card)
5. Complete payment (simulated in dev mode)

**Expected:** Wallet balance updates to ₹1000

---

## Step 4: Create a New Delivery

1. Click **New Delivery** in sidebar
2. Fill the delivery form:

### Pickup Details
| Field | Value |
|-------|-------|
| Pickup Address | 123 Business Park, Connaught Place, Delhi |
| Contact Name | BC Test User |
| Contact Phone | 7772223333 |
| Landmark | Near Metro Station |

### Drop Details
| Field | Value |
|-------|-------|
| Drop Address | 456 Residential Colony, Vasant Kunj, Delhi |
| Contact Name | Test Recipient |
| Contact Phone | 9876543210 |
| Landmark | Blue Gate |

### Package Details
| Field | Value |
|-------|-------|
| Weight (kg) | 2 |
| Package Type | Parcel |
| Description | Business documents |

3. Click **Calculate Price** (if available)
4. Click **Create Delivery**

**Expected:** Delivery created successfully, tracking ID displayed

---

## Step 5: View My Deliveries

1. Click **My Deliveries** in sidebar
2. Verify your delivery appears in the list

### Delivery List Features:
- [ ] Delivery card with tracking ID
- [ ] Status badge (Created, Assigned, etc.)
- [ ] Pickup and Drop addresses
- [ ] Estimated price
- [ ] Track button
- [ ] Cancel button (if not yet picked up)

---

## Step 6: Track a Delivery

1. Click **Track** on a delivery
2. View delivery details:
   - Current status
   - Pickup/Drop locations
   - Assigned DP (if any)
   - Status timeline

---

## Step 7: Test Wallet

1. Click **Wallet** in sidebar
2. Verify:
   - [ ] Current balance displayed
   - [ ] Transaction history (credits/debits)
   - [ ] Add Money option

---

## Step 8: File a Complaint

1. Click **My Complaints** in sidebar
2. Click **New Complaint** (if available)
3. Fill complaint form:
   - Select delivery (dropdown)
   - Category: Delivery Issue
   - Description: Test complaint
4. Submit complaint

---

## Test Checklist

- [ ] Login as BC (7772223333)
- [ ] Dashboard loads directly (no registration)
- [ ] Wallet page loads
- [ ] Add money to wallet works
- [ ] New Delivery form loads
- [ ] Create delivery successfully
- [ ] Delivery appears in My Deliveries
- [ ] Track delivery works
- [ ] Complaints page loads
- [ ] Profile shows correct info
- [ ] Logout works

---

## Expected Results

| Page | URL | Expected |
|------|-----|----------|
| Dashboard | /Dashboard | BC stats and quick actions |
| New Delivery | /Delivery/Create | Delivery form |
| My Deliveries | /Delivery | List of deliveries |
| Track | /Delivery/Track/{id} | Delivery details |
| Wallet | /Wallet | Balance and transactions |
| Complaints | /Complaint | Complaint list |
| Profile | /Account/Profile | BC user info |

---

## Create Delivery Form Fields

### Pickup Details
| Field | Required | Example |
|-------|----------|---------|
| Address | Yes | 123 Business Park |
| Contact Name | Yes | BC User |
| Contact Phone | Yes | 7772223333 |
| Landmark | No | Near Metro |

### Drop Details
| Field | Required | Example |
|-------|----------|---------|
| Address | Yes | 456 Residential |
| Contact Name | Yes | Recipient |
| Contact Phone | Yes | 9876543210 |
| Landmark | No | Blue Gate |

### Package Details
| Field | Required | Example |
|-------|----------|---------|
| Weight (kg) | Yes | 2 |
| Type | Yes | Parcel |
| Description | No | Documents |

---

## Delivery Status Flow

```
CREATED → ASSIGNED → PICKED_UP → IN_TRANSIT → DELIVERED
    ↓
CANCELLED (if cancelled before pickup)
```

### What BC Can Do:
- **Create** new deliveries
- **Track** delivery status
- **Cancel** before pickup
- **Rate** DP after delivery
- **File complaint** if issues

---

## Troubleshooting

### Can't create delivery
- Check wallet balance (needs funds)
- Ensure pickup/drop are valid addresses
- Check if DPs are available in area

### Delivery not assigned
- DPs may be offline
- Location may be outside service area
- Wait a few minutes for DP matching

### Can't cancel delivery
- Already picked up (contact support)
- Only possible before pickup

### Wallet issues
- Refresh page after payment
- Check transaction history

---

## What Happens After Creating Delivery

1. Delivery status: CREATED
2. System finds available DPs
3. DP accepts → Status: ASSIGNED
4. DP picks up → Status: PICKED_UP
5. DP in transit → Status: IN_TRANSIT
6. DP delivers → Status: DELIVERED
7. You can rate the DP

---

## Next Step

Proceed to **05_EC_Test.md** to test as End Consumer.

Or go back to **03_DP_Test.md** to accept and complete this delivery as a DP!
