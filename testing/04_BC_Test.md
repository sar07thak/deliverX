# BC (Business Consumer) Role Testing Guide

## Role: BC (Business Consumer)
**Hierarchy Level: 4**
**Description:** Business users who send packages regularly, can subscribe to plans for discounts

---

## Pre-requisites
- API running on `http://localhost:5205`
- UI running on `http://localhost:3000`
- At least one DP registered and online (for delivery matching)

---

## What is a Business Consumer (BC)?

A **Business Consumer (BC)** is a business entity that:
- Registers with business details
- Creates delivery orders regularly
- Can subscribe to business plans for discounts
- Tracks all sent deliveries
- Rates delivery partners
- Manages wallet for payments
- Can apply promo codes
- Files and tracks complaints

---

## Method 1: Register via UI (Recommended)

### Step 1: Open Login Page
1. Open browser and go to `http://localhost:3000`
2. You'll see the login page with role selection

### Step 2: Select Business Role
1. Click on **"Business"** option (with briefcase icon)
2. The option will be highlighted with a blue border

### Step 3: Enter Phone Number
1. Enter phone number: `8888899999` (without +91)
2. Click **"Send OTP"** button

### Step 4: Verify OTP
1. A success message will appear with the OTP (e.g., "OTP: 123456")
2. Enter the 6-digit OTP shown in the message
3. Click **"Verify & Login"**

### Step 5: Dashboard Access
- You will be redirected to `/dashboard` (BC Dashboard)
- The navbar will show:
  - **"DeliverX"** in the logo area
  - Blue **"BUSINESS"** badge next to your phone number
  - BC-specific navigation links

---

## Method 2: Register via API (For Testing)

### Step 1: Send OTP
```bash
curl -X POST http://localhost:5205/api/v1/auth/otp/send \
  -H "Content-Type: application/json" \
  -d "{\"phone\": \"8888899999\", \"role\": \"BC\"}"
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

### Step 2: Verify OTP with BC Role
```bash
curl -X POST http://localhost:5205/api/v1/auth/otp/verify \
  -H "Content-Type: application/json" \
  -d "{\"phone\": \"8888899999\", \"otp\": \"123456\", \"role\": \"BC\", \"deviceId\": \"test-bc-device\"}"
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
      "role": "BC",
      "phone": "8888899999",
      "profileComplete": true
    }
  }
}
```

### Step 3: Save Token for API Testing
```bash
BC_TOKEN="eyJhbGciOiJIUzI1NiIs..."
```

---

## BC Dashboard Features (UI Testing)

### Dashboard Overview
After login, you'll see the main dashboard with:

**Stats Cards:**
| Card | Description |
|------|-------------|
| Total Deliveries | Number of deliveries created |
| Active Deliveries | Deliveries in progress |
| Wallet Balance | Current wallet balance |
| Subscription | Current plan (if any) |

**Quick Actions:**
- Create New Delivery
- View All Deliveries
- Recharge Wallet
- View Subscription Plans

### Create Delivery Page (`/create-delivery`)
Form to create a new delivery order:

**Pickup Details:**
| Field | Description |
|-------|-------------|
| Pickup Address | Full address with landmark |
| Contact Name | Sender's name |
| Contact Phone | Sender's phone |
| Pickup Instructions | Special instructions |

**Drop Details:**
| Field | Description |
|-------|-------------|
| Drop Address | Recipient's full address |
| Contact Name | Recipient's name |
| Contact Phone | Recipient's phone |
| Drop Instructions | Delivery instructions |

**Package Details:**
| Field | Description |
|-------|-------------|
| Weight (kg) | Package weight |
| Type | parcel/document/fragile |
| Dimensions | Length x Width x Height |
| Value | Declared value (for insurance) |
| Description | Package description |

**Delivery Options:**
| Field | Description |
|-------|-------------|
| Priority | ASAP / SCHEDULED / ECONOMY |
| Scheduled Time | (if scheduled) |
| Special Instructions | Any special requirements |

### Deliveries List Page (`/deliveries`)
Shows all your deliveries:

**Filter Options:** All, Created, Assigned, In Transit, Delivered, Cancelled

**Delivery List:**
| Column | Description |
|--------|-------------|
| Tracking ID | Click to view details |
| Status | Current status with color |
| Pickup | Pickup location summary |
| Drop | Drop location summary |
| Amount | Delivery charge |
| Created | Date created |
| Actions | Track / Cancel buttons |

### Wallet Page (`/wallet`)
Manage your payment wallet:

**Wallet Summary:**
- Current Balance
- Total Spent
- Last Recharge

**Actions:**
- Recharge Wallet (UPI/Card/Net Banking)
- View Transaction History

**Transaction History:**
| Column | Description |
|--------|-------------|
| Date | Transaction date |
| Type | CREDIT / DEBIT |
| Amount | Transaction amount |
| Description | What the transaction was for |
| Balance | Balance after transaction |

### Subscriptions Page (`/subscriptions`)
View and subscribe to business plans:

**Available Plans:**
| Plan | Features |
|------|----------|
| Basic | X deliveries/month, Y% discount |
| Standard | More deliveries, higher discount |
| Premium | Unlimited deliveries, max discount |

**My Subscription:**
- Current Plan
- Valid Until
- Deliveries Used/Remaining
- Renewal Options

---

## API Testing (With Token)

### Service Area & Pricing

#### Get Service Areas
```bash
curl -X GET "http://localhost:5205/api/v1/service-area?page=1&pageSize=10"
```

#### Check Serviceability
```bash
curl -X POST http://localhost:5205/api/v1/service-area/check \
  -H "Content-Type: application/json" \
  -d '{
    "lat": 28.6139,
    "lng": 77.2090
  }'
