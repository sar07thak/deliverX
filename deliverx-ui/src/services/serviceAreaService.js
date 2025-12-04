import api from './api';

const BASE_URL = '/api/v1/service-area';

const serviceAreaService = {
  // Set or update service area for current DP
  setServiceArea: async (serviceAreaData) => {
    const response = await api.post(BASE_URL, serviceAreaData);
    return response.data;
  },

  // Get current DP's service area (no dpId means current user)
  getMyServiceArea: async () => {
    const response = await api.get(BASE_URL);
    return response.data;
  },

  // Check if location is within service area
  checkCoverage: async (lat, lng) => {
    const response = await api.get(`${BASE_URL}/check`, {
      params: { lat, lng }
    });
    return response.data;
  },

  // Find eligible DPs for a route
  findEligibleDPs: async (pickupLat, pickupLng, dropLat, dropLng, maxResults = 10) => {
    const response = await api.post(`${BASE_URL}/find-eligible`, {
      pickupLat,
      pickupLng,
      dropLat,
      dropLng,
      maxResults
    });
    return response.data;
  },

  // Get all service areas (admin)
  getAllServiceAreas: async () => {
    const response = await api.get(`${BASE_URL}/all`);
    return response.data;
  },

  // Delete service area
  deleteServiceArea: async () => {
    const response = await api.delete(BASE_URL);
    return response.data;
  }
};

// Named exports for convenience
export const setServiceArea = serviceAreaService.setServiceArea;
export const getMyServiceArea = serviceAreaService.getMyServiceArea;
export const checkCoverage = serviceAreaService.checkCoverage;
export const findEligibleDPs = serviceAreaService.findEligibleDPs;
export const getAllServiceAreas = serviceAreaService.getAllServiceAreas;
export const getServiceAreas = serviceAreaService.getAllServiceAreas; // Alias
export const deleteServiceArea = serviceAreaService.deleteServiceArea;

export default serviceAreaService;
