# Complete Delivery Flow Testing Guide

## Overview

This guide walks through a complete end-to-end delivery test using all roles in the MVC application.

**URL:** http://localhost:5300

---

## Test Accounts Summary

| Role | Phone | Purpose |
|------|-------|---------|
| SuperAdmin | 9999999999 | Monitor platform |
| DPCM | 8888800001 | Manage DPs, earn commission |
| DP | 7878798797 | Accept and complete deliveries |
| BC | 7772223333 | Create business deliveries |
| EC | 6666666666 | Create personal deliveries |

---

## Prerequisites

Before testing the delivery flow:

1. **Database is fresh** (run reset if needed)
2. **Application running** at http://localhost:5300
3. **Complete these registrations first:**
   - DPCM (02_DPCM_Test.md) - get referral code
   - DP (03_DP_Test.md) - register under DPCM
   - BC or EC (04/05_Test.md) - for creating deliveries

---

## Complete Delivery Flow

### Step 1: Create Delivery (as BC or EC)

1. Login as BC (7772223333) or EC (6666666666)
2. Ensure wallet has balance (add ₹1000 if needed)
3. Go to **New Delivery**
4. Fill the form:

| Field | Value |
|-------|-------|
| Pickup Address | 123 Sender Street, Delhi |
| Pickup Contact | Your Name |
| Pickup Phone | Your phone |
| Drop Address | 456 Receiver Street, Delhi |
| Drop Contact | Recipient Name |
| Drop Phone | 9876543210 |
| Weight | 2 kg |
| Type | Parcel |

5. Click **Create Delivery**
6. Note the **Tracking ID** (e.g., DLX-XXXXXX)

**Status:** CREATED

---

### Step 2: DP Goes Online

1. Login as DP (7878798797)
2. Go to Dashboard
3. Click **Go Online** button
4. Ensure status shows **Online** (green)

---

### Step 3: DP Accepts Delivery

1. Go to **Available Deliveries**
2. Find the delivery created in Step 1
3. Review:
   - Pickup/Drop locations
   - Estimated earnings
4. Click **Accept**

**Status:** ASSIGNED

---

### Step 4: Consumer Tracks Delivery

1. Login as BC/EC (the one who created delivery)
2. Go to **My Deliveries**
3. Click **Track** on the delivery
4. Verify:
   - Status: ASSIGNED
   - DP info visible

---

### Step 5: DP Picks Up Package

1. Login as DP
2. Go to **Active Delivery**
3. View pickup details
4. When at pickup location, click **Picked Up**

**Status:** PICKED_UP

---

### Step 6: DP In Transit

1. DP clicks **In Transit** (or it auto-updates)
2. Consumer can see live tracking

**Status:** IN_TRANSIT

---

### Step 7: DP Delivers Package

1. DP arrives at drop location
2. Click **Delivered**
3. Enter OTP (if required) or capture signature/photo
4. Complete delivery

**Status:** DELIVERED

---

### Step 8: Rating and Completion

1. Consumer can rate the DP (1-5 stars)
2. DP can view earnings in Wallet
3. DPCM can view commission

---

## Status Flow Diagram

```
CREATED
    │
    ▼
ASSIGNED (DP accepts)
    │
    ▼
PICKED_UP (DP collects package)
    │
    ▼
IN_TRANSIT (DP traveling)
    │
    ▼
DELIVERED (Complete!)
```

### Cancellation Points:
- **Before ASSIGNED:** Full refund
- **After ASSIGNED, before PICKED_UP:** Partial refund
- **After PICKED_UP:** Contact support

---

## Test Checklist

### 1. Delivery Creation (BC/EC)
- [ ] Login as BC or EC
- [ ] Add wallet balance if needed
- [ ] Create new delivery
- [ ] Get tracking ID
- [ ] Status is CREATED

### 2. DP Acceptance
- [ ] Login as DP
- [ ] Go Online
- [ ] See available delivery
- [ ] Accept delivery
- [ ] Status changes to ASSIGNED

### 3. Pickup
- [ ] DP views active delivery
- [ ] DP marks as Picked Up
- [ ] Status changes to PICKED_UP
- [ ] Consumer sees update

### 4. Delivery
- [ ] DP marks In Transit
- [ ] DP arrives at drop
- [ ] DP completes delivery
- [ ] Status changes to DELIVERED

### 5. Post-Delivery
- [ ] Consumer can rate DP
- [ ] DP sees earnings
- [ ] DPCM sees commission
- [ ] Transaction in wallets

---

## Multi-Role View

During delivery, different roles see different things:

| Stage | BC/EC View | DP View | DPCM View | Admin View |
|-------|------------|---------|-----------|------------|
| CREATED | Track page | N/A | N/A | All deliveries |
| ASSIGNED | DP info visible | Active delivery | Managed DP's delivery | All deliveries |
| PICKED_UP | Status update | Active controls | Status visible | All deliveries |
| IN_TRANSIT | Live tracking | Navigation | Tracking | All deliveries |
| DELIVERED | Complete, rate | Earnings | Commission | All deliveries |

---

## Commission Flow (After Delivery)

When delivery is completed (e.g., ₹100 total):

```
Total: ₹100
├── Platform Fee (10%): ₹10 → Platform
├── DPCM Commission (5%): ₹5 → DPCM Wallet
└── DP Earnings (85%): ₹85 → DP Wallet
```

---

## Troubleshooting

### Delivery not showing for DP
- DP must be **Online**
- DP must be in **service area**
- Check DP registration is complete

### Can't accept delivery
- May already be accepted by another DP
- DP may be offline
- Refresh the page

### Status not updating
- Refresh the page
- Check network connection
- Verify action was successful

### Payment not deducted/credited
- Check wallet transactions
- Delivery must be DELIVERED status
- May take a few seconds to process

---

## Test Scenarios

### Scenario 1: Happy Path
1. BC creates delivery
2. DP accepts immediately
3. DP completes delivery
4. Everyone happy!

### Scenario 2: Cancellation Before Pickup
1. EC creates delivery
2. DP accepts
3. EC cancels (before pickup)
4. Full refund to wallet

### Scenario 3: No DP Available
1. BC creates delivery
2. No DP online
3. Status stays CREATED
4. DP goes online later and accepts

### Scenario 4: Multiple Deliveries
1. BC creates 3 deliveries
2. DP accepts one at a time
3. Completes each
4. Earnings accumulate

---

## Summary

The delivery flow involves:

1. **Consumer** creates delivery and pays
2. **DP** accepts, picks up, and delivers
3. **Consumer** tracks and rates
4. **DPCM** earns commission
5. **Admin** monitors everything

All roles work together to complete a successful delivery!
