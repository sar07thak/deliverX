# EC (End Consumer) Role Testing Guide

## Role: EC (End Consumer)
**Hierarchy Level: 5**
**Description:** Individual users who send or receive packages occasionally

---

## Pre-requisites
- API running on `http://localhost:5205`
- UI running on `http://localhost:3000`
- At least one DP registered and online (for delivery matching)

---

## What is an End Consumer (EC)?

An **End Consumer (EC)** is an individual user who:
- Sends packages to friends/family
- Receives packages from others
- Creates occasional delivery orders
- Tracks deliveries (sent or received)
- Rates delivery partners
- Uses wallet for payments
- Can file complaints if issues arise

---

## Method 1: Register via UI (Recommended)

### Step 1: Open Login Page
1. Open browser and go to `http://localhost:3000`
2. You'll see the login page with role selection

### Step 2: Select End Consumer Role
1. Click on **"End Consumer"** option (with user icon)
2. This is the default/first option
3. The option will be highlighted with a green border

### Step 3: Enter Phone Number
1. Enter phone number: `9999988888` (without +91)
2. Click **"Send OTP"** button

### Step 4: Verify OTP
1. A success message will appear with the OTP (e.g., "OTP: 123456")
2. Enter the 6-digit OTP shown in the message
3. Click **"Verify & Login"**

### Step 5: Dashboard Access
- You will be redirected to `/dashboard` (EC Dashboard)
- The navbar will show:
  - **"DeliverX"** in the logo area
  - Green **"CONSUMER"** badge next to your phone number
  - EC-specific navigation links

---

## Method 2: Register via API (For Testing)

### Step 1: Send OTP
```bash
curl -X POST http://localhost:5205/api/v1/auth/otp/send \
  -H "Content-Type: application/json" \
  -d "{\"phone\": \"9999988888\", \"role\": \"EC\"}"
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

### Step 2: Verify OTP with EC Role
```bash
curl -X POST http://localhost:5205/api/v1/auth/otp/verify \
  -H "Content-Type: application/json" \
  -d "{\"phone\": \"9999988888\", \"otp\": \"123456\", \"role\": \"EC\", \"deviceId\": \"test-ec-device\"}"
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
      "role": "EC",
      "phone": "9999988888",
      "profileComplete": true
    }
  }
}
```

### Step 3: Save Token for API Testing
```bash
EC_TOKEN="eyJhbGciOiJIUzI1NiIs..."
```

---

## EC Dashboard Features (UI Testing)

### Dashboard Overview
After login, you'll see the main dashboard with:

**Stats Cards:**
| Card | Description |
|------|-------------|
| Packages Sent | Number of deliveries you created |
| Packages Received | Number of deliveries sent to you |
| Active Deliveries | Deliveries in progress |
| Wallet Balance | Current wallet balance |

**Quick Actions:**
- Send a Package
- Track Delivery
- Recharge Wallet

### Send Package Page (`/create-delivery`)
Simple form to send a package:

**From (Pickup):**
| Field | Description |
|-------|-------------|
| Your Address | Your full address |
| Your Name | Your name |
| Your Phone | Auto-filled |
| Pickup Instructions | Any special instructions |

**To (Drop):**
| Field | Description |
|-------|-------------|
| Recipient Address | Where to deliver |
| Recipient Name | Who to deliver to |
| Recipient Phone | Contact number |
| Delivery Instructions | Instructions for DP |

**Package:**
| Field | Description |
|-------|-------------|
| Weight | Approximate weight in kg |
| Type | Document / Parcel / Fragile |
| Description | What's in the package |
| Value | Declared value (optional) |

**Options:**
| Field | Description |
|-------|-------------|
| Priority | ASAP / Scheduled / Economy |
| Schedule Time | If scheduled delivery |

### My Deliveries Page (`/deliveries`)
Shows deliveries you sent or will receive:

**Tabs:**
- **Sent** - Packages you're sending
- **Receiving** - Packages coming to you
- **All** - All deliveries

**Delivery Card:**
| Info | Description |
|------|-------------|
| Tracking ID | Click to track |
| Status | Current status |
| From/To | Route summary |
| Date | Created date |
| Actions | Track button |

### Track Delivery Page (`/delivery-tracking/{id}`)
Real-time tracking for a delivery:

**Status Timeline:**
```
Order Placed → DP Assigned → Picked Up → In Transit → Delivered
     ✓            ✓            ✓           ●           ○
