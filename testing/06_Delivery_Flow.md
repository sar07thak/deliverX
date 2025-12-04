# Complete Delivery Flow Guide

## Overview

This document explains the complete delivery lifecycle in DeliverX, including who can perform each action and all possible status transitions.

---

## Roles Involved in Delivery

| Role | Code | Can Create | Can Accept | Can Cancel | Can Complete |
|------|------|------------|------------|------------|--------------|
| End Consumer | EC | Yes | No | Yes (own) | No |
| Business Consumer | BC/DBC | Yes | No | Yes (own) | No |
| Delivery Partner | DP | No | Yes | Yes (assigned) | Yes |
| Channel Manager | DPCM | No | No | Yes (managed DPs) | No |
| Super Admin | SA | No | No | Yes (any) | No |

---

## Delivery Status Flow

```
                                    ┌─────────────────┐
                                    │    CANCELLED    │
                                    └────────▲────────┘
                                             │
    ┌────────────┐    ┌────────────┐    ┌────┴───────┐    ┌────────────┐
    │  CREATED   │───►│  MATCHING  │───►│  ASSIGNED  │───►│ PICKED_UP  │
    └────────────┘    └─────┬──────┘    └────────────┘    └─────┬──────┘
                            │                                    │
                            ▼                                    ▼
                    ┌──────────────┐                      ┌────────────┐
                    │ UNASSIGNABLE │                      │ IN_TRANSIT │
                    └──────────────┘                      └─────┬──────┘
                                                                │
                                                                ▼
                                                         ┌────────────┐
                                                         │  DELIVERED │
                                                         └────────────┘
```

---

## Status Definitions

| Status | Description | Next Possible Status |
|--------|-------------|---------------------|
| **CREATED** | Delivery request submitted, pending DP matching | MATCHING, CANCELLED |
| **MATCHING** | System searching for available DPs | ASSIGNED, UNASSIGNABLE, CANCELLED |
| **ASSIGNED** | DP accepted the delivery | PICKED_UP, CANCELLED |
| **PICKED_UP** | DP collected package from sender | IN_TRANSIT, CANCELLED |
| **IN_TRANSIT** | DP en route to delivery location | DELIVERED, CANCELLED |
| **DELIVERED** | Package delivered, POD captured | (Final) |
| **CANCELLED** | Delivery cancelled by any party | (Final) |
| **UNASSIGNABLE** | No DP available/accepted | MATCHING (retry), CANCELLED |

---

## Phase 1: Delivery Creation

### Who Can Create?
- **EC (End Consumer)** - Personal deliveries
- **BC (Business Consumer)** - Business deliveries

### API Endpoint
```bash
POST /api/v1/deliveries
Authorization: Bearer <EC_TOKEN or BC_TOKEN>
```

### Request Body
```json
{
  "requesterType": "EC",  // or "DBC" for business
  "pickup": {
    "lat": 28.6139,
    "lng": 77.2090,
    "address": "123 Pickup Street, Delhi",
    "contactName": "Sender Name",
    "contactPhone": "9876543210",
    "instructions": "Ring doorbell twice"
  },
  "drop": {
    "lat": 28.7041,
    "lng": 77.1025,
    "address": "456 Drop Avenue, Delhi",
    "contactName": "Recipient Name",
    "contactPhone": "9876500000",
    "instructions": "Leave at door"
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
    "description": "Electronics"
  },
  "priority": "ASAP",
  "specialInstructions": "Fragile - handle with care"
}
```

### Response
```json
{
  "success": true,
  "deliveryId": "d1234567-89ab-cdef-0123-456789abcdef",
  "trackingId": "DLX1234567",
  "status": "CREATED",
  "estimatedPrice": 150.00
}
```

### Priority Options
| Priority | Description | Price Multiplier |
|----------|-------------|------------------|
| ASAP | Immediate pickup | 1.5x |
| SCHEDULED | Specific date/time | 1.0x |
| ECONOMY | Flexible timing | 0.8x |

---

## Phase 2: DP Matching

### Automatic Matching
After creation, system automatically searches for available DPs based on:
- DP location (within service area)
- DP availability status (ONLINE)
- Vehicle type compatibility
- Current workload
- Rating score

### Manual Trigger (if needed)
```bash
POST /api/v1/deliveries/{deliveryId}/match
Authorization: Bearer <EC_TOKEN or BC_TOKEN>
```

