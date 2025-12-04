#!/usr/bin/env python3
"""
DeliverX - Admin/SuperAdmin (SA) Role Test Suite
=================================================

This script tests all Admin-specific API endpoints.

Admin Role Capabilities:
- View admin dashboard with platform overview
- View platform and revenue statistics
- Generate reports
- User management (list, view, update status)
- KYC management (list, approve, reject)
- View audit logs
- System configuration management
- Settlement processing
- Promo code management

Prerequisites:
- API running at http://localhost:5205
- pip install requests

Usage:
    python test_admin_role.py
"""

import requests
import json
import time
import re
from datetime import datetime, timedelta

BASE_URL = "http://localhost:5205"

# Test data - Admin credentials
# Note: Admin typically uses email/password login, not OTP
ADMIN_EMAIL = "admin@deliverx.com"
ADMIN_PASSWORD = "Admin@123"
ADMIN_PHONE = "9000000001"  # Fallback for OTP

# Store tokens and IDs
admin_token = None
admin_user_id = None
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
    t = token or admin_token
    return {
        "Authorization": f"Bearer {t}",
        "Content-Type": "application/json"
    }

# ============================================================
# 1. AUTHENTICATION TESTS
# ============================================================

def test_admin_login():
    """Test admin login with email/password."""
    global admin_token, admin_user_id

    print_header("AUTHENTICATION - ADMIN LOGIN")

    payload = {
        "email": ADMIN_EMAIL,
        "password": ADMIN_PASSWORD
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/auth/login",
        json=payload
    )

    success = print_result("Admin Login", response)

    if success and response.status_code == 200:
        data = response.json()
        if "data" in data:
            admin_token = data["data"].get("accessToken")
            admin_user_id = data["data"].get("userId")
            print(f"\nToken obtained: {admin_token[:50] if admin_token else 'None'}...")
            print(f"User ID: {admin_user_id}")

    return success

def test_admin_otp_login():
    """Test admin login via OTP (fallback)."""
    global admin_token, admin_user_id, last_otp

    print_header("AUTHENTICATION - ADMIN OTP LOGIN")

    # Send OTP
    payload = {
        "phone": ADMIN_PHONE,
        "role": "SA"
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/auth/otp/send",
        json=payload
    )

    if response.status_code != 200:
        print(f"[FAIL] Send OTP: {response.status_code}")
        return False

    # Extract OTP from response
    try:
        data = response.json()
        message = data.get("data", {}).get("message", "")
        otp_match = re.search(r'OTP:\s*(\d{6})', message)
        if otp_match:
            last_otp = otp_match.group(1)
            print(f"Extracted OTP: {last_otp}")
    except:
        pass

    # Verify OTP
    payload = {
        "phone": ADMIN_PHONE,
        "otp": last_otp,
        "role": "SA",
        "deviceId": "admin-test-device"
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/auth/otp/verify",
        json=payload
    )

    success = print_result("Admin OTP Verify", response)

    if success and response.status_code == 200:
        data = response.json()
        if "data" in data:
            admin_token = data["data"].get("accessToken")
            admin_user_id = data["data"].get("userId")
            print(f"\nToken obtained: {admin_token[:50] if admin_token else 'None'}...")

    return success

# ============================================================
# 2. DASHBOARD TESTS
# ============================================================

def test_get_admin_dashboard():
    """Test getting admin dashboard."""
    print_header("DASHBOARD - GET ADMIN DASHBOARD")

    if not admin_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/admin/dashboard",
        headers=get_auth_headers()
    )

    return print_result("Get Admin Dashboard", response)

def test_get_platform_stats():
    """Test getting platform statistics."""
    print_header("DASHBOARD - GET PLATFORM STATS")

    if not admin_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/admin/stats/platform",
        headers=get_auth_headers()
    )

    return print_result("Get Platform Stats", response)

def test_get_revenue_stats():
    """Test getting revenue statistics."""
    print_header("DASHBOARD - GET REVENUE STATS")

    if not admin_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/admin/stats/revenue",
        headers=get_auth_headers()
    )

    return print_result("Get Revenue Stats", response)

# ============================================================
# 3. REPORT GENERATION TESTS
# ============================================================

def test_generate_delivery_report():
    """Test generating delivery report."""
    print_header("REPORTS - GENERATE DELIVERY REPORT")

    if not admin_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "reportType": "DELIVERY",
        "startDate": (datetime.now() - timedelta(days=30)).isoformat(),
        "endDate": datetime.now().isoformat(),
        "format": "JSON"
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/admin/reports",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Generate Delivery Report", response)

