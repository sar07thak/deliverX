import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

/**
 * Profile Page Component
 * Displays complete delivery partner profile information
 */
const ProfilePage = () => {
  const { user } = useAuth();
  const navigate = useNavigate();

  if (!user) {
    return (
      <div className="container">
        <div className="alert alert-warning">
          No profile data available. Please login again.
        </div>
      </div>
    );
  }

  return (
    <div className="container">
      <div style={{ maxWidth: '800px', margin: '30px auto' }}>
        <h1 style={{ marginBottom: '30px', color: '#2c3e50' }}>
          My Profile
        </h1>

        {/* Personal Information */}
        <div className="card" style={{ marginBottom: '20px' }}>
          <h2 style={{ fontSize: '18px', marginBottom: '20px', color: '#2c3e50' }}>
            Personal Information
          </h2>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px' }}>
            <div>
              <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Full Name</p>
              <p style={{ fontSize: '16px', fontWeight: '500' }}>{user.name || 'Not provided'}</p>
            </div>

            <div>
              <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Phone Number</p>
              <p style={{ fontSize: '16px', fontWeight: '500' }}>{user.phoneNumber}</p>
            </div>

            <div>
              <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Email Address</p>
              <p style={{ fontSize: '16px', fontWeight: '500' }}>{user.email || 'Not provided'}</p>
            </div>

            <div>
              <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Date of Birth</p>
              <p style={{ fontSize: '16px', fontWeight: '500' }}>{user.dateOfBirth || 'Not provided'}</p>
            </div>

            <div>
              <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Gender</p>
              <p style={{ fontSize: '16px', fontWeight: '500' }}>{user.gender || 'Not provided'}</p>
            </div>

            <div>
              <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>User ID</p>
              <p style={{ fontSize: '16px', fontWeight: '500', fontFamily: 'monospace' }}>{user.userId}</p>
            </div>
          </div>

          {user.address && (
            <div style={{ marginTop: '20px', paddingTop: '20px', borderTop: '1px solid #e0e0e0' }}>
              <p style={{ fontSize: '12px', color: '#666', marginBottom: '8px' }}>Address</p>
              <p style={{ fontSize: '16px', fontWeight: '500' }}>
                {user.address.street}, {user.address.city}, {user.address.state} - {user.address.pincode}
              </p>
            </div>
          )}
        </div>

        {/* Service Details */}
        <div className="card" style={{ marginBottom: '20px' }}>
          <h2 style={{ fontSize: '18px', marginBottom: '20px', color: '#2c3e50' }}>
            Service Details
          </h2>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px' }}>
            <div>
              <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Vehicle Type</p>
              <p style={{ fontSize: '16px', fontWeight: '500' }}>{user.vehicleType || 'Not provided'}</p>
            </div>

            <div>
              <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Availability</p>
              <p style={{ fontSize: '16px', fontWeight: '500' }}>{user.availability || 'Not provided'}</p>
            </div>

            <div>
              <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Service Area</p>
              <p style={{ fontSize: '16px', fontWeight: '500' }}>{user.serviceArea || 'Not provided'}</p>
            </div>

            <div>
              <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Languages Known</p>
              <p style={{ fontSize: '16px', fontWeight: '500' }}>
                {user.languagesKnown && user.languagesKnown.length > 0
                  ? user.languagesKnown.join(', ')
                  : 'Not provided'}
              </p>
            </div>
          </div>

          {user.pricing && (
            <div style={{ marginTop: '20px', paddingTop: '20px', borderTop: '1px solid #e0e0e0' }}>
              <p style={{ fontSize: '12px', color: '#666', marginBottom: '12px' }}>Pricing</p>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '15px' }}>
                <div>
                  <p style={{ fontSize: '12px', color: '#666' }}>Base Rate</p>
                  <p style={{ fontSize: '18px', fontWeight: '600', color: '#4CAF50' }}>
                    ₹{user.pricing.baseRate}
                  </p>
                </div>
                <div>
                  <p style={{ fontSize: '12px', color: '#666' }}>Per KM</p>
                  <p style={{ fontSize: '18px', fontWeight: '600', color: '#4CAF50' }}>
                    ₹{user.pricing.perKmRate}
                  </p>
                </div>
                <div>
                  <p style={{ fontSize: '12px', color: '#666' }}>Per Minute</p>
                  <p style={{ fontSize: '18px', fontWeight: '600', color: '#4CAF50' }}>
                    ₹{user.pricing.perMinuteRate}
                  </p>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Account Status */}
        <div className="card">
          <h2 style={{ fontSize: '18px', marginBottom: '20px', color: '#2c3e50' }}>
            Account Status
          </h2>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px' }}>
            <div>
              <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Registration Status</p>
              <span className={`badge ${user.registrationCompleted ? 'badge-success' : 'badge-warning'}`}>
                {user.registrationCompleted ? 'Completed' : 'Pending'}
              </span>
            </div>

            <div>
              <p style={{ fontSize: '12px', color: '#666', marginBottom: '4px' }}>Account Role</p>
              <span className="badge badge-info">
                {user.role || 'Delivery Partner'}
              </span>
            </div>
          </div>
        </div>

        {/* Action Buttons */}
        <div className="btn-group" style={{ marginTop: '30px' }}>
          <button
            className="btn btn-secondary"
            onClick={() => navigate('/dashboard')}
          >
            Back to Dashboard
          </button>

          <button
            className="btn btn-primary"
            onClick={() => navigate('/kyc')}
          >
            KYC Verification
          </button>
        </div>
      </div>
    </div>
  );
};

export default ProfilePage;
