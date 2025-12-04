import api from './api';

// Get available subscription plans
export const getPlans = async (planType = null) => {
  let url = '/api/v1/subscriptions/plans';
  if (planType) url += `?planType=${planType}`;
  const response = await api.get(url);
  return response.data;
};

// Get plan details
export const getPlan = async (planId) => {
  const response = await api.get(`/api/v1/subscriptions/plans/${planId}`);
  return response.data;
};

// Get current subscription
export const getMySubscription = async () => {
  const response = await api.get('/api/v1/subscriptions/my');
  return response.data;
};

// Subscribe to a plan
export const subscribe = async (planId, paymentMethod = 'WALLET', promoCode = null) => {
  const response = await api.post('/api/v1/subscriptions', {
    planId,
    paymentMethod,
    promoCode
  });
  return response.data;
};

// Cancel subscription
export const cancelSubscription = async (reason = null) => {
  const response = await api.post('/api/v1/subscriptions/cancel', { reason });
  return response.data;
};

// Toggle auto-renewal
export const toggleAutoRenew = async (autoRenew) => {
  const response = await api.put('/api/v1/subscriptions/auto-renew', { autoRenew });
  return response.data;
};

// Get invoices
export const getInvoices = async (page = 1, pageSize = 20) => {
  const response = await api.get(`/api/v1/subscriptions/invoices?page=${page}&pageSize=${pageSize}`);
  return response.data;
};

// Get invoice details
export const getInvoice = async (invoiceId) => {
  const response = await api.get(`/api/v1/subscriptions/invoices/${invoiceId}`);
  return response.data;
};

// Validate promo code
export const validatePromoCode = async (code, planId) => {
  const response = await api.post('/api/v1/subscriptions/promo/validate', {
    code,
    planId
  });
  return response.data;
};
