# 🧪 How to Test DeliverX IAM APIs in Postman

## ⚠️ Important Notes

1. **No Swagger UI Available** - Due to .NET 10 compatibility issues, we use Postman instead
2. **API is Headless** - Opening `http://localhost:5205` in browser will show "Page not found" - **this is normal!**
3. **Use Postman** - Download from https://www.postman.com/downloads/

---

## 🚀 Quick Start (3 Steps)

### Step 1: Start the API Server
Open a terminal and run:
```bash
cd src/DeliverX.API
dotnet run
```

**Wait for this message:**
```
Now listening on: http://localhost:5205
```

**IMPORTANT:** Keep this terminal window open! Don't close it.

---

### Step 2: Download & Open Postman
- Download: https://www.postman.com/downloads/
- Install and open Postman
- Click "New" → "HTTP Request" (or just click the + tab)

---

### Step 3: Start Testing!
Follow the test cases below ⬇️

---

## 📝 Postman Testing Guide

### ✅ Test 1: Send OTP (Phone Login)

**In Postman:**

1. **Method:** Select `POST` from dropdown
2. **URL:** Enter `http://localhost:5205/api/v1/auth/otp/send`
3. **Headers Tab:**
   - Click "Headers"
   - Add: Key = `Content-Type`, Value = `application/json`
4. **Body Tab:**
   - Click "Body"
   - Select "raw"
   - Select "JSON" from the dropdown (right side)
   - Paste this:
   ```json
   {
     "phone": "9876543210",
     "countryCode": "+91"
   }
   ```
5. **Click:** "Send" button (blue button on right)

**✅ Expected Response:**
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

**📋 COPY THE OTP:** Look in the "message" field and copy the 6-digit OTP (e.g., `123456`)

---

### ✅ Test 2: Verify OTP & Get Tokens

**In Postman:**

1. **Method:** Select `POST`
2. **URL:** `http://localhost:5205/api/v1/auth/otp/verify`
3. **Headers:**
   - Key = `Content-Type`, Value = `application/json`
4. **Body (raw → JSON):**
   ```json
   {
     "phone": "9876543210",
     "otp": "123456",
     "deviceId": "my-laptop"
   }
   ```
   **⚠️ IMPORTANT:** Replace `123456` with the actual OTP from Test 1!
5. **Click:** "Send"

**✅ Expected Response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "550e8400-e29b-41d4-a716-446655440000",
    "expiresIn": 900,
    "user": {
      "id": "guid-here",
      "role": "EC",
      "phone": "9876543210"
    }
  }
}
```

**📋 COPY THE ACCESS TOKEN:** Copy the entire `accessToken` value (the long string starting with `eyJ...`)

---

### ✅ Test 3: Get Sessions (Protected Endpoint - Requires Token)

**In Postman:**

1. **Method:** Select `GET`
2. **URL:** `http://localhost:5205/api/v1/auth/sessions`
3. **Headers:**
   - Key = `Authorization`, Value = `Bearer YOUR_ACCESS_TOKEN`

   **⚠️ IMPORTANT:**
   - Type the word `Bearer` followed by a space, then paste your access token
   - Example: `Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`
4. **Body:** Leave empty (no body needed for GET requests)
5. **Click:** "Send"

**✅ Expected Response:**
```json
{
  "success": true,
  "data": {
    "sessions": [
      {
        "id": "guid-here",
        "deviceId": "my-laptop",
        "ipAddress": "::1",
        "lastActive": "2025-11-19T10:30:00Z",
        "isCurrent": true
      }
    ]
  }
}
```

**🎉 Success!** You can see your active session!

---

### ✅ Test 4: Email/Password Login (Optional)

**First, create a test user in the database:**

Open a new terminal and run:
```bash
sqlite3 src/DeliverX.API/deliverx.db

INSERT INTO Users (Id, Email, PasswordHash, Role, IsActive, Is2FAEnabled, CreatedAt, UpdatedAt)
VALUES (
  lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1, 1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6))),
  'admin@deliverx.com',
  '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYpuEJUqVqC',
  'SuperAdmin',
  1,
  0,
  datetime('now'),
  datetime('now')
);
.quit
```

**Now test login in Postman:**

1. **Method:** Select `POST`
2. **URL:** `http://localhost:5205/api/v1/auth/login`
3. **Headers:**
   - Key = `Content-Type`, Value = `application/json`
4. **Body (raw → JSON):**
   ```json
   {
     "email": "admin@deliverx.com",
     "password": "SecurePassword123!",
     "deviceId": "my-laptop"
   }
   ```
5. **Click:** "Send"

