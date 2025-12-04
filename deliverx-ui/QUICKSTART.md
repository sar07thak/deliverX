# DeliverX UI - Quick Start Guide

## Prerequisites Check

Before starting, ensure:
- ✅ Backend API is running on http://localhost:5205
- ✅ Node.js is installed (v14+)
- ✅ npm dependencies are installed

## Step-by-Step Launch

### 1. Start the Application

```bash
cd C:\Users\HP\Desktop\finnidTech\deliverx-ui
npm start
```

The app will automatically open at http://localhost:3000

### 2. Test the Complete Flow

#### Phase 1: Login
1. Open http://localhost:3000
2. Enter phone number: `9876543210` (or any 10-digit number)
3. Click "Send OTP"
4. Enter the OTP received (check backend logs for OTP)
5. Click "Verify & Login"

#### Phase 2: Registration
After successful login, you'll be redirected to registration:

**Step 1 - Personal Information**
- Name: `John Doe`
- DOB: `1990-01-01`
- Gender: `Male`
- Email: `john@example.com`
- Address:
  - Street: `123 Main Street`
  - City: `Bangalore`
  - State: `Karnataka`
  - Pincode: `560001`
- Click "Next"

**Step 2 - Service Details**
- Vehicle Type: `Bike`
- Languages: Select `English` and `Hindi`
- Availability: `Full-time`
- Service Area: `Bangalore Central`
- Pricing:
  - Base Rate: `30`
  - Per KM: `10`
  - Per Minute: `2`
- Click "Next"

**Step 3 - Review & Submit**
- Review all details
- Click "Complete Registration"

#### Phase 3: KYC Verification

You'll be redirected to the dashboard. Complete each verification:

**A. Aadhaar Verification**
1. Go to "KYC" page
2. Click "Start Verification" under Aadhaar
3. Click "Verify with DigiLocker"
4. Wait for simulated verification (2 seconds)
5. See verified details

**B. PAN Verification**
1. Click "Start Verification" under PAN
2. Enter PAN: `ABCDE1234F` (use valid format)
3. Click "Verify PAN"
4. See verification result with name match score

**C. Bank Verification**
1. Click "Start Verification" under Bank
2. Enter details:
   - Account Number: `1234567890123`
   - IFSC Code: `SBIN0001234`
   - Holder Name: `John Doe`
3. Click "Verify Account"
4. Wait for penny drop simulation
5. See verified bank details

#### Phase 4: Dashboard
- Go to Dashboard
- See overall status: "FULLY_VERIFIED"
- All three verifications should show green checkmarks
- "Start Delivering" button should be enabled

## Testing Different Scenarios

### Test 1: Fresh Registration
```bash
# Clear localStorage
# In browser console:
localStorage.clear()
# Then refresh and start from login
```

### Test 2: Existing User Login
```bash
# Login with a phone number that completed registration
# Should redirect directly to dashboard
```

### Test 3: Partial KYC
```bash
# Complete registration
# Complete only Aadhaar verification
# Check dashboard shows "PENDING" status
```

## Common Test Data

### Phone Numbers
- `9876543210`
- `9999999999`
- `8888888888`

### PAN Numbers (Valid Format)
- `ABCDE1234F`
- `PQRST5678Z`
- `LMNOP9012X`

### IFSC Codes (Valid Format)
- `SBIN0001234` - State Bank of India
- `HDFC0001234` - HDFC Bank
- `ICIC0001234` - ICICI Bank

### Bank Account Numbers
- `1234567890123` (13 digits)
- `987654321012345` (15 digits)

## API Endpoints Being Used

### Authentication Flow
1. POST `/api/auth/send-otp` - Sends OTP to phone
2. POST `/api/auth/verify-otp` - Verifies OTP, returns token

### Registration Flow
3. POST `/api/registration/initiate` - Creates user account
4. POST `/api/registration/complete-profile` - Saves full profile

### KYC Flow
5. POST `/api/kyc/aadhaar/initiate` - Starts Aadhaar verification
6. POST `/api/kyc/aadhaar/complete` - Completes Aadhaar verification
7. POST `/api/kyc/pan/verify` - Verifies PAN card
8. POST `/api/kyc/bank/verify` - Verifies bank account
9. GET `/api/kyc/status/:userId` - Gets overall KYC status

