import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import kycService from '../services/kycService';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

/**
 * Bank Verification Page Component
 * Handles bank account verification via penny drop
 */
const BankVerificationPage = () => {
  const navigate = useNavigate();

  const [formData, setFormData] = useState({
    accountNumber: '',
    ifscCode: '',
    accountHolderName: ''
  });

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);
  const [verificationData, setVerificationData] = useState(null);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  // Format IFSC code to uppercase
  const handleIfscChange = (e) => {
    const value = e.target.value.toUpperCase().replace(/[^A-Z0-9]/g, '').slice(0, 11);
    setFormData(prev => ({
      ...prev,
      ifscCode: value
    }));
  };

  // Format account number (digits only)
  const handleAccountChange = (e) => {
    const value = e.target.value.replace(/\D/g, '').slice(0, 18);
    setFormData(prev => ({
      ...prev,
      accountNumber: value
    }));
  };

  // Validate IFSC format
  const validateIFSC = () => {
    const ifscPattern = /^[A-Z]{4}0[A-Z0-9]{6}$/;
    return ifscPattern.test(formData.ifscCode);
  };

  const handleVerify = async (e) => {
    e.preventDefault();
    setError('');
    setSuccess(false);

    // Validation
    if (formData.accountNumber.length < 9 || formData.accountNumber.length > 18) {
      setError('Account number should be between 9 and 18 digits');
      return;
    }

    if (!validateIFSC()) {
      setError('Invalid IFSC code format. Should be like: SBIN0001234');
      return;
    }

    if (formData.accountHolderName.trim().length < 3) {
      setError('Please enter valid account holder name');
      return;
    }

    setLoading(true);

    try {
      console.log('Starting bank verification...');
      const response = await kycService.verifyBank({
        accountNumber: formData.accountNumber,
        ifscCode: formData.ifscCode,
        accountHolderName: formData.accountHolderName
      });

      console.log('Bank verification response:', response);

      if (response.success) {
        setVerificationData(response.data);
        setSuccess(true);

        // Redirect to KYC page after 4 seconds
        setTimeout(() => {
          navigate('/kyc');
        }, 4000);
      } else {
        setError(response.message || 'Bank verification failed');
      }
    } catch (err) {
      console.error('Bank verification error:', err);
      setError(err.response?.data?.message || err.message || 'Failed to verify bank account. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container">
      <div style={{ maxWidth: '600px', margin: '50px auto' }}>
        <div className="card">
          <h1 style={{ marginBottom: '10px', color: '#2c3e50' }}>
            Bank Account Verification
          </h1>
          <p style={{ marginBottom: '30px', color: '#666' }}>
            Verify your bank account for payment processing
          </p>

          {error && <ErrorMessage message={error} onClose={() => setError('')} />}

          {success && verificationData ? (
            <div>
              <div className="alert alert-success">
                ✓ Bank account verified successfully!
              </div>

              <div style={{ backgroundColor: '#f9f9f9', padding: '20px', borderRadius: '8px', marginTop: '20px' }}>
                <h3 style={{ fontSize: '16px', marginBottom: '15px', color: '#2c3e50' }}>
                  Verified Information
                </h3>
                <p><strong>Account Number:</strong> ****{(verificationData.accountNumber || formData.accountNumber)?.slice(-4)}</p>
                <p><strong>IFSC Code:</strong> {verificationData.ifscCode || formData.ifscCode}</p>
                <p><strong>Account Holder:</strong> {verificationData.accountHolderName || formData.accountHolderName}</p>
                {verificationData.bankName && <p><strong>Bank Name:</strong> {verificationData.bankName}</p>}
                {verificationData.branch && <p><strong>Branch:</strong> {verificationData.branch}</p>}
                <p><strong>Status:</strong> <span className="badge badge-success">{verificationData.status || 'VERIFIED'}</span></p>

                {verificationData.pennyDropAmount && (
                  <div className="alert alert-info" style={{ marginTop: '15px', fontSize: '12px' }}>
                    A test amount of ₹{verificationData.pennyDropAmount} was deposited and verified.
                    This will be refunded automatically.
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
                <strong>Penny Drop Verification:</strong>
                <p style={{ marginTop: '8px', marginBottom: 0, fontSize: '13px' }}>
                  We'll deposit a small amount (₹1) to verify your account.
                  This will be refunded immediately after verification.
                </p>
              </div>

              <form onSubmit={handleVerify}>
                <div className="form-group">
                  <label className="form-label">Account Number *</label>
                  <input
                    type="text"
                    name="accountNumber"
                    className="form-input"
                    placeholder="Enter your bank account number"
                    value={formData.accountNumber}
                    onChange={handleAccountChange}
                    required
                    disabled={loading}
                  />
                  <p style={{ fontSize: '12px', color: '#666', marginTop: '5px' }}>
                    9-18 digit account number
                  </p>
                </div>

                <div className="form-group">
                  <label className="form-label">IFSC Code *</label>
                  <input
                    type="text"
                    name="ifscCode"
                    className="form-input"
                    placeholder="SBIN0001234"
                    value={formData.ifscCode}
                    onChange={handleIfscChange}
                    maxLength="11"
                    required
                    disabled={loading}
                    style={{ textTransform: 'uppercase' }}
                  />
                  <p style={{ fontSize: '12px', color: '#666', marginTop: '5px' }}>
                    11-character IFSC code (e.g., SBIN0001234)
                  </p>
                </div>

                <div className="form-group">
                  <label className="form-label">Account Holder Name *</label>
                  <input
                    type="text"
                    name="accountHolderName"
                    className="form-input"
                    placeholder="Enter name as per bank records"
                    value={formData.accountHolderName}
                    onChange={handleChange}
                    required
                    disabled={loading}
                  />
                  <p style={{ fontSize: '12px', color: '#666', marginTop: '5px' }}>
                    Name should match your bank account
                  </p>
                </div>

                {loading && (
                  <div>
                    <LoadingSpinner message="Verifying bank account..." />
                    <div className="alert alert-info" style={{ marginTop: '15px' }}>
                      <strong>Verification in progress:</strong>
                      <ol style={{ marginTop: '8px', marginBottom: 0, paddingLeft: '20px', fontSize: '13px' }}>
                        <li>Validating IFSC code</li>
                        <li>Initiating penny drop (₹1 deposit)</li>
                        <li>Verifying account details</li>
                        <li>Processing refund</li>
                      </ol>
                    </div>
                  </div>
                )}

                <button
                  type="submit"
                  className="btn btn-primary btn-full"
                  disabled={loading}
                  style={{ marginTop: '20px' }}
                >
                  {loading ? 'Verifying...' : 'Verify Account'}
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
                <strong>Security Note:</strong> Your bank details are encrypted and securely stored.
                We use industry-standard penny drop verification to confirm account ownership.
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

export default BankVerificationPage;
