import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import kycService from '../services/kycService';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

/**
 * PAN Verification Page Component
 * Handles PAN card verification
 */
const PANVerificationPage = () => {
  const navigate = useNavigate();

  const [panNumber, setPanNumber] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);
  const [verificationData, setVerificationData] = useState(null);

  // Format PAN number to uppercase
  const handlePanChange = (e) => {
    const value = e.target.value.toUpperCase();
    // Only allow alphanumeric characters and limit to 10
    const formatted = value.replace(/[^A-Z0-9]/g, '').slice(0, 10);
    setPanNumber(formatted);
  };

  // Validate PAN format (ABCDE1234F)
  const validatePAN = () => {
    const panPattern = /^[A-Z]{5}[0-9]{4}[A-Z]{1}$/;
    return panPattern.test(panNumber);
  };

  const handleVerify = async (e) => {
    e.preventDefault();
    setError('');
    setSuccess(false);

    if (!validatePAN()) {
      setError('Invalid PAN format. Should be like: ABCDE1234F');
      return;
    }

    setLoading(true);

    try {
      console.log('Starting PAN verification for:', panNumber);
      const response = await kycService.verifyPAN(panNumber);

      console.log('PAN verification response:', response);

      if (response.success) {
        setVerificationData(response.data);
        setSuccess(true);

        // Redirect to KYC page after 3 seconds
        setTimeout(() => {
          navigate('/kyc');
        }, 3000);
      } else {
        setError(response.message || 'PAN verification failed');
      }
    } catch (err) {
      console.error('PAN verification error:', err);
      setError(err.response?.data?.message || err.message || 'Failed to verify PAN. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container">
      <div style={{ maxWidth: '600px', margin: '50px auto' }}>
        <div className="card">
          <h1 style={{ marginBottom: '10px', color: '#2c3e50' }}>
            PAN Verification
          </h1>
          <p style={{ marginBottom: '30px', color: '#666' }}>
            Verify your PAN card for tax compliance
          </p>

          {error && <ErrorMessage message={error} onClose={() => setError('')} />}

          {success && verificationData ? (
            <div>
              <div className="alert alert-success">
                ✓ PAN verified successfully!
              </div>

              <div style={{ backgroundColor: '#f9f9f9', padding: '20px', borderRadius: '8px', marginTop: '20px' }}>
                <h3 style={{ fontSize: '16px', marginBottom: '15px', color: '#2c3e50' }}>
                  Verified Information
                </h3>
                <p><strong>PAN Number:</strong> {verificationData.panNumber}</p>
                <p><strong>Name on PAN:</strong> {verificationData.name}</p>
                <p><strong>Status:</strong> {verificationData.status}</p>

                {verificationData.nameMatchScore && (
                  <div style={{ marginTop: '15px' }}>
                    <p style={{ marginBottom: '5px' }}><strong>Name Match Score:</strong></p>
                    <div style={{
                      backgroundColor: '#e0e0e0',
                      borderRadius: '4px',
                      height: '24px',
                      overflow: 'hidden',
                      position: 'relative'
                    }}>
                      <div style={{
                        backgroundColor: verificationData.nameMatchScore >= 80 ? '#4CAF50' : '#FFC107',
                        width: `${verificationData.nameMatchScore}%`,
                        height: '100%',
                        transition: 'width 0.3s ease'
                      }} />
                      <span style={{
                        position: 'absolute',
                        top: '50%',
                        left: '50%',
                        transform: 'translate(-50%, -50%)',
                        fontSize: '12px',
                        fontWeight: 'bold',
                        color: '#333'
                      }}>
                        {verificationData.nameMatchScore}%
                      </span>
                    </div>
                  </div>
                )}
              </div>

              <div className="alert alert-info" style={{ marginTop: '20px' }}>
                Redirecting to KYC page...
              </div>
            </div>
          ) : (
            <div>
              <div className="alert alert-info">
                <strong>Required Format:</strong> ABCDE1234F
                <br />
                <span style={{ fontSize: '12px', color: '#666' }}>
                  5 letters, 4 digits, 1 letter (all uppercase)
                </span>
              </div>

              <form onSubmit={handleVerify}>
                <div className="form-group">
                  <label className="form-label">PAN Number *</label>
                  <input
                    type="text"
                    className="form-input"
                    placeholder="ABCDE1234F"
                    value={panNumber}
                    onChange={handlePanChange}
                    maxLength="10"
                    required
                    disabled={loading}
                    style={{
                      fontSize: '16px',
                      fontWeight: 'bold',
                      letterSpacing: '1px',
                      textTransform: 'uppercase'
                    }}
                  />
                  <p style={{ fontSize: '12px', color: '#666', marginTop: '5px' }}>
                    Enter your 10-character PAN number
                  </p>
                </div>

                {loading && <LoadingSpinner message="Verifying PAN with government database..." />}

                <button
                  type="submit"
                  className="btn btn-primary btn-full"
                  disabled={loading || panNumber.length !== 10}
                  style={{ marginTop: '20px' }}
                >
                  {loading ? 'Verifying...' : 'Verify PAN'}
                </button>
              </form>

              <div style={{
                marginTop: '20px',
                padding: '15px',
                backgroundColor: '#f0f0f0',
                borderRadius: '4px',
                fontSize: '12px',
                color: '#666'
              }}>
                <strong>Note:</strong> Your PAN details will be verified with the Income Tax Department database.
                We'll match your name with the name registered on your Aadhaar for verification.
              </div>
            </div>
          )}

          <div className="btn-group" style={{ marginTop: '30px' }}>
            <button
              className="btn btn-secondary"
              onClick={() => navigate('/kyc')}
              disabled={loading}
            >
              Back to KYC
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default PANVerificationPage;
