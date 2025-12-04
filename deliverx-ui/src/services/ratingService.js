import api from './api';

// Submit a rating for a delivery
export const submitRating = async (ratingData) => {
  const response = await api.post('/api/v1/ratings', ratingData);
  return response.data;
};

// Get ratings received by current user
export const getMyRatings = async (page = 1, pageSize = 10) => {
  const response = await api.get(`/api/v1/ratings/received?page=${page}&pageSize=${pageSize}`);
  return response.data;
};

// Get ratings given by current user
export const getGivenRatings = async (page = 1, pageSize = 10) => {
  const response = await api.get(`/api/v1/ratings/given?page=${page}&pageSize=${pageSize}`);
  return response.data;
};

// Get rating summary for current user
export const getRatingSummary = async () => {
  const response = await api.get('/api/v1/ratings/summary');
  return response.data;
};

// Get behavior index for current user
export const getBehaviorIndex = async () => {
  const response = await api.get('/api/v1/ratings/behavior-index');
  return response.data;
};

// Get ratings for a specific delivery
export const getDeliveryRatings = async (deliveryId) => {
  const response = await api.get(`/api/v1/ratings/delivery/${deliveryId}`);
  return response.data;
};