```

#### Calculate Price
```bash
curl -X POST http://localhost:5205/api/v1/pricing/calculate \
  -H "Authorization: Bearer $BC_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "pickupLat": 28.6139,
    "pickupLng": 77.2090,
    "dropLat": 28.6350,
    "dropLng": 77.2250,
    "packageWeightKg": 2.5,
    "packageType": "parcel",
    "vehicleType": "BIKE",
    "priority": "ASAP"
  }'
```

### Delivery Management

#### Create Delivery
```bash
curl -X POST http://localhost:5205/api/v1/deliveries \
  -H "Authorization: Bearer $BC_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "requesterType": "DBC",
    "pickup": {
      "lat": 28.6139,
      "lng": 77.2090,
      "address": "123 Business Park, Connaught Place, New Delhi",
      "contactName": "BC Test User",
      "contactPhone": "8888899999",
      "instructions": "Please collect from reception"
    },
    "drop": {
      "lat": 28.6350,
      "lng": 77.2250,
      "address": "456 Residential Colony, Defence Colony, New Delhi",
      "contactName": "Test Recipient",
      "contactPhone": "9876543210",
      "instructions": "Ring doorbell twice"
    },
    "package": {
      "weightKg": 2.5,
      "type": "parcel",
      "dimensions": {
        "lengthCm": 30,
        "widthCm": 20,
        "heightCm": 15
      },
      "value": 500,
      "description": "Business documents"
    },
    "priority": "ASAP",
    "specialInstructions": "Handle with care"
  }'
```

**Response:**
```json
{
  "success": true,
  "deliveryId": "abc123-...",
  "trackingId": "DLX1234567",
  "status": "CREATED",
  "estimatedPrice": 150.00
}
```

#### Get My Deliveries
```bash
curl -X GET "http://localhost:5205/api/v1/deliveries?page=1&pageSize=10" \
  -H "Authorization: Bearer $BC_TOKEN"
```

#### Get Delivery Details
```bash
curl -X GET http://localhost:5205/api/v1/deliveries/{deliveryId} \
  -H "Authorization: Bearer $BC_TOKEN"
```

#### Trigger DP Matching
```bash
curl -X POST http://localhost:5205/api/v1/deliveries/{deliveryId}/match \
  -H "Authorization: Bearer $BC_TOKEN"
```

#### Cancel Delivery
```bash
curl -X POST http://localhost:5205/api/v1/deliveries/{deliveryId}/cancel \
  -H "Authorization: Bearer $BC_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"reason": "Test cancellation"}'
```

### Wallet Operations

#### Get Wallet
```bash
curl -X GET http://localhost:5205/api/v1/wallet \
  -H "Authorization: Bearer $BC_TOKEN"
```

#### Get Transactions
```bash
curl -X GET "http://localhost:5205/api/v1/wallet/transactions?page=1&pageSize=10" \
  -H "Authorization: Bearer $BC_TOKEN"
```

#### Initiate Recharge
```bash
curl -X POST http://localhost:5205/api/v1/wallet/recharge \
  -H "Authorization: Bearer $BC_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 1000,
    "paymentMethod": "UPI"
  }'
```

### Subscriptions

#### Get Subscription Plans
```bash
curl -X GET http://localhost:5205/api/v1/subscriptions/plans
```

#### Get My Subscription
```bash
curl -X GET http://localhost:5205/api/v1/subscriptions/my \
  -H "Authorization: Bearer $BC_TOKEN"
```

#### Get Invoices
```bash
curl -X GET http://localhost:5205/api/v1/subscriptions/invoices \
  -H "Authorization: Bearer $BC_TOKEN"
```

### Ratings

#### Get My Behavior Index
```bash
curl -X GET http://localhost:5205/api/v1/ratings/behavior-index \
  -H "Authorization: Bearer $BC_TOKEN"
```

#### Submit Rating (after delivery)
```bash
curl -X POST http://localhost:5205/api/v1/ratings \
  -H "Authorization: Bearer $BC_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "deliveryId": "{deliveryId}",
    "targetId": "{dpUserId}",
    "targetType": "DP",
    "stars": 5,
    "comment": "Excellent service!",
    "tags": ["on-time", "polite"]
  }'
```

### Complaints

#### Get My Complaints
```bash
curl -X GET "http://localhost:5205/api/v1/complaints?page=1&pageSize=10" \
  -H "Authorization: Bearer $BC_TOKEN"
