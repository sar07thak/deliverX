import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import authService from '../services/authService';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

/**
 * Login Page Component
 * Handles OTP-based authentication with role selection
 */
const LoginPage = () => {
  const [phoneNumber, setPhoneNumber] = useState('');
  const [otp, setOtp] = useState('');
  const [selectedRole, setSelectedRole] = useState('');
  const [otpSent, setOtpSent] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [showAdvancedRoles, setShowAdvancedRoles] = useState(false);

  const { login } = useAuth();
  const navigate = useNavigate();

  const basicRoles = [
    { value: 'EC', label: 'End Consumer', description: 'Individual user for personal deliveries', icon: '👤' },
    { value: 'BC', label: 'Business Consumer', description: 'Business owner for commercial deliveries', icon: '🏪' },
    { value: 'DP', label: 'Delivery Partner', description: 'Deliver packages and earn money', icon: '🚴' }
  ];

  const advancedRoles = [
    { value: 'DPCM', label: 'DPCM (Channel Manager)', description: 'Manage delivery partners and operations', icon: '👔' },
    { value: 'SA', label: 'Super Admin', description: 'Full platform administration access', icon: '🛡️' }
  ];

  // Handle phone number input
  const handlePhoneChange = (e) => {
    const value = e.target.value.replace(/\D/g, ''); // Remove non-digits
    setPhoneNumber(value);
  };

  // Send OTP to phone number
  const handleSendOTP = async (e) => {
    e.preventDefault();
    setError('');
    setSuccess('');

    // Validate phone number
    if (phoneNumber.length !== 10) {
      setError('Please enter a valid 10-digit phone number');
      return;
    }

    // Validate role selection for new users
    if (!selectedRole) {
      setError('Please select your account type');
      return;
    }

    setLoading(true);

    try {
      const formattedPhone = `+91${phoneNumber}`;
      const response = await authService.sendOTP(formattedPhone, selectedRole);

      if (response.success) {
        setOtpSent(true);
        // Display the OTP from the response (for testing - backend includes OTP in message)
        const otpMessage = response.data?.message || 'OTP sent successfully!';
        setSuccess(otpMessage);
      } else {
        setError(response.message || 'Failed to send OTP');
      }
    } catch (err) {
      setError(err.message || 'Failed to send OTP. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  // Verify OTP and login
  const handleVerifyOTP = async (e) => {
    e.preventDefault();
    setError('');
    setSuccess('');

    // Validate OTP
    if (otp.length !== 6) {
      setError('Please enter a valid 6-digit OTP');
      return;
    }

    setLoading(true);

    try {
      const formattedPhone = `+91${phoneNumber}`;
      const response = await authService.verifyOTP(formattedPhone, otp, selectedRole);

      if (response.success && response.data.accessToken) {
        login(response.data.user, response.data.accessToken);

        // Redirect based on user role and profile status
        const userRole = response.data.user.role;
        const profileComplete = response.data.user.profileComplete;

        // Admin and DPCM go directly to their dashboards
        if (userRole === 'SuperAdmin' || userRole === 'SA') {
          navigate('/admin');
        } else if (userRole === 'DPCM') {
          navigate('/dpcm');
        } else if (profileComplete) {
          navigate('/dashboard');
        } else {
          // New user - redirect to role-specific registration
          navigate('/register');
        }
      } else {
        setError(response.message || 'Invalid OTP');
      }
    } catch (err) {
      setError(err.message || 'Invalid OTP. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const getRoleLabel = () => {
    const allRoles = [...basicRoles, ...advancedRoles];
    return allRoles.find(r => r.value === selectedRole)?.label || selectedRole;
  };

  return (
    <div className="container">
      <div style={{ maxWidth: '500px', margin: '50px auto' }}>
        <div className="card">
          <h1 style={{ marginBottom: '10px', color: '#2c3e50' }}>DeliverX</h1>
          <p style={{ marginBottom: '30px', color: '#666' }}>
            Login or Register to continue
          </p>

          {error && <ErrorMessage message={error} onClose={() => setError('')} />}
          {success && <div className="alert alert-success">{success}</div>}

          {!otpSent ? (
            // Phone number and role selection form
            <form onSubmit={handleSendOTP}>
              {/* Role Selection */}
              <div className="form-group" style={{ marginBottom: '20px' }}>
                <label className="form-label">I am a</label>
                <p style={{ fontSize: '12px', color: '#888', margin: '5px 0' }}>
                  (For new users only - existing users will login with their registered role)
                </p>

                {/* Basic Roles */}
                <div style={{ display: 'flex', flexDirection: 'column', gap: '10px', marginTop: '10px' }}>
                  {basicRoles.map((role) => (
                    <label
                      key={role.value}
                      style={{
                        display: 'flex',
                        alignItems: 'flex-start',
                        padding: '15px',
                        border: selectedRole === role.value ? '2px solid #4CAF50' : '2px solid #ddd',
                        borderRadius: '8px',
                        cursor: 'pointer',
                        backgroundColor: selectedRole === role.value ? '#f0fff0' : 'white',
                        transition: 'all 0.2s'
                      }}
                    >
                      <input
                        type="radio"
                        name="role"
                        value={role.value}
                        checked={selectedRole === role.value}
                        onChange={(e) => setSelectedRole(e.target.value)}
                        style={{ marginRight: '12px', marginTop: '3px' }}
                      />
                      <div style={{ flex: 1 }}>
                        <div style={{ fontWeight: '600', color: '#2c3e50' }}>
                          <span style={{ marginRight: '8px' }}>{role.icon}</span>
                          {role.label}
                        </div>
                        <div style={{ fontSize: '12px', color: '#666', marginTop: '2px' }}>{role.description}</div>
                      </div>
                    </label>
                  ))}
                </div>

                {/* Toggle for Advanced Roles */}
                <button
                  type="button"
                  onClick={() => setShowAdvancedRoles(!showAdvancedRoles)}
                  style={{
                    marginTop: '15px',
                    background: 'none',
                    border: 'none',
                    color: '#667eea',
                    cursor: 'pointer',
                    fontSize: '13px',
                    display: 'flex',
                    alignItems: 'center',
                    gap: '5px'
                  }}
                >
                  {showAdvancedRoles ? '▼' : '▶'} Admin / Manager Login
                </button>

                {/* Advanced Roles (Admin & DPCM) */}
                {showAdvancedRoles && (
                  <div style={{
                    display: 'flex',
                    flexDirection: 'column',
                    gap: '10px',
                    marginTop: '10px',
                    padding: '15px',
                    backgroundColor: '#f8f9fa',
                    borderRadius: '8px',
                    border: '1px solid #e0e0e0'
                  }}>
                    <p style={{ fontSize: '11px', color: '#dc3545', margin: '0 0 10px 0', fontWeight: '500' }}>
                      ⚠️ Admin/Manager accounts require authorization
                    </p>
                    {advancedRoles.map((role) => (
                      <label
                        key={role.value}
                        style={{
                          display: 'flex',
                          alignItems: 'flex-start',
                          padding: '12px',
                          border: selectedRole === role.value ? '2px solid #667eea' : '2px solid #ddd',
                          borderRadius: '8px',
                          cursor: 'pointer',
                          backgroundColor: selectedRole === role.value ? '#f0f4ff' : 'white',
                          transition: 'all 0.2s'
                        }}
                      >
                        <input
                          type="radio"
                          name="role"
                          value={role.value}
                          checked={selectedRole === role.value}
                          onChange={(e) => setSelectedRole(e.target.value)}
                          style={{ marginRight: '12px', marginTop: '3px' }}
                        />
                        <div style={{ flex: 1 }}>
                          <div style={{ fontWeight: '600', color: '#2c3e50' }}>
                            <span style={{ marginRight: '8px' }}>{role.icon}</span>
                            {role.label}
                          </div>
                          <div style={{ fontSize: '12px', color: '#666', marginTop: '2px' }}>{role.description}</div>
                        </div>
                      </label>
                    ))}
                  </div>
                )}
              </div>

              {/* Phone Number Input */}
              <div className="form-group">
                <label className="form-label">Phone Number</label>
                <div style={{ display: 'flex', alignItems: 'center' }}>
                  <span style={{
                    padding: '10px 12px',
                    backgroundColor: '#f5f5f5',
                    border: '1px solid #ddd',
                    borderRight: 'none',
                    borderRadius: '4px 0 0 4px',
                    fontWeight: '500'
                  }}>
                    +91
                  </span>
                  <input
                    type="tel"
                    className="form-input"
                    style={{ borderRadius: '0 4px 4px 0' }}
                    placeholder="9876543210"
                    value={phoneNumber}
                    onChange={handlePhoneChange}
                    maxLength="10"
                    required
                    disabled={loading}
                  />
                </div>
              </div>

              <button
                type="submit"
                className="btn btn-primary btn-full"
                disabled={loading || phoneNumber.length !== 10 || !selectedRole}
              >
                {loading ? 'Sending...' : 'Send OTP'}
              </button>
            </form>
          ) : (
            // OTP verification form
            <form onSubmit={handleVerifyOTP}>
              <div style={{
                padding: '15px',
                backgroundColor: selectedRole === 'SA' || selectedRole === 'DPCM' ? '#e8eaf6' : '#e8f5e9',
                borderRadius: '8px',
                marginBottom: '20px'
              }}>
                <p style={{ margin: 0, color: selectedRole === 'SA' || selectedRole === 'DPCM' ? '#3f51b5' : '#2e7d32' }}>
                  <strong>Account Type:</strong> {getRoleLabel()}
                </p>
                <p style={{ margin: '5px 0 0', color: selectedRole === 'SA' || selectedRole === 'DPCM' ? '#3f51b5' : '#2e7d32' }}>
                  <strong>Phone:</strong> +91{phoneNumber}
                </p>
              </div>

              <div className="form-group">
                <label className="form-label">Enter OTP</label>
                <input
                  type="text"
                  className="form-input"
                  placeholder="Enter 6-digit OTP"
                  value={otp}
                  onChange={(e) => setOtp(e.target.value.replace(/\D/g, '').slice(0, 6))}
                  maxLength="6"
                  required
                  disabled={loading}
                  autoFocus
                  style={{ fontSize: '1.5em', letterSpacing: '0.5em', textAlign: 'center' }}
                />
                <p style={{ fontSize: '12px', color: '#666', marginTop: '8px' }}>
                  OTP sent to +91{phoneNumber}
                </p>
              </div>

              <div className="btn-group">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => {
                    setOtpSent(false);
                    setOtp('');
                    setError('');
                    setSuccess('');
                  }}
                  disabled={loading}
                >
                  Change Number
                </button>
                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={loading || otp.length !== 6}
                >
                  {loading ? 'Verifying...' : 'Verify & Login'}
                </button>
              </div>
            </form>
          )}

          {loading && <LoadingSpinner size="small" message="" />}
        </div>

        <div style={{ marginTop: '20px', textAlign: 'center', color: '#666', fontSize: '14px' }}>
          <p>New users will be redirected to complete their profile after verification.</p>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;