### Matching Algorithm
1. Find DPs within pickup radius (default: 5km)
2. Filter by availability (ONLINE only)
3. Filter by vehicle type (if specified)
4. Sort by:
   - Distance to pickup
   - Rating score
   - Completion rate
5. Send notification to top 5 DPs
6. First to accept gets assigned

### Status Transitions
- **Success**: CREATED → MATCHING → ASSIGNED
- **No DP Available**: CREATED → MATCHING → UNASSIGNABLE

---

## Phase 3: UNASSIGNABLE Handling

### When Does This Happen?
- No DPs online in service area
- All DPs rejected the request
- Matching timeout (default: 15 minutes)

### Who Gets Notified?
- **Consumer (EC/BC)**: Push notification + SMS
- **Admin**: Dashboard alert

### Options for Consumer
1. **Retry Matching** - Try again later
   ```bash
   POST /api/v1/deliveries/{deliveryId}/retry-match
   Authorization: Bearer <TOKEN>
   ```

2. **Cancel Delivery** - Get full refund
   ```bash
   POST /api/v1/deliveries/{deliveryId}/cancel
   Authorization: Bearer <TOKEN>
   Content-Type: application/json

   {"reason": "No delivery partners available"}
   ```

3. **Schedule for Later** - Change to scheduled delivery
   ```bash
   PUT /api/v1/deliveries/{deliveryId}
   Authorization: Bearer <TOKEN>
   Content-Type: application/json

   {
     "priority": "SCHEDULED",
     "scheduledAt": "2025-12-05T14:00:00"
   }
   ```

---

## Phase 4: DP Acceptance

### DP Receives Notification
DPs see available jobs in their app/dashboard with:
- Pickup & drop locations
- Distance & estimated time
- Package details
- Estimated earnings

### Accept Delivery
```bash
POST /api/v1/deliveries/{deliveryId}/accept
Authorization: Bearer <DP_TOKEN>
```

### Reject Delivery
```bash
POST /api/v1/deliveries/{deliveryId}/reject
Authorization: Bearer <DP_TOKEN>
Content-Type: application/json

{"reason": "Too far from current location"}
```

### After Acceptance
- Status: MATCHING → ASSIGNED
- Consumer notified with DP details
- DP gets pickup navigation

---

## Phase 5: Pickup

### DP Arrives at Pickup Location
```bash
POST /api/v1/deliveries/{deliveryId}/pickup
Authorization: Bearer <DP_TOKEN>
Content-Type: application/json

{
  "pickupLocation": {
    "latitude": 28.6139,
    "longitude": 77.2090
  },
  "pickupPhoto": "base64_encoded_image",
  "notes": "Collected from reception"
}
```

### Status Change
- ASSIGNED → PICKED_UP
- Consumer notified: "Your package has been picked up"

---

## Phase 6: In Transit

### DP Starts Journey
```bash
POST /api/v1/deliveries/{deliveryId}/transit
Authorization: Bearer <DP_TOKEN>
Content-Type: application/json

{
  "currentLocation": {
    "latitude": 28.6500,
    "longitude": 77.1500
  }
}
```

### Live Tracking Updates
DPs should send location updates every 30 seconds:
```bash
PUT /api/v1/deliveries/{deliveryId}/location
Authorization: Bearer <DP_TOKEN>
Content-Type: application/json

{
  "latitude": 28.6800,
  "longitude": 77.1200
}
```

### Status Change
- PICKED_UP → IN_TRANSIT

---

## Phase 7: Delivery Completion

### Step 1: Send OTP to Recipient
```bash
POST /api/v1/deliveries/{deliveryId}/otp/send
Authorization: Bearer <DP_TOKEN>
```
- OTP sent to recipient's phone
- Valid for 5 minutes

### Step 2: Verify OTP
```bash
POST /api/v1/deliveries/{deliveryId}/otp/verify
Authorization: Bearer <DP_TOKEN>
Content-Type: application/json

{"otp": "123456"}
```

### Step 3: Complete Delivery with POD
```bash
POST /api/v1/deliveries/{deliveryId}/deliver
Authorization: Bearer <DP_TOKEN>
Content-Type: application/json

{
  "deliveryLocation": {
    "latitude": 28.7041,
    "longitude": 77.1025
  },
  "recipientName": "John Doe",
  "signatureImage": "base64_encoded_signature",
  "deliveryPhoto": "base64_encoded_photo",
  "notes": "Delivered to recipient at door",
  "verificationMethod": "OTP"
}
```

