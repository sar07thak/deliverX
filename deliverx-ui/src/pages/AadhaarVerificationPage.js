import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import kycService from '../services/kycService';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

/**
 * Aadhaar Verification Page Component
 * Handles Aadhaar verification via DigiLocker or Manual Upload
 */
const AadhaarVerificationPage = () => {
  const navigate = useNavigate();
  const { user } = useAuth();

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);
  const [verificationData, setVerificationData] = useState(null);
  const [verificationMethod, setVerificationMethod] = useState('DIGILOCKER');

  // Manual upload fields
  const [aadhaarLast4, setAadhaarLast4] = useState('');
  const [documentFile, setDocumentFile] = useState(null);

  const handleDigiLockerVerify = async () => {
    setLoading(true);
    setError('');
    setSuccess(false);

    try {
      console.log('Initiating DigiLocker verification...');

      // Step 1: Initiate Aadhaar verification via DigiLocker
      const redirectUrl = `${window.location.origin}/kyc/aadhaar/callback`;
      const initiateResponse = await kycService.initiateAadhaar('DIGILOCKER', redirectUrl);

      console.log('DigiLocker initiate response:', initiateResponse);

      if (!initiateResponse.success) {
        setError(initiateResponse.message || 'Failed to initiate verification');
        setLoading(false);
        return;
      }

      // In a real application, user would be redirected to DigiLocker URL
      // For MVP testing, we simulate this by auto-completing
      const digilockerToken = initiateResponse.data?.digilockerUrl || 'test-token-' + Date.now();

      console.log('Simulating DigiLocker redirect with token:', digilockerToken);

      // Simulate delay for DigiLocker redirect
      await new Promise(resolve => setTimeout(resolve, 2000));

      // Step 2: Complete Aadhaar verification
      const completeResponse = await kycService.completeAadhaar(digilockerToken);

      console.log('DigiLocker complete response:', completeResponse);

      if (completeResponse.success) {
        setVerificationData(completeResponse.data);
        setSuccess(true);

        // Redirect to KYC page after 3 seconds
        setTimeout(() => {
          navigate('/kyc');
        }, 3000);
      } else {
        setError(completeResponse.message || 'Verification failed');
      }
    } catch (err) {
      console.error('DigiLocker verification error:', err);
      setError(err.response?.data?.message || err.message || 'Failed to verify Aadhaar. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleManualUpload = async () => {
    if (!aadhaarLast4 || aadhaarLast4.length !== 4) {
      setError('Please enter the last 4 digits of your Aadhaar');
      return;
    }

    if (!documentFile) {
      setError('Please select an Aadhaar document to upload');
      return;
    }

    setLoading(true);
    setError('');
    setSuccess(false);

    try {
      console.log('Starting manual upload verification...');

      // In a real application, we would upload the file to a storage service
      // For MVP, we'll use a placeholder URL
      const documentUrl = 'https://storage.deliverx.com/aadhaar/' + Date.now() + '.pdf';

      console.log('Document URL:', documentUrl);

      const response = await kycService.initiateAadhaar('MANUAL_UPLOAD', null, documentUrl, aadhaarLast4);

      console.log('Manual upload response:', response);

      if (response.success) {
        setVerificationData(response.data);
        setSuccess(true);

        // Redirect to KYC page after 3 seconds
        setTimeout(() => {
          navigate('/kyc');
        }, 3000);
      } else {
        setError(response.message || 'Verification failed');
      }
    } catch (err) {
      console.error('Manual upload error:', err);
      setError(err.response?.data?.message || err.message || 'Failed to verify Aadhaar. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container">
      <div style={{ maxWidth: '700px', margin: '50px auto' }}>
        <div className="card">
          <h1 style={{ marginBottom: '10px', color: '#2c3e50' }}>
            Aadhaar Verification
          </h1>
          <p style={{ marginBottom: '30px', color: '#666' }}>
            Verify your identity using your Aadhaar card
          </p>

          {error && <ErrorMessage message={error} onClose={() => setError('')} />}

          {success && verificationData ? (
            <div>
              <div className="alert alert-success">
                ✓ Aadhaar verified successfully!
              </div>

              <div style={{ backgroundColor: '#f9f9f9', padding: '20px', borderRadius: '8px', marginTop: '20px' }}>
                <h3 style={{ fontSize: '16px', marginBottom: '15px', color: '#2c3e50' }}>
                  Verified Information
                </h3>
                <p><strong>Status:</strong> {verificationData.status || 'VERIFIED'}</p>
                <p><strong>Verification Method:</strong> {verificationData.method || verificationMethod}</p>
                {verificationData.message && <p><strong>Message:</strong> {verificationData.message}</p>}
              </div>

              <div className="alert alert-info" style={{ marginTop: '20px' }}>
                Redirecting to KYC page...
              </div>
            </div>
          ) : (
            <div>
              {/* Verification Method Selection */}
              <div style={{ marginBottom: '30px' }}>
                <label className="form-label">Select Verification Method</label>
                <div style={{ display: 'flex', gap: '15px', marginTop: '10px' }}>
                  <button
                    className={`btn ${verificationMethod === 'DIGILOCKER' ? 'btn-primary' : 'btn-secondary'}`}
                    onClick={() => setVerificationMethod('DIGILOCKER')}
                    disabled={loading}
                    style={{ flex: 1 }}
                  >
                    DigiLocker
                  </button>
                  <button
                    className={`btn ${verificationMethod === 'MANUAL_UPLOAD' ? 'btn-primary' : 'btn-secondary'}`}
                    onClick={() => setVerificationMethod('MANUAL_UPLOAD')}
                    disabled={loading}
                    style={{ flex: 1 }}
                  >
                    Manual Upload
                  </button>
                </div>
              </div>

              {/* DigiLocker Method */}
              {verificationMethod === 'DIGILOCKER' && (
                <div>
                  <div className="alert alert-info">
                    <strong>How it works:</strong>
                    <ol style={{ marginTop: '10px', marginBottom: 0, paddingLeft: '20px' }}>
                      <li>Click the button below to start verification</li>
                      <li>You'll be redirected to DigiLocker (simulated in MVP)</li>
                      <li>Grant access to your Aadhaar details</li>
                      <li>Your information will be securely verified</li>
                    </ol>
                  </div>

                  {loading ? (
                    <div>
                      <LoadingSpinner message="Verifying with DigiLocker..." />
                      <div className="alert alert-info" style={{ marginTop: '20px' }}>
                        Please wait while we fetch your Aadhaar details securely...
                      </div>
                    </div>
                  ) : (
                    <div>
                      <button
                        className="btn btn-primary btn-full"
                        onClick={handleDigiLockerVerify}
                        style={{ marginTop: '20px' }}
                      >
                        Verify with DigiLocker
                      </button>

                      <div style={{
                        marginTop: '20px',
                        padding: '15px',
                        backgroundColor: '#f0f0f0',
                        borderRadius: '4px',
                        fontSize: '12px',
                        color: '#666'
                      }}>
                        <strong>Note:</strong> This is a simulated DigiLocker integration for MVP.
                        In production, you'll be redirected to the official DigiLocker website.
                      </div>
                    </div>
                  )}
                </div>
              )}

              {/* Manual Upload Method */}
              {verificationMethod === 'MANUAL_UPLOAD' && (
                <div>
                  <div className="alert alert-info">
                    <strong>Manual Upload Requirements:</strong>
                    <ul style={{ marginTop: '10px', marginBottom: 0, paddingLeft: '20px' }}>
                      <li>Clear scanned copy or photo of your Aadhaar card</li>
                      <li>Last 4 digits of your Aadhaar number</li>
                      <li>Supported formats: PDF, JPG, PNG (Max 5MB)</li>
                    </ul>
                  </div>

                  {loading ? (
                    <LoadingSpinner message="Uploading and verifying document..." />
                  ) : (
                    <div>
                      <div className="form-group">
                        <label className="form-label">Last 4 Digits of Aadhaar *</label>
                        <input
                          type="text"
                          className="form-input"
                          placeholder="Enter last 4 digits"
                          value={aadhaarLast4}
                          onChange={(e) => {
                            const value = e.target.value.replace(/\D/g, '').slice(0, 4);
                            setAadhaarLast4(value);
                          }}
                          maxLength="4"
                          required
                        />
                      </div>

                      <div className="form-group">
                        <label className="form-label">Upload Aadhaar Document *</label>
                        <input
                          type="file"
                          className="form-input"
                          accept=".pdf,.jpg,.jpeg,.png"
                          onChange={(e) => setDocumentFile(e.target.files[0])}
                          required
                        />
                        {documentFile && (
                          <p style={{ marginTop: '8px', fontSize: '12px', color: '#666' }}>
                            Selected: {documentFile.name}
                          </p>
                        )}
                      </div>

                      <button
                        className="btn btn-primary btn-full"
                        onClick={handleManualUpload}
                        style={{ marginTop: '20px' }}
                      >
                        Upload and Verify
                      </button>

                      <div style={{
                        marginTop: '20px',
                        padding: '15px',
                        backgroundColor: '#f0f0f0',
                        borderRadius: '4px',
                        fontSize: '12px',
                        color: '#666'
                      }}>
                        <strong>Note:</strong> Manual verification may take 24-48 hours for admin review.
                      </div>
                    </div>
                  )}
                </div>
              )}
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

export default AadhaarVerificationPage;
