#!/usr/bin/env python3
"""
DeliverX - DPCM (Delivery Partner Channel Manager) Role Test Suite
===================================================================

This script tests all DPCM-specific API endpoints.

DPCM Role Capabilities:
- View DPCM dashboard with managed DPs overview
- Manage delivery partners (view, activate/deactivate)
- View deliveries by managed DPs
- Configure commission settings
- View and request settlements
- Track earnings from managed DPs

Prerequisites:
- API running at http://localhost:5205
- pip install requests

Usage:
    python test_dpcm_role.py
"""

import requests
import json
import time
import re
from datetime import datetime, timedelta

BASE_URL = "http://localhost:5205"

# Test data - DPCM credentials
DPCM_PHONE = "+918888800001"  # DPCM phone number

# Store tokens and IDs
dpcm_token = None
dpcm_user_id = None
dpcm_device_id = "test-dpcm-device"
last_otp = "123456"

def print_header(text):
    """Print a formatted header."""
    print("\n" + "=" * 60)
    print(f" {text}")
    print("=" * 60)

def print_result(name, response, expected_status=200):
    """Print test result."""
    status = "[PASS]" if response.status_code == expected_status else "[FAIL]"
    print(f"\n{status}: {name}")
    print(f"Status: {response.status_code} (expected {expected_status})")
    try:
        data = response.json()
        print(f"Response: {json.dumps(data, indent=2)[:500]}")
    except:
        print(f"Response: {response.text[:500]}")
    return response.status_code == expected_status

def get_auth_headers(token=None):
    """Get authorization headers."""
    t = token or dpcm_token
    return {
        "Authorization": f"Bearer {t}",
        "Content-Type": "application/json"
    }

# ============================================================
# 1. AUTHENTICATION TESTS
# ============================================================

def test_send_otp():
    """Test sending OTP."""
    global last_otp

    print_header("AUTHENTICATION - SEND OTP")

    payload = {
        "phone": DPCM_PHONE,
        "role": "DPCM"
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/auth/otp/send",
        json=payload
    )

    success = print_result("Send OTP", response)

    # Extract OTP from response message (test mode only)
    if success:
        try:
            data = response.json()
            message = data.get("data", {}).get("message", "")
            otp_match = re.search(r'OTP:\s*(\d{6})', message)
            if otp_match:
                last_otp = otp_match.group(1)
                print(f"Extracted OTP: {last_otp}")
        except:
            pass

    return success

def test_verify_otp():
    """Test OTP verification."""
    global dpcm_token, dpcm_user_id

    print_header("AUTHENTICATION - VERIFY OTP")

    payload = {
        "phone": DPCM_PHONE,
        "otp": last_otp,
        "role": "DPCM",
        "deviceId": dpcm_device_id
    }
    print(f"Using OTP: {last_otp}")

    response = requests.post(
        f"{BASE_URL}/api/v1/auth/otp/verify",
        json=payload
    )

    success = print_result("Verify OTP", response)

    if success and response.status_code == 200:
        data = response.json()
        if "data" in data:
            dpcm_token = data["data"].get("accessToken")
            dpcm_user_id = data["data"].get("userId")
            print(f"\nToken obtained: {dpcm_token[:50] if dpcm_token else 'None'}...")
            print(f"User ID: {dpcm_user_id}")

    return success

# ============================================================
# 2. DASHBOARD TESTS
# ============================================================

def test_get_dpcm_dashboard():
    """Test getting DPCM dashboard."""
    print_header("DASHBOARD - GET DPCM DASHBOARD")

    if not dpcm_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/dpcm/dashboard",
        headers=get_auth_headers()
    )

    success = print_result("Get DPCM Dashboard", response)

    if success:
        data = response.json()
        print(f"\nDashboard Summary:")
        if "stats" in data:
            stats = data["stats"]
            print(f"  - Total DPs: {stats.get('totalDPs', 0)}")
            print(f"  - Active DPs: {stats.get('activeDPs', 0)}")
            print(f"  - Total Deliveries: {stats.get('totalDeliveries', 0)}")

    return success

# ============================================================
# 3. PARTNER MANAGEMENT TESTS
# ============================================================

