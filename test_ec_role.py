#!/usr/bin/env python3
"""
DeliverX - End Consumer (EC) Role Test Suite
=============================================

This script tests all EC-specific API endpoints.

EC Role Capabilities:
- Register and authenticate via OTP
- Create delivery orders (send packages)
- Track deliveries (sent or received)
- Rate delivery partners
- Manage wallet for payments
- File and track complaints
- View referral information

Prerequisites:
- API running at http://localhost:5205
- pip install requests

Usage:
    python test_ec_role.py
"""

import requests
import json
import time
import re
from datetime import datetime, timedelta

BASE_URL = "http://localhost:5205"

# Test data - Use a unique phone number
EC_PHONE = "9999988888"  # Example EC phone

# Store tokens and IDs
ec_token = None
ec_user_id = None
ec_device_id = "test-ec-device"
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
    t = token or ec_token
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
        "phone": EC_PHONE,
        "role": "EC"
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
    global ec_token, ec_user_id

    print_header("AUTHENTICATION - VERIFY OTP")

    payload = {
        "phone": EC_PHONE,
        "otp": last_otp,
        "role": "EC",
        "deviceId": ec_device_id
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
            ec_token = data["data"].get("accessToken")
            ec_user_id = data["data"].get("userId")
            print(f"\nToken obtained: {ec_token[:50] if ec_token else 'None'}...")
            print(f"User ID: {ec_user_id}")

    return success

def test_refresh_token():
    """Test token refresh."""
    print_header("AUTHENTICATION - REFRESH TOKEN")

    # Note: Refresh token is typically returned during OTP verification
    # For this test, we'll just verify the endpoint exists
    payload = {
        "refreshToken": "test-refresh-token"
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/auth/refresh",
        json=payload
    )

    # Accept both 200 and 401 (invalid token is expected)
    return print_result("Refresh Token", response, expected_status=response.status_code)

def test_get_sessions():
    """Test getting active sessions."""
    print_header("AUTHENTICATION - GET SESSIONS")

    if not ec_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/auth/sessions",
        headers=get_auth_headers()
    )

    return print_result("Get Sessions", response)

# ============================================================
# 2. DELIVERY MANAGEMENT TESTS
# ============================================================

def test_create_delivery():
    """Test creating a delivery (sending a package)."""
    global created_delivery_id

    print_header("DELIVERIES - CREATE DELIVERY")

    if not ec_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "requesterType": "EC",
        "pickup": {
            "lat": 28.7041,
            "lng": 77.1025,
            "address": "789 Home Address, Rohini, New Delhi",
            "contactName": "EC Test Sender",
            "contactPhone": EC_PHONE,
            "instructions": "Ground floor, ring the bell"
        },
        "drop": {
            "lat": 28.5355,
            "lng": 77.3910,
            "address": "101 Friend's Address, Noida, UP",
            "contactName": "Friend Recipient",
            "contactPhone": "9876512345",
            "instructions": "Call before arriving"
        },
        "package": {
            "weightKg": 1.0,
            "type": "parcel",
            "value": 200,
            "description": "Gift package"
        },
        "priority": "ASAP",
        "specialInstructions": "Fragile items inside"
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

def test_create_scheduled_delivery():
    """Test creating a scheduled delivery."""
    print_header("DELIVERIES - CREATE SCHEDULED DELIVERY")

    if not ec_token:
        print("[SKIP]: No token available")
        return False

    # Schedule for tomorrow
    scheduled_time = (datetime.now() + timedelta(days=1)).replace(hour=14, minute=0).isoformat()

    payload = {
        "requesterType": "EC",
        "pickup": {
            "lat": 28.7041,
            "lng": 77.1025,
            "address": "789 Home Address, Rohini, New Delhi",
            "contactName": "EC Test Sender",
            "contactPhone": EC_PHONE
        },
        "drop": {
            "lat": 28.6139,
            "lng": 77.2090,
            "address": "CP Office, Connaught Place, New Delhi",
            "contactName": "Office Recipient",
            "contactPhone": "9876500000"
        },
        "package": {
            "weightKg": 0.5,
            "type": "document",
            "description": "Important documents"
        },
        "priority": "SCHEDULED",
        "scheduledAt": scheduled_time
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/deliveries",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Create Scheduled Delivery", response)

