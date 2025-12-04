import api from './api';

// Admin Dashboard
export const getAdminDashboard = async () => {
  const response = await api.get('/api/v1/admin/dashboard');
  return response.data;
};

export const getPlatformStats = async () => {
  const response = await api.get('/api/v1/admin/stats/platform');
  return response.data;
};

export const getRevenueStats = async () => {
  const response = await api.get('/api/v1/admin/stats/revenue');
  return response.data;
};

// Report Generation
export const generateReport = async (reportType, startDate, endDate, groupBy = 'DAY') => {
  const response = await api.post('/api/v1/admin/reports', {
    reportType,
    startDate,
    endDate,
    groupBy
  });
  return response.data;
};

// User Management
export const getUsers = async (params = {}) => {
  const { page = 1, pageSize = 20, role, status, searchTerm, sortBy, sortDesc } = params;
  let url = `/api/v1/admin/users?page=${page}&pageSize=${pageSize}`;
  if (role) url += `&role=${role}`;
  if (status) url += `&status=${status}`;
  if (searchTerm) url += `&searchTerm=${encodeURIComponent(searchTerm)}`;
  if (sortBy) url += `&sortBy=${sortBy}&sortDesc=${sortDesc || false}`;
  const response = await api.get(url);
  return response.data;
};

export const getUser = async (userId) => {
  const response = await api.get(`/api/v1/admin/users/${userId}`);
  return response.data;
};

export const updateUserStatus = async (userId, status, reason = null) => {
  const response = await api.put(`/api/v1/admin/users/${userId}/status`, {
    status,
    reason
  });
  return response.data;
};

// KYC Management
export const getKYCRequests = async (params = {}) => {
  const { page = 1, pageSize = 20, status, documentType } = params;
  let url = `/api/v1/admin/kyc?page=${page}&pageSize=${pageSize}`;
  if (status) url += `&status=${status}`;
  if (documentType) url += `&documentType=${documentType}`;
  const response = await api.get(url);
  return response.data;
};

export const approveKYC = async (kycId, notes = null) => {
  const response = await api.post(`/api/v1/admin/kyc/${kycId}/approve`, { notes });
  return response.data;
};

export const rejectKYC = async (kycId, reason) => {
  const response = await api.post(`/api/v1/admin/kyc/${kycId}/reject`, { reason });
  return response.data;
};

// Audit Logs
export const getAuditLogs = async (params = {}) => {
  const { page = 1, pageSize = 50, userId, action, startDate, endDate } = params;
  let url = `/api/v1/admin/audit-logs?page=${page}&pageSize=${pageSize}`;
  if (userId) url += `&userId=${userId}`;
  if (action) url += `&action=${action}`;
  if (startDate) url += `&startDate=${startDate}`;
  if (endDate) url += `&endDate=${endDate}`;
  const response = await api.get(url);
  return response.data;
};

// System Configuration
export const getSystemConfig = async () => {
  const response = await api.get('/api/v1/admin/config');
  return response.data;
};

export const updateSystemConfig = async (key, value) => {
  const response = await api.put('/api/v1/admin/config', { key, value });
  return response.data;
};

// DPCM Dashboard
export const getDPCMDashboard = async () => {
  const response = await api.get('/api/v1/dpcm/dashboard');
  return response.data;
};
