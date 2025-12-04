import api from './api';

/**
 * DPCM (Delivery Partner Channel Manager) Service
 * API calls for DPCM dashboard and management
 */

// Get DPCM dashboard data
export const getDPCMDashboard = async () => {
  const response = await api.get('/api/v1/dpcm/dashboard');
  return response.data;
};

// Get managed delivery partners
export const getDPCMPartners = async ({ status = 'all', page = 1, pageSize = 20 } = {}) => {
  const params = new URLSearchParams();
  if (status && status !== 'all') params.append('status', status);
  params.append('page', page);
  params.append('pageSize', pageSize);

  const response = await api.get(`/api/v1/dpcm/partners?${params.toString()}`);
  return response.data;
};

// Update delivery partner status (activate/deactivate)
export const updateDPStatus = async (dpId, isActive) => {
  const response = await api.put(`/api/v1/dpcm/partners/${dpId}/status`, { isActive });
  return response.data;
};

// Get deliveries by managed DPs
export const getDPCMDeliveries = async ({ status = '', dpId = '', page = 1, pageSize = 20 } = {}) => {
  const params = new URLSearchParams();
  if (status) params.append('status', status);
  if (dpId) params.append('dpId', dpId);
  params.append('page', page);
  params.append('pageSize', pageSize);

  const response = await api.get(`/api/v1/dpcm/deliveries?${params.toString()}`);
  return response.data;
};

// Get commission configuration
export const getDPCMCommissionConfig = async () => {
  const response = await api.get('/api/v1/dpcm/commission');
  return response.data;
};

// Update commission configuration
export const updateDPCMCommissionConfig = async (config) => {
  const response = await api.put('/api/v1/dpcm/commission', config);
  return response.data;
};

// Get settlements
export const getDPCMSettlements = async ({ status = '', page = 1, pageSize = 20 } = {}) => {
  const params = new URLSearchParams();
  if (status) params.append('status', status);
  params.append('page', page);
  params.append('pageSize', pageSize);

  const response = await api.get(`/api/v1/dpcm/settlements?${params.toString()}`);
  return response.data;
};

// Request settlement
export const requestDPCMSettlement = async (amount) => {
  const response = await api.post('/api/v1/dpcm/settlements/request', { amount });
  return response.data;
};

export default {
  getDPCMDashboard,
  getDPCMPartners,
  updateDPStatus,
  getDPCMDeliveries,
  getDPCMCommissionConfig,
  updateDPCMCommissionConfig,
  getDPCMSettlements,
  requestDPCMSettlement
};