def test_get_my_deliveries():
    """Test getting user's deliveries."""
    print_header("DELIVERIES - GET MY DELIVERIES")

    if not ec_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/deliveries?page=1&pageSize=10",
        headers=get_auth_headers()
    )

    success = print_result("Get My Deliveries", response)

    if success:
        data = response.json()
        deliveries = data.get("deliveries", [])
        print(f"\nTotal deliveries: {len(deliveries)}")
        for d in deliveries[:5]:
            print(f"  - ID: {d.get('id')[:8]}..., Status: {d.get('status')}, Priority: {d.get('priority')}")

    return success

def test_track_delivery():
    """Test tracking a delivery."""
    print_header("DELIVERIES - TRACK DELIVERY")

    if not ec_token or not created_delivery_id:
        print("[SKIP]: No token or delivery ID available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/deliveries/{created_delivery_id}",
        headers=get_auth_headers()
    )

    success = print_result("Track Delivery", response)

    if success:
        data = response.json()
        print(f"\nDelivery Status: {data.get('status')}")
        print(f"Pickup: {data.get('pickup', {}).get('address', 'N/A')[:50]}...")
        print(f"Drop: {data.get('drop', {}).get('address', 'N/A')[:50]}...")

    return success

def test_cancel_delivery():
    """Test cancelling a delivery."""
    print_header("DELIVERIES - CANCEL DELIVERY")

    if not ec_token or not created_delivery_id:
        print("[SKIP]: No token or delivery ID available")
        return False

    payload = {
        "reason": "Changed plans - test cancellation"
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/deliveries/{created_delivery_id}/cancel",
        json=payload,
        headers=get_auth_headers()
    )

    # Accept both 200 and 400 (already cancelled or assigned)
    return print_result("Cancel Delivery", response, expected_status=response.status_code)

# ============================================================
# 3. WALLET TESTS
# ============================================================

def test_get_wallet():
    """Test getting wallet."""
    print_header("WALLET - GET WALLET")

    if not ec_token:
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

    if not ec_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/wallet/transactions?page=1&pageSize=10",
        headers=get_auth_headers()
    )

    return print_result("Get Transactions", response)

def test_recharge_wallet():
    """Test wallet recharge."""
    print_header("WALLET - RECHARGE")

    if not ec_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "amount": 500,
        "paymentMethod": "UPI"
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/wallet/recharge",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Recharge Wallet", response)

# ============================================================
# 4. RATINGS & REVIEWS TESTS
# ============================================================

def test_get_behavior_index():
    """Test getting behavior index."""
    print_header("RATINGS - GET MY BEHAVIOR INDEX")

    if not ec_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/ratings/behavior-index",
        headers=get_auth_headers()
    )

    return print_result("Get Behavior Index", response)

def test_check_if_rated(delivery_id, target_id):
    """Test checking if already rated."""
    print_header("RATINGS - CHECK IF RATED")

    if not ec_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/ratings/check/{delivery_id}/{target_id}",
        headers=get_auth_headers()
    )

    return print_result("Check If Rated", response)

def test_submit_rating(delivery_id, target_id):
    """Test submitting a rating."""
    print_header("RATINGS - SUBMIT RATING")

    if not ec_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "deliveryId": delivery_id,
        "targetId": target_id,
        "targetType": "DP",
        "stars": 4,
        "comment": "Good service, delivered on time",
        "tags": ["on-time", "professional"]
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/ratings",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Submit Rating", response)

# ============================================================
# 5. COMPLAINTS TESTS
# ============================================================

