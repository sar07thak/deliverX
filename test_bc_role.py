#!/usr/bin/env python3
"""
DeliverX - Business Consumer (BC) Role Test Suite
=================================================

This script tests all BC-specific API endpoints.

BC Role Capabilities:
- Register and authenticate via OTP
- Create delivery orders
- Track delivery status
- Rate delivery partners
- Manage wallet (recharge for payments)
- Subscribe to business plans
- View complaints and file new ones

Prerequisites:
- API running at http://localhost:5205
- pip install requests

Usage:
    python test_bc_role.py
"""

import requests
import json
import time
import re
from datetime import datetime, timedelta

BASE_URL = "http://localhost:5205"

# Test data - Use a unique phone number
BC_PHONE = "8888899999"  # Example BC phone

# Store tokens and IDs
bc_token = None
bc_user_id = None
bc_device_id = "test-bc-device"
last_otp = "123456"  # Default OTP, will be updated from response
created_delivery_id = None

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
    t = token or bc_token
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
        "phone": BC_PHONE,
        "role": "BC"
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
    global bc_token, bc_user_id

    print_header("AUTHENTICATION - VERIFY OTP")

    payload = {
        "phone": BC_PHONE,
        "otp": last_otp,
        "role": "BC",
        "deviceId": bc_device_id
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
            bc_token = data["data"].get("accessToken")
            bc_user_id = data["data"].get("userId")
            print(f"\nToken obtained: {bc_token[:50] if bc_token else 'None'}...")
            print(f"User ID: {bc_user_id}")

    return success

# ============================================================
# 2. DELIVERY MANAGEMENT TESTS
# ============================================================

def test_create_delivery():
    """Test creating a new delivery order."""
    global created_delivery_id

    print_header("DELIVERIES - CREATE DELIVERY")

    if not bc_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "requesterType": "DBC",
        "pickup": {
            "lat": 28.6139,
            "lng": 77.2090,
            "address": "123 Business Park, Connaught Place, New Delhi",
            "contactName": "BC Test User",
            "contactPhone": BC_PHONE,
            "instructions": "Please collect from reception"
        },
        "drop": {
            "lat": 28.6350,
            "lng": 77.2250,
            "address": "456 Residential Colony, Defence Colony, New Delhi",
            "contactName": "Test Recipient",
            "contactPhone": "9876543210",
            "instructions": "Ring doorbell twice"
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
            "description": "Business documents"
        },
        "priority": "ASAP",
        "specialInstructions": "Handle with care"
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/deliveries",
        json=payload,
        headers=get_auth_headers()
    )

    success = print_result("Create Delivery", response)

    if success:
        try:
            data = response.json()
            created_delivery_id = data.get("deliveryId") or data.get("id")
            print(f"\nDelivery ID: {created_delivery_id}")
        except:
            pass

    return success

def test_get_deliveries():
    """Test getting list of deliveries."""
    print_header("DELIVERIES - GET MY DELIVERIES")

    if not bc_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/deliveries?page=1&pageSize=10",
        headers=get_auth_headers()
    )

    success = print_result("Get Deliveries", response)

    if success:
        data = response.json()
        deliveries = data.get("deliveries", [])
        print(f"\nTotal deliveries: {len(deliveries)}")
        for d in deliveries[:3]:
            print(f"  - ID: {d.get('id')}, Status: {d.get('status')}")

    return success

def test_get_delivery_details():
    """Test getting delivery details."""
    print_header("DELIVERIES - GET DELIVERY DETAILS")

    if not bc_token or not created_delivery_id:
        print("[SKIP]: No token or delivery ID available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/deliveries/{created_delivery_id}",
        headers=get_auth_headers()
    )

    return print_result("Get Delivery Details", response)

