import api from './api';

/**
 * Authentication service for OTP-based login
 */
const authService = {
  /**
   * Send OTP to phone number
   * @param {string} phoneNumber - Phone number in format +91XXXXXXXXXX
   * @param {string} role - User role (EC, BC, DP) for new registrations
   * @returns {Promise} Response with success status
   */
  sendOTP: async (phoneNumber, role = null) => {
    try {
      const payload = {
        phone: phoneNumber.replace('+91', ''),
        countryCode: '+91'
      };
      if (role) {
        payload.role = role;
      }
      const response = await api.post('/api/v1/auth/otp/send', payload);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Verify OTP and authenticate user
   * @param {string} phoneNumber - Phone number used for OTP
   * @param {string} otp - One-time password received
   * @param {string} role - User role (EC, BC, DP) for new registrations
   * @returns {Promise} Response with token and user data
   */
  verifyOTP: async (phoneNumber, otp, role = null) => {
    try {
      const payload = {
        phone: phoneNumber.replace('+91', ''),
        otp,
        deviceId: 'web-browser'
      };
      if (role) {
        payload.role = role;
      }
      const response = await api.post('/api/v1/auth/otp/verify', payload);

      // Store token and user data in localStorage
      if (response.data.success && response.data.data.accessToken) {
        localStorage.setItem('token', response.data.data.accessToken);
        localStorage.setItem('user', JSON.stringify(response.data.data.user));
      }

      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Logout user by clearing stored data
   */
  logout: () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    window.location.href = '/login';
  },

  /**
   * Get current user from localStorage
   * @returns {Object|null} User object or null if not logged in
   */
  getCurrentUser: () => {
    const userStr = localStorage.getItem('user');
    if (userStr) {
      try {
        return JSON.parse(userStr);
      } catch (error) {
        return null;
      }
    }
    return null;
  },

  /**
   * Get current token from localStorage
   * @returns {string|null} Token or null if not logged in
   */
  getToken: () => {
    return localStorage.getItem('token');
  },

  /**
   * Check if user is authenticated
   * @returns {boolean} True if user has valid token
   */
  isAuthenticated: () => {
    return !!localStorage.getItem('token');
  }
};

export default authService;
