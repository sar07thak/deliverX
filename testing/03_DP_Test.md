# DP (Delivery Partner) Role Testing Guide

## Role: DP (Delivery Partner)
**Hierarchy Level: 3**
**Description:** Delivery personnel who pick up and deliver packages, earn per delivery

---

## Pre-requisites
- API running on `http://localhost:5205`
- UI running on `http://localhost:3000`
- DPCM account created (optional, for linking)

---

## What is a Delivery Partner (DP)?

A **Delivery Partner (DP)** is the core service provider who:
- Registers and completes KYC verification
- Sets availability status (online/offline)
- Receives delivery notifications (push/pull)
- Accepts or rejects delivery requests
- Manages delivery lifecycle (pickup -> transit -> deliver)
- Captures Proof of Delivery (POD)
- Views earnings and requests settlements

---

## Method 1: Register via UI (Recommended)

### Step 1: Open Login Page
1. Open browser and go to `http://localhost:3000`
2. You'll see the login page with role selection

### Step 2: Select Delivery Partner Role
1. Click on **"Delivery Partner"** option (with motorcycle icon)
2. The option will be highlighted with a yellow border

### Step 3: Enter Phone Number
1. Enter phone number: `7878798797` (without +91)
2. Click **"Send OTP"** button

### Step 4: Verify OTP
1. A success message will appear with the OTP (e.g., "OTP: 123456")
2. Enter the 6-digit OTP shown in the message
3. Click **"Verify & Login"**

### Step 5: Automatic Redirect to Profile Registration
**For NEW DP accounts**, you'll be automatically redirected to `/register` page:
- The system checks if `profileComplete: false` in the login response
- New DPs without a completed profile are redirected to the registration wizard

### Step 6: Complete Profile (Registration Wizard)
On the registration page, complete the multi-step form:
1. Enter Full Name
2. Enter Email (optional)
3. Enter Date of Birth
4. Select Gender
5. Enter Address details
6. Select Vehicle Type (BIKE/CAR/VAN/TRUCK)
7. Select Languages spoken
8. Set Service Area (map/coordinates)
9. Set Pricing (per km, per kg, minimum charge)
10. Click **"Complete Registration"**

### Step 7: KYC Redirect (After Profile Completion)
- After profile completion, DPs are redirected to `/kyc` page
- Complete KYC verification (Aadhaar, PAN, Bank details)
- Wait for Admin approval before accepting deliveries

### Step 8: Dashboard Access (After KYC)
- You will be redirected to `/dashboard` (DP Dashboard)
- The navbar will show:
  - **"DeliverX"** in the logo area
  - Yellow **"PARTNER"** badge next to your phone number
  - DP-specific navigation links

---

## Method 2: Register via API (For Testing)

### Step 1: Initiate Registration
```bash
curl -X POST http://localhost:5205/api/v1/registration/dp/initiate \
  -H "Content-Type: application/json" \
  -d "{\"phone\": \"7878798797\"}"
```

### Step 2: Send OTP
```bash
curl -X POST http://localhost:5205/api/v1/auth/otp/send \
  -H "Content-Type: application/json" \
  -d "{\"phone\": \"7878798797\", \"role\": \"DP\"}"
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

### Step 3: Verify OTP with DP Role
```bash
curl -X POST http://localhost:5205/api/v1/auth/otp/verify \
  -H "Content-Type: application/json" \
  -d "{\"phone\": \"7878798797\", \"otp\": \"123456\", \"role\": \"DP\", \"deviceId\": \"test-dp-device\"}"
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
      "role": "DP",
      "phone": "7878798797",
      "profileComplete": false
    }
  }
}
```

### Step 4: Complete Profile via API
```bash
DP_TOKEN="eyJhbGciOiJIUzI1NiIs..."

curl -X POST http://localhost:5205/api/v1/registration/dp/profile \
  -H "Authorization: Bearer $DP_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Test Delivery Partner",
    "email": "dp.test@example.com",
    "dob": "1995-05-15",
    "gender": "Male",
    "address": {
      "line1": "123 Delivery Street",
      "line2": "Near Market",
      "city": "Delhi",
      "state": "Delhi",
      "pincode": "110001"
    },
    "vehicleType": "BIKE",
    "languages": ["Hindi", "English"],
    "availability": "FULL_TIME",
    "serviceArea": {
      "centerLat": 28.6139,
      "centerLng": 77.2090,
      "radiusKm": 10
    },
    "pricing": {
      "perKmRate": 10.0,
      "perKgRate": 5.0,
      "minCharge": 50.0,
      "maxDistanceKm": 20
    }
  }'
