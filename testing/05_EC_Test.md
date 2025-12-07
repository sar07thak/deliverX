# EC (End Consumer) Testing Guide

## Role: EC (End Consumer)
**Phone Number:** 6666666666

---

## Step 1: Login as EC

1. Open http://localhost:5300/Account/Login
2. Select **"Consumer"** role (person icon) - this is the default
3. Enter phone: `6666666666`
4. Click **Send OTP**
5. Enter the OTP shown on screen
6. Click **Verify & Continue**

**Expected:** Redirected directly to Dashboard (no registration required for EC)

---

## Step 2: Verify EC Dashboard

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

1. Click **Wallet** in sidebar
2. Click **Add Money** button
3. Enter amount: `500`
4. Select payment method (UPI/Card)
5. Complete payment (simulated in dev mode)

**Expected:** Wallet balance updates to ₹500

---

## Step 4: Create a New Delivery

1. Click **New Delivery** in sidebar
2. Fill the delivery form:

### Pickup Details (Your Address)
| Field | Value |
|-------|-------|
| Pickup Address | 123 My Home, Rohini, Delhi |
| Contact Name | EC Test User |
| Contact Phone | 6666666666 |
| Landmark | Near Park |

### Drop Details (Recipient)
| Field | Value |
|-------|-------|
| Drop Address | 456 Friend's House, Noida |
| Contact Name | Friend Name |
| Contact Phone | 9876512345 |
| Landmark | Red Gate |

### Package Details
| Field | Value |
|-------|-------|
| Weight (kg) | 1 |
| Package Type | Parcel |
| Description | Gift for friend |

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

---

## Step 6: Track a Delivery

1. Click **Track** on a delivery
2. View delivery details:
   - Current status
   - Status timeline
   - Pickup/Drop locations
   - Assigned DP info (when assigned)

---

## Step 7: Test Wallet

1. Click **Wallet** in sidebar
2. Verify:
   - [ ] Current balance displayed
   - [ ] Transaction history
   - [ ] Add Money option

---

## Step 8: Test Profile

1. Click **Profile** in sidebar
2. Verify:
   - [ ] Phone number displayed
   - [ ] Role: EC (Consumer)
   - [ ] Account info

---

## Test Checklist

- [ ] Login as EC (6666666666)
- [ ] Dashboard loads directly (no registration)
- [ ] Wallet page loads
- [ ] Add money works
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
| Dashboard | /Dashboard | EC stats and quick actions |
| New Delivery | /Delivery/Create | Delivery form |
| My Deliveries | /Delivery | List of deliveries |
| Track | /Delivery/Track/{id} | Delivery details |
| Wallet | /Wallet | Balance and transactions |
| Complaints | /Complaint | Complaint list |
| Profile | /Account/Profile | EC user info |

---

## Delivery Status Flow

```
CREATED → ASSIGNED → PICKED_UP → IN_TRANSIT → DELIVERED
    ↓
CANCELLED (if cancelled before pickup)
```

### What EC Can Do:
- **Create** new deliveries
- **Track** delivery status
- **Cancel** before pickup
- **Rate** DP after delivery
- **File complaint** if issues

---

## EC vs BC Difference

| Feature | EC (Consumer) | BC (Business) |
|---------|---------------|---------------|
| Target User | Personal | Business |
| Volume | Occasional | Regular |
| Registration | Simple | May need business details |
| Pricing | Standard | May have discounts |
| Features | Basic | Advanced |

---

## Troubleshooting

### Can't create delivery
- Check wallet balance
- Ensure addresses are valid
- Check if DPs available

### Delivery not assigned
- Wait a few minutes
- DPs may be offline
- Try different location

### Wallet issues
- Refresh page after payment
- Check transaction history

---

## Next Step

Proceed to **06_Delivery_Flow.md** to test the complete end-to-end delivery flow!

This combines all roles:
- BC/EC creates delivery
- DP accepts and delivers
- Everyone tracks and rates
