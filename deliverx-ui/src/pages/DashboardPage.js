import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import kycService from '../services/kycService';
import deliveryService from '../services/deliveryService';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

/**
 * Dashboard Page Component
 * Main dashboard showing KYC status and delivery partner info
 */
const DashboardPage = () => {
  const { user } = useAuth();
  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [kycStatus, setKycStatus] = useState(null);
  const [deliverySummary, setDeliverySummary] = useState({
    total: 0,
    active: 0,
    completed: 0,
    recentDeliveries: []
  });

  useEffect(() => {
    if (user && (user.userId || user.id)) {
      fetchData();
    } else {
      setLoading(false);
    }
  }, [user]);

  const fetchData = async () => {
    await Promise.all([fetchKYCStatus(), fetchDeliverySummary()]);
  };

  const fetchDeliverySummary = async () => {
    try {
      const result = await deliveryService.getMyDeliveries();
      const deliveries = result.deliveries || [];

      const active = deliveries.filter(d =>
        !['DELIVERED', 'CLOSED', 'CANCELLED'].includes(d.status)
      ).length;

      const completed = deliveries.filter(d =>
        d.status === 'DELIVERED' || d.status === 'CLOSED'
      ).length;

      setDeliverySummary({
        total: deliveries.length,
        active,
        completed,
        recentDeliveries: deliveries.slice(0, 3)
      });
    } catch (err) {
      console.error('Failed to fetch delivery summary:', err);
    }
  };

  const fetchKYCStatus = async () => {
    try {
      setLoading(true);
      const userId = user.userId || user.id;
      console.log('Fetching dashboard KYC status for user:', userId);

      const response = await kycService.getKYCStatus(userId);
      console.log('Dashboard KYC status response:', response);

      if (response.success) {
        setKycStatus(response.data);
      } else {
        setError(response.message || 'Failed to fetch KYC status');
      }
    } catch (err) {
      console.error('Dashboard KYC error:', err);
      setError(err.response?.data?.message || err.message || 'Failed to fetch KYC status');
    } finally {
      setLoading(false);
    }
  };

  const getStatusBadge = (status) => {
    const statusMap = {
      'VERIFIED': 'badge-success',
      'PENDING': 'badge-warning',
      'FAILED': 'badge-danger',
      'NOT_STARTED': 'badge-info'
    };

    return (
      <span className={`badge ${statusMap[status] || 'badge-info'}`}>
        {status.replace('_', ' ')}
      </span>
    );
  };

  const getVerificationProgress = () => {
    if (!kycStatus) return 0;

    let completed = 0;
    let total = 3;

    if (kycStatus.aadhaarStatus === 'VERIFIED') completed++;
    if (kycStatus.panStatus === 'VERIFIED') completed++;
    if (kycStatus.bankStatus === 'VERIFIED') completed++;

    return Math.round((completed / total) * 100);
  };

  if (loading) {
    return (
      <div className="container">
        <LoadingSpinner message="Loading dashboard..." />
      </div>
    );
  }

  return (
    <div className="container">
      <div style={{ maxWidth: '1000px', margin: '30px auto' }}>
        <h1 style={{ marginBottom: '10px', color: '#2c3e50' }}>
          Welcome, {user?.name || user?.phoneNumber}!
        </h1>
        <p style={{ marginBottom: '30px', color: '#666' }}>
          Delivery Partner Dashboard
        </p>

        {error && <ErrorMessage message={error} onClose={() => setError('')} />}

        {/* Overall Status Card */}
        <div className="card" style={{ marginBottom: '20px' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
            <div>
              <h2 style={{ fontSize: '18px', marginBottom: '5px', color: '#2c3e50' }}>
                Account Status
              </h2>
              {getStatusBadge(kycStatus?.overallStatus || 'NOT_STARTED')}
            </div>

            <button
              className="btn btn-outline"
              onClick={fetchKYCStatus}
            >
              Refresh
            </button>
          </div>

          <div className="progress" style={{ marginBottom: '15px' }}>
            <div
              className="progress-bar"
              style={{ width: `${getVerificationProgress()}%` }}
            />
          </div>

          <p style={{ color: '#666', fontSize: '14px' }}>
            Verification Progress: {getVerificationProgress()}% Complete
          </p>

          {kycStatus?.overallStatus === 'FULLY_VERIFIED' ? (
            <div className="alert alert-success" style={{ marginTop: '20px' }}>
              <strong>🎉 Congratulations!</strong>
              <p style={{ marginTop: '8px', marginBottom: 0 }}>
                Your account is fully verified. You can now start accepting deliveries!
              </p>
            </div>
          ) : (
            <div className="alert alert-warning" style={{ marginTop: '20px' }}>
              <strong>⚠️ Verification Pending</strong>
              <p style={{ marginTop: '8px', marginBottom: 0 }}>
                Please complete all KYC verifications to start delivering.
              </p>
            </div>
          )}
        </div>

        {/* Verification Status Cards */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '20px', marginBottom: '20px' }}>
          {/* Aadhaar Card */}
          <div className="card">
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '10px' }}>
              <h3 style={{ fontSize: '16px', color: '#2c3e50', margin: 0 }}>
                Aadhaar
              </h3>
              {kycStatus?.aadhaarStatus === 'VERIFIED' ? (
                <span style={{ fontSize: '24px', color: '#4CAF50' }}>✓</span>
              ) : (
                <span style={{ fontSize: '24px', color: '#ccc' }}>○</span>
              )}
            </div>
            {getStatusBadge(kycStatus?.aadhaarStatus || 'NOT_STARTED')}
            {kycStatus?.aadhaarStatus !== 'VERIFIED' && (
              <button
                className="btn btn-primary"
                style={{ marginTop: '15px', width: '100%' }}
                onClick={() => navigate('/kyc/aadhaar')}
              >
                Verify Now
              </button>
            )}
          </div>

          {/* PAN Card */}
          <div className="card">
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '10px' }}>
              <h3 style={{ fontSize: '16px', color: '#2c3e50', margin: 0 }}>
                PAN Card
              </h3>
              {kycStatus?.panStatus === 'VERIFIED' ? (
                <span style={{ fontSize: '24px', color: '#4CAF50' }}>✓</span>
              ) : (
                <span style={{ fontSize: '24px', color: '#ccc' }}>○</span>
              )}
            </div>
            {getStatusBadge(kycStatus?.panStatus || 'NOT_STARTED')}
            {kycStatus?.panStatus !== 'VERIFIED' && (
              <button
                className="btn btn-primary"
                style={{ marginTop: '15px', width: '100%' }}
                onClick={() => navigate('/kyc/pan')}
              >
                Verify Now
              </button>
            )}
          </div>

          {/* Bank Account Card */}
          <div className="card">
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '10px' }}>
              <h3 style={{ fontSize: '16px', color: '#2c3e50', margin: 0 }}>
                Bank Account
              </h3>
              {kycStatus?.bankStatus === 'VERIFIED' ? (
                <span style={{ fontSize: '24px', color: '#4CAF50' }}>✓</span>
              ) : (
                <span style={{ fontSize: '24px', color: '#ccc' }}>○</span>
              )}
            </div>
            {getStatusBadge(kycStatus?.bankStatus || 'NOT_STARTED')}
            {kycStatus?.bankStatus !== 'VERIFIED' && (
              <button
                className="btn btn-primary"
                style={{ marginTop: '15px', width: '100%' }}
                onClick={() => navigate('/kyc/bank')}
              >
                Verify Now
              </button>
            )}
          </div>
        </div>

        {/* Profile Information Card */}
        {user && (
          <div className="card">
            <h2 style={{ fontSize: '18px', marginBottom: '15px', color: '#2c3e50' }}>
              Profile Information
            </h2>

            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))', gap: '15px' }}>
              <div>
                <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Name</p>
                <p style={{ fontSize: '14px', fontWeight: '500' }}>{user.name || 'Not provided'}</p>
              </div>

              <div>
                <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Phone Number</p>
                <p style={{ fontSize: '14px', fontWeight: '500' }}>{user.phoneNumber}</p>
              </div>

              <div>
                <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Email</p>
                <p style={{ fontSize: '14px', fontWeight: '500' }}>{user.email || 'Not provided'}</p>
              </div>

              <div>
                <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Vehicle Type</p>
                <p style={{ fontSize: '14px', fontWeight: '500' }}>{user.vehicleType || 'Not provided'}</p>
              </div>

              <div>
                <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Service Area</p>
                <p style={{ fontSize: '14px', fontWeight: '500' }}>{user.serviceArea || 'Not provided'}</p>
              </div>

              <div>
                <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Availability</p>
                <p style={{ fontSize: '14px', fontWeight: '500' }}>{user.availability || 'Not provided'}</p>
              </div>
            </div>

            <button
              className="btn btn-secondary"
              style={{ marginTop: '20px' }}
              onClick={() => navigate('/profile')}
            >
              View Full Profile
            </button>
          </div>
        )}

        {/* Delivery Summary */}
        <div className="card" style={{ marginBottom: '20px' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
            <h2 style={{ fontSize: '18px', color: '#2c3e50', margin: 0 }}>
              Delivery Summary
            </h2>
            <button
              className="btn btn-outline"
              onClick={() => navigate('/deliveries')}
            >
              View All
            </button>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '15px', marginBottom: '20px' }}>
            <div style={{ textAlign: 'center', padding: '15px', background: '#f5f5f5', borderRadius: '8px' }}>
              <p style={{ fontSize: '28px', fontWeight: '700', color: '#667eea', margin: 0 }}>
                {deliverySummary.total}
              </p>
              <p style={{ fontSize: '12px', color: '#666', margin: '5px 0 0 0' }}>Total Deliveries</p>
            </div>
            <div style={{ textAlign: 'center', padding: '15px', background: '#fff3e0', borderRadius: '8px' }}>
              <p style={{ fontSize: '28px', fontWeight: '700', color: '#f57c00', margin: 0 }}>
                {deliverySummary.active}
              </p>
              <p style={{ fontSize: '12px', color: '#666', margin: '5px 0 0 0' }}>Active</p>
            </div>
            <div style={{ textAlign: 'center', padding: '15px', background: '#e8f5e9', borderRadius: '8px' }}>
              <p style={{ fontSize: '28px', fontWeight: '700', color: '#2e7d32', margin: 0 }}>
                {deliverySummary.completed}
              </p>
              <p style={{ fontSize: '12px', color: '#666', margin: '5px 0 0 0' }}>Completed</p>
            </div>
          </div>

          {deliverySummary.recentDeliveries.length > 0 ? (
            <div>
              <p style={{ fontSize: '14px', color: '#666', marginBottom: '10px' }}>Recent Deliveries</p>
              {deliverySummary.recentDeliveries.map(delivery => (
                <div
                  key={delivery.id}
                  style={{
                    padding: '10px',
                    background: '#f9f9f9',
                    borderRadius: '6px',
                    marginBottom: '8px',
                    cursor: 'pointer'
                  }}
                  onClick={() => navigate(`/deliveries/${delivery.id}`)}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <span style={{ fontSize: '13px', color: '#333' }}>
                      {delivery.pickupAddress?.substring(0, 30)}...
                    </span>
                    <span style={{
                      fontSize: '11px',
                      padding: '2px 8px',
                      borderRadius: '10px',
                      background: delivery.status === 'DELIVERED' ? '#c8e6c9' : '#fff3e0',
                      color: delivery.status === 'DELIVERED' ? '#2e7d32' : '#f57c00'
                    }}>
                      {delivery.status}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div style={{ textAlign: 'center', padding: '20px', color: '#999' }}>
              <p>No deliveries yet</p>
              <button
                className="btn btn-primary"
                onClick={() => navigate('/deliveries/create')}
                style={{ marginTop: '10px' }}
              >
                Create Your First Delivery
              </button>
            </div>
          )}
        </div>

        {/* Action Buttons */}
        <div className="btn-group" style={{ marginTop: '30px' }}>
          {kycStatus?.overallStatus === 'FULLY_VERIFIED' ? (
            <button
              className="btn btn-primary"
              style={{ fontSize: '16px', padding: '12px 24px' }}
              onClick={() => navigate('/service-area')}
            >
              Manage Service Area
            </button>
          ) : (
            <button
              className="btn btn-primary"
              onClick={() => navigate('/kyc')}
              style={{ fontSize: '16px', padding: '12px 24px' }}
            >
              Complete KYC Verification
            </button>
          )}
          <button
            className="btn btn-secondary"
            style={{ fontSize: '16px', padding: '12px 24px', marginLeft: '10px' }}
            onClick={() => navigate('/deliveries/create')}
          >
            + New Delivery
          </button>
        </div>
      </div>
    </div>
  );
};

export default DashboardPage;