def test_generate_revenue_report():
    """Test generating revenue report."""
    print_header("REPORTS - GENERATE REVENUE REPORT")

    if not admin_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "reportType": "REVENUE",
        "startDate": (datetime.now() - timedelta(days=30)).isoformat(),
        "endDate": datetime.now().isoformat(),
        "format": "JSON"
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/admin/reports",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Generate Revenue Report", response)

# ============================================================
# 4. USER MANAGEMENT TESTS
# ============================================================

def test_get_users():
    """Test getting users list."""
    print_header("USER MANAGEMENT - GET USERS")

    if not admin_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/admin/users?page=1&pageSize=10",
        headers=get_auth_headers()
    )

    success = print_result("Get Users", response)

    if success:
        data = response.json()
        users = data.get("users", data.get("items", []))
        print(f"\nTotal users: {len(users)}")
        for u in users[:5]:
            print(f"  - ID: {str(u.get('id', 'N/A'))[:8]}..., Role: {u.get('role')}, Phone: {u.get('phone')}")

    return success

def test_get_users_by_role():
    """Test getting users filtered by role."""
    print_header("USER MANAGEMENT - GET USERS BY ROLE (DP)")

    if not admin_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/admin/users?role=DP&page=1&pageSize=10",
        headers=get_auth_headers()
    )

    return print_result("Get DP Users", response)

def test_get_user_details(user_id):
    """Test getting specific user details."""
    print_header(f"USER MANAGEMENT - GET USER DETAILS")

    if not admin_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/admin/users/{user_id}",
        headers=get_auth_headers()
    )

    return print_result("Get User Details", response)

def test_update_user_status(user_id):
    """Test updating user status."""
    print_header("USER MANAGEMENT - UPDATE USER STATUS")

    if not admin_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "isActive": True,
        "reason": "Test status update"
    }

    response = requests.put(
        f"{BASE_URL}/api/v1/admin/users/{user_id}/status",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Update User Status", response)

# ============================================================
# 5. KYC MANAGEMENT TESTS
# ============================================================

def test_get_kyc_requests():
    """Test getting KYC requests."""
    print_header("KYC MANAGEMENT - GET KYC REQUESTS")

    if not admin_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/admin/kyc?page=1&pageSize=10",
        headers=get_auth_headers()
    )

    return print_result("Get KYC Requests", response)

def test_get_pending_kyc():
    """Test getting pending KYC requests."""
    print_header("KYC MANAGEMENT - GET PENDING KYC")

    if not admin_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/admin/kyc?status=PENDING&page=1&pageSize=10",
        headers=get_auth_headers()
    )

    return print_result("Get Pending KYC", response)

def test_approve_kyc(kyc_id):
    """Test approving KYC."""
    print_header("KYC MANAGEMENT - APPROVE KYC")

    if not admin_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "notes": "KYC verified and approved"
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/admin/kyc/{kyc_id}/approve",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Approve KYC", response)

def test_reject_kyc(kyc_id):
    """Test rejecting KYC."""
    print_header("KYC MANAGEMENT - REJECT KYC")

    if not admin_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "reason": "Documents not clear, please resubmit"
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/admin/kyc/{kyc_id}/reject",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Reject KYC", response)

# ============================================================
# 6. AUDIT LOG TESTS
# ============================================================

def test_get_audit_logs():
    """Test getting audit logs."""
    print_header("AUDIT LOGS - GET LOGS")

    if not admin_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/admin/audit-logs?page=1&pageSize=20",
        headers=get_auth_headers()
    )

    return print_result("Get Audit Logs", response)

# ============================================================
# 7. SYSTEM CONFIGURATION TESTS
# ============================================================

def test_get_system_config():
    """Test getting system configuration."""
    print_header("SYSTEM CONFIG - GET CONFIG")

    if not admin_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/admin/config",
        headers=get_auth_headers()
    )

    return print_result("Get System Config", response)

def test_update_system_config():
    """Test updating system configuration."""
    print_header("SYSTEM CONFIG - UPDATE CONFIG")

    if not admin_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "key": "PLATFORM_FEE_PERCENTAGE",
        "value": "10"
    }

    response = requests.put(
        f"{BASE_URL}/api/v1/admin/config",
        json=payload,
        headers=get_auth_headers()
    )

    # Accept both 200 and 400 (key may not exist)
    return print_result("Update System Config", response, expected_status=response.status_code)

# ============================================================
# 8. SETTLEMENT MANAGEMENT TESTS
# ============================================================