## Browser Console Checks

### Check Authentication
```javascript
// Get current token
localStorage.getItem('token')

// Get current user
JSON.parse(localStorage.getItem('user'))
```

### Check Network Requests
1. Open DevTools (F12)
2. Go to Network tab
3. Filter by "Fetch/XHR"
4. Watch API calls as you navigate

### Debug Errors
```javascript
// Clear all data and start fresh
localStorage.clear()
sessionStorage.clear()
location.reload()
```

## Expected Behavior

### Login Page
- Phone input accepts only 10 digits
- OTP input accepts only 6 digits
- Loading spinner shows during API calls
- Error messages display on failure
- Success message shows "OTP sent successfully"

### Registration Page
- Progress indicator shows 1/3, 2/3, 3/3
- Form validation on each step
- Can navigate back to edit previous steps
- Review page shows all entered data
- Redirects to KYC page on completion

### KYC Pages
- Each verification is independent
- Loading states during verification
- Success messages on completion
- Auto-redirect to KYC hub after success
- Status badges update in real-time

### Dashboard
- Shows verification progress percentage
- Green checkmarks for completed verifications
- "Start Delivering" enabled only when fully verified
- Profile card shows user details
- Refresh button updates status

## Troubleshooting

### Issue: "No response from server"
**Solution:**
```bash
# Check if backend is running
curl http://localhost:5205/health
# or
curl http://localhost:5205/api/auth/send-otp -X POST -H "Content-Type: application/json" -d "{\"phoneNumber\":\"+919876543210\"}"
```

### Issue: "Invalid token" or auto-logout
**Solution:**
```javascript
// Clear localStorage and login again
localStorage.clear()
// Then refresh page
```

### Issue: App shows blank page
**Solution:**
```bash
# Check console for errors (F12)
# Restart the dev server
npm start
```

### Issue: "Cannot read property of undefined"
**Solution:**
```javascript
// User data might be corrupted
localStorage.removeItem('user')
localStorage.removeItem('token')
location.reload()
```

## Performance Tips

- Open DevTools Network tab to see API response times
- Check Console for any warnings or errors
- Use React DevTools extension to inspect component state
- Monitor localStorage size (should stay < 5MB)

## What to Check

✅ **Login works** - OTP sent and verified
✅ **Registration saves** - All form data persists
✅ **Aadhaar verifies** - DigiLocker simulation works
✅ **PAN verifies** - Name matching works
✅ **Bank verifies** - Penny drop simulation works
✅ **Dashboard updates** - Status reflects completed KYC
✅ **Navigation works** - All routes accessible
✅ **Logout works** - Clears session and redirects
✅ **Protected routes** - Redirect to login when not authenticated
✅ **Error handling** - Friendly messages on failures
✅ **Loading states** - Spinners show during API calls
✅ **Responsive design** - Works on mobile viewport

## Mobile Testing

1. Open Chrome DevTools (F12)
2. Click "Toggle device toolbar" (Ctrl+Shift+M)
3. Select "iPhone 12 Pro" or "Galaxy S20"
4. Test the complete flow on mobile viewport

## Next Steps After Testing

1. ✅ Verify all 8 API endpoints work
2. ✅ Check error handling for each API
3. ✅ Test with invalid data
4. ✅ Test with network failures
5. ✅ Verify localStorage persistence
6. ✅ Test browser back/forward buttons
7. ✅ Test direct URL access to protected routes
8. ✅ Test logout and re-login

## Production Checklist

Before deploying to production:

- [ ] Update API base URL in `src/services/api.js`
- [ ] Enable real DigiLocker integration
- [ ] Add environment variables for API URL
- [ ] Build production bundle: `npm run build`
- [ ] Test production build: `npx serve -s build`
- [ ] Configure CORS on backend for production domain
- [ ] Add analytics tracking
- [ ] Add error logging (Sentry, etc.)
- [ ] Set up CI/CD pipeline
- [ ] Configure HTTPS

## Support

If you encounter any issues:
1. Check browser console for errors
2. Check backend logs for API errors
3. Verify all environment variables
4. Clear cache and try again
5. Check this guide for common solutions

---

**Ready to test! Start the backend, then run `npm start`** 🚀
