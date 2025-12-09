# DeliveryDost - Quick Reference Card

## Application URL
```
http://localhost:5300
```

## Test Accounts - Quick Reference

### Admin Accounts (Profile Complete)
| Role | Phone | Password |
|------|-------|----------|
| SuperAdmin | 9999999999 | OTP |
| Admin | 9999999998 | OTP |

### DPCM Accounts
| Status | Phone | Notes |
|--------|-------|-------|
| New (Register) | 9100000001 | Complete registration flow |
| Complete | 9100000002 | Dashboard access |

### DP Accounts
| Status | Phone | Notes |
|--------|-------|-------|
| New (Register) | 9200000001 | Registration without referral |
| New (Register) | 9200000002 | Registration with DPCM referral |
| Complete | 9200000003 | Dashboard access |

### BC Accounts
| Status | Phone | Notes |
|--------|-------|-------|
| New (Register) | 9300000001 | Complete registration flow |
| Complete | 9300000002 | Dashboard access |

### EC Accounts
| Status | Phone | Notes |
|--------|-------|-------|
| New (Register) | 9400000001 | Enterprise registration |

### DBC Accounts
| Status | Phone | Notes |
|--------|-------|-------|
| New (Register) | 9500000001 | DBC registration |

## Common Test Data

### Bank Details (for registration)
```
Account Number: 1234567890123456
Confirm Account: 1234567890123456
IFSC Code: SBIN0001234
```

### PAN Numbers (for KYC)
```
ABCDE1234F (DPCM)
FGHIJ5678K (DP)
LMNOP9876Q (BC)
QRSTU4567V (DBC)
AABCE1234F (EC)
```

### Aadhaar (Last 4 digits)
```
1234
5678
9012
```

## Testing Flow Order

### Recommended Test Sequence:
1. **DPCM Registration** (9100000001)
   - Complete all steps
   - Get referral code

2. **DP Registration with Referral** (9200000002)
   - Use DPCM referral code from step 1
   - Complete registration

3. **BC Registration** (9300000001)
   - Complete registration
   - Add wallet balance

4. **Book Delivery** (as BC 9300000002)
   - Book a delivery
   - DP accepts (9200000003)
   - Complete delivery flow

5. **Verify Commission** (DPCM 9100000002)
   - Check commission from DP's delivery

## Reset Database
```sql
-- Run: scripts/reset-and-seed-test-data.sql
```

## OTP in Development
Check console logs for OTP during testing.

## Common Issues

### 1. "Bank verification failed"
- Fixed: Encryption key issue resolved

### 2. Form validation errors
- Use `novalidate` forms
- Check required fields

### 3. Session expired
- Re-login with OTP

### 4. Dashboard not loading
- Check if profile is complete
- New users redirect to registration