```

**Live Info:**
- Current status with timestamp
- DP details (name, phone, rating)
- Estimated delivery time
- Map with route (if available)

**Actions:**
- Call DP (if assigned)
- Share tracking link
- Cancel (if not picked up)

### Wallet Page (`/wallet`)
Manage your payment wallet:

**Balance Section:**
- Available Balance
- Last Transaction

**Recharge Options:**
- Quick amounts: ₹100, ₹200, ₹500, ₹1000
- Custom amount
- Payment methods: UPI, Card, Net Banking

**Transaction History:**
| Column | Description |
|--------|-------------|
| Date | Transaction date |
| Description | Transaction details |
| Amount | + for credit, - for debit |

---

## API Testing (With Token)

### Authentication

#### Get Active Sessions
```bash
curl -X GET http://localhost:5205/api/v1/auth/sessions \
  -H "Authorization: Bearer $EC_TOKEN"
```

#### Refresh Token
```bash
curl -X POST http://localhost:5205/api/v1/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken": "your-refresh-token"}'
```

### Delivery Management

#### Create Delivery
```bash
curl -X POST http://localhost:5205/api/v1/deliveries \
  -H "Authorization: Bearer $EC_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "requesterType": "EC",
    "pickup": {
      "lat": 28.7041,
      "lng": 77.1025,
      "address": "789 Home Address, Rohini, New Delhi",
      "contactName": "EC Test Sender",
      "contactPhone": "9999988888",
      "instructions": "Ground floor, ring the bell"
    },
    "drop": {
      "lat": 28.5355,
      "lng": 77.3910,
      "address": "101 Friends Address, Noida, UP",
      "contactName": "Friend Recipient",
      "contactPhone": "9876512345",
      "instructions": "Call before arriving"
    },
    "package": {
      "weightKg": 1.0,
      "type": "parcel",
      "value": 200,
      "description": "Gift package"
    },
    "priority": "ASAP",
    "specialInstructions": "Fragile items inside"
  }'
```

**Response:**
```json
{
  "success": true,
  "deliveryId": "xyz789-...",
  "trackingId": "DLX7654321",
  "status": "CREATED",
  "estimatedPrice": 120.00
}
```

#### Create Scheduled Delivery
```bash
curl -X POST http://localhost:5205/api/v1/deliveries \
  -H "Authorization: Bearer $EC_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "requesterType": "EC",
    "pickup": {
      "lat": 28.7041,
      "lng": 77.1025,
      "address": "789 Home Address, Rohini, New Delhi",
      "contactName": "EC Test Sender",
      "contactPhone": "9999988888"
    },
    "drop": {
      "lat": 28.6139,
      "lng": 77.2090,
      "address": "CP Office, Connaught Place, New Delhi",
      "contactName": "Office Recipient",
      "contactPhone": "9876500000"
    },
    "package": {
      "weightKg": 0.5,
      "type": "document",
      "description": "Important documents"
    },
    "priority": "SCHEDULED",
    "scheduledAt": "2025-12-05T14:00:00"
  }'
```

#### Get My Deliveries
```bash
curl -X GET "http://localhost:5205/api/v1/deliveries?page=1&pageSize=10" \
  -H "Authorization: Bearer $EC_TOKEN"
```

#### Track Delivery
```bash
curl -X GET http://localhost:5205/api/v1/deliveries/{deliveryId} \
  -H "Authorization: Bearer $EC_TOKEN"
```

#### Cancel Delivery
```bash
curl -X POST http://localhost:5205/api/v1/deliveries/{deliveryId}/cancel \
  -H "Authorization: Bearer $EC_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"reason": "Changed plans"}'
```

### Wallet Operations

#### Get Wallet
```bash
curl -X GET http://localhost:5205/api/v1/wallet \
  -H "Authorization: Bearer $EC_TOKEN"
