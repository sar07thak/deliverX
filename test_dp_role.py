#!/usr/bin/env python3
"""
DeliverX - Delivery Partner (DP) Role Test Suite
=================================================

This script tests all DP-specific API endpoints.

DP Role Capabilities:
- Register and complete profile
- Set availability status (online/offline)
- Receive delivery notifications (pending deliveries)
- Accept/Reject delivery requests
- Manage delivery lifecycle (pickup -> transit -> deliver)
- Handle Proof of Delivery (POD)
- View wallet and earnings
- View settlement history

Prerequisites:
- API running at http://localhost:5205
- pip install requests

Usage:
    python test_dp_role.py
"""

import requests
import json
import time
from datetime import datetime, timedelta

BASE_URL = "http://localhost:5205"

# Test data - Use a unique phone number
DP_PHONE = "7878798797"  # Example DP phone
DPCM_REFERRAL_CODE = None  # Set this to link DP to a DPCM

# Store tokens and IDs
dp_token = None
dp_user_id = None
dp_device_id = "test-dp-device"
last_otp = "123456"  # Default OTP, will be updated from response

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
    t = token or dp_token
    return {
        "Authorization": f"Bearer {t}",
        "Content-Type": "application/json"
    }

# ============================================================
# 1. REGISTRATION TESTS
# ============================================================

def test_dp_registration_initiate():
    """Test DP registration initiation."""
    print_header("DP REGISTRATION - INITIATE")

    payload = {
        "phone": DP_PHONE
    }

    # Add referral code if available (links DP to DPCM)
    if DPCM_REFERRAL_CODE:
        payload["referralCode"] = DPCM_REFERRAL_CODE

    response = requests.post(
        f"{BASE_URL}/api/v1/registration/dp/initiate",
        json=payload
    )

    # Accept both 200 (new registration) and 409 (already registered)
    success = response.status_code in [200, 409]
    print_result("DP Registration Initiate", response, expected_status=response.status_code)

    if response.status_code == 200:
        data = response.json()
        if "data" in data:
            global dp_user_id
            dp_user_id = data["data"].get("userId")
            print(f"User ID: {dp_user_id}")

    return success

# ============================================================
# 2. AUTHENTICATION TESTS
# ============================================================

def test_send_otp():
    """Test sending OTP."""
    global last_otp

    print_header("AUTHENTICATION - SEND OTP")

    payload = {
        "phone": DP_PHONE,
        "role": "DP"
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
            # Extract OTP from message like "OTP: 304747 (expires in 5 minutes)"
            import re
            otp_match = re.search(r'OTP:\s*(\d{6})', message)
            if otp_match:
                last_otp = otp_match.group(1)
                print(f"Extracted OTP: {last_otp}")
        except:
            pass

    return success

def test_verify_otp():
    """Test OTP verification."""
    global dp_token, dp_user_id

    print_header("AUTHENTICATION - VERIFY OTP")

    # Use the OTP extracted from send_otp response
    payload = {
        "phone": DP_PHONE,
        "otp": last_otp,
        "role": "DP",
        "deviceId": dp_device_id
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
            dp_token = data["data"].get("accessToken")
            dp_user_id = data["data"].get("userId")
            print(f"\nToken obtained: {dp_token[:50] if dp_token else 'None'}...")
            print(f"User ID: {dp_user_id}")

    return success

# ============================================================
# 3. PROFILE MANAGEMENT TESTS
# ============================================================

def test_complete_profile():
    """Test completing DP profile."""
    print_header("PROFILE - COMPLETE PROFILE")

    if not dp_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "fullName": "Test Delivery Partner",
        "email": "dp.test@example.com",
        "dob": "1995-05-15",
        "gender": "Male",
        "address": {
            "line1": "123 Delivery Street",
            "line2": "Near Market",
            "city": "Delhi",
            "state": "Delhi",
            "pincode": "110001"
        },
        "vehicleType": "BIKE",
        "languages": ["Hindi", "English"],
        "availability": "FULL_TIME",
        "serviceArea": {
            "centerLat": 28.6139,
            "centerLng": 77.2090,
            "radiusKm": 10
        },
        "pricing": {
            "perKmRate": 10.0,
            "perKgRate": 5.0,
            "minCharge": 50.0,
            "maxDistanceKm": 20
        }
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/registration/dp/profile",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Complete Profile", response)

# ============================================================
# 4. AVAILABILITY MANAGEMENT TESTS
# ============================================================

def test_get_availability():
    """Test getting current availability status."""
    print_header("AVAILABILITY - GET STATUS")

    if not dp_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/deliveries/availability",
        headers=get_auth_headers()
    )

    return print_result("Get Availability", response)