```

#### Create Complaint
```bash
curl -X POST http://localhost:5205/api/v1/complaints \
  -H "Authorization: Bearer $BC_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "deliveryId": "{deliveryId}",
    "category": "DELIVERY_ISSUE",
    "description": "Package was damaged during delivery",
    "priority": "MEDIUM"
  }'
```

### Referrals

#### Get My Referral Info
```bash
curl -X GET http://localhost:5205/api/v1/referrals/my \
  -H "Authorization: Bearer $BC_TOKEN"
```

---

## BC Navigation Menu

**Main Navigation Bar:**
| Link | Destination |
|------|-------------|
| Dashboard | /dashboard |
| Create Delivery | /create-delivery |
| My Deliveries | /deliveries |
| Wallet | /wallet |

**More Menu (Dropdown):**
| Link | Description |
|------|-------------|
| Subscriptions | /subscriptions |
| Ratings | /ratings |
| Complaints | /complaints |
| Referrals | /referrals |
| Profile | /profile |

---

## Role Badge Colors (UI Reference)

| Role | Badge Color | Display Name |
|------|-------------|--------------|
| SuperAdmin | Red (#dc3545) | ADMIN |
| DPCM | Purple (#6f42c1) | MANAGER |
| DP | Yellow (#ffc107) | PARTNER |
| **BC** | **Blue (#17a2b8)** | **BUSINESS** |
| EC | Green (#28a745) | CONSUMER |

---

## BC Capabilities Summary

| Feature | Access Level | UI Location |
|---------|--------------|-------------|
| Create Deliveries | Yes | /create-delivery |
| Track Deliveries | Own | /deliveries |
| Cancel Deliveries | Own (before pickup) | /deliveries |
| Manage Wallet | Own | /wallet |
| Recharge Wallet | Yes | /wallet |
| Subscribe to Plans | Yes | /subscriptions |
| Rate DPs | After delivery | /ratings |
| File Complaints | Yes | /complaints |
| View Referrals | Own | /referrals |
| Apply Promo Codes | Yes | /create-delivery |

---

## BC vs EC Comparison

| Feature | BC (Business) | EC (End Consumer) |
|---------|---------------|-------------------|
| Subscription Plans | Yes | Limited |
| Bulk Discounts | Yes | No |
| Business Dashboard | Yes | No |
| Invoice Generation | Yes | No |
| API Access | Yes | No |
| Credit Terms | Available | No |
| Priority Support | Yes | Standard |

---

## Delivery Flow for BC

### Step 1: Check Serviceability
Before creating a delivery, check if both pickup and drop locations are serviceable.

### Step 2: Calculate Price
Get estimated price based on distance, weight, and priority.

### Step 3: Create Delivery
Fill in all details and submit the delivery request.

### Step 4: Payment
- Wallet balance is checked
- If sufficient: Amount is held
- If insufficient: Prompt to recharge

### Step 5: DP Matching
System automatically finds available DPs in the area.

### Step 6: Track Delivery
Monitor real-time status updates:
- CREATED → ASSIGNED → PICKED_UP → IN_TRANSIT → DELIVERED

### Step 7: Rate & Review
After delivery completion, rate the DP.

---

## Troubleshooting

### Issue: "No DPs available" when creating delivery
**Solution:**
- Ensure DPs are registered in the area
- DPs must be ONLINE and have completed KYC
- Check service area coverage

### Issue: Wallet recharge fails
**Solution:**
- Check payment gateway connectivity
- Try different payment method
- Minimum recharge amount may apply

### Issue: Can't cancel delivery
**Solution:**
- Deliveries can only be cancelled before DP pickup
- Once picked up, contact support

### Issue: Delivery price too high
**Solution:**
- Check subscription plans for discounts
- Apply promo codes if available
- Try ECONOMY priority for lower price

### Issue: Subscription not reflecting
**Solution:**
- Logout and login again
- Check subscription expiry date
- Contact support if issue persists

---

## Test Checklist

- [ ] Login as BC via UI
- [ ] Verify redirect to /dashboard
- [ ] Check wallet balance
- [ ] Recharge wallet (test payment)
- [ ] Check service area coverage
- [ ] Calculate delivery price
- [ ] Create a new delivery
- [ ] View delivery in list
- [ ] Track delivery status
- [ ] View subscription plans
- [ ] Check subscription details
- [ ] View complaints page
- [ ] View referrals page
- [ ] Test More dropdown menu links
- [ ] Verify logout works

---

## Complete BC Test Flow

1. **Register/Login as BC**
2. **Recharge wallet** with test amount
3. **Check serviceability** of test locations
4. **Calculate price** for delivery
5. **Create delivery** with all details
6. **Track delivery** status changes
7. **Rate DP** after delivery completes
8. **Check wallet** for deduction
9. **View transaction** history
10. **Subscribe to plan** (optional)

---

## Next Steps
After completing BC testing, say **"yes"** to proceed with **EC (End Consumer)** role testing.