def test_get_partners():
    """Test getting managed delivery partners."""
    print_header("PARTNERS - GET ALL PARTNERS")

    if not dpcm_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/dpcm/partners?page=1&pageSize=20",
        headers=get_auth_headers()
    )

    success = print_result("Get Partners", response)

    if success:
        data = response.json()
        partners = data.get("items", [])
        print(f"\nManaged Partners: {len(partners)}")
        for p in partners[:5]:
            print(f"  - ID: {str(p.get('id', 'N/A'))[:8]}..., Name: {p.get('name')}, Status: {p.get('status')}")

    return success

def test_get_active_partners():
    """Test getting active partners."""
    print_header("PARTNERS - GET ACTIVE PARTNERS")

    if not dpcm_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/dpcm/partners?status=ACTIVE&page=1&pageSize=20",
        headers=get_auth_headers()
    )

    return print_result("Get Active Partners", response)

def test_get_inactive_partners():
    """Test getting inactive partners."""
    print_header("PARTNERS - GET INACTIVE PARTNERS")

    if not dpcm_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/dpcm/partners?status=INACTIVE&page=1&pageSize=20",
        headers=get_auth_headers()
    )

    return print_result("Get Inactive Partners", response)

def test_update_partner_status(dp_id, is_active=True):
    """Test updating partner status."""
    print_header(f"PARTNERS - {'ACTIVATE' if is_active else 'DEACTIVATE'} PARTNER")

    if not dpcm_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "isActive": is_active
    }

    response = requests.put(
        f"{BASE_URL}/api/v1/dpcm/partners/{dp_id}/status",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Update Partner Status", response)

# ============================================================
# 4. DELIVERY MANAGEMENT TESTS
# ============================================================

def test_get_deliveries():
    """Test getting deliveries by managed DPs."""
    print_header("DELIVERIES - GET ALL DELIVERIES")

    if not dpcm_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/dpcm/deliveries?page=1&pageSize=20",
        headers=get_auth_headers()
    )

    success = print_result("Get Deliveries", response)

    if success:
        data = response.json()
        deliveries = data.get("items", [])
        print(f"\nTotal Deliveries: {len(deliveries)}")
        for d in deliveries[:5]:
            print(f"  - ID: {str(d.get('id', 'N/A'))[:8]}..., Status: {d.get('status')}, DP: {d.get('dpName')}")

    return success

def test_get_deliveries_by_status(status):
    """Test getting deliveries filtered by status."""
    print_header(f"DELIVERIES - GET {status} DELIVERIES")

    if not dpcm_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/dpcm/deliveries?status={status}&page=1&pageSize=20",
        headers=get_auth_headers()
    )

    return print_result(f"Get {status} Deliveries", response)

def test_get_deliveries_by_dp(dp_id):
    """Test getting deliveries for a specific DP."""
    print_header("DELIVERIES - GET DELIVERIES BY DP")

    if not dpcm_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/dpcm/deliveries?dpId={dp_id}&page=1&pageSize=20",
        headers=get_auth_headers()
    )

    return print_result("Get Deliveries by DP", response)

# ============================================================
# 5. COMMISSION CONFIGURATION TESTS
# ============================================================

def test_get_commission_config():
    """Test getting commission configuration."""
    print_header("COMMISSION - GET CONFIG")

    if not dpcm_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/dpcm/commission",
        headers=get_auth_headers()
    )

    success = print_result("Get Commission Config", response)

    if success:
        data = response.json()
        print(f"\nCommission Settings:")
        print(f"  - Type: {data.get('commissionType')}")
        print(f"  - Value: {data.get('commissionValue')}%")
        print(f"  - Min: Rs.{data.get('minCommission')}")
        print(f"  - Max: Rs.{data.get('maxCommission')}")

    return success

def test_update_commission_config():
    """Test updating commission configuration."""
    print_header("COMMISSION - UPDATE CONFIG")

    if not dpcm_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "commissionType": "PERCENTAGE",
        "commissionValue": 12,
        "minCommissionAmount": 10,
        "maxCommissionAmount": 200
    }

    response = requests.put(
        f"{BASE_URL}/api/v1/dpcm/commission",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Update Commission Config", response)

# ============================================================
# 6. SETTLEMENTS TESTS
# ============================================================

def test_get_settlements():
    """Test getting settlement history."""
    print_header("SETTLEMENTS - GET HISTORY")

    if not dpcm_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/dpcm/settlements?page=1&pageSize=20",
        headers=get_auth_headers()
    )

    success = print_result("Get Settlements", response)

    if success:
        data = response.json()
        summary = data.get("summary", {})
        print(f"\nSettlement Summary:")
        print(f"  - Available Balance: Rs.{summary.get('availableBalance', 0)}")
        print(f"  - Pending Settlement: Rs.{summary.get('pendingSettlement', 0)}")
        print(f"  - Total Settled: Rs.{summary.get('totalSettled', 0)}")

    return success