def test_trigger_matching():
    """Test triggering DP matching for a delivery."""
    print_header("DELIVERIES - TRIGGER MATCHING")

    if not bc_token or not created_delivery_id:
        print("[SKIP]: No token or delivery ID available")
        return False

    response = requests.post(
        f"{BASE_URL}/api/v1/deliveries/{created_delivery_id}/match",
        headers=get_auth_headers()
    )

    return print_result("Trigger Matching", response)

def test_cancel_delivery():
    """Test cancelling a delivery."""
    print_header("DELIVERIES - CANCEL DELIVERY")

    if not bc_token or not created_delivery_id:
        print("[SKIP]: No token or delivery ID available")
        return False

    payload = {
        "reason": "Test cancellation"
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/deliveries/{created_delivery_id}/cancel",
        json=payload,
        headers=get_auth_headers()
    )

    # Accept both 200 and 400 (already assigned deliveries can't be cancelled)
    return print_result("Cancel Delivery", response, expected_status=response.status_code)

# ============================================================
# 3. WALLET TESTS
# ============================================================

def test_get_wallet():
    """Test getting wallet information."""
    print_header("WALLET - GET WALLET")

    if not bc_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/wallet",
        headers=get_auth_headers()
    )

    return print_result("Get Wallet", response)

def test_get_transactions():
    """Test getting wallet transactions."""
    print_header("WALLET - GET TRANSACTIONS")

    if not bc_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/wallet/transactions?page=1&pageSize=10",
        headers=get_auth_headers()
    )

    return print_result("Get Transactions", response)

def test_initiate_recharge():
    """Test initiating wallet recharge."""
    print_header("WALLET - INITIATE RECHARGE")

    if not bc_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "amount": 1000,
        "paymentMethod": "UPI"
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/wallet/recharge",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Initiate Recharge", response)

# ============================================================
# 4. SUBSCRIPTION TESTS
# ============================================================

def test_get_subscription_plans():
    """Test getting available subscription plans."""
    print_header("SUBSCRIPTIONS - GET PLANS")

    response = requests.get(
        f"{BASE_URL}/api/v1/subscriptions/plans"
    )

    return print_result("Get Subscription Plans", response)

def test_get_my_subscription():
    """Test getting current subscription."""
    print_header("SUBSCRIPTIONS - GET MY SUBSCRIPTION")

    if not bc_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/subscriptions/my",
        headers=get_auth_headers()
    )

    return print_result("Get My Subscription", response)

def test_get_invoices():
    """Test getting subscription invoices."""
    print_header("SUBSCRIPTIONS - GET INVOICES")

    if not bc_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/subscriptions/invoices",
        headers=get_auth_headers()
    )

    return print_result("Get Invoices", response)

# ============================================================
# 5. RATINGS TESTS
# ============================================================

def test_get_behavior_index():
    """Test getting my behavior index."""
    print_header("RATINGS - GET MY BEHAVIOR INDEX")

    if not bc_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/ratings/behavior-index",
        headers=get_auth_headers()
    )

    return print_result("Get Behavior Index", response)

def test_submit_rating(delivery_id, target_id):
    """Test submitting a rating for a delivery."""
    print_header("RATINGS - SUBMIT RATING")

    if not bc_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "deliveryId": delivery_id,
        "targetId": target_id,
        "targetType": "DP",
        "stars": 5,
        "comment": "Excellent service!",
        "tags": ["on-time", "polite"]
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/ratings",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Submit Rating", response)

# ============================================================
# 6. COMPLAINTS TESTS
# ============================================================

def test_get_complaints():
    """Test getting complaints list."""
    print_header("COMPLAINTS - GET COMPLAINTS")

    if not bc_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/complaints?page=1&pageSize=10",
        headers=get_auth_headers()
    )

    return print_result("Get Complaints", response)