```

#### Get Transactions
```bash
curl -X GET "http://localhost:5205/api/v1/wallet/transactions?page=1&pageSize=10" \
  -H "Authorization: Bearer $EC_TOKEN"
```

#### Recharge Wallet
```bash
curl -X POST http://localhost:5205/api/v1/wallet/recharge \
  -H "Authorization: Bearer $EC_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 500,
    "paymentMethod": "UPI"
  }'
```

### Pricing

#### Calculate Delivery Price
```bash
curl -X POST http://localhost:5205/api/v1/pricing/calculate \
  -H "Authorization: Bearer $EC_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "pickupLat": 28.7041,
    "pickupLng": 77.1025,
    "dropLat": 28.5355,
    "dropLng": 77.3910,
    "packageWeightKg": 1.0,
    "packageType": "parcel",
    "vehicleType": "BIKE",
    "priority": "ASAP"
  }'
```

### Ratings

#### Get My Behavior Index
```bash
curl -X GET http://localhost:5205/api/v1/ratings/behavior-index \
  -H "Authorization: Bearer $EC_TOKEN"
```

#### Check If Already Rated
```bash
curl -X GET http://localhost:5205/api/v1/ratings/check/{deliveryId}/{targetId} \
  -H "Authorization: Bearer $EC_TOKEN"
```

#### Submit Rating
```bash
curl -X POST http://localhost:5205/api/v1/ratings \
  -H "Authorization: Bearer $EC_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "deliveryId": "{deliveryId}",
    "targetId": "{dpUserId}",
    "targetType": "DP",
    "stars": 4,
    "comment": "Good service, delivered on time",
    "tags": ["on-time", "professional"]
  }'
```

### Complaints

#### Get My Complaints
```bash
curl -X GET "http://localhost:5205/api/v1/complaints?page=1&pageSize=10" \
  -H "Authorization: Bearer $EC_TOKEN"
```

#### Create Complaint
```bash
curl -X POST http://localhost:5205/api/v1/complaints \
  -H "Authorization: Bearer $EC_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "deliveryId": "{deliveryId}",
    "category": "DELAYED_DELIVERY",
    "description": "Delivery was delayed by more than 1 hour",
    "priority": "LOW"
  }'
```

### Subscriptions (Optional for EC)

#### Get Subscription Plans
```bash
curl -X GET http://localhost:5205/api/v1/subscriptions/plans
```

#### Get My Subscription
```bash
curl -X GET http://localhost:5205/api/v1/subscriptions/my \
  -H "Authorization: Bearer $EC_TOKEN"
```

### Referrals

#### Get My Referrals
```bash
curl -X GET http://localhost:5205/api/v1/referrals/my \
  -H "Authorization: Bearer $EC_TOKEN"
