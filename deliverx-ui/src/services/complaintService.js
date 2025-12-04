import api from './api';

// File a new complaint
export const fileComplaint = async (complaintData) => {
  // Prepare data for backend - remove severity (backend sets it based on category)
  const payload = {
    deliveryId: complaintData.deliveryId,
    category: complaintData.category,
    subject: complaintData.subject,
    description: complaintData.description
  };
  const response = await api.post('/api/v1/complaints', payload);
  return response.data;
};

// Get user's complaints
export const getMyComplaints = async (page = 1, pageSize = 10, status = null) => {
  let url = `/api/v1/complaints?page=${page}&pageSize=${pageSize}`;
  if (status) url += `&status=${status}`;
  const response = await api.get(url);
  // Backend returns { Complaints, TotalCount, Page, PageSize, TotalPages }
  // Frontend expects { items, totalPages }
  const data = response.data;
  return {
    items: data.complaints || data.Complaints || [],
    totalPages: data.totalPages || data.TotalPages || 1,
    totalCount: data.totalCount || data.TotalCount || 0
  };
};

// Get complaint details
export const getComplaint = async (complaintId) => {
  const response = await api.get(`/api/v1/complaints/${complaintId}`);
  return response.data;
};

// Add comment to complaint
export const addComment = async (complaintId, content, isInternal = false) => {
  const response = await api.post(`/api/v1/complaints/${complaintId}/comments`, {
    content,
    isInternal
  });
  return response.data;
};

// Upload evidence
export const uploadEvidence = async (complaintId, evidenceData) => {
  const response = await api.post(`/api/v1/complaints/${complaintId}/evidence`, evidenceData);
  return response.data;
};

// Get available categories
export const getCategories = () => {
  return [
    { value: 'DAMAGE', label: 'Package Damaged' },
    { value: 'THEFT', label: 'Package Theft' },
    { value: 'DELAY', label: 'Delivery Delay' },
    { value: 'BEHAVIOR', label: 'Unprofessional Behavior' },
    { value: 'FRAUD', label: 'Fraud/Scam' },
    { value: 'OTHER', label: 'Other Issue' }
  ];
};

// Get severity levels
export const getSeverityLevels = () => {
  return [
    { value: 'LOW', label: 'Low', color: 'green' },
    { value: 'MEDIUM', label: 'Medium', color: 'orange' },
    { value: 'HIGH', label: 'High', color: 'red' },
    { value: 'CRITICAL', label: 'Critical', color: 'darkred' }
  ];
};
