import api from './api';

/**
 * Registration service for delivery partner onboarding
 */
const registrationService = {
  /**
   * Initiate registration process with phone number
   * @param {string} phoneNumber - Phone number in format +91XXXXXXXXXX
   * @returns {Promise} Response with userId
   */
  initiateRegistration: async (phoneNumber) => {
    try {
      const response = await api.post('/api/v1/registration/dp/initiate', { phone: phoneNumber });
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Complete delivery partner profile
   * @param {Object} profileData - Complete profile data
   * @param {string} profileData.userId - User ID from initiation
   * @param {string} profileData.name - Full name
   * @param {string} profileData.dateOfBirth - Date of birth (YYYY-MM-DD)
   * @param {string} profileData.gender - Gender (Male/Female/Other)
   * @param {string} profileData.email - Email address
   * @param {Object} profileData.address - Address object
   * @param {string} profileData.vehicleType - Vehicle type
   * @param {Array} profileData.languagesKnown - Array of languages
   * @param {string} profileData.availability - Availability (Full-time/Part-time)
   * @param {string} profileData.serviceArea - Service area/city
   * @param {Object} profileData.pricing - Pricing object
   * @returns {Promise} Response with registration status
   */
  completeProfile: async (profileData) => {
    try {
      console.log('Sending profile data:', profileData);
      const response = await api.post('/api/v1/registration/dp/profile', profileData);
      console.log('Profile response:', response.data);

      // Update user in localStorage if registration successful
      if (response.data.success && response.data.data) {
        const userData = response.data.data.user || response.data.data;
        localStorage.setItem('user', JSON.stringify(userData));
      }

      return response.data;
    } catch (error) {
      console.error('Profile completion error:', error);
      console.error('Error response:', error.response?.data);
      throw error;
    }
  },

  /**
   * Get registration status for a user
   * @param {string} userId - User ID
   * @returns {Promise} Response with registration status
   */
  getRegistrationStatus: async (userId) => {
    try {
      const response = await api.get(`/api/v1/registration/status/${userId}`);
      return response.data;
    } catch (error) {
      throw error;
    }
  }
};

export default registrationService;