```

---

## EC Navigation Menu

**Main Navigation Bar:**
| Link | Destination |
|------|-------------|
| Dashboard | /dashboard |
| Send Package | /create-delivery |
| My Deliveries | /deliveries |
| Wallet | /wallet |

**More Menu (Dropdown):**
| Link | Description |
|------|-------------|
| Track Delivery | /delivery-tracking |
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
| BC | Blue (#17a2b8) | BUSINESS |
| **EC** | **Green (#28a745)** | **CONSUMER** |

---

## EC Capabilities Summary

| Feature | Access Level | UI Location |
|---------|--------------|-------------|
| Create Deliveries | Yes | /create-delivery |
| Track Deliveries | Own | /deliveries |
| Cancel Deliveries | Own (before pickup) | /deliveries |
| Manage Wallet | Own | /wallet |
| Recharge Wallet | Yes | /wallet |
| Rate DPs | After delivery | /ratings |
| File Complaints | Yes | /complaints |
| View Referrals | Own | /referrals |
| View Profile | Own | /profile |

---

## EC Use Cases

### Use Case 1: Send Gift to Friend
1. Login as EC
2. Click "Send Package"
3. Enter your address (pickup)
4. Enter friend's address (drop)
5. Describe package (gift, fragile)
6. Select ASAP delivery
7. Confirm and pay

### Use Case 2: Schedule Document Delivery
1. Login as EC
2. Click "Send Package"
3. Enter pickup details
4. Enter office/drop details
5. Select "Document" type
6. Choose "Scheduled" priority
7. Select date and time
8. Confirm

### Use Case 3: Track Incoming Package
1. Login as EC
2. Go to "My Deliveries"
3. Click "Receiving" tab
4. Find the package
5. Click "Track"
6. View real-time status

### Use Case 4: Report Issue
1. Login as EC
2. Go to delivery details
3. Click "Report Issue"
4. Select category
5. Describe the problem
6. Submit complaint

---

## EC vs BC Comparison

| Feature | EC (Individual) | BC (Business) |
|---------|-----------------|---------------|
| Target User | Personal use | Business use |
| Volume | Occasional | Regular/bulk |
| Subscription | Basic/none | Business plans |
| Pricing | Standard | Volume discounts |
| Invoice/GST | No | Yes |
| API Access | No | Yes |
| Dashboard | Simple | Advanced |
| Support | Standard | Priority |

---

## Troubleshooting

### Issue: "Service not available in your area"
**Solution:**
- Check if DPs operate in your area
- Try different pickup/drop locations
- Contact support for area expansion

### Issue: High delivery price
**Solution:**
- Try ECONOMY priority (slower, cheaper)
- Reduce package weight if possible
- Check for active promo codes

### Issue: No DP assigned after long time
**Solution:**
- DPs may be busy or offline
- Try during peak hours
- Cancel and retry later

### Issue: Can't track delivery
**Solution:**
- Use correct tracking ID
- Login to same account used for booking
- Check "Receiving" tab if you're recipient

### Issue: Rating option not available
**Solution:**
- Rating is available only after delivery completion
- Check if delivery status is "DELIVERED"

---

## Test Checklist

- [ ] Login as EC via UI
- [ ] Verify green CONSUMER badge
- [ ] Check dashboard stats
- [ ] Recharge wallet (test amount)
- [ ] Create ASAP delivery
- [ ] Create scheduled delivery
- [ ] View deliveries list
- [ ] Track a delivery
- [ ] Check wallet transactions
- [ ] View complaints page
- [ ] View referrals page
- [ ] Test profile page
- [ ] Test logout

---

## Complete EC Test Flow

1. **Register/Login as EC** with phone
2. **Recharge wallet** with ₹500
3. **Check price** for test delivery
4. **Create ASAP delivery**
   - Pickup: Your location
   - Drop: Test location
   - Package: Small parcel
5. **Track delivery** status
6. **Wait for DP assignment** (or simulate)
7. **Monitor status updates**
   - Created → Assigned → Picked Up → In Transit → Delivered
8. **Rate DP** after delivery
9. **Check wallet** balance deducted
10. **View transaction** in history

---

## Testing Tips

### For Quick Testing:
- Use simple coordinates within Delhi NCR
- Keep package weight under 5kg
- Use ASAP priority
- Ensure at least one DP is online

### For Complete Flow:
- Have another user (or API) act as DP
- Simulate all status transitions
- Test OTP verification at delivery
- Capture POD

### Common Test Coordinates:
| Location | Lat | Lng |
|----------|-----|-----|
| Connaught Place | 28.6139 | 77.2090 |
| Rohini | 28.7041 | 77.1025 |
| Noida | 28.5355 | 77.3910 |
| Gurgaon | 28.4595 | 77.0266 |

---

## All Roles Testing Complete!

Congratulations! You have completed testing documentation for all roles:

1. **Admin (SA)** - Platform administrator
2. **DPCM** - Channel Manager
3. **DP** - Delivery Partner
4. **BC** - Business Consumer
5. **EC** - End Consumer

### Run Python Test Scripts:
```bash
# Test all roles
python test_admin_role.py
python test_dpcm_role.py
python test_dp_role.py
python test_bc_role.py
python test_ec_role.py
```

### Full Integration Test:
1. Register Admin → Configure platform
2. Register DPCM → Set commission
3. Register DP (linked to DPCM) → Complete KYC → Go online
4. Register BC → Create delivery
5. DP accepts → Pickup → Transit → Deliver
6. BC rates DP
7. DP views earnings
8. DPCM views commission
9. Admin views reports
