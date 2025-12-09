# DeliveryDost - Test Accounts & Testing Guide

## Application URL
- **Development**: http://localhost:5300

## Authentication
All accounts use OTP-based authentication. Enter phone number, receive OTP, and login.

## Test Accounts Overview

| Role | Phone (New/Register) | Phone (Complete Profile) | Description |
|------|---------------------|-------------------------|-------------|
| SuperAdmin | - | 9999999999 | Full system access |
| Admin | - | 9999999998 | Administrative access |
| DPCM | 9100000001 | 9100000002 | Delivery Partner Channel Manager |
| DP | 9200000001, 9200000002 | 9200000003 | Delivery Partner |
| BC | 9300000001 | 9300000002 | Business Consumer |
| EC | 9400000001 | - | Enterprise Client |
| DBC | 9500000001 | - | Delivery Business Consumer |

### Account States
- **New/Register**: Needs to complete registration flow
- **Complete Profile**: Already has profile set up, can access dashboard

## Quick Start Testing

### 1. Test Registration Flow
Use "New" phone numbers to test the complete registration process.

### 2. Test Dashboard Features
Use "Complete Profile" phone numbers to test features without registration.

## Individual Role Test Files
- [SuperAdmin Testing](./01-superadmin-testing.md)
- [Admin Testing](./02-admin-testing.md)
- [DPCM Testing](./03-dpcm-testing.md)
- [DP Testing](./04-dp-testing.md)
- [BC Testing](./05-bc-testing.md)
- [EC Testing](./06-ec-testing.md)
- [DBC Testing](./07-dbc-testing.md)

## Reset Test Data
To reset all data and recreate test accounts:
```sql
-- Run this script in SQL Server Management Studio
-- File: scripts/reset-and-seed-test-data.sql
```
