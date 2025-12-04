import api from './api';

// Get or create referral code
export const getMyReferralCode = async () => {
  const response = await api.get('/api/v1/referrals/my-code');
  return response.data;
};

// Apply a referral code
export const applyReferralCode = async (code) => {
  const response = await api.post('/api/v1/referrals/apply', { code });
  return response.data;
};

// Get referral statistics
export const getReferralStats = async () => {
  const response = await api.get('/api/v1/referrals/stats');
  return response.data;
};

// Donation APIs

// Get available charities
export const getCharities = async () => {
  const response = await api.get('/api/v1/donations/charities');
  return response.data;
};

// Make a donation
export const makeDonation = async (charityId, amount, isAnonymous = false, message = null) => {
  const response = await api.post('/api/v1/donations', {
    charityId,
    amount,
    isAnonymous,
    message
  });
  return response.data;
};

// Get donation statistics
export const getDonationStats = async () => {
  const response = await api.get('/api/v1/donations/stats');
  return response.data;
};

// Get donation preferences
export const getDonationPreferences = async () => {
  const response = await api.get('/api/v1/donations/preferences');
  return response.data;
};

// Update donation preferences
export const updateDonationPreferences = async (enableRoundUp, preferredCharityId = null, monthlyLimit = null) => {
  const response = await api.put('/api/v1/donations/preferences', {
    enableRoundUp,
    preferredCharityId,
    monthlyLimit
  });
  return response.data;
};
