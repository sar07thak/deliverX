import api from './api';

/**
 * KYC service for identity verification
 */
const kycService = {
  /**
   * Initiate Aadhaar verification
   * @param {string} method - Verification method: "DIGILOCKER" or "MANUAL_UPLOAD"
   * @param {string} redirectUrl - URL to redirect after DigiLocker verification
   * @param {string} documentUrl - Document URL for manual upload
   * @param {string} aadhaarLast4 - Last 4 digits of Aadhaar for manual upload
   * @returns {Promise} Response with verification initiation result
   */
  initiateAadhaar: async (method = 'DIGILOCKER', redirectUrl = null, documentUrl = null, aadhaarLast4 = null) => {
    try {
      const requestBody = {
        method: method
      };

      if (method === 'DIGILOCKER' && redirectUrl) {
        requestBody.redirectUrl = redirectUrl;
      }

      if (method === 'MANUAL_UPLOAD') {
        requestBody.documentUrl = documentUrl;
        requestBody.aadhaarLast4 = aadhaarLast4;
      }

      const response = await api.post('/api/v1/kyc/aadhaar/initiate', requestBody);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Complete Aadhaar verification with DigiLocker token
   * @param {string} digilockerToken - DigiLocker token from callback
   * @returns {Promise} Response with Aadhaar verification status
   */
  completeAadhaar: async (digilockerToken) => {
    try {
      const response = await api.post('/api/v1/kyc/aadhaar/verify', {
        digilockerToken: digilockerToken
      });
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  /**
   * Verify PAN card
   * @param {string} panNumber - PAN number (format: ABCDE1234F)
   * @returns {Promise} Response with PAN verification status
   */
  verifyPAN: async (panNumber) => {
    try {
      console.log('Verifying PAN:', panNumber);
      const response = await api.post('/api/v1/kyc/pan/verify', {
        pan: panNumber
      });
      console.log('PAN verification response:', response.data);
      return response.data;
    } catch (error) {
      console.error('PAN verification error:', error);
      throw error;
    }
  },

  /**
   * Verify bank account using penny drop
   * @param {Object} bankData - Bank account details
   * @param {string} bankData.accountNumber - Bank account number
   * @param {string} bankData.ifscCode - IFSC code
   * @param {string} bankData.accountHolderName - Account holder name
   * @param {string} method - Verification method (default: "PENNY_DROP")
   * @returns {Promise} Response with bank verification status
   */
  verifyBank: async (bankData, method = 'PENNY_DROP') => {
    try {
      console.log('Verifying bank account:', bankData);
      const response = await api.post('/api/v1/kyc/bank/verify', {
        accountNumber: bankData.accountNumber,
        ifscCode: bankData.ifscCode,
        accountHolderName: bankData.accountHolderName,
        method: method
      });
      console.log('Bank verification response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Bank verification error:', error);
      throw error;
    }
  },

  /**
   * Get overall KYC status for a user
   * @param {string} userId - User ID
   * @returns {Promise} Response with complete KYC status
   */
  getKYCStatus: async (userId) => {
    try {
      const response = await api.get(`/api/v1/kyc/${userId}/status`);
      return response.data;
    } catch (error) {
      throw error;
    }
  }
};

export default kycService;