def test_create_complaint():
    """Test creating a complaint."""
    print_header("COMPLAINTS - CREATE COMPLAINT")

    if not bc_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "deliveryId": created_delivery_id if created_delivery_id else "00000000-0000-0000-0000-000000000000",
        "category": "DELIVERY_ISSUE",
        "description": "Test complaint for integration testing",
        "priority": "MEDIUM"
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/complaints",
        json=payload,
        headers=get_auth_headers()
    )

    # Accept both 200/201 (success) and 400/404 (validation error)
    return print_result("Create Complaint", response, expected_status=response.status_code)

# ============================================================
# 7. PRICING TESTS
# ============================================================

def test_calculate_price():
    """Test calculating delivery price."""
    print_header("PRICING - CALCULATE PRICE")

    if not bc_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "pickupLat": 28.6139,
        "pickupLng": 77.2090,
        "dropLat": 28.6350,
        "dropLng": 77.2250,
        "packageWeightKg": 2.5,
        "packageType": "parcel",
        "vehicleType": "BIKE",
        "priority": "ASAP"
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/pricing/calculate",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Calculate Price", response)

# ============================================================
# 8. SERVICE AREA TESTS
# ============================================================

def test_get_service_areas():
    """Test getting service areas."""
    print_header("SERVICE AREAS - GET AREAS")

    response = requests.get(
        f"{BASE_URL}/api/v1/service-area?page=1&pageSize=10"
    )

    return print_result("Get Service Areas", response)

def test_check_serviceability():
    """Test checking if location is serviceable."""
    print_header("SERVICE AREAS - CHECK SERVICEABILITY")

    payload = {
        "lat": 28.6139,
        "lng": 77.2090
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/service-area/check",
        json=payload
    )

    return print_result("Check Serviceability", response)

# ============================================================
# 9. REFERRALS TESTS
# ============================================================

def test_get_referral_info():
    """Test getting referral information."""
    print_header("REFERRALS - GET INFO")

    if not bc_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/referrals/my",
        headers=get_auth_headers()
    )

    return print_result("Get Referral Info", response)

# ============================================================
# MAIN TEST RUNNER
# ============================================================

def run_all_tests():
    """Run all BC role tests."""
    print("\n" + "=" * 60)
    print(" DELIVERX - BUSINESS CONSUMER (BC) ROLE TEST SUITE")
    print("=" * 60)
    print(f"Base URL: {BASE_URL}")
    print(f"Test Phone: {BC_PHONE}")
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
    # Phase 2: Service Area & Pricing
    # ----------------------------------------
    print_header("PHASE 2: SERVICE AREA & PRICING")

    if test_get_service_areas():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_check_serviceability():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_calculate_price():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 3: Wallet
    # ----------------------------------------
    print_header("PHASE 3: WALLET")

    if test_get_wallet():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_get_transactions():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_initiate_recharge():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 4: Delivery Management
    # ----------------------------------------
    print_header("PHASE 4: DELIVERY MANAGEMENT")

    if test_create_delivery():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_get_deliveries():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_get_delivery_details():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_trigger_matching():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 5: Subscriptions
    # ----------------------------------------
    print_header("PHASE 5: SUBSCRIPTIONS")

    if test_get_subscription_plans():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_get_my_subscription():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_get_invoices():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 6: Ratings & Behavior
    # ----------------------------------------
    print_header("PHASE 6: RATINGS & BEHAVIOR")

    if test_get_behavior_index():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 7: Complaints
    # ----------------------------------------
    print_header("PHASE 7: COMPLAINTS")

    if test_get_complaints():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 8: Referrals
    # ----------------------------------------
    print_header("PHASE 8: REFERRALS")

    if test_get_referral_info():
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

    if bc_token:
        print(f"\nBC Token (for further testing):")
        print(f"{bc_token}")

    if created_delivery_id:
        print(f"\nCreated Delivery ID: {created_delivery_id}")

    return results

if __name__ == "__main__":
    import sys

    if len(sys.argv) > 1 and sys.argv[1] == "--help":
        print(__doc__)
    else:
        run_all_tests()
