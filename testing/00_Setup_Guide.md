# DeliveryDost MVC - Setup & Testing Guide

## Quick Start

### Start the Web Application
```bash
cd src/DeliveryDost.Web
dotnet run
```

**Application URL:** http://localhost:5300

---

## Test Accounts (Recommended Phone Numbers)

| Role | Phone Number | Description |
|------|--------------|-------------|
| **SuperAdmin** | 9999999999 | Platform administrator |
| **DPCM** | 8888800001 | Delivery Partner Cluster Manager |
| **DP** | 7878798797 | Delivery Partner |
| **BC** | 7772223333 | Business Consumer |
| **EC** | 6666666666 | End Consumer |

---

## Testing Order (Follow This Sequence)

### Step 1: Admin Test (01_Admin_Test.md)
- Login as SuperAdmin (9999999999)
- View admin dashboard
- Access User Management, KYC Requests, Complaints

### Step 2: DPCM Test (02_DPCM_Test.md)
- Login as DPCM (8888800001)
- Complete registration (3 steps)
- Set service area
- Get referral code for DPs

### Step 3: DP Test (03_DP_Test.md)
- Login as DP (7878798797)
- Complete 5-step registration
- Use DPCM referral code
- Configure pricing & service area

### Step 4: BC Test (04_BC_Test.md)
- Login as BC (7772223333)
- View dashboard
- Create delivery requests

### Step 5: EC Test (05_EC_Test.md)
- Login as EC (6666666666)
- Create delivery request
- Track deliveries

### Step 6: Delivery Flow (06_Delivery_Flow.md)
- Complete end-to-end delivery test
- DP accepts, picks up, delivers
- Consumer tracks and rates

---

## How to Login

1. Open http://localhost:5300/Account/Login
2. Select your role (Consumer, Business, Delivery, Manager, Admin)
3. Enter phone number (10 digits)
4. Click **Send OTP**
5. Enter the OTP shown on screen (Dev Mode shows OTP)
6. Click **Verify & Continue**

---

## Features by Role

### SuperAdmin
- Admin Dashboard with platform stats
- User Management (view, activate/deactivate users)
- KYC Requests (approve/reject)
- Complaints Management

### DPCM (Manager)
- DPCM Dashboard with managed DP stats
- My DPs (view registered delivery partners)
- Service Area configuration
- Wallet & settlements

### DP (Delivery Partner)
- DP Dashboard with earnings
- Available Deliveries (accept new orders)
- Active Delivery (current delivery)
- Delivery History
- Service Area configuration
- Wallet

### BC/EC (Consumers)
- Dashboard with delivery stats
- New Delivery (create requests)
- My Deliveries (track orders)
- Wallet (top-up, transactions)
- My Complaints

---

## Database Reset (Fresh Start)

```bash
# Delete databases
rm src/DeliverX.API/deliverx.db
rm src/DeliveryDost.Web/deliverydost.db

# Run migrations
dotnet ef database update --project src/DeliverX.Infrastructure --startup-project src/DeliverX.API

# Copy to web project
cp src/DeliverX.API/deliverx.db src/DeliveryDost.Web/deliverydost.db
```

---

## Troubleshooting

### Port 5300 already in use
```bash
netstat -ano | findstr :5300
taskkill /PID <pid> /F
```

### Database locked error
Stop the running application before deleting database files.

### OTP not working
- OTP is displayed on the verification screen in dev mode
- Enter the exact 6-digit code shown
- OTP expires in 5 minutes

### Page not found after login
- This is fixed - BC/EC users now go directly to Dashboard
- Only DP and DPCM have registration flows

---

## Project Structure

```
src/
├── DeliverX.API/           # Backend API (port 5205)
├── DeliverX.Application/   # Business logic
├── DeliverX.Domain/        # Entities
├── DeliverX.Infrastructure/# Database
└── DeliveryDost.Web/       # MVC Web UI (port 5300)
```

---

## Next Steps

1. Follow test guides in order (01 through 06)
2. Each guide has step-by-step instructions
3. Screenshots/expected results are described
4. Complete all steps before moving to next guide
