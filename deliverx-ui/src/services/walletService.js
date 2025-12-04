import api from './api';

// Get wallet balance
export const getWallet = async () => {
  const response = await api.get('/api/v1/wallet');
  return response.data;
};

// Get transaction history
export const getTransactions = async (page = 1, pageSize = 20, category = null) => {
  let url = `/api/v1/wallet/transactions?page=${page}&pageSize=${pageSize}`;
  if (category) url += `&category=${category}`;
  const response = await api.get(url);
  return response.data;
};

// Recharge wallet
export const rechargeWallet = async (amount, paymentMethod = 'UPI') => {
  const response = await api.post('/api/v1/wallet/recharge', {
    amount,
    paymentMethod
  });
  return response.data;
};

// Request withdrawal
export const requestWithdrawal = async (amount, paymentMethod = 'BANK_TRANSFER') => {
  const response = await api.post('/api/v1/wallet/withdraw', {
    amount,
    paymentMethod
  });
  return response.data;
};

// Get payment history
export const getPayments = async (page = 1, pageSize = 20) => {
  const response = await api.get(`/api/v1/payments?page=${page}&pageSize=${pageSize}`);
  return response.data;
};

// Get payment details
export const getPayment = async (paymentId) => {
  const response = await api.get(`/api/v1/payments/${paymentId}`);
  return response.data;
};

// Get settlements (for DP/DPCM)
export const getSettlements = async (page = 1, pageSize = 20) => {
  const response = await api.get(`/api/v1/settlements?page=${page}&pageSize=${pageSize}`);
  return response.data;
};

// Get settlement details
export const getSettlement = async (settlementId) => {
  const response = await api.get(`/api/v1/settlements/${settlementId}`);
  return response.data;
};

// Transaction categories
export const getTransactionCategories = () => {
  return [
    { value: 'DELIVERY_PAYMENT', label: 'Delivery Payment' },
    { value: 'DELIVERY_EARNING', label: 'Delivery Earning' },
    { value: 'COMMISSION', label: 'Commission' },
    { value: 'PLATFORM_FEE', label: 'Platform Fee' },
    { value: 'RECHARGE', label: 'Wallet Recharge' },
    { value: 'WITHDRAWAL', label: 'Withdrawal' },
    { value: 'REFUND', label: 'Refund' }
  ];
};