def test_update_availability_online():
    """Test setting availability to ONLINE."""
    print_header("AVAILABILITY - GO ONLINE")

    if not dp_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "status": "ONLINE",
        "latitude": 28.6139,
        "longitude": 77.2090,
        "vehicleType": "BIKE"
    }

    response = requests.put(
        f"{BASE_URL}/api/v1/deliveries/availability",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Go Online", response)

def test_update_availability_offline():
    """Test setting availability to OFFLINE."""
    print_header("AVAILABILITY - GO OFFLINE")

    if not dp_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "status": "OFFLINE"
    }

    response = requests.put(
        f"{BASE_URL}/api/v1/deliveries/availability",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Go Offline", response)

# ============================================================
# 5. DELIVERY MANAGEMENT TESTS
# ============================================================

def test_get_pending_deliveries():
    """Test getting pending deliveries (notifications)."""
    print_header("DELIVERIES - GET PENDING (NOTIFICATIONS)")

    if not dp_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/deliveries/pending",
        headers=get_auth_headers()
    )

    return print_result("Get Pending Deliveries", response)

def test_get_my_deliveries():
    """Test getting DP's assigned deliveries."""
    print_header("DELIVERIES - GET MY DELIVERIES")

    if not dp_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/deliveries?role=dp",
        headers=get_auth_headers()
    )

    success = print_result("Get My Deliveries", response)

    if success:
        data = response.json()
        items = data.get("items", [])
        print(f"\nTotal deliveries: {len(items)}")
        for d in items[:3]:  # Show first 3
            print(f"  - ID: {d.get('id')}, Status: {d.get('status')}")

    return success

def test_accept_delivery(delivery_id):
    """Test accepting a delivery."""
    print_header(f"DELIVERIES - ACCEPT DELIVERY {delivery_id}")

    if not dp_token:
        print("[SKIP]: No token available")
        return False

    response = requests.post(
        f"{BASE_URL}/api/v1/deliveries/{delivery_id}/accept",
        headers=get_auth_headers()
    )

    return print_result("Accept Delivery", response)

def test_reject_delivery(delivery_id):
    """Test rejecting a delivery."""
    print_header(f"DELIVERIES - REJECT DELIVERY {delivery_id}")

    if not dp_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "reason": "Too far from current location"
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/deliveries/{delivery_id}/reject",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Reject Delivery", response)

def test_get_delivery_state(delivery_id):
    """Test getting delivery state info."""
    print_header(f"DELIVERIES - GET STATE INFO {delivery_id}")

    if not dp_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/deliveries/{delivery_id}/state",
        headers=get_auth_headers()
    )

    success = print_result("Get Delivery State", response)

    if success:
        data = response.json()
        print(f"\nCurrent Status: {data.get('currentStatus')}")
        print(f"Allowed Transitions: {data.get('allowedTransitions')}")

    return success

def test_pickup_delivery(delivery_id):
    """Test marking delivery as picked up."""
    print_header(f"DELIVERIES - PICKUP {delivery_id}")

    if not dp_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "pickupLocation": {
            "latitude": 28.6129,
            "longitude": 77.2295
        },
        "pickupPhoto": "base64_encoded_photo_data",
        "notes": "Picked up from sender"
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/deliveries/{delivery_id}/pickup",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Pickup Delivery", response)