### Verification Methods
| Method | Description |
|--------|-------------|
| OTP | Recipient enters OTP |
| SIGNATURE | Recipient signs on device |
| PHOTO | Photo of delivered package |
| PIN | Pre-shared PIN code |

### Status Change
- IN_TRANSIT → DELIVERED

---

## Cancellation Rules

### Who Can Cancel & When?

| Role | Can Cancel | Conditions | Refund |
|------|------------|------------|--------|
| EC/BC | Own deliveries | Before PICKED_UP | Full |
| EC/BC | Own deliveries | After PICKED_UP | Partial (50%) |
| DP | Assigned deliveries | Before PICKED_UP | N/A |
| DP | Assigned deliveries | After PICKED_UP | Penalty applied |
| DPCM | Managed DP deliveries | Any time | Per policy |
| Admin | Any delivery | Any time | Per policy |

### Cancel API
```bash
POST /api/v1/deliveries/{deliveryId}/cancel
Authorization: Bearer <TOKEN>
Content-Type: application/json

{
  "reason": "Customer requested cancellation",
  "cancelledBy": "CONSUMER"  // CONSUMER, DP, ADMIN, SYSTEM
}
```

### Cancellation Reasons (Standard)
| Code | Description |
|------|-------------|
| CUSTOMER_REQUEST | Customer changed mind |
| DP_UNAVAILABLE | DP became unavailable |
| ADDRESS_ISSUE | Invalid pickup/drop address |
| PACKAGE_ISSUE | Package not as described |
| PAYMENT_FAILED | Payment authorization failed |
| WEATHER | Adverse weather conditions |
| EMERGENCY | Emergency situation |
| OTHER | Other reason (specify) |

---

## Complete API Reference

### Consumer APIs (EC/BC)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/v1/deliveries | Create delivery |
| GET | /api/v1/deliveries | List my deliveries |
| GET | /api/v1/deliveries/{id} | Get delivery details |
| POST | /api/v1/deliveries/{id}/cancel | Cancel delivery |
| POST | /api/v1/deliveries/{id}/match | Trigger DP matching |
| POST | /api/v1/deliveries/{id}/retry-match | Retry matching |
| GET | /api/v1/deliveries/{id}/track | Real-time tracking |
| GET | /api/v1/deliveries/{id}/pod | Get proof of delivery |

### DP APIs

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/v1/deliveries/pending | Get available jobs |
| GET | /api/v1/deliveries?role=dp | Get my assigned deliveries |
| POST | /api/v1/deliveries/{id}/accept | Accept delivery |
| POST | /api/v1/deliveries/{id}/reject | Reject delivery |
| POST | /api/v1/deliveries/{id}/pickup | Mark as picked up |
| POST | /api/v1/deliveries/{id}/transit | Mark as in transit |
| PUT | /api/v1/deliveries/{id}/location | Update location |
| POST | /api/v1/deliveries/{id}/otp/send | Send delivery OTP |
| POST | /api/v1/deliveries/{id}/otp/verify | Verify delivery OTP |
| POST | /api/v1/deliveries/{id}/deliver | Complete delivery |
| POST | /api/v1/deliveries/{id}/cancel | Cancel (with penalty) |

### Admin APIs

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/v1/admin/deliveries | List all deliveries |
| GET | /api/v1/admin/deliveries/{id} | Get any delivery |
| POST | /api/v1/admin/deliveries/{id}/cancel | Force cancel |
| POST | /api/v1/admin/deliveries/{id}/reassign | Reassign to another DP |

---

## Payment Flow

### When Payment Happens

| Stage | Action |
|-------|--------|
| CREATED | Amount held from wallet |
| CANCELLED (before pickup) | Full refund to wallet |
| CANCELLED (after pickup) | Partial refund (50%) |
| DELIVERED | Amount transferred to DP wallet |

### Commission Distribution (on DELIVERED)

```
Total Amount: ₹100
├── Platform Fee (10%): ₹10 → Platform
├── DPCM Commission (5%): ₹5 → Channel Manager
└── DP Earnings (85%): ₹85 → Delivery Partner
```

---

## Event Timeline Example