**✅ Expected Response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "550e8400-e29b-41d4-a716-446655440000",
    "expiresIn": 900,
    "user": {
      "id": "guid-here",
      "role": "SuperAdmin",
      "email": "admin@deliverx.com"
    }
  }
}
```

---

### ✅ Test 5: Refresh Token

**In Postman:**

1. **Method:** Select `POST`
2. **URL:** `http://localhost:5205/api/v1/auth/refresh`
3. **Headers:**
   - Key = `Content-Type`, Value = `application/json`
4. **Body (raw → JSON):**
   ```json
   {
     "refreshToken": "YOUR_REFRESH_TOKEN"
   }
   ```
   **⚠️ Replace with refreshToken from Test 2 or Test 4**
5. **Click:** "Send"

**✅ Expected Response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "550e8400-e29b-41d4-a716-446655440000",
    "expiresIn": 900
  }
}
```

**📋 You got a new access token!**

---

### ✅ Test 6: Logout

**In Postman:**

1. **Method:** Select `POST`
2. **URL:** `http://localhost:5205/api/v1/auth/logout`
3. **Headers:**
   - Key = `Content-Type`, Value = `application/json`
   - Key = `Authorization`, Value = `Bearer YOUR_ACCESS_TOKEN`
4. **Body (raw → JSON):**
   ```json
   {
     "refreshToken": "YOUR_REFRESH_TOKEN",
     "logoutAll": false
   }
   ```
   **⚠️ Replace `YOUR_REFRESH_TOKEN` with the refreshToken from Test 2**

   **💡 Tip:** Set `"logoutAll": true` to logout from ALL devices
5. **Click:** "Send"

**✅ Expected Response:**
```json
{
  "success": true,
  "message": "Logged out successfully"
}
```

---

### ✅ Test 7: Delete Specific Session

**In Postman:**

1. **First, get a session ID from Test 3** (copy the `id` field)
2. **Method:** Select `DELETE`
3. **URL:** `http://localhost:5205/api/v1/auth/sessions/SESSION_ID_HERE`

   **Example:** `http://localhost:5205/api/v1/auth/sessions/550e8400-e29b-41d4-a716-446655440000`
4. **Headers:**
   - Key = `Authorization`, Value = `  YOUR_ACCESS_TOKEN`
5. **Body:** Leave empty (no body needed for DELETE)
6. **Click:** "Send"

**✅ Expected Response:**
```json
{
  "success": true,
  "message": "Session revoked successfully"
}
```

---

## 🎯 All Tests Complete!

You've successfully tested all 7 APIs:
- ✅ Test 1: Send OTP
- ✅ Test 2: Verify OTP & Get Tokens
- ✅ Test 3: Get Sessions (Protected)
- ✅ Test 4: Email/Password Login
- ✅ Test 5: Refresh Token
- ✅ Test 6: Logout
- ✅ Test 7: Delete Specific Session

---

## 📊 All Available API Endpoints

| # | Endpoint | Method | Auth? | What it does |
|---|----------|--------|-------|--------------|
| 1 | `/api/v1/auth/otp/send` | POST | No | Send OTP to phone |
| 2 | `/api/v1/auth/otp/verify` | POST | No | Verify OTP, get tokens |
| 3 | `/api/v1/auth/sessions` | GET | Yes | Get all user sessions |
| 4 | `/api/v1/auth/logout` | POST | Yes | Logout (revoke session) |
| 5 | `/api/v1/auth/refresh` | POST | No | Refresh access token |
| 6 | `/api/v1/auth/login` | POST | No | Email/password login |
| 7 | `/api/v1/auth/sessions/{id}` | DELETE | Yes | Delete specific session |

**For Auth = "Yes":** Add `Authorization: Bearer YOUR_TOKEN` header

---

## 🐛 Common Issues & Solutions

### ❌ "Page cannot be found" when opening localhost:5205 in browser
**This is normal!** The API has no web page. You must use Postman to send requests.

### ❌ "Connection refused" or "Could not get response"
**Solution:** Make sure the API is running:
- Check your terminal shows: `Now listening on: http://localhost:5205`
- If not running, start it: `cd src/DeliverX.API && dotnet run`

### ❌ "401 Unauthorized" on protected endpoints
**Solutions:**
- Make sure you added the `Authorization` header
- Check you typed `Bearer ` (with space) before the token
- Token expires after 15 minutes - get a new one if needed

### ❌ Can't find the OTP in the response
**Solution:** Look in the `message` field:
```json
"message": "OTP sent successfully. OTP: 123456 (expires in 5 minutes)"
```
Copy the 6-digit number after "OTP: "

---

## 🎉 Summary

**You've learned to test:**
- ✅ Send OTP to phone
- ✅ Verify OTP and get authentication tokens
- ✅ Access protected endpoints with Bearer token
- ✅ Logout from session

**API is running on:** `http://localhost:5205`

**Need more endpoints?** Check the table above for all 7 available APIs!

---

**💡 Pro Tip:** Save your Postman requests in a Collection so you don't have to retype them every time!