def test_transit_delivery(delivery_id):
    """Test marking delivery as in transit."""
    print_header(f"DELIVERIES - MARK IN TRANSIT {delivery_id}")

    if not dp_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "currentLocation": {
            "latitude": 28.6200,
            "longitude": 77.2100
        },
        "notes": "On the way to delivery location"
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/deliveries/{delivery_id}/transit",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Mark In Transit", response)

def test_send_delivery_otp(delivery_id):
    """Test sending OTP to recipient before delivery."""
    print_header(f"DELIVERIES - SEND OTP TO RECIPIENT {delivery_id}")

    if not dp_token:
        print("[SKIP]: No token available")
        return False

    response = requests.post(
        f"{BASE_URL}/api/v1/deliveries/{delivery_id}/otp/send",
        headers=get_auth_headers()
    )

    return print_result("Send Delivery OTP", response)

def test_verify_delivery_otp(delivery_id, otp="123456"):
    """Test verifying delivery OTP."""
    print_header(f"DELIVERIES - VERIFY DELIVERY OTP {delivery_id}")

    if not dp_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "otp": otp
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/deliveries/{delivery_id}/otp/verify",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Verify Delivery OTP", response)

def test_deliver_delivery(delivery_id):
    """Test marking delivery as delivered with POD."""
    print_header(f"DELIVERIES - MARK DELIVERED {delivery_id}")

    if not dp_token:
        print("[SKIP]: No token available")
        return False

    payload = {
        "deliveryLocation": {
            "latitude": 28.6350,
            "longitude": 77.2250
        },
        "recipientName": "Test Recipient",
        "signatureImage": "base64_encoded_signature",
        "deliveryPhoto": "base64_encoded_photo",
        "notes": "Delivered to recipient",
        "verificationMethod": "SIGNATURE"
    }

    response = requests.post(
        f"{BASE_URL}/api/v1/deliveries/{delivery_id}/deliver",
        json=payload,
        headers=get_auth_headers()
    )

    return print_result("Mark Delivered", response)

def test_get_pod(delivery_id):
    """Test getting Proof of Delivery."""
    print_header(f"DELIVERIES - GET POD {delivery_id}")

    if not dp_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/deliveries/{delivery_id}/pod",
        headers=get_auth_headers()
    )

    return print_result("Get POD", response)

# ============================================================
# 6. WALLET & EARNINGS TESTS
# ============================================================

def test_get_wallet():
    """Test getting wallet information."""
    print_header("WALLET - GET WALLET")

    if not dp_token:
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

    if not dp_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/wallet/transactions?page=1&pageSize=10",
        headers=get_auth_headers()
    )

    return print_result("Get Transactions", response)

def test_get_earnings():
    """Test getting earnings summary."""
    print_header("WALLET - GET EARNINGS SUMMARY")

    if not dp_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/wallet/earnings",
        headers=get_auth_headers()
    )

    return print_result("Get Earnings Summary", response)

# ============================================================
# 7. SETTLEMENTS TESTS
# ============================================================

def test_get_settlements():
    """Test getting settlement history."""
    print_header("SETTLEMENTS - GET HISTORY")

    if not dp_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/settlements?page=1&pageSize=10",
        headers=get_auth_headers()
    )

    return print_result("Get Settlements", response)

# ============================================================
# 8. RATINGS TESTS
# ============================================================

def test_get_my_ratings():
    """Test getting DP's received ratings."""
    print_header("RATINGS - GET MY RATINGS")

    if not dp_token:
        print("[SKIP]: No token available")
        return False

    response = requests.get(
        f"{BASE_URL}/api/v1/ratings?page=1&pageSize=10",
        headers=get_auth_headers()
    )

    return print_result("Get My Ratings", response)

# ============================================================
# MAIN TEST RUNNER
# ============================================================

