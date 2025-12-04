import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import kycService from '../services/kycService';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

/**
 * KYC Page Component
 * Main KYC verification hub with tabs for different verification types
 */
const KYCPage = () => {
  const { user } = useAuth();
  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [kycStatus, setKycStatus] = useState(null);

  useEffect(() => {
    fetchKYCStatus();
  }, []);

  const fetchKYCStatus = async () => {
    try {
      setLoading(true);
      const userId = user.userId || user.id;
      console.log('Fetching KYC status for user:', userId);

      const response = await kycService.getKYCStatus(userId);
      console.log('KYC status response:', response);

      if (response.success) {
        setKycStatus(response.data);
      } else {
        setError(response.message || 'Failed to fetch KYC status');
      }
    } catch (err) {
      console.error('KYC status error:', err);
      setError(err.message || 'Failed to fetch KYC status');
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

  if (loading) {
    return (
      <div className="container">
        <LoadingSpinner message="Loading KYC status..." />
      </div>
    );
  }

  return (
    <div className="container">
      <div style={{ maxWidth: '900px', margin: '30px auto' }}>
        <h1 style={{ marginBottom: '10px', color: '#2c3e50' }}>KYC Verification</h1>
        <p style={{ marginBottom: '30px', color: '#666' }}>
          Complete all verifications to start delivering
        </p>

        {error && <ErrorMessage message={error} onClose={() => setError('')} />}

        {/* Overall Status Card */}
        <div className="card" style={{ marginBottom: '20px' }}>
          <h2 style={{ fontSize: '18px', marginBottom: '15px', color: '#2c3e50' }}>
            Overall Status
          </h2>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <div>
              <p style={{ fontSize: '14px', color: '#666', marginBottom: '5px' }}>
                Current Status
              </p>
              <div style={{ fontSize: '20px', fontWeight: 'bold' }}>
                {getStatusBadge(kycStatus?.overallStatus || 'NOT_STARTED')}
              </div>
            </div>

            {kycStatus?.overallStatus === 'FULLY_VERIFIED' && (
              <div className="alert alert-success" style={{ margin: 0, padding: '10px 15px' }}>
                ✓ All verifications complete! You can start delivering.
              </div>
            )}
          </div>
        </div>

        {/* Aadhaar Verification Card */}
        <div className="card" style={{ marginBottom: '20px' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '15px' }}>
            <h2 style={{ fontSize: '18px', color: '#2c3e50', margin: 0 }}>
              Aadhaar Verification
            </h2>
            {getStatusBadge(kycStatus?.aadhaarStatus || 'NOT_STARTED')}
          </div>

          <p style={{ color: '#666', marginBottom: '15px' }}>
            Verify your identity using Aadhaar via DigiLocker
          </p>

          {kycStatus?.aadhaarVerifiedAt && (
            <p style={{ fontSize: '12px', color: '#666', marginBottom: '15px' }}>
              Verified on: {new Date(kycStatus.aadhaarVerifiedAt).toLocaleString()}
            </p>
          )}

          <button
            className="btn btn-primary"
            onClick={() => navigate('/kyc/aadhaar')}
            disabled={kycStatus?.aadhaarStatus === 'VERIFIED'}
          >
            {kycStatus?.aadhaarStatus === 'VERIFIED' ? 'Verified ✓' : 'Start Verification'}
          </button>
        </div>

        {/* PAN Verification Card */}
        <div className="card" style={{ marginBottom: '20px' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '15px' }}>
            <h2 style={{ fontSize: '18px', color: '#2c3e50', margin: 0 }}>
              PAN Verification
            </h2>
            {getStatusBadge(kycStatus?.panStatus || 'NOT_STARTED')}
          </div>

          <p style={{ color: '#666', marginBottom: '15px' }}>
            Verify your PAN card for tax compliance
          </p>

          {kycStatus?.panVerifiedAt && (
            <p style={{ fontSize: '12px', color: '#666', marginBottom: '15px' }}>
              Verified on: {new Date(kycStatus.panVerifiedAt).toLocaleString()}
              {kycStatus?.panNameMatchScore && ` (Match Score: ${kycStatus.panNameMatchScore}%)`}
            </p>
          )}

          <button
            className="btn btn-primary"
            onClick={() => navigate('/kyc/pan')}
            disabled={kycStatus?.panStatus === 'VERIFIED'}
          >
            {kycStatus?.panStatus === 'VERIFIED' ? 'Verified ✓' : 'Start Verification'}
          </button>
        </div>

        {/* Bank Verification Card */}
        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '15px' }}>
            <h2 style={{ fontSize: '18px', color: '#2c3e50', margin: 0 }}>
              Bank Account Verification
            </h2>
            {getStatusBadge(kycStatus?.bankStatus || 'NOT_STARTED')}
          </div>

          <p style={{ color: '#666', marginBottom: '15px' }}>
            Verify your bank account for payment processing
          </p>

          {kycStatus?.bankVerifiedAt && (
            <div>
              <p style={{ fontSize: '12px', color: '#666', marginBottom: '5px' }}>
                Verified on: {new Date(kycStatus.bankVerifiedAt).toLocaleString()}
              </p>
              {kycStatus?.bankAccountNumber && (
                <p style={{ fontSize: '12px', color: '#666', marginBottom: '15px' }}>
                  Account: ****{kycStatus.bankAccountNumber.slice(-4)}
                </p>
              )}
            </div>
          )}

          <button
            className="btn btn-primary"
            onClick={() => navigate('/kyc/bank')}
            disabled={kycStatus?.bankStatus === 'VERIFIED'}
          >
            {kycStatus?.bankStatus === 'VERIFIED' ? 'Verified ✓' : 'Start Verification'}
          </button>
        </div>

        {/* Action Buttons */}
        <div className="btn-group" style={{ marginTop: '30px' }}>
          <button
            className="btn btn-secondary"
            onClick={() => navigate('/dashboard')}
          >
            Go to Dashboard
          </button>

          {kycStatus?.overallStatus !== 'FULLY_VERIFIED' && (
            <button
              className="btn btn-primary"
              onClick={fetchKYCStatus}
            >
              Refresh Status
            </button>
          )}
        </div>
      </div>
    </div>
  );
};

export default KYCPage;
