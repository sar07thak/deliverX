import api from './api';

const BASE_URL = '/api/v1/deliveries';

const deliveryService = {
  // Create a new delivery
  createDelivery: async (deliveryData) => {
    const response = await api.post(BASE_URL, deliveryData);
    return response.data;
  },

  // Get delivery by ID
  getDelivery: async (deliveryId) => {
    const response = await api.get(`${BASE_URL}/${deliveryId}`);
    return response.data;
  },

  // Get user's deliveries
  getMyDeliveries: async (params = {}) => {
    const response = await api.get(BASE_URL, { params });
    return response.data;
  },

  // Trigger matching for a delivery
  matchDelivery: async (deliveryId) => {
    const response = await api.post(`${BASE_URL}/${deliveryId}/match`);
    return response.data;
  },

  // Accept a delivery (for DP)
  acceptDelivery: async (deliveryId) => {
    const response = await api.post(`${BASE_URL}/${deliveryId}/accept`);
    return response.data;
  },

  // Reject a delivery (for DP)
  rejectDelivery: async (deliveryId, reason) => {
    const response = await api.post(`${BASE_URL}/${deliveryId}/reject`, { reason });
    return response.data;
  },

  // Get delivery state info
  getDeliveryState: async (deliveryId) => {
    const response = await api.get(`${BASE_URL}/${deliveryId}/state`);
    return response.data;
  },

  // Mark as picked up
  markAsPickedUp: async (deliveryId, pickupData) => {
    const response = await api.post(`${BASE_URL}/${deliveryId}/pickup`, pickupData);
    return response.data;
  },

  // Mark as in transit
  markAsInTransit: async (deliveryId, transitData) => {
    const response = await api.post(`${BASE_URL}/${deliveryId}/transit`, transitData);
    return response.data;
  },

  // Mark as delivered
  markAsDelivered: async (deliveryId, deliveryData) => {
    const response = await api.post(`${BASE_URL}/${deliveryId}/deliver`, deliveryData);
    return response.data;
  },

  // Close delivery
  closeDelivery: async (deliveryId, reason) => {
    const response = await api.post(`${BASE_URL}/${deliveryId}/close`, { reason });
    return response.data;
  },

  // Cancel delivery
  cancelDelivery: async (deliveryId, reason) => {
    const response = await api.post(`${BASE_URL}/${deliveryId}/cancel`, { reason });
    return response.data;
  },

  // Get Proof of Delivery
  getPOD: async (deliveryId) => {
    const response = await api.get(`${BASE_URL}/${deliveryId}/pod`);
    return response.data;
  },

  // Send delivery OTP
  sendDeliveryOTP: async (deliveryId) => {
    const response = await api.post(`${BASE_URL}/${deliveryId}/otp/send`);
    return response.data;
  },

  // Verify delivery OTP
  verifyDeliveryOTP: async (deliveryId, otp) => {
    const response = await api.post(`${BASE_URL}/${deliveryId}/otp/verify`, { otp });
    return response.data;
  },

  // Update DP availability
  updateAvailability: async (availabilityData) => {
    const response = await api.put(`${BASE_URL}/availability`, availabilityData);
    return response.data;
  },

  // Get DP availability
  getAvailability: async () => {
    const response = await api.get(`${BASE_URL}/availability`);
    return response.data;
  },

  // Get pending deliveries for DP
  getPendingDeliveries: async () => {
    const response = await api.get(`${BASE_URL}/pending`);
    return response.data;
  },

  // Calculate delivery estimate
  getDeliveryEstimate: async (pickupLat, pickupLng, dropLat, dropLng) => {
    const response = await api.post(`/api/v1/pricing/calculate`, {
      pickupLat,
      pickupLng,
      dropLat,
      dropLng
    });
    return response.data;
  }
};

export default deliveryService;