def run_all_tests():
    """Run all DP role tests."""
    print("\n" + "=" * 60)
    print(" DELIVERX - DELIVERY PARTNER (DP) ROLE TEST SUITE")
    print("=" * 60)
    print(f"Base URL: {BASE_URL}")
    print(f"Test Phone: {DP_PHONE}")
    print(f"Time: {datetime.now().isoformat()}")

    results = {
        "passed": 0,
        "failed": 0,
        "skipped": 0
    }

    # ----------------------------------------
    # Phase 1: Registration & Authentication
    # ----------------------------------------
    print_header("PHASE 1: REGISTRATION & AUTHENTICATION")

    # Test registration
    if test_dp_registration_initiate():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # Test OTP flow
    if test_send_otp():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # Wait for OTP to be sent
    time.sleep(1)

    if test_verify_otp():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 2: Profile Management
    # ----------------------------------------
    print_header("PHASE 2: PROFILE MANAGEMENT")

    if test_complete_profile():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 3: Availability Management
    # ----------------------------------------
    print_header("PHASE 3: AVAILABILITY MANAGEMENT")

    if test_get_availability():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_update_availability_online():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # ----------------------------------------
    # Phase 4: Delivery Management
    # ----------------------------------------
    print_header("PHASE 4: DELIVERY MANAGEMENT")

    if test_get_pending_deliveries():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_get_my_deliveries():
        results["passed"] += 1
    else:
        results["failed"] += 1

    # Note: To test accept/reject/pickup/transit/deliver,
    # we need an actual delivery assigned to this DP.
    # These will be tested if there are pending deliveries.

    # ----------------------------------------
    # Phase 5: Wallet & Earnings
    # ----------------------------------------
    print_header("PHASE 5: WALLET & EARNINGS")

    if test_get_wallet():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_get_transactions():
        results["passed"] += 1
    else:
        results["failed"] += 1

    if test_get_earnings():
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

    # ----------------------------------------
    # Phase 7: Cleanup - Go Offline
    # ----------------------------------------
    print_header("PHASE 7: CLEANUP")

    if test_update_availability_offline():
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

    if dp_token:
        print(f"\nDP Token (for further testing):")
        print(f"{dp_token}")

    return results

def run_delivery_lifecycle_test(delivery_id):
    """Run a complete delivery lifecycle test for a specific delivery."""
    print("\n" + "=" * 60)
    print(" DELIVERY LIFECYCLE TEST")
    print("=" * 60)
    print(f"Delivery ID: {delivery_id}")

    if not dp_token:
        print("Error: No DP token available. Run authentication first.")
        return

    # Step 1: Get delivery state
    test_get_delivery_state(delivery_id)

    # Step 2: Accept delivery
    test_accept_delivery(delivery_id)
    time.sleep(1)

    # Step 3: Pickup
    test_pickup_delivery(delivery_id)
    time.sleep(1)

    # Step 4: Transit
    test_transit_delivery(delivery_id)
    time.sleep(1)

    # Step 5: Send OTP to recipient
    test_send_delivery_otp(delivery_id)
    time.sleep(1)

    # Step 6: Verify OTP and deliver
    test_verify_delivery_otp(delivery_id)
    time.sleep(1)

    # Step 7: Mark as delivered
    test_deliver_delivery(delivery_id)
    time.sleep(1)

    # Step 8: Get POD
    test_get_pod(delivery_id)

if __name__ == "__main__":
    import sys

    if len(sys.argv) > 1:
        if sys.argv[1] == "--lifecycle" and len(sys.argv) > 2:
            # Run delivery lifecycle test
            delivery_id = sys.argv[2]

            # First authenticate
            test_send_otp()
            time.sleep(1)
            test_verify_otp()

            # Then run lifecycle
            run_delivery_lifecycle_test(delivery_id)
        elif sys.argv[1] == "--help":
            print(__doc__)
            print("\nUsage:")
            print("  python test_dp_role.py              # Run all tests")
            print("  python test_dp_role.py --lifecycle <delivery_id>  # Run delivery lifecycle test")
    else:
        run_all_tests()