```

---

## DP Dashboard Features (UI Testing)

### Dashboard Overview
After login, you'll see the main dashboard with:

**Stats Cards:**
| Card | Description |
|------|-------------|
| Today's Deliveries | Number of deliveries completed today |
| Total Earnings | All-time earnings |
| Current Rating | Average rating from customers |
| Availability | Current online/offline status |

**Quick Actions:**
- Go Online / Go Offline toggle
- View Available Jobs
- View My Deliveries

### Available Jobs Page (`/available-jobs`)
Shows deliveries waiting for a DP to accept:

**Job Card Details:**
| Field | Description |
|-------|-------------|
| Pickup Location | Address and distance from you |
| Drop Location | Destination address |
| Package Info | Weight, type, special instructions |
| Estimated Earnings | Amount you'll earn |
| Accept/Reject | Action buttons |

### My Deliveries Page (`/deliveries`)
Shows all your assigned and completed deliveries:

**Filter Options:** All, Active, Completed, Cancelled

**Delivery Card Details:**
| Field | Description |
|-------|-------------|
| Tracking ID | Unique delivery identifier |
| Status | ASSIGNED, PICKED_UP, IN_TRANSIT, DELIVERED |
| Pickup | Sender address |
| Drop | Recipient address |
| Amount | Delivery charge |
| Action | Update status button |

### Delivery Tracking Page (`/delivery-tracking/{id}`)
Detailed view for managing a specific delivery:

**Status Flow:**
```
ASSIGNED → PICKED_UP → IN_TRANSIT → DELIVERED
```

**Actions:**
1. **Mark Picked Up** - When package is collected from sender
2. **Mark In Transit** - When en route to destination
3. **Send OTP** - Send verification OTP to recipient
4. **Mark Delivered** - Complete with POD (signature/photo)

---

## Availability Management

### Go Online
```bash
curl -X PUT http://localhost:5205/api/v1/deliveries/availability \
  -H "Authorization: Bearer $DP_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "ONLINE",
    "latitude": 28.6139,
    "longitude": 77.2090,
    "vehicleType": "BIKE"
  }'
```

### Go Offline
```bash
curl -X PUT http://localhost:5205/api/v1/deliveries/availability \
  -H "Authorization: Bearer $DP_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"status": "OFFLINE"}'
```

### Get Current Status
```bash
curl -X GET http://localhost:5205/api/v1/deliveries/availability \
  -H "Authorization: Bearer $DP_TOKEN"
```

---

## Delivery Lifecycle API Testing

### 1. Get Pending Deliveries (Notifications)
```bash
curl -X GET http://localhost:5205/api/v1/deliveries/pending \
  -H "Authorization: Bearer $DP_TOKEN"
```

### 2. Get My Deliveries
```bash
curl -X GET "http://localhost:5205/api/v1/deliveries?role=dp" \
  -H "Authorization: Bearer $DP_TOKEN"
```

### 3. Accept Delivery
```bash
curl -X POST http://localhost:5205/api/v1/deliveries/{deliveryId}/accept \
  -H "Authorization: Bearer $DP_TOKEN"
```

### 4. Reject Delivery
```bash
curl -X POST http://localhost:5205/api/v1/deliveries/{deliveryId}/reject \
  -H "Authorization: Bearer $DP_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"reason": "Too far from current location"}'
```

### 5. Mark as Picked Up
```bash
curl -X POST http://localhost:5205/api/v1/deliveries/{deliveryId}/pickup \
  -H "Authorization: Bearer $DP_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "pickupLocation": {
      "latitude": 28.6129,
      "longitude": 77.2295
    },
    "pickupPhoto": "base64_encoded_photo_data",
    "notes": "Picked up from sender"
  }'
```

### 6. Mark as In Transit
```bash
curl -X POST http://localhost:5205/api/v1/deliveries/{deliveryId}/transit \
  -H "Authorization: Bearer $DP_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "currentLocation": {
      "latitude": 28.6200,
      "longitude": 77.2100
    },
    "notes": "On the way to delivery location"
  }'
```

### 7. Send Delivery OTP (to Recipient)
```bash
curl -X POST http://localhost:5205/api/v1/deliveries/{deliveryId}/otp/send \
  -H "Authorization: Bearer $DP_TOKEN"
```

### 8. Verify Delivery OTP
```bash
curl -X POST http://localhost:5205/api/v1/deliveries/{deliveryId}/otp/verify \
  -H "Authorization: Bearer $DP_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"otp": "123456"}'
```

### 9. Mark as Delivered (with POD)
```bash
curl -X POST http://localhost:5205/api/v1/deliveries/{deliveryId}/deliver \
  -H "Authorization: Bearer $DP_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "deliveryLocation": {
      "latitude": 28.6350,
      "longitude": 77.2250
    },
    "recipientName": "Test Recipient",
    "signatureImage": "base64_encoded_signature",
    "deliveryPhoto": "base64_encoded_photo",
    "notes": "Delivered to recipient",
    "verificationMethod": "SIGNATURE"
  }'