```
Time     | Status      | Action                    | Actor
---------|-------------|---------------------------|--------
10:00:00 | CREATED     | Delivery request created  | BC
10:00:05 | MATCHING    | Searching for DPs         | System
10:00:30 | ASSIGNED    | DP accepted delivery      | DP
10:15:00 | PICKED_UP   | Package collected         | DP
10:16:00 | IN_TRANSIT  | En route to destination   | DP
10:45:00 | DELIVERED   | Package delivered + POD   | DP
10:45:30 | -           | Rating requested          | System
```

---

## Testing the Complete Flow

### Prerequisites
1. API running on `http://localhost:5205`
2. At least one EC/BC user
3. At least one DP user (KYC approved, online)

### Step-by-Step Test

**1. Create Delivery (as BC/EC)**
```bash
BC_TOKEN="your_bc_token"

curl -X POST http://localhost:5205/api/v1/deliveries \
  -H "Authorization: Bearer $BC_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "requesterType": "DBC",
    "pickup": {"lat": 28.6139, "lng": 77.2090, "address": "CP Delhi", "contactName": "Test", "contactPhone": "9999999999"},
    "drop": {"lat": 28.7041, "lng": 77.1025, "address": "Rohini Delhi", "contactName": "Recipient", "contactPhone": "8888888888"},
    "package": {"weightKg": 1, "type": "parcel", "description": "Test package"},
    "priority": "ASAP"
  }'
```

**2. DP Goes Online**
```bash
DP_TOKEN="your_dp_token"

curl -X PUT http://localhost:5205/api/v1/deliveries/availability \
  -H "Authorization: Bearer $DP_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"status": "ONLINE", "latitude": 28.6139, "longitude": 77.2090}'
```

**3. DP Checks Pending Deliveries**
```bash
curl -X GET http://localhost:5205/api/v1/deliveries/pending \
  -H "Authorization: Bearer $DP_TOKEN"
```

**4. DP Accepts Delivery**
```bash
curl -X POST http://localhost:5205/api/v1/deliveries/{deliveryId}/accept \
  -H "Authorization: Bearer $DP_TOKEN"
```

**5. DP Marks Pickup**
```bash
curl -X POST http://localhost:5205/api/v1/deliveries/{deliveryId}/pickup \
  -H "Authorization: Bearer $DP_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"pickupLocation": {"latitude": 28.6139, "longitude": 77.2090}}'
```

**6. DP Marks In Transit**
```bash
curl -X POST http://localhost:5205/api/v1/deliveries/{deliveryId}/transit \
  -H "Authorization: Bearer $DP_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"currentLocation": {"latitude": 28.65, "longitude": 77.15}}'
```

**7. DP Sends OTP**
```bash
curl -X POST http://localhost:5205/api/v1/deliveries/{deliveryId}/otp/send \
  -H "Authorization: Bearer $DP_TOKEN"
```

**8. DP Verifies OTP & Completes**
```bash
curl -X POST http://localhost:5205/api/v1/deliveries/{deliveryId}/deliver \
  -H "Authorization: Bearer $DP_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "deliveryLocation": {"latitude": 28.7041, "longitude": 77.1025},
    "recipientName": "Test Recipient",
    "verificationMethod": "OTP"
  }'
```

---

## Error Handling

| Error Code | Description | Resolution |
|------------|-------------|------------|
| DELIVERY_NOT_FOUND | Invalid delivery ID | Check delivery ID |
| INVALID_STATUS_TRANSITION | Cannot change to requested status | Follow status flow |
| UNAUTHORIZED_ACTION | User cannot perform this action | Check role permissions |
| DP_NOT_AVAILABLE | DP went offline | Reassign or cancel |
| INSUFFICIENT_BALANCE | Wallet balance too low | Recharge wallet |
| OTP_INVALID | Wrong OTP entered | Resend OTP |
| OTP_EXPIRED | OTP validity expired | Resend OTP |

---

## Notifications Sent

| Event | Recipient | Channel |
|-------|-----------|---------|
| Delivery Created | Consumer | App, Email |
| DP Assigned | Consumer, DP | App, SMS |
| Pickup Complete | Consumer | App |
| In Transit | Consumer | App |
| Delivery OTP | Recipient | SMS |
| Delivered | Consumer, DP | App, Email |
| Cancelled | Consumer, DP | App, SMS |
| Unassignable | Consumer | App, SMS |