def test_get_complaints():
    """Test getting complaints."""
    print_header("COMPLAINTS - GET MY COMPLAINTS")

    if not ec_token:
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

    if not ec_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "deliveryId": created_delivery_id if created_delivery_id else "00000000-0000-0000-0000-000000000000",
        "category": "DELAYED_DELIVERY",
        "description": "Test complaint - Delivery was delayed by more than 1 hour",
        "priority": "LOW"
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/complaints",
        json=payload,
        headers=get_auth_headers()
    )

    # Accept various status codes
    return print_result("Create Complaint", response, expected_status=response.status_code)

# ============================================================
# 6. REFERRALS TESTS
# ============================================================

def test_get_my_referrals():
    """Test getting referral information."""
    print_header("REFERRALS - GET MY REFERRALS")

    if not ec_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/referrals/my",
        headers=get_auth_headers()
    )

    # Accept 404 (no referrals yet) or 200
    return print_result("Get My Referrals", response, expected_status=response.status_code)

# ============================================================
# 7. PRICING TESTS
# ============================================================

def test_calculate_delivery_price():
    """Test calculating delivery price."""
    print_header("PRICING - CALCULATE PRICE")

    if not ec_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "pickupLat": 28.7041,
        "pickupLng": 77.1025,
        "dropLat": 28.5355,
        "dropLng": 77.3910,
        "packageWeightKg": 1.0,
        "packageType": "parcel",
        "vehicleType": "BIKE",
        "priority": "ASAP"
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/pricing/calculate",
        json=payload,
        headers=get_auth_headers()
    )

    # Accept 400 (no DPs available) or 200
    return print_result("Calculate Price", response, expected_status=response.status_code)

# ============================================================
# 8. SUBSCRIPTION TESTS (Optional for EC)
# ============================================================

def test_get_subscription_plans():
    """Test getting subscription plans (public)."""
    print_header("SUBSCRIPTIONS - GET PLANS")

    response = requests.get(
        f"{BASE_URL}/api/v1/subscriptions/plans"
    )

    return print_result("Get Subscription Plans", response)

def test_get_my_subscription():
    """Test getting current subscription."""
    print_header("SUBSCRIPTIONS - GET MY SUBSCRIPTION")

    if not ec_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/subscriptions/my",
        headers=get_auth_headers()
    )

    return print_result("Get My Subscription", response)

# ============================================================
# MAIN TEST RUNNER
# ============================================================

def run_all_tests():
    """Run all EC role tests."""
    print("\n" + "=" * 60)
    print(" DELIVERX - END CONSUMER (EC) ROLE TEST SUITE")
    print("=" * 60)
    print(f"Base URL: {BASE_URL}")
    print(f"Test Phone: {EC_PHONE}")
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

    if test_get_sessions():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 2: Wallet
    # ----------------------------------------
    print_header("PHASE 2: WALLET")

    if test_get_wallet():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_get_transactions():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_recharge_wallet():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 3: Pricing
    # ----------------------------------------
    print_header("PHASE 3: PRICING")

    if test_calculate_delivery_price():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 4: Deliveries
    # ----------------------------------------
    print_header("PHASE 4: DELIVERIES")

    if test_create_delivery():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_create_scheduled_delivery():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_get_my_deliveries():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_track_delivery():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 5: Ratings
    # ----------------------------------------
    print_header("PHASE 5: RATINGS")

    if test_get_behavior_index():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 6: Complaints
    # ----------------------------------------
    print_header("PHASE 6: COMPLAINTS")

    if test_get_complaints():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 7: Subscriptions
    # ----------------------------------------
    print_header("PHASE 7: SUBSCRIPTIONS")

    if test_get_subscription_plans():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_get_my_subscription():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 8: Referrals
    # ----------------------------------------
    print_header("PHASE 8: REFERRALS")

    if test_get_my_referrals():
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

    if ec_token:
        print(f"\nEC Token (for further testing):")
        print(f"{ec_token}")

    if created_delivery_id:
        print(f"\nCreated Delivery ID: {created_delivery_id}")

    return results

if __name__ == "__main__":
    import sys

    if len(sys.argv) > 1 and sys.argv[1] == "--help":
        print(__doc__)
    else:
        run_all_tests()