```

### 10. Get Proof of Delivery
```bash
curl -X GET http://localhost:5205/api/v1/deliveries/{deliveryId}/pod \
  -H "Authorization: Bearer $DP_TOKEN"
```

---

## Wallet & Earnings

### Get Wallet
```bash
curl -X GET http://localhost:5205/api/v1/wallet \
  -H "Authorization: Bearer $DP_TOKEN"
```

### Get Transactions
```bash
curl -X GET "http://localhost:5205/api/v1/wallet/transactions?page=1&pageSize=10" \
  -H "Authorization: Bearer $DP_TOKEN"
```

### Get Earnings Summary
```bash
curl -X GET http://localhost:5205/api/v1/wallet/earnings \
  -H "Authorization: Bearer $DP_TOKEN"
```

---

## Settlements

### Get Settlement History
```bash
curl -X GET "http://localhost:5205/api/v1/settlements?page=1&pageSize=10" \
  -H "Authorization: Bearer $DP_TOKEN"
```

---

## Ratings

### Get My Ratings
```bash
curl -X GET "http://localhost:5205/api/v1/ratings?page=1&pageSize=10" \
  -H "Authorization: Bearer $DP_TOKEN"
```

---

## DP Navigation Menu

**Main Navigation Bar:**
| Link | Destination |
|------|-------------|
| Dashboard | /dashboard |
| Deliveries | /deliveries |
| Available Jobs | /available-jobs |
| Wallet | /wallet |

**More Menu (Dropdown):**
| Link | Description |
|------|-------------|
| KYC Status | /kyc |
| Service Areas | /service-area |
| Ratings | /ratings |
| Complaints | /complaints |
| Profile | /profile |

---

## Role Badge Colors (UI Reference)

| Role | Badge Color | Display Name |
|------|-------------|--------------|
| SuperAdmin | Red (#dc3545) | ADMIN |
| DPCM | Purple (#6f42c1) | MANAGER |
| **DP** | **Yellow (#ffc107)** | **PARTNER** |
| BC | Blue (#17a2b8) | BUSINESS |
| EC | Green (#28a745) | CONSUMER |

---

## DP Capabilities Summary

| Feature | Access Level | UI Location |
|---------|--------------|-------------|
| Complete Profile | Own | /profile |
| Go Online/Offline | Yes | /dashboard |
| View Available Jobs | Yes | /available-jobs |
| Accept/Reject Deliveries | Yes | /available-jobs |
| Manage Delivery Lifecycle | Yes | /delivery-tracking |
| Capture POD | Yes | /delivery-tracking |
| View Wallet/Earnings | Own | /wallet |
| View Settlements | Own | /settlements |
| View Ratings | Own | /ratings |
| File Complaints | Yes | /complaints |
| KYC Submission | Own | /kyc |

---

## KYC Process for DP

### KYC Documents Required:
1. **Aadhaar Card** - Identity verification
2. **PAN Card** - Tax identification
3. **Driving License** - Vehicle license
4. **Bank Account** - Settlement details

### KYC Submission Flow:
1. Login as DP
2. Go to KYC page (/kyc)
3. Upload required documents
4. Submit for verification
5. Wait for Admin approval
6. Once approved, can accept deliveries

---

## Troubleshooting

### Issue: "No available jobs"
**Solution:** Jobs only appear when you're ONLINE and within a serviceable area

### Issue: Can't accept deliveries
**Solution:** Complete KYC verification first - Admin must approve

### Issue: Availability toggle not working
**Solution:** Ensure location permissions are granted in browser

### Issue: Delivery status update fails
**Solution:** Follow the correct status flow: ASSIGNED → PICKED_UP → IN_TRANSIT → DELIVERED

### Issue: POD capture fails
**Solution:** Ensure camera permissions are granted; try manual signature

---

## Test Checklist

- [ ] Register as DP via UI
- [ ] Complete profile registration
- [ ] Verify redirect to /dashboard
- [ ] Toggle Online/Offline status
- [ ] Check available jobs page
- [ ] View deliveries list
- [ ] Check wallet page
- [ ] Check ratings page
- [ ] Submit KYC documents
- [ ] Test More dropdown menu links
- [ ] Verify logout works

---

## Complete Delivery Test Flow

To test a complete delivery cycle:

1. **Create a delivery** (as BC or EC user)
2. **DP goes online** with location near pickup
3. **DP receives notification** / checks available jobs
4. **DP accepts delivery**
5. **DP goes to pickup** → Mark as Picked Up
6. **DP travels** → Mark as In Transit
7. **DP arrives** → Send OTP to recipient
8. **Recipient provides OTP** → Verify OTP
9. **DP completes** → Mark as Delivered with POD
10. **Both parties can rate** each other

---

## Next Steps
After completing DP testing, say **"yes"** to proceed with **BC (Business Consumer)** role testing.