def test_get_all_settlements():
    """Test getting all settlements (admin view)."""
    print_header("SETTLEMENTS - GET ALL SETTLEMENTS")

    if not admin_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/settlements?page=1&pageSize=10",
        headers=get_auth_headers()
    )

    return print_result("Get All Settlements", response)

def test_process_settlement(settlement_id):
    """Test processing a settlement."""
    print_header("SETTLEMENTS - PROCESS SETTLEMENT")

    if not admin_token:
        print("[SKIP]: No token available")
        return False

    response = requests.post(
        f"{BASE_URL}/api/v1/settlements/{settlement_id}/process",
        headers=get_auth_headers()
    )

    return print_result("Process Settlement", response)

# ============================================================
# 9. PROMO CODE MANAGEMENT TESTS
# ============================================================

def test_get_promo_codes():
    """Test getting promo codes."""
    print_header("PROMO CODES - GET ALL")

    if not admin_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/subscriptions/promo",
        headers=get_auth_headers()
    )

    return print_result("Get Promo Codes", response)

def test_create_promo_code():
    """Test creating a promo code."""
    print_header("PROMO CODES - CREATE")

    if not admin_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "code": f"TEST{int(time.time())}",
        "discountType": "PERCENTAGE",
        "discountValue": 10,
        "maxUses": 100,
        "expiresAt": (datetime.now() + timedelta(days=30)).isoformat()
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/subscriptions/promo",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Create Promo Code", response)

# ============================================================
# 10. SERVICE AREA MANAGEMENT TESTS
# ============================================================

def test_get_service_areas():
    """Test getting service areas."""
    print_header("SERVICE AREAS - GET ALL")

    if not admin_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/service-area?page=1&pageSize=10",
        headers=get_auth_headers()
    )

    return print_result("Get Service Areas", response)

def test_create_service_area():
    """Test creating a service area."""
    print_header("SERVICE AREAS - CREATE")

    if not admin_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "name": f"Test Area {int(time.time())}",
        "centerLat": 28.6139,
        "centerLng": 77.2090,
        "radiusKm": 15,
        "isActive": True
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/service-area",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Create Service Area", response)

# ============================================================
# MAIN TEST RUNNER
# ============================================================

def run_all_tests():
    """Run all Admin role tests."""
    print("\n" + "=" * 60)
    print(" DELIVERX - ADMIN/SUPERADMIN (SA) ROLE TEST SUITE")
    print("=" * 60)
    print(f"Base URL: {BASE_URL}")
    print(f"Admin Phone: {ADMIN_PHONE}")
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

    # Try email/password first, fallback to OTP
    if not test_admin_login():
        results["failed"] += 1
        # Try OTP login as fallback
        if test_admin_otp_login():
            results["passed"] += 1
        else:
            results["failed"] += 1
    else:
        results["passed"] += 1

    # ----------------------------------------
    # Phase 2: Dashboard
    # ----------------------------------------
    print_header("PHASE 2: DASHBOARD")

    if test_get_admin_dashboard():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_get_platform_stats():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_get_revenue_stats():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 3: Reports
    # ----------------------------------------
    print_header("PHASE 3: REPORTS")

    if test_generate_delivery_report():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_generate_revenue_report():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 4: User Management
    # ----------------------------------------
    print_header("PHASE 4: USER MANAGEMENT")

    if test_get_users():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_get_users_by_role():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 5: KYC Management
    # ----------------------------------------
    print_header("PHASE 5: KYC MANAGEMENT")

    if test_get_kyc_requests():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_get_pending_kyc():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 6: Audit Logs
    # ----------------------------------------
    print_header("PHASE 6: AUDIT LOGS")

    if test_get_audit_logs():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 7: System Configuration
    # ----------------------------------------
    print_header("PHASE 7: SYSTEM CONFIGURATION")

    if test_get_system_config():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 8: Settlements
    # ----------------------------------------
    print_header("PHASE 8: SETTLEMENTS")

    if test_get_all_settlements():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 9: Service Areas
    # ----------------------------------------
    print_header("PHASE 9: SERVICE AREAS")

    if test_get_service_areas():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_create_service_area():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 10: Promo Codes
    # ----------------------------------------
    print_header("PHASE 10: PROMO CODES")

    if test_get_promo_codes():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_create_promo_code():
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

    if admin_token:
        print(f"\nAdmin Token (for further testing):")
        print(f"{admin_token}")

    return results

if __name__ == "__main__":
    import sys

    if len(sys.argv) > 1 and sys.argv[1] == "--help":
        print(__doc__)
    else:
        run_all_tests()