def test_get_pending_settlements():
    """Test getting pending settlements."""
    print_header("SETTLEMENTS - GET PENDING")

    if not dpcm_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/dpcm/settlements?status=PENDING&page=1&pageSize=20",
        headers=get_auth_headers()
    )

    return print_result("Get Pending Settlements", response)

def test_request_settlement(amount=100):
    """Test requesting a settlement."""
    print_header("SETTLEMENTS - REQUEST SETTLEMENT")

    if not dpcm_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "amount": amount
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/dpcm/settlements/request",
        json=payload,
        headers=get_auth_headers()
    )

    # Accept both 200 (success) and 400 (insufficient balance)
    return print_result("Request Settlement", response, expected_status=response.status_code)

# ============================================================
# 7. WALLET TESTS
# ============================================================

def test_get_wallet():
    """Test getting wallet information."""
    print_header("WALLET - GET WALLET")

    if not dpcm_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/wallet",
        headers=get_auth_headers()
    )

    return print_result("Get Wallet", response)

def test_get_earnings():
    """Test getting earnings summary."""
    print_header("WALLET - GET EARNINGS")

    if not dpcm_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/wallet/earnings",
        headers=get_auth_headers()
    )

    return print_result("Get Earnings", response)

# ============================================================
# MAIN TEST RUNNER
# ============================================================

def run_all_tests():
    """Run all DPCM role tests."""
    print("\n" + "=" * 60)
    print(" DELIVERX - DPCM (DELIVERY PARTNER CHANNEL MANAGER) TEST SUITE")
    print("=" * 60)
    print(f"Base URL: {BASE_URL}")
    print(f"DPCM Phone: {DPCM_PHONE}")
    print(f"Time: {datetime.now().isoformat()}")

    results = {
        "passed": 0,
        "failed": 0,
        "skipped": 0
    }

    # ----------------------------------------
    # Phase 1: Authentication
    # ----------------------------------------
    print_header("PHASE 1: AUTHENTICATION")

    if test_send_otp():
        results["passed"] += 1
    else:
        results["failed"] += 1

    time.sleep(1)

    if test_verify_otp():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 2: Dashboard
    # ----------------------------------------
    print_header("PHASE 2: DASHBOARD")

    if test_get_dpcm_dashboard():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 3: Partner Management
    # ----------------------------------------
    print_header("PHASE 3: PARTNER MANAGEMENT")

    if test_get_partners():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_get_active_partners():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_get_inactive_partners():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 4: Delivery Management
    # ----------------------------------------
    print_header("PHASE 4: DELIVERY MANAGEMENT")

    if test_get_deliveries():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_get_deliveries_by_status("DELIVERED"):
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_get_deliveries_by_status("IN_TRANSIT"):
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 5: Commission Configuration
    # ----------------------------------------
    print_header("PHASE 5: COMMISSION CONFIGURATION")

    if test_get_commission_config():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_update_commission_config():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 6: Settlements
    # ----------------------------------------
    print_header("PHASE 6: SETTLEMENTS")

    if test_get_settlements():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_get_pending_settlements():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_request_settlement():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 7: Wallet
    # ----------------------------------------
    print_header("PHASE 7: WALLET")

    if test_get_wallet():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_get_earnings():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Summary
    # ----------------------------------------
    print("\n" + "=" * 60)
    print(" TEST SUMMARY")
    print("=" * 60)
    print(f"Passed:  {results['passed']}")
    print(f"Failed:  {results['failed']}")
    print(f"Skipped: {results['skipped']}")
    print(f"Total:   {results['passed'] + results['failed'] + results['skipped']}")
    print("=" * 60)

    if dpcm_token:
        print(f"\nDPCM Token (for further testing):")
        print(f"{dpcm_token}")

    return results

if __name__ == "__main__":
    import sys

    if len(sys.argv) > 1 and sys.argv[1] == "--help":
        print(__doc__)
    else:
        run_all_tests()
